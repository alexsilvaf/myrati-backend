<div align="center">

# Myrati Backend

**Monólito modular em .NET 10 para o painel administrativo, área pública e fluxos em tempo real da Myrati.**

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
- [Autenticação e autorização](#autenticação-e-autorização)
- [SSE e tempo real](#sse-e-tempo-real)
- [Rotas da API](#rotas-da-api)
  - [Auth](#auth)
  - [Dashboard](#dashboard)
  - [Products](#products)
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
| **Catálogo** | Produtos, planos e gestão de licenças |
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

O projeto usa um **monólito modular com separação por camadas** — baixo custo operacional sem sacrificar organização para evolução futura.

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
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Realtime/
│   │   └── Program.cs
│   ├── Myrati.Application/
│   │   ├── Abstractions/
│   │   ├── Common/
│   │   ├── Contracts/
│   │   ├── DependencyInjection/
│   │   ├── Realtime/
│   │   ├── Services/
│   │   └── Validation/
│   ├── Myrati.Domain/
│   │   ├── Clients/
│   │   ├── Common/
│   │   ├── Dashboard/
│   │   ├── Identity/
│   │   ├── Products/
│   │   ├── Public/
│   │   └── Settings/
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
```

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

### 2. Rodar a API

```powershell
cd c:\Projects\myrati\myrati-backend
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
| PostgreSQL | `localhost:5432` |
| API | `http://localhost:8080` |

---

## Autenticação e autorização

### Papéis

| Papel | BackofficeRead | BackofficeWrite |
|-------|:-:|:-:|
| **Super Admin** | ✓ | ✓ |
| **Admin** | ✓ | ✓ |
| **Viewer** | ✓ | — |

### Fluxo

```
Cliente  ──POST /api/v1/auth/login──>  API  ──>  JWT + dados do usuário
Cliente  ──Authorization: Bearer {token}──>  Rotas autenticadas
Cliente  ──?access_token={jwt}──>  Stream SSE autenticado
```

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

#### `GET /api/v1/backoffice/products`

Lista todos os produtos com seus planos e métricas.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`

**Response `200 OK`:**

```json
[
  {
    "id": "PRD-001",
    "name": "Myrati ERP",
    "description": "Sistema completo de gestão empresarial",
    "category": "ERP",
    "status": "Ativo",
    "totalLicenses": 85,
    "activeLicenses": 72,
    "monthlyRevenue": 28500.00,
    "createdDate": "2025-01-15",
    "version": "2.4.1",
    "plans": [
      {
        "id": "PLN-001",
        "name": "Starter",
        "maxUsers": 5,
        "monthlyPrice": 199.90
      },
      {
        "id": "PLN-002",
        "name": "Professional",
        "maxUsers": 25,
        "monthlyPrice": 499.90
      }
    ]
  }
]
```

---

#### `GET /api/v1/backoffice/products/{productId}`

Retorna o detalhe de um produto, incluindo planos e licenças vinculadas.

- **Acesso:** `BackofficeRead`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `productId` (string) — ex: `PRD-001`

**Response `200 OK`:**

```json
{
  "id": "PRD-001",
  "name": "Myrati ERP",
  "description": "Sistema completo de gestão empresarial",
  "category": "ERP",
  "status": "Ativo",
  "totalLicenses": 85,
  "activeLicenses": 72,
  "monthlyRevenue": 28500.00,
  "createdDate": "2025-01-15",
  "version": "2.4.1",
  "plans": [
    {
      "id": "PLN-001",
      "name": "Starter",
      "maxUsers": 5,
      "monthlyPrice": 199.90
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

#### `POST /api/v1/backoffice/products`

Cria um novo produto com seus planos.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`

**Request body:**

```json
{
  "name": "Myrati Analytics",
  "description": "Plataforma de análise de dados e business intelligence",
  "category": "Analytics",
  "status": "Em desenvolvimento",
  "version": "1.0.0",
  "plans": [
    {
      "name": "Basic",
      "maxUsers": 3,
      "monthlyPrice": 149.90
    },
    {
      "name": "Enterprise",
      "maxUsers": 50,
      "monthlyPrice": 899.90
    }
  ]
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `name` | string | Sim | Máx. 120 caracteres |
| `description` | string | Sim | Máx. 500 caracteres |
| `category` | string | Sim | Máx. 120 caracteres |
| `status` | string | Sim | `Ativo`, `Inativo` ou `Em desenvolvimento` |
| `version` | string | Sim | Máx. 30 caracteres |
| `plans` | array | Não | Lista de planos (pode ser vazia) |
| `plans[].name` | string | Sim | Máx. 60 caracteres |
| `plans[].maxUsers` | int | Sim | Maior que 0 |
| `plans[].monthlyPrice` | decimal | Sim | Maior ou igual a 0 |

**Response `201 Created`:** retorna o `ProductDetailDto` completo (mesmo formato do GET por ID).

---

#### `PUT /api/v1/backoffice/products/{productId}`

Atualiza um produto existente.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `productId` (string) — ex: `PRD-001`

**Request body:** mesmo formato do POST de criação.

**Response `200 OK`:** retorna o `ProductDetailDto` atualizado.

---

#### `DELETE /api/v1/backoffice/products/{productId}`

Remove um produto que não possua licenças vinculadas.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `productId` (string) — ex: `PRD-001`

**Response `204 No Content`**

> Retorna `409 Conflict` se o produto possuir licenças vinculadas.

---

### Licenses

#### `POST /api/v1/backoffice/products/{productId}/licenses`

Cria uma nova licença vinculada a um produto.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `productId` (string) — ex: `PRD-001`

**Request body:**

```json
{
  "clientId": "CLI-003",
  "plan": "Professional",
  "monthlyValue": 499.90,
  "startDate": "2026-04-01",
  "expiryDate": "2027-04-01"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|:-----------:|-----------|
| `clientId` | string | Sim | ID válido de cliente existente |
| `plan` | string | Sim | Máx. 60 caracteres |
| `monthlyValue` | decimal | Sim | Maior que 0 |
| `startDate` | string | Sim | Formato de data (ex: `2026-04-01`) |
| `expiryDate` | string | Sim | Formato de data, deve ser posterior a `startDate` |

**Response `201 Created`:**

```json
{
  "id": "LIC-042",
  "clientId": "CLI-003",
  "clientName": "Tech Solutions SA",
  "productId": "PRD-001",
  "productName": "Myrati ERP",
  "plan": "Professional",
  "maxUsers": 25,
  "activeUsers": 0,
  "status": "Ativa",
  "startDate": "2026-04-01",
  "expiryDate": "2027-04-01",
  "monthlyValue": 499.90
}
```

---

#### `PUT /api/v1/backoffice/licenses/{licenseId}`

Atualiza uma licença existente.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `licenseId` (string) — ex: `LIC-042`

**Request body:** mesmo formato do POST de criação de licença.

**Response `200 OK`:** retorna o `LicenseDto` atualizado.

---

#### `POST /api/v1/backoffice/licenses/{licenseId}/suspend`

Suspende uma licença ativa.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `licenseId` (string) — ex: `LIC-042`
- **Request body:** nenhum

**Response `200 OK`:** retorna o `LicenseDto` com `status: "Suspensa"`.

---

#### `POST /api/v1/backoffice/licenses/{licenseId}/reactivate`

Reativa uma licença suspensa.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `licenseId` (string) — ex: `LIC-042`
- **Request body:** nenhum

**Response `200 OK`:** retorna o `LicenseDto` com `status: "Ativa"`.

---

#### `DELETE /api/v1/backoffice/licenses/{licenseId}`

Exclui uma licença.

- **Acesso:** `BackofficeWrite`
- **Header:** `Authorization: Bearer {token}`
- **Path param:** `licenseId` (string) — ex: `LIC-042`

**Response `204 No Content`**

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
| `role` | string | Sim | `Super Admin`, `Admin` ou `Viewer` |

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
| `role` | string | Sim | `Super Admin`, `Admin` ou `Viewer` |
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

Registra um lead de contato a partir do formulário público.

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
  "message": "Mensagem recebida com sucesso"
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
| Licença | `Ativa`, `Suspensa` |
| Membro da equipe (role) | `Super Admin`, `Admin`, `Viewer` |
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

### Restrições numéricas

| Campo | Regra |
|-------|-------|
| `maxUsers` (plano) | Maior que 0 |
| `monthlyPrice` (plano) | Maior ou igual a 0 |
| `monthlyValue` (licença) | Maior que 0 |

### Outras regras

- Campos de e-mail devem ser um endereço válido
- `password` no login: mínimo 6 caracteres
- `newPassword` na troca de senha: mínimo 8 caracteres
- `confirmPassword` deve ser igual a `newPassword`
- `expiryDate` da licença deve ser posterior a `startDate`

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
| `409 Conflict` | Conflito de estado | Deletar produto com licenças ou cliente com licenças ativas |
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
- Rotas autenticadas devem usar `BackofficeRead` ou `BackofficeWrite` explicitamente

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
