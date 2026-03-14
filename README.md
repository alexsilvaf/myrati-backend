<div align="center">

# Myrati Backend

**Backend em .NET 10 com gateway e serviços por contexto para o painel administrativo, área pública e fluxos em tempo real da Myrati.**

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)

</div>

---

## Sumário

- [Visão geral](#visão-geral)
- [Stack e tecnologias](#stack-e-tecnologias)
- [Arquitetura](#arquitetura)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Configuração e ambiente](#configuração-e-ambiente)
- [Execução local](#execução-local)
- [Execução com Docker](#execução-com-docker)
- [Backups e auditoria](#backups-e-auditoria)
- [Autenticação e autorização](#autenticação-e-autorização)
- [Automação por agentes](#automação-por-agentes)
- [SSE e tempo real](#sse-e-tempo-real)
- [Rotas da API](#rotas-da-api)
  - [Auth](#auth)
  - [Dashboard](#dashboard)
  - [Products](#products)
  - [Kanban por produto](#kanban-por-produto)
  - [Licenses](#licenses)
  - [Clients](#clients)
  - [Users](#users)
  - [Settings](#settings)
  - [Profile](#profile)
  - [Public](#public)
  - [Streams e saúde](#streams-e-saúde)
- [Regras de validação](#regras-de-validação)
- [Códigos de resposta HTTP](#códigos-de-resposta-http)
- [Testes](#testes)
- [Seeds e credenciais iniciais](#seeds-e-credenciais-iniciais)
- [Guia de contribuição](#guia-de-contribuição)
- [Observações importantes](#observações-importantes)

---

## Visão geral

O backend suporta todos os módulos expostos no frontend da Myrati:

| Módulo | Descrição |
|--------|-----------|
| **Autenticação** | Login JWT para administradores |
| **Dashboard** | KPIs, receita mensal e atividades recentes |
| **Catálogo** | Produtos, estratégias de venda, planos e gestão de licenças |
| **Desenvolvimento** | Kanban por produto com sprints e tarefas |
| **Clientes** | CRUD com usuários e licenças vinculadas |
| **Usuários** | Diretório de usuários conectados |
| **Configurações** | Empresa, equipe, preferências e chaves de API |
| **Perfil** | Dados pessoais, senha, sessões e log de atividade |
| **Público** | Ativação de licença, status page e formulário de contato |
| **Tempo real** | Streams SSE para backoffice e status page |

O projeto mantém regras de negócio centralizadas na camada de aplicação, controllers finos e infraestrutura substituível entre PostgreSQL e SQLite.

---

## Stack e tecnologias

<table>
<tr>
<td width="50%">

**Runtime e framework**
- .NET 10 / ASP.NET Core
- C# com Nullable habilitado

**Persistência**
- Entity Framework Core 10
- Npgsql (PostgreSQL)
- SQLite (dev e testes)

**Segurança**
- JWT Bearer
- BCrypt.Net-Next
- Políticas de autorização por papel

</td>
<td width="50%">

**Validação e documentação**
- FluentValidation
- Swashbuckle / Swagger

**Tempo real e operação**
- Server-Sent Events (SSE)
- Rate Limiting nativo
- Health Checks / CORS

**Testes**
- xUnit / Moq
- WebApplicationFactory
- SQLite in-memory
- Coverlet

</td>
</tr>
</table>

---

## Arquitetura

O projeto agora usa uma **topologia de microsserviços por contexto com gateway na frente**, ainda reaproveitando as camadas de domínio, aplicação e infraestrutura do backend original.

### Serviços executáveis

| Serviço | Responsabilidade |
|--------|------------------|
| **Myrati.Gateway.API** | Entrada única do frontend e roteamento |
| **Myrati.IdentityService.API** | Auth, perfil, equipe e settings |
| **Myrati.BackofficeService.API** | Dashboard, produtos, licenças, clientes, usuários e SSE |
| **Myrati.PublicService.API** | Contato público e ativação de licenças |
| **Myrati.API** | Host legado do monólito, mantido para compatibilidade e testes |

### Camadas

```
┌─────────────────────────────────────────────────────┐
│                    Myrati.API                        │
│         Controllers · Middleware · Swagger           │
├─────────────────────────────────────────────────────┤
│               Myrati.Application                    │
│      Contratos · Serviços · Validação · SSE         │
├─────────────────────────────────────────────────────┤
│              Myrati.Infrastructure                   │
│      DbContext · JWT · Hashing · Seed · SSE Hub     │
├─────────────────────────────────────────────────────┤
│                  Myrati.Domain                       │
│        Entidades · Relacionamentos · Regras         │
└─────────────────────────────────────────────────────┘
```

### Regras de dependência

```
API  ──>  Application  ──>  Domain
 │                             ▲
 └──>  Infrastructure  ────────┘
```

> `Domain` não depende de nenhuma outra camada.

### Módulos funcionais

| Módulo | Responsabilidade |
|--------|-----------------|
| **Identity** | Administradores, perfil, sessões e autenticação |
| **Catalog** | Produtos, planos e licenças |
| **CRM** | Clientes e usuários conectados |
| **Platform** | Configurações e chaves de API |
| **Operations** | Dashboard e feed operacional |
| **Public** | Status page e contato |
| **Realtime** | Publicação e consumo de eventos via SSE |

---

## Estrutura do projeto

```
myrati-backend/
├── src/
│   ├── Myrati.API/
│   ├── Myrati.BackofficeService.API/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Realtime/
│   │   └── Program.cs
│   ├── Myrati.Application/
│   ├── Myrati.Gateway.API/
│   │   ├── Abstractions/
│   │   ├── Common/
│   │   ├── Contracts/
│   │   ├── DependencyInjection/
│   │   ├── Realtime/
│   │   ├── Services/
│   │   └── Validation/
│   ├── Myrati.Domain/
│   ├── Myrati.IdentityService.API/
│   │   ├── Clients/
│   │   ├── Common/
│   │   ├── Dashboard/
│   │   ├── Identity/
│   │   ├── Products/
│   │   ├── Public/
│   │   └── Settings/
│   ├── Myrati.PublicService.API/
│   ├── Myrati.ServiceDefaults/
│   └── Myrati.Infrastructure/
│       ├── DependencyInjection/
│       ├── Persistence/
│       ├── Realtime/
│       ├── Security/
│       └── Seeding/
├── tests/
│   ├── Myrati.Application.Tests/
│   └── Myrati.API.Tests/
├── docker-compose.yml
├── ARCHITECTURE.md
└── README.md
```

---

## Configuração e ambiente

### Banco de dados

O provider é escolhido automaticamente pela connection string:

| Connection string contém | Provider |
|--------------------------|----------|
| `Data Source=` | SQLite |
| Qualquer outro formato | PostgreSQL |

Connection string padrão para desenvolvimento:

```
Host=localhost;Port=5432;Database=myrati;Username=postgres;Password=postgres
```

### JWT

| Parâmetro | Valor padrão |
|-----------|-------------|
| `Jwt:Issuer` | `Myrati` |
| `Jwt:Audience` | `Myrati.Backoffice` |
| `Jwt:ExpiresInMinutes` | `480` |

> **Importante:** troque `Jwt:Key` antes de qualquer ambiente real.

### CORS

Origens liberadas por padrão: `localhost:5173`, `localhost:4173`, `localhost:4174`

### Rate Limit

Política `public`: **20 requisições/minuto por IP** — aplicada nas rotas públicas e no login.

### Variáveis de ambiente

O arquivo `.env.example` contém os parâmetros usados no `docker-compose`:

```env
POSTGRES_DB=myrati
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
MYRATI_DB_CONNECTION=Host=postgres;Port=5432;Database=myrati;Username=postgres;Password=postgres
MYRATI_JWT_ISSUER=Myrati
MYRATI_JWT_AUDIENCE=Myrati.Backoffice
MYRATI_JWT_KEY=CHANGE_THIS_FOR_A_LONG_RANDOM_SECRET_KEY_32+
MYRATI_BACKUP_INTERVAL_SECONDS=86400
MYRATI_BACKUP_RETENTION_DAYS=7
MYRATI_BACKUP_FILE_PREFIX=myrati
MYRATI_BACKUP_COMPRESSION=gzip:level=6
MYRATI_BACKUP_ENCRYPTION_CIPHER=aes-256-cbc
MYRATI_BACKUP_ENCRYPTION_ITERATIONS=250000
MYRATI_BACKUP_ENCRYPTION_PASSPHRASE=
MYRATI_AUDIT_RETENTION_DAYS=365
MYRATI_COMPLIANCE_DATA_SUBJECT_REQUEST_DUE_DAYS=15
MYRATI_DATAPROTECTION_APPLICATION_NAME=Myrati
MYRATI_DATAPROTECTION_KEYS_PATH=/var/lib/myrati/dataprotection-keys
MYRATI_DATAPROTECTION_CERTIFICATE_PATH=
MYRATI_DATAPROTECTION_CERTIFICATE_PASSWORD=
MYRATI_EMAIL_FRONTEND_URL=http://localhost:4173
MYRATI_EMAIL_SENDER_NAME=Myrati
MYRATI_EMAIL_SENDER_ADDRESS=
MYRATI_EMAIL_LEAD_RECIPIENT_NAME=Yasmin
MYRATI_EMAIL_LEAD_RECIPIENT_ADDRESS=yasmin@myrati.com.br
MYRATI_GMAIL_CLIENT_ID=
MYRATI_GMAIL_CLIENT_SECRET=
MYRATI_GMAIL_REFRESH_TOKEN=
```

> O formulário público de contato encaminha o lead para `Email:LeadRecipientEmail`, que por padrão é `yasmin@myrati.com.br`. Sem credenciais Gmail válidas, o backend persiste o lead e registra o conteúdo em log como fallback.

### Arquivos locais de configuração

O backend aceita arquivos locais opcionais (ignorados pelo git), carregados por cima do `appsettings.json`:

- `src/Myrati.API/appsettings.Local.json`
- `src/Myrati.API/appsettings.Development.Local.json`

> Quando `SPRING_DATASOURCE_PASSWORD` estiver definida, ela tem precedência sobre a senha na connection string do PostgreSQL.

---

## Execução local

### Pré-requisitos

- .NET SDK 10
- Docker (opcional, para o PostgreSQL)

### 1. Subir o banco

```powershell
docker compose up -d postgres
```

### 2. Rodar os serviços

Gateway:

```powershell
dotnet run --project .\src\Myrati.Gateway.API\Myrati.Gateway.API.csproj
```

Identity:

```powershell
dotnet run --project .\src\Myrati.IdentityService.API\Myrati.IdentityService.API.csproj
```

Backoffice:

```powershell
dotnet run --project .\src\Myrati.BackofficeService.API\Myrati.BackofficeService.API.csproj
```

Public:

```powershell
dotnet run --project .\src\Myrati.PublicService.API\Myrati.PublicService.API.csproj
```

Host legado do monólito:

```powershell
dotnet run --project .\src\Myrati.API\Myrati.API.csproj
```

Com hot reload:

```powershell
dotnet watch --project .\src\Myrati.API\Myrati.API.csproj run
```

### 3. Acessar

| Recurso | URL |
|---------|-----|
| Swagger | `/swagger` (apenas em Development) |
| Health check | `GET /health` |

---

## Execução com Docker

```powershell
docker compose up --build
```

| Serviço | Endereço |
|---------|----------|
| PostgreSQL | interno à rede Docker |
| Gateway | `http://localhost:5118` |
| Identity Service | `http://localhost:5119` |
| Backoffice Service | `http://localhost:5120` |
| Public Service | `http://localhost:5121` |

---

## Backups e auditoria

### Backup automático do PostgreSQL

Os `docker-compose` do projeto sobem um sidecar dedicado de backup:

- Compose da raiz: `myrati-postgres-backup`
- Compose do backend: `postgres-backup`

Esse container executa `pg_dump` em formato customizado no volume `myrati-postgres-backups`, aplica compressão nativa, criptografa o artefato final e pode promover as cópias para o bucket R2 configurado.

Variáveis relevantes:

| Variável | Padrão | Função |
|----------|--------|--------|
| `MYRATI_BACKUP_INTERVAL_SECONDS` | `86400` | intervalo entre backups |
| `MYRATI_BACKUP_RETENTION_DAYS` | `7` | retenção dos dumps |
| `MYRATI_BACKUP_FILE_PREFIX` | `myrati` | prefixo do arquivo |
| `MYRATI_BACKUP_COMPRESSION` | `gzip:level=6` | compressão do dump customizado |
| `MYRATI_BACKUP_ENCRYPTION_PASSPHRASE` | vazio | segredo para gerar `.dump.enc` |
| `MYRATI_R2_*` | vazio | upload offsite e retenção GFS no Cloudflare R2 |

Exemplo de inspeção local:

```powershell
docker logs myrati-postgres-backup --tail 20
docker exec myrati-postgres-backup sh -lc "ls -lah /backups"
```

Exemplo de restauração com o helper do sidecar:

```powershell
docker exec -e POSTGRES_PASSWORD=postgres `
  -e BACKUP_ENCRYPTION_PASSPHRASE=seu-segredo `
  -i myrati-postgres-backup restore-postgres-backup.sh `
  /backups/myrati_YYYYMMDDTHHMMSSZ.dump.enc
```

O sidecar gera:

- `*.dump.enc` criptografado
- `*.sha256` para verificação de integridade
- promoção automática em `daily/`, `monthly/` e `yearly/` quando o R2 está configurado

> O backup automático melhora resiliência operacional, mas continua exigindo teste periódico de restore e revisão operacional do segredo de criptografia.

### Auditoria estruturada

O backend agora grava trilha técnica de auditoria para operações autenticadas e mutações públicas sensíveis.

Cobertura atual:

- `/api/v1/auth/*`
- `/api/v1/backoffice/*` (exceto streams/health/swagger)
- `POST /api/v1/public/contact`
- `POST /api/v1/public/licenses/activate`

Cada registro persiste:

- data/hora UTC
- serviço
- evento
- método HTTP
- rota
- recurso e identificador quando disponíveis
- status HTTP e resultado
- ator autenticado (`userId`, `email`, `role`) quando existir
- IP, user-agent e trace identifier

Consulta administrativa:

- `GET /api/v1/backoffice/audit-logs?limit=100`
- acesso: política `BackofficeWrite`

Retenção:

- `MYRATI_AUDIT_RETENTION_DAYS` / `Audit:RetentionDays`
- limpeza aplicada no startup

### Compliance operacional

O backoffice agora expõe um módulo técnico para suportar as principais obrigações operacionais de privacidade:

- solicitações do titular (`data-subject-requests`)
- registro de operações de tratamento (`processing-activities`)
- incidentes de segurança com dados pessoais (`security-incidents`)

Rotas:

- `GET /api/v1/backoffice/compliance`
- `POST /api/v1/backoffice/compliance/data-subject-requests`
- `PUT /api/v1/backoffice/compliance/data-subject-requests/{requestId}`
- `POST /api/v1/backoffice/compliance/processing-activities`
- `PUT /api/v1/backoffice/compliance/processing-activities/{activityId}`
- `POST /api/v1/backoffice/compliance/security-incidents`
- `PUT /api/v1/backoffice/compliance/security-incidents/{incidentId}`

Controle de acesso:

- leitura: política `BackofficeRead`
- escrita: política `BackofficeWrite`

Prazo padrão da solicitação do titular:

- `MYRATI_COMPLIANCE_DATA_SUBJECT_REQUEST_DUE_DAYS`

### Data Protection

Os serviços agora aceitam persistência compartilhada de chaves do ASP.NET Data Protection para evitar rotação efêmera a cada recreate de container.

Variáveis relevantes:

- `MYRATI_DATAPROTECTION_APPLICATION_NAME`
- `MYRATI_DATAPROTECTION_KEYS_PATH`
- `MYRATI_DATAPROTECTION_CERTIFICATE_PATH`
- `MYRATI_DATAPROTECTION_CERTIFICATE_PASSWORD`

> Em produção, para eliminar chaves persistidas em claro, configure também um certificado PFX e use `MYRATI_DATAPROTECTION_CERTIFICATE_PATH`.

### Linha de base técnica para LGPD

Do ponto de vista de software, o projeto agora cobre uma base técnica importante para os artigos 37, 46, 48 e 49 da LGPD:

- registro técnico de operações relevantes
- backup recorrente do banco com compressão, criptografia e cópia offsite opcional
- retenção configurável para logs de auditoria
- segregação de acesso aos logs via autorização administrativa
- módulo técnico para solicitações do titular
- registro estruturado das atividades de tratamento
- workflow técnico para incidentes de segurança com dados pessoais
- persistência configurável de chaves de proteção da aplicação

Ainda assim, conformidade LGPD completa não depende só do código. Permanecem itens operacionais e jurídicos fora deste repositório:

- definição de base legal e finalidade por fluxo de negócio
- revisão jurídica do conteúdo cadastrado em `processing-activities`
- procedimento formal de resposta e comunicação para incidentes reais
- política interna de retenção e descarte
- contratos com operadores e nomeação do encarregado quando aplicável

---

## Produção

Para produção em instância única, o compose foi preparado para publicar apenas o frontend na interface pública e deixar PostgreSQL, gateway e microserviços acessíveis apenas em `127.0.0.1`. O container Nginx do frontend faz proxy de `/api/*` internamente para o gateway, evitando CORS no navegador e eliminando a necessidade de expor a API na internet.

### Checklist mínimo antes do deploy

- Defina `MYRATI_JWT_KEY` com um segredo longo e aleatório. Em `Production`, o backend agora falha no startup se a chave padrão ainda estiver configurada.
- Defina `MYRATI_EMAIL_FRONTEND_URL` com a URL pública real do site, para os links de convite e definição de senha.
- Defina as credenciais de e-mail (`MYRATI_EMAIL_SENDER_ADDRESS`, `MYRATI_GMAIL_CLIENT_ID`, `MYRATI_GMAIL_CLIENT_SECRET`, `MYRATI_GMAIL_REFRESH_TOKEN`) ou o envio cairá no fallback de log.
- Se optar por usar a API pelo mesmo domínio do frontend, deixe `MYRATI_FRONTEND_API_URL` vazio.
- O compose padrão não publica a porta do PostgreSQL. Se precisar acesso administrativo ao banco, use `docker exec`, SSH tunnel ou publique a porta explicitamente só no ambiente necessário.

### Exemplo de variáveis para ambiente real

```env
MYRATI_JWT_KEY=<segredo-longo-e-aleatorio>
MYRATI_FRONTEND_BIND=127.0.0.1
MYRATI_FRONTEND_PORT=4173
MYRATI_FRONTEND_API_URL=
MYRATI_EMAIL_FRONTEND_URL=https://app.myrati.com.br
MYRATI_EMAIL_SENDER_ADDRESS=myratisolucoestecnologicas@gmail.com
MYRATI_EMAIL_LEAD_RECIPIENT_ADDRESS=yasmin@myrati.com.br
```

### HTTPS barato com Caddy

Para Lightsail em instância única, o caminho mais barato é evitar o Load Balancer do Lightsail e subir a borda HTTPS no próprio host com Caddy:

```powershell
docker compose -f .\docker-compose.yml -f .\docker-compose.prod.yml up -d --build
```

Defina antes:

```env
MYRATI_SITE_HOST=app.myrati.com.br
MYRATI_FRONTEND_BIND=127.0.0.1
MYRATI_FRONTEND_PORT=4173
MYRATI_FRONTEND_API_URL=
MYRATI_EMAIL_FRONTEND_URL=https://app.myrati.com.br
```

### Frontend na Vercel + backend na Lightsail

Se o frontend ficar fora da instância, a topologia recomendada passa a ser:

- `myrati.com.br` na Vercel
- `api.myrati.com.br` apontando para a Lightsail
- gateway publicado via Caddy na Lightsail
- microserviços e PostgreSQL acessíveis só pela rede Docker interna

> O DNS do subdomínio da API pode ficar integralmente na Vercel. Não há necessidade de criar zona DNS na AWS para esse cenário. O backend só precisa receber o tráfego na Lightsail; a resolução do nome pode ser feita pelo provedor DNS que você já usa para o domínio.

> Para evitar quebra futura de DNS, use uma **Static IP** da Lightsail. O IPv4 público padrão da instância pode mudar quando a máquina é parada e iniciada novamente.

> `api.myrati.com.br` não deve ser conectado ao projeto do frontend na Vercel. Ele deve existir apenas como registro DNS do domínio, apontando para a Lightsail.

Arquivos de apoio:

- `docker-compose.yml` para os serviços internos
- `docker-compose.lightsail.yml` para publicar só o gateway com Caddy
- `deploy/Caddyfile.api` para terminar TLS e repassar ao gateway
- `deploy/VERCEL-LIGHTSAIL.md` para o passo a passo completo do apontamento

Variáveis recomendadas:

```env
MYRATI_SITE_HOST=api.myrati.com.br
MYRATI_EMAIL_FRONTEND_URL=https://myrati.com.br
MYRATI_CORS_ALLOWED_ORIGIN_0=https://myrati.com.br
MYRATI_CORS_ALLOWED_ORIGIN_1=https://www.myrati.com.br
```

Suba na Lightsail com:

```powershell
docker compose -f .\docker-compose.lightsail.yml up -d --build
```

O frontend da Vercel deve consumir `https://api.myrati.com.br`. Para isso, o repositório do frontend pode usar `VITE_API_URL=https://api.myrati.com.br` em produção.

### Importante: não use IP cru no frontend em produção

Quando o frontend está hospedado na Vercel, ele é servido em HTTPS. Nesse cenário, apontar `VITE_API_URL` para algo como `http://13.218.108.99:5118` é uma configuração frágil e inadequada para produção:

- o navegador pode bloquear chamadas HTTP feitas a partir de uma página HTTPS por mixed content;
- certificados públicos automáticos funcionam com domínio, não com um IPv4 solto nesse fluxo;
- se o IP da instância mudar, o frontend quebra até que a variável seja atualizada.

Use o IP direto apenas para smoke tests manuais com `curl` ou para acesso operacional via SSH tunnel. Para tráfego real de navegador, mantenha um subdomínio dedicado, por exemplo `https://api.myrati.com.br`.

---

## Autenticação e autorização

### Papéis

| Papel | BackofficeRead | BackofficeWrite |
|-------|:-:|:-:|
| **Super Admin** | ✓ | ✓ |
| **Admin** | ✓ | ✓ |
| **Vendedor** | ✓ | — |
| **Desenvolvedor** | ✓ | — |
| **Cliente** | Portal apenas | — |

### Fluxo

```
Cliente  ──POST /api/v1/auth/login──>  API  ──>  JWT + dados do usuário
Cliente  ──Authorization: Bearer {token}──>  Rotas autenticadas
Cliente  ──?access_token={jwt}──>  Stream SSE autenticado
```

> O mesmo fluxo é o contrato oficial para automação por agentes. O backend não aceita mutação anônima em catálogo, licenças ou kanban.

---

## Automação por agentes

O backend foi preparado para que um agente como Codex ou Claude opere o módulo de produtos e o kanban de desenvolvimento com segurança, sempre usando credenciais válidas e um token JWT de curta duração.

### Fluxo recomendado

1. O agente recebe do operador humano um usuário e senha válidos.
2. O agente faz `POST /api/v1/auth/login`.
3. O agente guarda o `accessToken` retornado.
4. O agente usa `Authorization: Bearer {token}` em todas as chamadas de backoffice.
5. O agente executa CRUD de produtos, licenças, sprints e tarefas conforme o papel do usuário.

### Escopo disponível para agentes

- Criar, listar, editar e excluir produtos
- Criar, editar, suspender, reativar e excluir licenças
- Ler o detalhe completo do produto, incluindo kanban
- Criar, editar e excluir sprints
- Criar, editar, mover entre colunas e excluir tarefas

### Restrições de segurança

- `Vendedor` não pode alterar catálogo nem kanban
- `Desenvolvedor` pode operar backlog/kanban conforme as políticas de produto
- O kanban só aceita mutações em produtos com status `Em desenvolvimento`
- Exclusão de sprint com tarefas vinculadas retorna `409 Conflict`
- Exclusão de produto com licenças vinculadas retorna `409 Conflict`

### Exemplo de sequência para um agente

```bash
# 1. Login
curl -X POST http://localhost:5118/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@myrati.com",
    "password": "Myrati@123"
  }'

# 2. Criar um produto em desenvolvimento
curl -X POST http://localhost:5118/api/v1/backoffice/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '{
    "name": "Myrati HRM",
    "description": "Gestão de RH e folha",
    "category": "RH",
    "status": "Em desenvolvimento",
    "salesStrategy": "development",
    "version": "0.9.0",
    "plans": [
      {
        "name": "Implantação Base",
        "maxUsers": 50,
        "monthlyPrice": 0,
        "developmentCost": 15000,
        "maintenanceCost": 1200,
        "revenueSharePercent": null
      }
    ]
  }'

# 3. Criar uma sprint
curl -X POST http://localhost:5118/api/v1/backoffice/products/PRD-004/sprints \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '{
    "name": "Sprint 1",
    "startDate": "2026-03-09",
    "endDate": "2026-03-23",
    "status": "Ativa"
  }'

# 4. Criar uma tarefa
curl -X POST http://localhost:5118/api/v1/backoffice/products/PRD-004/tasks \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <JWT>" \
  -d '{
    "sprintId": "SPR-001",
    "title": "Modelar tela de onboarding",
    "description": "Criar a primeira versão da jornada inicial",
    "column": "todo",
    "priority": "high",
    "assignee": "Admin Master",
    "tags": ["frontend", "ux"]
  }'
```

Esse fluxo já cobre o cenário descrito em prompts como: _"Conecte no Myrati com o usuário X e senha Y, vamos desenvolver o produto Z e crie as tarefas que eu indicar."_ O agente autentica, obtém o token e usa apenas rotas autorizadas.

---

## SSE e tempo real

### Streams disponíveis

| Stream | Rota | Acesso |
|--------|------|--------|
| Backoffice | `GET /api/v1/backoffice/events?access_token={jwt}` | Autenticado |
| Status público | `GET /api/v1/public/status/stream` | Público |

### Comportamento

1. Envia evento `connected` ao abrir conexão
2. Envia snapshot inicial logo após conectar
3. Envia `heartbeat` periódico
4. Reaplica snapshots em intervalo fixo
5. Entrega eventos de mutação publicados pela aplicação

### Intervalos

| Stream | Intervalo do snapshot |
|--------|----------------------|
| Backoffice | 15 segundos |
| Status público | 30 segundos |

<details>
<summary><strong>Eventos típicos do backoffice</strong></summary>

| Evento | Descrição |
|--------|-----------|
| `product.created` | Produto criado |
| `product.updated` | Produto atualizado |
| `product.deleted` | Produto removido |
| `sprint.created` | Sprint criada |
| `sprint.updated` | Sprint atualizada |
| `sprint.deleted` | Sprint removida |
| `task.created` | Tarefa criada |
| `task.updated` | Tarefa atualizada |
| `task.moved` | Tarefa movida no kanban |
| `task.deleted` | Tarefa removida |
| `client.created` | Cliente criado |
| `client.updated` | Cliente atualizado |
| `license.created` | Licença criada |
| `license.updated` | Licença atualizada |
| `license.suspended` | Licença suspensa |
| `license.reactivated` | Licença reativada |
| `license.deleted` | Licença removida |
| `settings.updated` | Configurações atualizadas |
| `profile.updated` | Perfil atualizado |

</details>

---

## Rotas da API

Base URL: `/api/v1`

---

### Auth

#### `POST /api/v1/auth/login`

Autentica um administrador e retorna um token JWT.

- **Acesso:** Público (rate limit aplicado)

**Request body:**

```json
{
  "email": "admin@myrati.com",
  "password": "Myrati@123"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `email` | string | Sim | E-mail válido |
| `password` | string | Sim | Mínimo 6 caracteres |

**Response `200 OK`:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-03-10T06:00:00Z",
  "user": {
    "id": "ADM-001",
    "name": "Alex Admin",
    "email": "admin@myrati.com",
    "role": "Super Admin"
  }
}
```

---

#### `GET /api/v1/auth/me`

Retorna os dados do usuário autenticado a partir do token JWT.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
{
  "id": "ADM-001",
  "name": "Alex Admin",
  "email": "admin@myrati.com",
  "role": "Super Admin"
}
```

---

### Dashboard

#### `GET /api/v1/backoffice/dashboard`

Retorna os KPIs, receitas mensais, receita por produto e atividades recentes.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
{
  "totalMonthlyRevenue": 45890.00,
  "activeLicensesCount": 142,
  "totalLicensesCount": 180,
  "onlineUsersCount": 89,
  "totalUsersCount": 234,
  "utilizationRate": 78,
  "activeClients": 56,
  "monthlyRevenue": [
    {
      "month": "Jan",
      "revenue": 38500.00,
      "licenses": 120
    },
    {
      "month": "Fev",
      "revenue": 41200.00,
      "licenses": 135
    }
  ],
  "revenueByProduct": [
    {
      "name": "Myrati ERP",
      "value": 28500.00
    },
    {
      "name": "Myrati CRM",
      "value": 17390.00
    }
  ],
  "recentActivity": [
    {
      "id": "ACT-001",
      "action": "license.created",
      "description": "Licença criada para cliente CLI-012",
      "time": "Há 5 minutos",
      "type": "license"
    }
  ]
}
```

---

### Products

Produtos agora expõem a estratégia comercial do item e, para telas de detalhe, o snapshot completo do kanban.

### Estratégias de venda suportadas

| `salesStrategy` | Uso | Regras de plano |
|-----------------|-----|-----------------|
| `subscription` | Licenciamento mensal tradicional | `monthlyPrice > 0` quando o produto nao estiver em `Em desenvolvimento` |
| `development` | Projeto sob encomenda + manutenção | `developmentCost > 0` e `maintenanceCost > 0` quando o produto nao estiver em `Em desenvolvimento` |
| `revenue_share` | Manutenção + participação no faturamento | `maintenanceCost > 0` e `revenueSharePercent > 0` quando o produto nao estiver em `Em desenvolvimento` |

#### `GET /api/v1/backoffice/products`

Lista os produtos com métricas, estratégia de venda e planos.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
[
  {
    "id": "PRD-004",
    "name": "Myrati HRM",
    "description": "Plataforma de RH em desenvolvimento",
    "category": "RH",
    "status": "Em desenvolvimento",
    "salesStrategy": "development",
    "totalLicenses": 2,
    "activeLicenses": 0,
    "monthlyRevenue": 0,
    "createdDate": "2026-03-01",
    "version": "0.9.0",
    "plans": [
      {
        "id": "PLN-001",
        "name": "Implantação Base",
        "maxUsers": 50,
        "monthlyPrice": 0,
        "developmentCost": 15000,
        "maintenanceCost": 1200,
        "revenueSharePercent": null
      }
    ]
  }
]
```

#### `GET /api/v1/backoffice/products/{productId}`

Retorna o detalhe do produto, incluindo licenças e o snapshot do kanban.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
{
  "id": "PRD-004",
  "name": "Myrati HRM",
  "status": "Em desenvolvimento",
  "salesStrategy": "development",
  "plans": [
    {
      "id": "PLN-001",
      "name": "Implantação Base",
      "maxUsers": 50,
      "monthlyPrice": 0,
      "developmentCost": 15000,
      "maintenanceCost": 1200,
      "revenueSharePercent": null
    }
  ],
  "licenses": [],
  "kanban": {
    "sprints": [
      {
        "id": "SPR-001",
        "productId": "PRD-004",
        "name": "Sprint 1",
        "startDate": "2026-03-09",
        "endDate": "2026-03-23",
        "status": "Ativa"
      }
    ],
    "tasks": [
      {
        "id": "TSK-001",
        "productId": "PRD-004",
        "sprintId": "SPR-001",
        "title": "Criar onboarding",
        "description": "Primeira etapa do fluxo inicial",
        "column": "todo",
        "priority": "high",
        "assignee": "Admin Master",
        "tags": ["frontend", "ux"],
        "createdDate": "2026-03-09"
      }
    ],
    "availableAssignees": ["Admin Master", "Maria Santos"]
  }
}
```

> Para produtos que não estão em `Em desenvolvimento`, o campo `kanban` volta vazio.

#### `POST /api/v1/backoffice/products`

Cria um produto com planos e estratégia de venda.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "name": "Myrati HRM",
  "description": "Plataforma de RH e folha",
  "category": "RH",
  "status": "Em desenvolvimento",
  "salesStrategy": "development",
  "version": "0.9.0",
  "plans": [
    {
      "name": "Implantação Base",
      "maxUsers": 50,
      "monthlyPrice": 0,
      "developmentCost": 15000,
      "maintenanceCost": 1200,
      "revenueSharePercent": null
    }
  ]
}
```

| Campo | Tipo | Obrigatório | Regra |
|-------|------|:-----------:|-------|
| `name` | string | Sim | Máx. 120 |
| `description` | string | Sim | Máx. 500 |
| `category` | string | Sim | Máx. 120 |
| `status` | string | Sim | `Ativo`, `Inativo`, `Em desenvolvimento` |
| `salesStrategy` | string | Sim | `subscription`, `development`, `revenue_share` |
| `version` | string | Sim | Máx. 30 |
| `plans` | array | Sim | Pelo menos 1 plano |
| `plans[].name` | string | Sim | Máx. 60 |
| `plans[].maxUsers` | int | Sim | `>= 0`; em `Ativo`/`Inativo` precisa ser `> 0` |
| `plans[].monthlyPrice` | decimal | Sim | `>= 0` |
| `plans[].developmentCost` | decimal | Condicional | Obrigatório em `development` fora do rascunho |
| `plans[].maintenanceCost` | decimal | Condicional | Obrigatório em `development` e `revenue_share` fora do rascunho |
| `plans[].revenueSharePercent` | decimal | Condicional | Obrigatório em `revenue_share` fora do rascunho |

**Response `201 Created`:** retorna `ProductDetailDto` em JSON, sem cabeçalho `Location`.

#### `PUT /api/v1/backoffice/products/{productId}`

Atualiza o produto, a estratégia de venda e todos os planos.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Request body:** mesmo contrato do `POST /products`.

**Response `200 OK`:** retorna `ProductDetailDto`.

#### `DELETE /api/v1/backoffice/products/{productId}`

Remove um produto.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Response `204 No Content`**

Regras:

- retorna `409 Conflict` se o produto ainda possuir licenças vinculadas
- usuários conectados, tarefas, sprints e planos do produto são removidos junto com o item

---

### Kanban por produto

O kanban fica em rotas de backoffice autenticadas e só aceita mutações quando o produto está com status `Em desenvolvimento`.

| Método | Rota | Política | Uso |
|--------|------|----------|-----|
| `GET` | `/api/v1/backoffice/products/{productId}/kanban` | `BackofficeRead` | Lê o quadro completo |
| `POST` | `/api/v1/backoffice/products/{productId}/sprints` | `BackofficeWrite` | Cria sprint |
| `PUT` | `/api/v1/backoffice/products/{productId}/sprints/{sprintId}` | `BackofficeWrite` | Atualiza sprint |
| `DELETE` | `/api/v1/backoffice/products/{productId}/sprints/{sprintId}` | `BackofficeWrite` | Exclui sprint |
| `POST` | `/api/v1/backoffice/products/{productId}/tasks` | `BackofficeWrite` | Cria tarefa |
| `PUT` | `/api/v1/backoffice/products/{productId}/tasks/{taskId}` | `BackofficeWrite` | Atualiza ou move tarefa |
| `DELETE` | `/api/v1/backoffice/products/{productId}/tasks/{taskId}` | `BackofficeWrite` | Exclui tarefa |

#### Exemplo de criação de sprint

```json
{
  "name": "Sprint 2",
  "startDate": "2026-03-24",
  "endDate": "2026-04-07",
  "status": "Planejada"
}
```

#### Exemplo de criação ou atualização de tarefa

```json
{
  "sprintId": "SPR-002",
  "title": "Implementar fluxo de autenticação",
  "description": "Conectar login do produto ao backend",
  "column": "in_progress",
  "priority": "critical",
  "assignee": "Admin Master",
  "tags": ["backend", "auth"]
}
```

Regras importantes:

- `status` da sprint: `Planejada`, `Ativa`, `Concluída`
- `column` da tarefa: `backlog`, `todo`, `in_progress`, `review`, `done`
- `priority` da tarefa: `low`, `medium`, `high`, `critical`
- só pode existir uma sprint `Ativa` por produto
- excluir sprint com tarefas vinculadas retorna `409 Conflict`
- tentar editar kanban de produto fora de desenvolvimento retorna `409 Conflict`

---

### Licenses

As licenças continuam sendo geridas no módulo de produtos, mas o payload agora suporta os cenários de desenvolvimento e participação no faturamento.

| Método | Rota | Política | Uso |
|--------|------|----------|-----|
| `POST` | `/api/v1/backoffice/products/{productId}/licenses` | `BackofficeWrite` | Cria licença |
| `PUT` | `/api/v1/backoffice/licenses/{licenseId}` | `BackofficeWrite` | Atualiza licença |
| `POST` | `/api/v1/backoffice/licenses/{licenseId}/suspend` | `BackofficeWrite` | Suspende |
| `POST` | `/api/v1/backoffice/licenses/{licenseId}/reactivate` | `BackofficeWrite` | Reativa |
| `DELETE` | `/api/v1/backoffice/licenses/{licenseId}` | `BackofficeWrite` | Exclui |

#### Contrato de criação e edição

```json
{
  "clientId": "CLI-003",
  "plan": "Implantação Base",
  "monthlyValue": 1200,
  "developmentCost": 15000,
  "revenueSharePercent": null,
  "startDate": "2026-04-01",
  "expiryDate": "2027-04-01"
}
```

| Campo | Tipo | Obrigatório | Regra |
|-------|------|:-----------:|-------|
| `clientId` | string | Sim | Cliente existente e ativo |
| `plan` | string | Sim | Plano pertencente ao produto |
| `monthlyValue` | decimal | Sim | `> 0` |
| `developmentCost` | decimal | Condicional | Necessário em produtos `development` se o plano exigir |
| `revenueSharePercent` | decimal | Condicional | Necessário em produtos `revenue_share` se o plano exigir |
| `startDate` | string | Sim | ISO date |
| `expiryDate` | string | Sim | ISO date maior que `startDate` |

**Response `200 OK`:**

```json
{
  "id": "LIC-042",
  "clientId": "CLI-003",
  "clientName": "Tech Solutions SA",
  "productId": "PRD-004",
  "productName": "Myrati HRM",
  "plan": "Implantação Base",
  "maxUsers": 50,
  "activeUsers": 0,
  "status": "Ativa",
  "startDate": "2026-04-01",
  "expiryDate": "2027-04-01",
  "monthlyValue": 1200,
  "developmentCost": 15000,
  "revenueSharePercent": null
}
```

---

### Clients

#### `GET /api/v1/backoffice/clients`

Lista todos os clientes com métricas resumidas.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
[
  {
    "id": "CLI-001",
    "name": "Empresa ABC Ltda",
    "email": "contato@empresaabc.com.br",
    "phone": "(11) 99999-0001",
    "document": "12.345.678/0001-90",
    "documentType": "CNPJ",
    "company": "Empresa ABC Ltda",
    "totalLicenses": 3,
    "activeLicenses": 2,
    "monthlyRevenue": 1299.70,
    "joinedDate": "2025-01-20",
    "status": "Ativo"
  }
]
```

---

#### `GET /api/v1/backoffice/clients/{clientId}`

Retorna o detalhe de um cliente com seus usuários e licenças vinculadas.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `clientId` (string) — ex: `CLI-001`

**Response `200 OK`:**

```json
{
  "id": "CLI-001",
  "name": "Empresa ABC Ltda",
  "email": "contato@empresaabc.com.br",
  "phone": "(11) 99999-0001",
  "document": "12.345.678/0001-90",
  "documentType": "CNPJ",
  "company": "Empresa ABC Ltda",
  "totalLicenses": 3,
  "activeLicenses": 2,
  "monthlyRevenue": 1299.70,
  "joinedDate": "2025-01-20",
  "status": "Ativo",
  "users": [
    {
      "id": "USR-001",
      "name": "João Silva",
      "email": "joao@empresaabc.com.br",
      "clientId": "CLI-001",
      "clientName": "Empresa ABC Ltda",
      "productId": "PRD-001",
      "productName": "Myrati ERP",
      "lastActive": "Há 2 minutos",
      "status": "Online"
    }
  ],
  "licenses": [
    {
      "id": "LIC-001",
      "clientId": "CLI-001",
      "clientName": "Empresa ABC Ltda",
      "productId": "PRD-001",
      "productName": "Myrati ERP",
      "plan": "Professional",
      "maxUsers": 25,
      "activeUsers": 18,
      "status": "Ativa",
      "startDate": "2025-02-01",
      "expiryDate": "2026-02-01",
      "monthlyValue": 499.90
    }
  ]
}
```

---

#### `POST /api/v1/backoffice/clients`

Cria um novo cliente.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "name": "Nova Empresa Tech",
  "email": "contato@novatech.com.br",
  "phone": "(21) 98888-1234",
  "document": "98.765.432/0001-10",
  "documentType": "CNPJ",
  "company": "Nova Empresa Tech Ltda",
  "status": "Ativo"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `name` | string | Sim | Máx. 120 caracteres |
| `email` | string | Sim | E-mail válido |
| `phone` | string | Sim | Máx. 25 caracteres |
| `document` | string | Sim | Máx. 20 caracteres |
| `documentType` | string | Sim | `CPF` ou `CNPJ` |
| `company` | string | Sim | Máx. 160 caracteres |
| `status` | string | Sim | `Ativo` ou `Inativo` |

**Response `201 Created`:** retorna o `ClientDetailDto` completo (mesmo formato do GET por ID, com `users` e `licenses` vazios).

---

#### `PUT /api/v1/backoffice/clients/{clientId}`

Atualiza um cliente existente.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `clientId` (string) — ex: `CLI-001`

**Request body:** mesmo formato do POST de criação.

**Response `200 OK`:** retorna o `ClientDetailDto` atualizado.

---

#### `DELETE /api/v1/backoffice/clients/{clientId}`

Remove um cliente que não possua licenças ativas.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `clientId` (string) — ex: `CLI-001`

**Response `204 No Content`**

> Retorna `409 Conflict` se o cliente possuir licenças ativas.

---

### Users

#### `GET /api/v1/backoffice/users`

Lista os usuários conectados com suporte a filtros por query parameters.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Query parameters:**

| Parâmetro | Tipo | Obrigatório | Descrição |
|-----------|------|:-----------:|-----------|
| `search` | string | Não | Filtra por nome ou e-mail do usuário |
| `status` | string | Não | Filtra por status (ex: `Online`, `Offline`) |
| `productId` | string | Não | Filtra por produto (ex: `PRD-001`) |

**Exemplo:** `GET /api/v1/backoffice/users?search=joao&status=Online&productId=PRD-001`

**Response `200 OK`:**

```json
[
  {
    "id": "USR-001",
    "name": "João Silva",
    "email": "joao@empresaabc.com.br",
    "clientId": "CLI-001",
    "clientName": "Empresa ABC Ltda",
    "productId": "PRD-001",
    "productName": "Myrati ERP",
    "lastActive": "Há 2 minutos",
    "status": "Online"
  }
]
```

---

### Settings

#### `GET /api/v1/backoffice/settings`

Retorna o snapshot completo das configurações da plataforma.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
{
  "companyInfo": {
    "name": "Myrati Software",
    "cnpj": "12.345.678/0001-90",
    "email": "contato@myrati.com",
    "phone": "(11) 3000-0000",
    "address": "Av. Paulista, 1000",
    "city": "São Paulo - SP"
  },
  "regional": {
    "language": "pt-BR",
    "timezone": "America/Sao_Paulo"
  },
  "notifications": {
    "emailNotifications": true,
    "pushNotifications": true,
    "licenseAlerts": true,
    "usageAlerts": false,
    "weeklyReport": true
  },
  "security": {
    "twoFactorAuth": false,
    "sessionTimeout": "8 horas"
  },
  "apiKeys": [
    {
      "id": "KEY-001",
      "label": "Produção Principal",
      "prefix": "mk_prod_",
      "key": "mk_prod_abc123def456",
      "active": true,
      "createdAt": "2025-06-15T10:30:00Z"
    }
  ],
  "teamMembers": [
    {
      "id": "TM-001",
      "name": "Alex Admin",
      "email": "admin@myrati.com",
      "role": "Super Admin",
      "status": "Ativo"
    }
  ]
}
```

---

#### `PUT /api/v1/backoffice/settings`

Atualiza as configurações gerais da plataforma (empresa, regional, notificações e segurança). Não altera chaves de API nem membros da equipe.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "companyInfo": {
    "name": "Myrati Software",
    "cnpj": "12.345.678/0001-90",
    "email": "contato@myrati.com",
    "phone": "(11) 3000-0000",
    "address": "Av. Paulista, 1000",
    "city": "São Paulo - SP"
  },
  "regional": {
    "language": "pt-BR",
    "timezone": "America/Sao_Paulo"
  },
  "notifications": {
    "emailNotifications": true,
    "pushNotifications": true,
    "licenseAlerts": true,
    "usageAlerts": false,
    "weeklyReport": true
  },
  "security": {
    "twoFactorAuth": false,
    "sessionTimeout": "8 horas"
  }
}
```

| Seção | Campo | Tipo | Obrigatório | Validação |
|-------|-------|------|:-----------:|-----------|
| `companyInfo` | `name` | string | Sim | — |
| `companyInfo` | `cnpj` | string | Sim | — |
| `companyInfo` | `email` | string | Sim | E-mail válido |
| `companyInfo` | `phone` | string | Não | — |
| `companyInfo` | `address` | string | Não | — |
| `companyInfo` | `city` | string | Não | — |
| `regional` | `language` | string | Sim | — |
| `regional` | `timezone` | string | Sim | — |
| `notifications` | `emailNotifications` | bool | Não | — |
| `notifications` | `pushNotifications` | bool | Não | — |
| `notifications` | `licenseAlerts` | bool | Não | — |
| `notifications` | `usageAlerts` | bool | Não | — |
| `notifications` | `weeklyReport` | bool | Não | — |
| `security` | `twoFactorAuth` | bool | Não | — |
| `security` | `sessionTimeout` | string | Sim | — |

**Response `200 OK`:** retorna o `SettingsSnapshotDto` completo (mesmo formato do GET).

---

#### `POST /api/v1/backoffice/settings/api-keys`

Cria uma nova chave de API.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "label": "Integração Staging",
  "environment": "staging"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `label` | string | Sim | Máx. 80 caracteres |
| `environment` | string | Sim | `production` ou `staging` |

**Response `201 Created`:**

```json
{
  "id": "KEY-003",
  "label": "Integração Staging",
  "prefix": "mk_stag_",
  "key": "mk_stag_xyz789ghi012",
  "active": true,
  "createdAt": "2026-03-09T14:00:00Z"
}
```

---

#### `POST /api/v1/backoffice/settings/api-keys/{apiKeyId}/rotate`

Rotaciona (regenera) uma chave de API existente.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `apiKeyId` (string) — ex: `KEY-001`
- **Request body:** nenhum

**Response `200 OK`:** retorna o `ApiKeyDto` com a nova chave gerada.

---

#### `POST /api/v1/backoffice/settings/api-keys/{apiKeyId}/toggle`

Ativa ou desativa uma chave de API.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `apiKeyId` (string) — ex: `KEY-001`
- **Request body:** nenhum

**Response `200 OK`:** retorna o `ApiKeyDto` com o campo `active` invertido.

---

#### `DELETE /api/v1/backoffice/settings/api-keys/{apiKeyId}`

Remove uma chave de API.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `apiKeyId` (string) — ex: `KEY-001`

**Response `204 No Content`**

---

#### `POST /api/v1/backoffice/settings/team-members`

Cria um novo membro na equipe administrativa.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "name": "Maria Souza",
  "email": "maria@myrati.com",
  "role": "Admin"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `name` | string | Sim | — |
| `email` | string | Sim | E-mail válido |
| `role` | string | Sim | `Admin`, `Vendedor` ou `Desenvolvedor` |

**Response `201 Created`:**

```json
{
  "id": "TM-004",
  "name": "Maria Souza",
  "email": "maria@myrati.com",
  "role": "Admin",
  "status": "Convite Pendente"
}
```

---

#### `PUT /api/v1/backoffice/settings/team-members/{teamMemberId}`

Atualiza um membro da equipe.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `teamMemberId` (string) — ex: `TM-004`

**Request body:**

```json
{
  "name": "Maria Souza",
  "email": "maria@myrati.com",
  "role": "Super Admin",
  "status": "Ativo"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `name` | string | Sim | — |
| `email` | string | Sim | E-mail válido |
| `role` | string | Sim | `Super Admin`, `Admin`, `Vendedor` ou `Desenvolvedor` |
| `status` | string | Sim | `Ativo` ou `Convite Pendente` |

**Response `200 OK`:** retorna o `TeamMemberDto` atualizado.

---

#### `DELETE /api/v1/backoffice/settings/team-members/{teamMemberId}`

Remove um membro da equipe.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `teamMemberId` (string) — ex: `TM-004`

**Response `204 No Content`**

---

### Profile

#### `GET /api/v1/backoffice/profile`

Retorna o perfil completo do usuário autenticado, incluindo sessões ativas e log de atividade.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
{
  "profile": {
    "id": "ADM-001",
    "name": "Alex Admin",
    "email": "admin@myrati.com",
    "phone": "(11) 99999-0000",
    "role": "Super Admin",
    "department": "Tecnologia",
    "location": "São Paulo, SP"
  },
  "activeSessions": [
    {
      "id": "SES-001",
      "location": "Chrome em Windows — São Paulo, SP",
      "lastActive": "Agora",
      "current": true
    },
    {
      "id": "SES-002",
      "location": "Safari em macOS — Rio de Janeiro, RJ",
      "lastActive": "Há 3 horas",
      "current": false
    }
  ],
  "activityLog": [
    {
      "action": "Login realizado",
      "date": "2026-03-09"
    },
    {
      "action": "Perfil atualizado",
      "date": "2026-03-08"
    }
  ]
}
```

---

#### `PUT /api/v1/backoffice/profile`

Atualiza o perfil do usuário autenticado.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "name": "Alex Admin",
  "email": "alex@myrati.com",
  "phone": "(11) 99999-0000",
  "department": "Tecnologia",
  "location": "São Paulo, SP"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `name` | string | Sim | — |
| `email` | string | Sim | E-mail válido |
| `phone` | string | Sim | — |
| `department` | string | Sim | — |
| `location` | string | Sim | — |

**Response `200 OK`:**

```json
{
  "id": "ADM-001",
  "name": "Alex Admin",
  "email": "alex@myrati.com",
  "phone": "(11) 99999-0000",
  "role": "Super Admin",
  "department": "Tecnologia",
  "location": "São Paulo, SP"
}
```

---

#### `POST /api/v1/backoffice/profile/change-password`

Altera a senha do usuário autenticado.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "currentPassword": "Myrati@123",
  "newPassword": "NovaSenha@456",
  "confirmPassword": "NovaSenha@456"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `currentPassword` | string | Sim | Deve corresponder à senha atual |
| `newPassword` | string | Sim | Mínimo 8 caracteres |
| `confirmPassword` | string | Sim | Deve ser igual a `newPassword` |

**Response `204 No Content`**

---

#### `POST /api/v1/backoffice/profile/sessions/{sessionId}/revoke`

Encerra uma sessão específica do usuário autenticado.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `sessionId` (string) — ex: `SES-002`
- **Request body:** nenhum

**Response `204 No Content`**

---

### Public

#### `POST /api/v1/public/licenses/activate`

Valida se uma licença pertence ao produto informado e se pode ser ativada. Verifica produto, cliente, status e validade.

- **Acesso:** Público (rate limit aplicado)

**Request body:**

```json
{
  "productId": "PRD-001",
  "licenseKey": "ABCD-1234-EFGH-5678"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `productId` | string | Sim | Máx. 40 caracteres |
| `licenseKey` | string | Sim | Máx. 64 caracteres |

**Response `200 OK`:**

```json
{
  "licenseId": "LIC-001",
  "productId": "PRD-001",
  "productName": "Myrati ERP",
  "clientId": "CLI-001",
  "clientName": "Empresa ABC Ltda",
  "plan": "Professional",
  "status": "Ativa",
  "startDate": "2025-02-01",
  "expiryDate": "2026-02-01",
  "maxUsers": 25,
  "activeUsers": 18,
  "message": "Licença válida e ativa"
}
```

---

#### `POST /api/v1/public/contact`

Registra um lead de contato a partir do formulário público e encaminha os dados para o inbox comercial configurado.

- **Acesso:** Público (rate limit aplicado)

**Request body:**

```json
{
  "name": "Carlos Oliveira",
  "email": "carlos@empresa.com.br",
  "company": "Empresa XYZ",
  "subject": "Interesse no Myrati ERP",
  "message": "Gostaria de agendar uma demonstração do produto para a nossa equipe de 15 pessoas."
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `name` | string | Sim | — |
| `email` | string | Sim | E-mail válido |
| `company` | string | Não | Máx. 160 caracteres |
| `subject` | string | Não | Máx. 120 caracteres |
| `message` | string | Sim | Máx. 2000 caracteres |

**Response `200 OK`:**

```json
{
  "message": "Mensagem enviada com sucesso."
}
```

---

#### `GET /api/v1/public/status`

Retorna o status atual da plataforma, incluindo serviços, incidentes e histórico de uptime.

- **Acesso:** Público (rate limit aplicado)

**Response `200 OK`:**

```json
{
  "overallStatus": "operational",
  "lastUpdated": "2026-03-09T12:00:00Z",
  "services": [
    {
      "id": "SVC-001",
      "name": "API Principal",
      "status": "operational",
      "uptime": "99.98%",
      "responseTime": "45ms"
    },
    {
      "id": "SVC-002",
      "name": "Banco de Dados",
      "status": "operational",
      "uptime": "99.99%",
      "responseTime": "12ms"
    },
    {
      "id": "SVC-003",
      "name": "Autenticação",
      "status": "degraded",
      "uptime": "99.85%",
      "responseTime": "120ms"
    }
  ],
  "incidents": [
    {
      "id": "INC-001",
      "date": "2026-03-07",
      "title": "Lentidão no serviço de autenticação",
      "description": "Identificamos lentidão no serviço de autenticação entre 14h e 15h.",
      "resolved": true
    }
  ],
  "uptimeHistory": [
    {
      "id": "UPT-001",
      "day": "2026-03-09",
      "pct": 99.98
    },
    {
      "id": "UPT-002",
      "day": "2026-03-08",
      "pct": 100.0
    }
  ]
}
```

---

### Streams e saúde

#### `GET /api/v1/backoffice/events`

Stream SSE autenticado do backoffice. Envia eventos de mutação e snapshots periódicos do dashboard.

- **Acesso:** `BackofficeRead`
- **Query param:** `access_token` (string) — token JWT (alternativa ao header `Authorization`)
- **Content-Type da resposta:** `text/event-stream`

**Eventos enviados:**

```
event: connected
data: {"channel":"backoffice","user":"ADM-001"}

event: dashboard.snapshot
data: { ... DashboardDto completo ... }

event: heartbeat
data: {}

event: product.created
data: {"id":"PRD-005","name":"Novo Produto"}

event: license.suspended
data: {"id":"LIC-042","status":"Suspensa"}
```

---

#### `GET /api/v1/public/status/stream`

Stream SSE público da status page. Envia snapshots periódicos do status e eventos de mudança.

- **Acesso:** Público (rate limit aplicado)
- **Content-Type da resposta:** `text/event-stream`

**Eventos enviados:**

```
event: connected
data: {"channel":"public-status"}

event: status.snapshot
data: { ... StatusPageDto completo ... }

event: heartbeat
data: {}
```

---

#### `GET /health`

Health check da aplicação.

- **Acesso:** Público

**Response `200 OK`:**

```json
{
  "status": "Healthy"
}
```

---

## Regras de validação

### Campos de status válidos

| Contexto | Valores aceitos |
|----------|----------------|
| Produto | `Ativo`, `Inativo`, `Em desenvolvimento` |
| Cliente | `Ativo`, `Inativo` |
| Licença | `Ativa`, `Pendente`, `Suspensa`, `Expirada` |
| Estratégia de venda | `subscription`, `development`, `revenue_share` |
| Sprint | `Planejada`, `Ativa`, `Concluída` |
| Coluna do kanban | `backlog`, `todo`, `in_progress`, `review`, `done` |
| Prioridade de tarefa | `low`, `medium`, `high`, `critical` |
| Membro da equipe (role) | `Super Admin`, `Admin`, `Vendedor`, `Desenvolvedor`, `Cliente` |
| Membro da equipe (status) | `Ativo`, `Convite Pendente` |
| Tipo de documento | `CPF`, `CNPJ` |
| Ambiente de API Key | `production`, `staging` |
| Status de serviço | `operational`, `degraded`, `down` |

### Limites de comprimento

| Campo | Máximo |
|-------|--------|
| `name` (produto/cliente) | 120 caracteres |
| `description` (produto) | 500 caracteres |
| `category` (produto) | 120 caracteres |
| `version` (produto) | 30 caracteres |
| `plan` (licença/plano) | 60 caracteres |
| `company` (cliente/contato) | 160 caracteres |
| `phone` (cliente) | 25 caracteres |
| `document` (cliente) | 20 caracteres |
| `productId` (ativação) | 40 caracteres |
| `licenseKey` (ativação) | 64 caracteres |
| `label` (API key) | 80 caracteres |
| `subject` (contato) | 120 caracteres |
| `message` (contato) | 2000 caracteres |
| `title` (tarefa) | 160 caracteres |
| `description` (tarefa) | 1000 caracteres |
| `tag` (tarefa) | 30 caracteres por item |

### Restrições numéricas

| Campo | Regra |
|-------|-------|
| `maxUsers` (plano) | Maior ou igual a 0; para licenciar precisa ser maior que 0 |
| `monthlyPrice` (plano) | Maior ou igual a 0 |
| `developmentCost` (plano/licença) | Maior que 0 quando informado |
| `maintenanceCost` (plano) | Maior que 0 quando exigido pela estratégia |
| `revenueSharePercent` (plano/licença) | Entre 0 e 100 |
| `monthlyValue` (licença) | Maior que 0 |

### Outras regras

- Campos de e-mail devem ser um endereço válido
- `password` no login: mínimo 6 caracteres
- `newPassword` na troca de senha: mínimo 8 caracteres
- `confirmPassword` deve ser igual a `newPassword`
- `expiryDate` da licença deve ser posterior a `startDate`
- `endDate` da sprint deve ser posterior a `startDate`
- produtos em `Em desenvolvimento` aceitam plano em rascunho com campos comerciais pendentes
- produtos `subscription` exigem `monthlyPrice > 0` em todos os planos quando saem do rascunho
- produtos `development` exigem `developmentCost` e `maintenanceCost` em todos os planos quando saem do rascunho
- produtos `revenue_share` exigem `maintenanceCost` e `revenueSharePercent` em todos os planos quando saem do rascunho
- licenças exigem plano com `maxUsers > 0`
- o kanban só aceita escrita quando o produto está em `Em desenvolvimento`

---

## Códigos de resposta HTTP

| Código | Significado | Quando ocorre |
|--------|-------------|---------------|
| `200 OK` | Sucesso | GET, PUT e POST que retornam dados |
| `201 Created` | Recurso criado | POST de criação |
| `204 No Content` | Sucesso sem corpo | DELETE, troca de senha, revogação de sessão |
| `400 Bad Request` | Erro de validação | Campos inválidos ou faltantes |
| `401 Unauthorized` | Não autenticado | Token ausente ou inválido |
| `403 Forbidden` | Sem permissão | Papel insuficiente para a política exigida |
| `404 Not Found` | Recurso não existe | ID inválido ou inexistente |
| `409 Conflict` | Conflito de estado | Produto com licenças, sprint com tarefas, kanban fora de produto em desenvolvimento |
| `429 Too Many Requests` | Rate limit excedido | Mais de 20 req/min por IP em rotas públicas |
| `500 Internal Server Error` | Erro interno | Falha não tratada no servidor |

---

## Testes

### Stack de testes

xUnit · Moq · WebApplicationFactory · SQLite in-memory · Coverlet

### Tipos de teste

- Testes de aplicação para regras de negócio
- Testes de integração HTTP da API
- Testes de autenticação e autorização
- Testes de endpoints públicos
- Testes de SSE com leitura de `text/event-stream`
- Testes de ciclo de vida de produto, licença e kanban
- Testes de conflitos de negócio com respostas amigáveis (`409`)

### Comandos

```powershell
# Build e testes
dotnet build .\Myrati.slnx
dotnet test .\Myrati.slnx

# Com cobertura
dotnet test .\Myrati.slnx --collect:"XPlat Code Coverage"
```

---

## Seeds e credenciais iniciais

O banco é inicializado com `EnsureCreated` e seed automático no startup.

### Credencial padrão

| Campo | Valor |
|-------|-------|
| E-mail | `admin@myrati.com` |
| Senha | `Myrati@123` |

### Dados pré-carregados

Produtos e planos · Clientes · Licenças · Usuários conectados · Dashboard snapshots · Configurações · Equipe · Perfil, sessões e atividades · Status público e incidentes

> O seed só é executado sobre base ainda não populada com os conjuntos principais.

---

## Guia de contribuição

### Princípios

- Controllers finos — regra de negócio fica em `Application/Services`
- Validação em `Validation/` com FluentValidation
- Entidades e relacionamentos em `Domain`
- Persistência, JWT, hashing e seed em `Infrastructure`
- Todo comportamento novo nasce com teste

### Fluxo para novas features

1. Defina contratos de entrada/saída em `Myrati.Application/Contracts`
2. Crie ou atualize validador em `Myrati.Application/Validation`
3. Implemente a regra em `Myrati.Application/Services`
4. Se necessário, ajuste `Myrati.Infrastructure`
5. Exponha via controller na `Myrati.API`
6. Se impactar o front em tempo real, publique evento via `IRealtimeEventPublisher`
7. Adicione testes de aplicação e/ou integração

### Boas práticas

- Preserve IDs legíveis (`PRD-001`, `CLI-001`, `TM-001`)
- Não acople regra de negócio ao formato do frontend
- Trate leituras e escritas a partir dos serviços, não direto nos controllers
- Ao alterar dados do painel, avalie se precisa publicar evento SSE
- Rotas públicas devem ter rate limit
- Rotas autenticadas devem usar política explícita (`BackofficeRead`, `BackofficeWrite` ou política dedicada, como `PortalRead`)

### Checklist antes de concluir

```powershell
dotnet build .\Myrati.slnx
dotnet test .\Myrati.slnx
```

- [ ] Impacto em políticas de autorização revisado
- [ ] Impacto em SSE revisado (se dashboard/status envolvidos)
- [ ] Seed atualizado (se a feature depende de dados iniciais)

---

## Observações importantes

- O banco é criado com `EnsureCreated` — ainda não há migrations versionadas
- O compose agora gera backups automáticos do PostgreSQL no volume `myrati-postgres-backups`
- O backend expõe trilha técnica em `GET /api/v1/backoffice/audit-logs`
- Swagger ativo apenas em `Development`
- O stream SSE do backoffice aceita token via query string para compatibilidade com `EventSource`
- O hub de tempo real é em memória (adequado para instância única)
- Para múltiplas instâncias, o realtime precisará de um barramento compartilhado
- A ativação pública valida produto, cliente, status e validade, mas ainda não persiste instalação ou fingerprint
- Documentação arquitetural complementar em [ARCHITECTURE.md](./ARCHITECTURE.md)

---

<div align="center">

**Myrati** · Construído com .NET 10

</div>
