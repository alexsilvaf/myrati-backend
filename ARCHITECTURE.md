# Architecture

## Diretriz

O backend nasceu como um monólito modular e agora passou a expor uma topologia de microsserviços por contexto. A separação atual privilegia isolamento de deploy e fronteiras HTTP claras, sem reescrever todo o domínio de uma vez.

## Topologia atual

- `Myrati.Gateway.API`
  - ponto único de entrada para o frontend;
  - roteia requests para os serviços internos;
  - preserva a URL pública única da plataforma.
- `Myrati.IdentityService.API`
  - autenticação;
  - perfil;
  - configurações administrativas e equipe.
- `Myrati.BackofficeService.API`
  - dashboard;
  - catálogo, licenças, clientes e usuários;
  - stream SSE do backoffice e stream público de status.
- `Myrati.PublicService.API`
  - formulário público;
  - ativação pública de licenças.

> O projeto `Myrati.API` foi mantido como host legado do monólito para compatibilidade e testes já existentes, mas a topologia operacional recomendada agora é gateway + serviços.

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
  - controllers e infraestrutura HTTP compartilhados pelo host legado e pelos novos serviços.
- `Myrati.ServiceDefaults`
  - bootstrap comum de autenticação, autorização, rate limiting, swagger, CORS e filtro de controllers por serviço.

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

## Limites e próximo passo

A separação atual ainda compartilha a camada de aplicação e o mesmo banco de dados. Isso reduz risco de regressão nesta etapa, mas não é o ponto final de uma arquitetura totalmente distribuída.

Evolução natural a partir daqui:

1. extrair bancos por serviço;
2. substituir o hub de eventos em memória por barramento compartilhado;
3. mover observabilidade e tracing para o gateway e para cada serviço;
4. remover o host legado `Myrati.API` quando a cobertura de testes dos novos hosts estiver equivalente.
