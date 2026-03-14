#!/bin/sh
set -eu

if [ "$#" -lt 1 ]; then
  echo "Usage: restore-postgres-backup.sh <backup-file>" >&2
  exit 1
fi

POSTGRES_HOST="${POSTGRES_HOST:-postgres}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-myrati}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"
BACKUP_ENCRYPTION_CIPHER="${BACKUP_ENCRYPTION_CIPHER:-aes-256-cbc}"
BACKUP_ENCRYPTION_ITERATIONS="${BACKUP_ENCRYPTION_ITERATIONS:-250000}"
BACKUP_ENCRYPTION_PASSPHRASE="${BACKUP_ENCRYPTION_PASSPHRASE:-}"

backup_file="$1"
restore_file="$backup_file"
temp_file=""

cleanup() {
  if [ -n "$temp_file" ] && [ -f "$temp_file" ]; then
    rm -f "$temp_file"
  fi
}

trap cleanup EXIT

if printf '%s' "$backup_file" | grep -q '\.enc$'; then
  if [ -z "$BACKUP_ENCRYPTION_PASSPHRASE" ]; then
    echo "BACKUP_ENCRYPTION_PASSPHRASE is required to restore encrypted backups." >&2
    exit 1
  fi

  temp_file="/tmp/restore_$(date -u +%Y%m%dT%H%M%SZ).dump"
  openssl enc -d \
    -"${BACKUP_ENCRYPTION_CIPHER}" \
    -pbkdf2 \
    -iter "$BACKUP_ENCRYPTION_ITERATIONS" \
    -in "$backup_file" \
    -out "$temp_file" \
    -pass pass:"$BACKUP_ENCRYPTION_PASSPHRASE"
  restore_file="$temp_file"
fi

export PGPASSWORD="$POSTGRES_PASSWORD"
pg_restore \
  --clean \
  --if-exists \
  --no-owner \
  --no-privileges \
  --host="$POSTGRES_HOST" \
  --port="$POSTGRES_PORT" \
  --username="$POSTGRES_USER" \
  --dbname="$POSTGRES_DB" \
  "$restore_file"
