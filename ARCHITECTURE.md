# Architecture

## Diretriz

O backend foi iniciado como um monólito modular. Isso segue a mesma linha do projeto de referência, mas sem antecipar complexidade operacional que o produto ainda não exige.

## Módulos

- `Identity`
  - autenticação, equipe administrativa, perfil, sessões e atividades.
- `Catalog`
  - produtos, planos e licenças.
- `CRM`
  - clientes e usuários conectados.
- `Operations`
  - dashboard, snapshots e feed de atividade.
- `Platform`
  - configurações da empresa e chaves de API.
- `Public`
  - contato e status page.
- `Realtime`
  - hub de eventos em memória e streams SSE.

## Camadas

- `Myrati.Domain`
  - entidades e regras estruturais simples.
- `Myrati.Application`
  - contratos, validações, serviços e regras de negócio.
- `Myrati.Infrastructure`
  - EF Core, JWT, BCrypt e seed.
- `Myrati.API`
  - HTTP, autenticação, autorização, rate limiting e middleware.

## Decisões principais

- IDs stringados como `PRD-001`, `CLI-001`, `TM-001` e chaves de licença no formato do frontend.
- leitura e escrita centralizadas em serviços por módulo.
- `DbContext` único no primeiro estágio para manter baixo custo de manutenção.
- autorização separada em políticas de leitura e escrita do backoffice.
- seed inicial alinhado às telas já implementadas no frontend.
- eventos de domínio publicados em mutações relevantes e expostos por SSE.

## Qualidade e desempenho

- consultas de listagem usam agregação derivada em memória a partir de dados normalizados;
- testes rodam com `SQLite`, sem dependência de `PostgreSQL` local;
- a infraestrutura aceita `SQLite` e `PostgreSQL`, o que reduz atrito em CI e testes;
- endpoints públicos possuem rate limiting;
- o backoffice e a status page podem operar em near real-time com SSE;
- validações ficam fora dos controllers.

## Evolução sugerida

Quando o produto justificar maior escala ou times independentes, a evolução natural é:

1. extrair migrations versionadas e observabilidade centralizada;
2. separar leitura analítica do dashboard de operações transacionais;
3. introduzir eventos de domínio para licenças, clientes e auditoria;
4. avaliar extração do módulo público e do módulo de autenticação.

Até lá, a forma atual é mais barata, mais simples de operar e suficiente para o escopo real do sistema.
