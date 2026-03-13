# Vercel + Lightsail

Este guia documenta o cenário recomendado para produção da Myrati quando:

- o frontend fica na Vercel;
- o backend e o PostgreSQL ficam em uma instância da Lightsail;
- o DNS do domínio também fica gerenciado na Vercel.

## Arquitetura final

- `https://myrati.com.br` -> frontend publicado na Vercel
- `https://www.myrati.com.br` -> opcionalmente redirecionado para o domínio principal
- `https://api.myrati.com.br` -> Caddy na Lightsail
- `gateway`, microserviços internos e `postgres` -> containers privados na instância

## Regra importante

Não configure o frontend de produção para consumir o backend por IP cru, por exemplo:

```env
VITE_API_URL=http://13.218.108.99:5118
```

Isso é inadequado em produção porque:

- o frontend da Vercel roda em HTTPS;
- chamadas do navegador para HTTP podem ser bloqueadas por mixed content;
- certificados TLS públicos automáticos dependem de domínio;
- o IP público padrão da Lightsail pode mudar se a instância for parada e iniciada novamente.

O caminho correto é usar um subdomínio dedicado da API.

## 1. Criar e anexar uma Static IP na Lightsail

Na Lightsail:

1. Abra a instância.
2. Vá em `Networking`.
3. Escolha `Create static IP` ou `Attach static IP`.
4. Anexe a Static IP à instância do backend.

Use a Static IP como alvo do DNS. Não use o IPv4 dinâmico padrão da instância.

> Na Lightsail, a Static IP não gera custo adicional enquanto estiver anexada a uma instância em execução.

## 2. Criar o DNS da API na Vercel

Se o domínio estiver usando nameservers da Vercel, crie um registro DNS:

- `Type`: `A`
- `Name`: `api`
- `Value`: `<STATIC_IP_DA_LIGHTSAIL>`
- `TTL`: padrão da Vercel já serve

> Não adicione `api.myrati.com.br` como domínio do projeto do frontend na Vercel. Esse subdomínio não será servido pela Vercel; ele só precisa existir como registro DNS apontando para a Lightsail.

Exemplo:

- `api.myrati.com.br` -> `13.218.108.99`

Se preferir CLI da Vercel:

```bash
vercel dns add myrati.com.br api A 13.218.108.99
```

## 3. Abrir apenas as portas necessárias na Lightsail

No firewall da instância:

- liberar `80/tcp`
- liberar `443/tcp`
- restringir `22/tcp` ao seu IP administrativo, se possível

Não exponha publicamente:

- `5118`
- `5119`
- `5120`
- `5121`
- `5122`
- `5432`

## 4. Configurar o backend

No `.env` da instância:

```env
MYRATI_SITE_HOST=api.myrati.com.br
MYRATI_EMAIL_FRONTEND_URL=https://myrati.com.br
MYRATI_CORS_ALLOWED_ORIGIN_0=https://myrati.com.br
MYRATI_CORS_ALLOWED_ORIGIN_1=https://www.myrati.com.br
```

Depois suba a stack:

```bash
docker compose -f docker-compose.lightsail.yml up -d --build
```

## 5. Ativar o Caddy

O Caddy usa `deploy/Caddyfile.api` e publica HTTPS para o host definido em `MYRATI_SITE_HOST`.

Quando o DNS já estiver propagado, suba ou recrie o container:

```bash
docker compose -f docker-compose.lightsail.yml up -d myrati-caddy
```

## 6. Configurar o frontend na Vercel

No projeto da Vercel:

```env
VITE_API_URL=https://api.myrati.com.br
```

Depois gere um novo deploy do frontend.

## 7. Validar

Valide DNS:

```bash
dig A api.myrati.com.br +short
```

Valide saúde da API:

```bash
curl -I https://api.myrati.com.br/health
```

Valide status público:

```bash
curl https://api.myrati.com.br/api/v1/public/status
```

## 8. Checklist de corte

- Static IP anexada à instância
- `api.myrati.com.br` resolvendo para a Static IP
- portas `80` e `443` abertas
- `myrati-caddy` em execução
- `VITE_API_URL` configurado na Vercel
- `MYRATI_EMAIL_FRONTEND_URL=https://myrati.com.br`
- CORS liberando `https://myrati.com.br` e `https://www.myrati.com.br`

## Operação futura

Sempre que a infraestrutura mudar, valide estes pontos antes de mexer em DNS:

1. confirme se a instância continua com Static IP anexada;
2. confirme se `MYRATI_SITE_HOST` ainda bate com o subdomínio publicado;
3. confirme se o frontend da Vercel continua usando a URL da API por domínio, nunca por IP;
4. confirme se o certificado HTTPS do Caddy renovou normalmente.
