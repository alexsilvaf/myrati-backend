#!/bin/sh
set -eu

POSTGRES_HOST="${POSTGRES_HOST:-postgres}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-myrati}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"
BACKUP_INTERVAL_SECONDS="${BACKUP_INTERVAL_SECONDS:-86400}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-7}"
BACKUP_FILE_PREFIX="${BACKUP_FILE_PREFIX:-myrati}"
BACKUP_COMPRESSION="${BACKUP_COMPRESSION:-gzip:level=6}"
BACKUP_ENCRYPTION_CIPHER="${BACKUP_ENCRYPTION_CIPHER:-aes-256-cbc}"
BACKUP_ENCRYPTION_ITERATIONS="${BACKUP_ENCRYPTION_ITERATIONS:-250000}"
BACKUP_ENCRYPTION_PASSPHRASE="${BACKUP_ENCRYPTION_PASSPHRASE:-}"
R2_ENDPOINT="${R2_ENDPOINT:-}"
R2_BUCKET_NAME="${R2_BUCKET_NAME:-}"
R2_ACCESS_KEY_ID="${R2_ACCESS_KEY_ID:-}"
R2_SECRET_ACCESS_KEY="${R2_SECRET_ACCESS_KEY:-}"
R2_OBJECT_PREFIX="${R2_OBJECT_PREFIX:-postgres}"
R2_DAILY_RETENTION="${R2_DAILY_RETENTION:-35}"
R2_MONTHLY_RETENTION="${R2_MONTHLY_RETENTION:-12}"
R2_YEARLY_RETENTION="${R2_YEARLY_RETENTION:-6}"

mkdir -p /backups

normalize_prefix() {
  printf '%s' "$1" | sed 's#^/*##; s#/*$##'
}

is_r2_enabled() {
  [ -n "$R2_ENDPOINT" ] \
    && [ -n "$R2_BUCKET_NAME" ] \
    && [ -n "$R2_ACCESS_KEY_ID" ] \
    && [ -n "$R2_SECRET_ACCESS_KEY" ]
}

r2_aws() {
  AWS_ACCESS_KEY_ID="$R2_ACCESS_KEY_ID" \
  AWS_SECRET_ACCESS_KEY="$R2_SECRET_ACCESS_KEY" \
  AWS_DEFAULT_REGION="auto" \
  AWS_EC2_METADATA_DISABLED="true" \
    aws --endpoint-url "$R2_ENDPOINT" "$@"
}

upload_to_r2() {
  source_file="$1"
  object_key="$2"

  if r2_aws s3 cp "$source_file" "s3://${R2_BUCKET_NAME}/${object_key}" --only-show-errors; then
    echo "Uploaded backup to s3://${R2_BUCKET_NAME}/${object_key}"
    return 0
  fi

  echo "Failed to upload backup to s3://${R2_BUCKET_NAME}/${object_key}" >&2
  return 1
}

encrypt_backup() {
  source_file="$1"
  encrypted_file="${source_file}.enc"
  checksum_file="${encrypted_file}.sha256"

  if [ -z "$BACKUP_ENCRYPTION_PASSPHRASE" ]; then
    printf '%s\n%s\n' "$source_file" ""
    return 0
  fi

  if openssl enc \
    -"${BACKUP_ENCRYPTION_CIPHER}" \
    -salt \
    -pbkdf2 \
    -iter "$BACKUP_ENCRYPTION_ITERATIONS" \
    -in "$source_file" \
    -out "$encrypted_file" \
    -pass pass:"$BACKUP_ENCRYPTION_PASSPHRASE"; then
    sha256sum "$encrypted_file" > "$checksum_file"
    rm -f "$source_file"
    printf '%s\n%s\n' "$encrypted_file" "$checksum_file"
    return 0
  fi

  echo "Backup encryption failed." >&2
  rm -f "$encrypted_file" "$checksum_file"
  printf '%s\n%s\n' "$source_file" ""
  return 1
}

prune_remote_prefix() {
  prefix="$1"
  keep_count="$2"

  if [ "$keep_count" -le 0 ]; then
    return 0
  fi

  keys="$(
    r2_aws s3api list-objects-v2 \
      --bucket "$R2_BUCKET_NAME" \
      --prefix "${prefix}/" \
      --query 'Contents[].Key' \
      --output text 2>/dev/null || true
  )"

  if [ -z "$keys" ] || [ "$keys" = "None" ]; then
    return 0
  fi

  printf '%s\n' "$keys" \
    | tr '\t' '\n' \
    | sed '/^$/d; /^None$/d' \
    | sort -r \
    | awk "NR > ${keep_count}" \
    | while IFS= read -r object_key; do
        [ -z "$object_key" ] && continue
        r2_aws s3 rm "s3://${R2_BUCKET_NAME}/${object_key}" --only-show-errors || true
      done
}

echo "Waiting for PostgreSQL at ${POSTGRES_HOST}:${POSTGRES_PORT}..."
until pg_isready -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; do
  sleep 2
done

while true
do
  timestamp="$(date -u +"%Y%m%dT%H%M%SZ")"
  backup_file="/backups/${BACKUP_FILE_PREFIX}_${timestamp}.dump"
  backup_upload_file="$backup_file"
  checksum_file=""
  object_prefix="$(normalize_prefix "$R2_OBJECT_PREFIX")"

  echo "Creating PostgreSQL backup ${backup_file} with compression ${BACKUP_COMPRESSION}"
  export PGPASSWORD="$POSTGRES_PASSWORD"

  if pg_dump \
    --format=custom \
    --compress="$BACKUP_COMPRESSION" \
    --no-owner \
    --no-privileges \
    --host="$POSTGRES_HOST" \
    --port="$POSTGRES_PORT" \
    --username="$POSTGRES_USER" \
    --dbname="$POSTGRES_DB" \
    --file="$backup_file"; then
    echo "Backup created successfully."
  else
    echo "Backup failed." >&2
    rm -f "$backup_file"
    sleep "$BACKUP_INTERVAL_SECONDS"
    continue
  fi

  encryption_result="$(encrypt_backup "$backup_file")"
  backup_upload_file="$(printf '%s' "$encryption_result" | sed -n '1p')"
  checksum_file="$(printf '%s' "$encryption_result" | sed -n '2p')"

  backup_filename="$(basename "$backup_upload_file")"
  month_suffix="$(printf '%s' "$backup_filename" | sed -E "s/^${BACKUP_FILE_PREFIX}_[0-9]{8}T[0-9]{6}Z/${BACKUP_FILE_PREFIX}_$(date -u +%Y-%m)/")"
  year_suffix="$(printf '%s' "$backup_filename" | sed -E "s/^${BACKUP_FILE_PREFIX}_[0-9]{8}T[0-9]{6}Z/${BACKUP_FILE_PREFIX}_$(date -u +%Y)/")"
  daily_key="${object_prefix}/daily/${backup_filename}"
  month_key="${object_prefix}/monthly/${month_suffix}"
  year_key="${object_prefix}/yearly/${year_suffix}"

  if is_r2_enabled; then
    upload_to_r2 "$backup_upload_file" "$daily_key" || true
    upload_to_r2 "$backup_upload_file" "$month_key" || true
    upload_to_r2 "$backup_upload_file" "$year_key" || true

    if [ -n "$checksum_file" ]; then
      upload_to_r2 "$checksum_file" "${daily_key}.sha256" || true
      upload_to_r2 "$checksum_file" "${month_key}.sha256" || true
      upload_to_r2 "$checksum_file" "${year_key}.sha256" || true
    fi

    prune_remote_prefix "${object_prefix}/daily" "$R2_DAILY_RETENTION"
    prune_remote_prefix "${object_prefix}/monthly" "$R2_MONTHLY_RETENTION"
    prune_remote_prefix "${object_prefix}/yearly" "$R2_YEARLY_RETENTION"
  else
    echo "R2 upload disabled. Missing endpoint, bucket or credentials."
  fi

  find /backups -type f -name "${BACKUP_FILE_PREFIX}_*" -mtime +"$BACKUP_RETENTION_DAYS" -delete
  sleep "$BACKUP_INTERVAL_SECONDS"
done
