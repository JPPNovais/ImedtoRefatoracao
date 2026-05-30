# Wave 6 — Admin global: dashboard completo (KPIs + gráfico + alertas + audit feed)

**ID**: 2026-05-30_006
**Status**: Aprovado por usuário em 2026-05-30
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (4 endpoints + 4 componentes + refator da view, sem migration)
**Áreas regressivas tocadas**: `/admin` (view raiz), nenhuma alteração em código de tenant, nenhuma migration de schema
**Próximo agente**: `imedto-developer`

**Referências**:
- Wave 1 — `planejamentos/2026-05-30_001_admin-global-mvp.md` (criação de `imedto_admins`, `imedto_assinaturas`, `imedto_admin_audit_log`, política `ImedtoAdmin`, CA16 — listas de leitura não geram audit)
- Wave 2 — `planejamentos/2026-05-30_002_admin-global-wave2.md`
- Wave 3 — `planejamentos/2026-05-30_003_admin-global-wave3-redesign.md` (redesign do `AdminLayout`/`AdminDashboard` com `app-page` + `AppPageHeader` + `AppCard`)
- Wave 4 — `planejamentos/2026-05-30_004_admin-global-wave4-catalogos-livelink.md`
- Wave 5 — `planejamentos/2026-05-30_005_admin-global-wave5-builder-visual.md` (commit `21d47a9`)

---

## 1. Contexto e feedback literal do usuário

A view `frontend/src/modules/admin/views/AdminDashboard.vue` é, desde o redesign da Wave 3, um **placeholder declarado como tal no próprio código**:

```vue
<AppCard>
    <p>Estabelecimentos, planos e admins serão listados aqui.</p>
</AppCard>
```

Comentário do dev original: *"Devs paralelos substituirão os placeholders por widgets reais."* Nunca foi feito. O usuário identificou:

> "nesse dashboard fala q vai mostrar as alteracoes, mas nao tem nenhuma informação retornada, o q pretendia colocar nessa parte?"

Após a proposta do orquestrador apresentar três escopos (mínimo, intermediário, completo), o usuário confirmou:

> "pode fazer o completo"

O escopo C transforma a view raiz `/admin` em um **dashboard operacional do super-admin**: panorama do SaaS em um relance, alertas acionáveis e feed do audit log para enxergar o que outros admins fizeram recentemente.

---

## 2. Objetivo

Substituir o placeholder do `AdminDashboard.vue` por uma página com quatro blocos vivos — **KPIs de panorama, gráfico de crescimento mensal, alertas de assinatura, feed de atividade recente do audit log** — todos servidos por endpoints novos sob `/api/admin/dashboard/*`, com filtros no feed, paginação no log, política `ImedtoAdmin`, zero PII de paciente e zero quebra do que já existe.

---

## 3. Escopo

### 3.1 Inclui

**Bloco 1 — KPIs no topo (4 cards via `AppStatCard`)**
- Estabelecimentos: `ativos / inativos` (separados visualmente em 1 card com dois números, ou 2 cards lado a lado — decisão do dev, recomendação: 1 card com `valor` = ativos e `legenda` = `"N inativos"`).
- Admins ativos: contagem de `imedto_admins WHERE ativo = TRUE`.
- Trials em andamento: assinaturas vigentes (`fim_em IS NULL` OU `fim_em > NOW()`) cujo plano é `imedto_planos.gratuito = TRUE`, e quantas delas têm `fim_em <= NOW() + INTERVAL '7 days'` (legenda: `"N expirando em 7 dias"`).
- Assinaturas pagas vs gratuidades vigentes: relação `pagas / gratuitas` (assinatura vigente é a com `fim_em IS NULL`).

**Bloco 2 — Gráfico de crescimento mensal**
- Novos estabelecimentos por mês, **últimos 12 meses** contados a partir do mês corrente (inclusivo).
- Fonte: `estabelecimentos.criado_em` agrupado por `DATE_TRUNC('month', criado_em)`.
- Tooltip com mês (formato "MM/AAAA") e total inteiro.
- Sem lib externa de gráfico — **SVG inline** desenhado em componente próprio (`CrescimentoChart.vue`) usando tokens HSL do design system. Decisão técnica em §6.4.
- Estado vazio: mensagem "Sem novos estabelecimentos no período."

**Bloco 3 — Alertas acionáveis**
- **Trials expirando nos próximos 7 dias** (assinatura com `gratuita = TRUE` e `fim_em BETWEEN NOW() AND NOW() + INTERVAL '7 days'`): lista até 10 itens (nome fantasia, dono, data fim, dias restantes). Cada item linka para `/admin/estabelecimentos/{id}`.
- **Estabelecimentos sem assinatura vigente** (sem registro em `imedto_assinaturas` com `fim_em IS NULL` ou todos encerrados): lista até 10 itens (nome fantasia, dono, criado em).
- Cada lista tem contador total ("N estabelecimentos sem assinatura — mostrando 10").
- Quando listas vazias: `AppEmptyState` com mensagem positiva ("Nenhum trial expirando nos próximos 7 dias.").

**Bloco 4 — Feed de atividade recente (audit log)**
- Tabela com colunas: **Quando** (relativa: "há 5 min" + absoluta no tooltip), **Admin** (nome + email — `imedto_admins`), **Ação** (constante traduzida via mapa PT-BR; ver §6.5), **Recurso** (tipo + id quando aplicável), **Tenant afetado** (nome fantasia do estabelecimento — apenas quando `tenant_afetado_id` não é nulo), **Motivo** (truncado em 100 caracteres, com tooltip completo).
- **Filtros** (em `AppFilterPills` ou `AppSelect` conforme padrão do módulo admin):
  - Ação: dropdown com todas as constantes existentes (`LOGIN_OK`, `CRIAR_ADMIN`, `CONCEDER_GRATUIDADE`, `RESETAR_TENANT`, etc — opção "Todas").
  - Admin: dropdown listando `imedto_admins.ativo = TRUE` (opção "Todos").
  - Período: presets em `AppFilterPills` — **Hoje / 7 dias / 30 dias / 90 dias / Todos** (default: 7 dias).
- **Paginação**: 20 itens por página via `AppPagination` (componente do DS já em uso em outras telas admin).
- Ordenação fixa: `criado_em DESC`.
- Botão "**Atualizar**" manual ao lado do título do bloco (recarrega só este bloco; sem auto-refresh — decisão §6.6).

### 3.2 NÃO inclui

- Export CSV/Excel do audit log (extensão futura — Wave 7 ou 8).
- Dashboards customizáveis pelo admin (escolher widgets, reordenar) — out.
- Permissionamento granular por widget (todos os admins ativos veem tudo) — out.
- Comparativos histórico ("mês anterior +12%") nos KPIs — out.
- Impersonate de tenant ou MFA TOTP — out, briefings separados já mencionados em Wave 1.
- Métricas avançadas (MRR, churn, LTV, conversão trial→pago) — out, demanda separada quando o produto tiver dados suficientes.
- Logs operacionais de aplicação (request/response, erros 500) — out, isso é observabilidade de infra (CloudWatch).
- Audit log de tenant (`prontuario_acessos`, etc) — out, escopo desta tela é só `imedto_admin_audit_log`.
- Alterações no schema (migrations, novos índices) — os índices existentes em `imedto_admin_audit_log` (`(acao, criado_em)`, `(admin_id, criado_em)`, `(criado_em)`, `(tenant_afetado_id, criado_em)`) são suficientes para os filtros previstos. §10 detalha mitigação se isso mudar.

---

## 4. Objetivo de UX

O super-admin abre `/admin` e em 2 segundos vê:
1. Quantos clientes ativos tem o produto.
2. Crescimento dos últimos 12 meses em uma linha visual.
3. O que precisa de atenção (trials acabando, estabelecimentos órfãos).
4. O que outros admins (ou ele mesmo) acabaram de fazer.

Cada bloco carrega independente (loading próprio). Se um endpoint falha, os outros 3 continuam funcionando — robustez parcial em vez de tela em branco.

---

## 5. Decisões cravadas (premissas técnicas do BA)

| # | Decisão | Justificativa |
|---|---------|---------------|
| D1 | Todos os widgets são live-query (sem cache no backend). Cache 60s opcional via store no front. | Volume de dados baixo (super-admin = 1-5 usuários), latência de Dapper sobre RDS aceitável. |
| D2 | 4 endpoints separados: `GET /api/admin/dashboard/kpis`, `GET /api/admin/dashboard/crescimento-mensal`, `GET /api/admin/dashboard/alertas`, `GET /api/admin/dashboard/audit-log`. | Carregamento independente, falha de um não derruba os outros, testabilidade isolada. |
| D3 | 4 query handlers separados, **mesma classe de QueryRepository** Dapper (`DashboardAdminQueryRepository`) com 4 métodos. | Reuso do `connStr` e padrão já consolidado em `AdminEstabelecimentosQueryRepository`. |
| D4 | Política `ImedtoAdmin` em todos os endpoints (igual aos demais controllers admin). | RBAC consistente com Wave 1 CA1. |
| D5 | Acesso ao dashboard (leitura) **NÃO gera linha em `imedto_admin_audit_log`**. | Wave 1 CA16 já estabelece que listas/leituras de tela não auditam — só ações sensíveis (revelar CPF, resetar tenant, conceder gratuidade, etc). Auditar leitura de dashboard geraria ruído inútil. |
| D6 | "Assinatura vigente" = `imedto_assinaturas` com `fim_em IS NULL`. "Trial" = assinatura vigente cujo `imedto_planos.gratuito = TRUE`. | Convenção já usada em `AdminEstabelecimentosQueryRepository.ListarAsync` (linha 79: `ia.fim_em IS NULL`). |
| D7 | "Trial expirando" = `imedto_assinaturas.gratuita = TRUE` AND `fim_em BETWEEN NOW() AND NOW() + INTERVAL '7 days'`. Nota: a flag `gratuita` está em `imedto_assinaturas` (coluna de instância), independente de `imedto_planos.gratuito` (coluna de catálogo). Para filtrar trial, o handler usa `imedto_assinaturas.gratuita` direto. | Wave 1 explicitamente desenhou `imedto_assinaturas.gratuita` como "este registro é gratuidade" para permitir gratuidade pontual sobre plano pago. |
| D8 | Gráfico em SVG inline (sem chart.js, sem ApexCharts). | Premissa do CLAUDE.md: "Minimum code that solves the problem. Nothing speculative." 12 pontos, line/area simples, ~50 linhas de SVG resolvem; lib externa adiciona 100-300kB ao bundle do admin. |
| D9 | Sem auto-refresh — botão "Atualizar" manual no bloco audit log. | Evita polling desnecessário; super-admin não precisa ver atualização em tempo real. Auto-refresh é candidato a backlog se houver demanda. |
| D10 | Audit log mostra mapa PT-BR das constantes (sem expor `LOGIN_OK` cru). Mapa definido no front (componente `AuditLogFeed.vue`) em const tipada. | Constantes são identificadores estáveis no back; tradução é UX no front. Fallback: se ação não está no mapa, mostra a constante crua (preserva forward-compat com ações futuras). |
| D11 | Dropdown de admin no filtro lista **só admins ativos** (`ativo = TRUE`). Admins inativos podem aparecer como autores de ações antigas (filtro mostra "Admin desativado" quando `admin_id` resolve mas o admin está inativo, ou "—" quando `admin_id IS NULL` — Wave 1 já permite `admin_id` nulo no audit). | Lista de filtro fica curta; histórico continua íntegro. |
| D12 | Endpoint `audit-log` aceita query params: `acao?`, `adminId?`, `periodo?` (`hoje`/`7d`/`30d`/`90d`/`todos`), `pagina`, `tamanhoPagina` (default 20, máx 100). | Padrão idêntico ao `ListarEstabelecimentosAdminQueryHandler` (Wave 1). |
| D13 | Endpoint `crescimento-mensal` retorna **sempre 12 pontos** (mês corrente + 11 anteriores), preenchendo com `total = 0` os meses sem novos estabelecimentos. | Frontend não precisa preencher gaps; gráfico fica consistente. |
| D14 | "Estabelecimentos sem assinatura vigente" considera **todos** os estabelecimentos com status `Ativo` (`e.status = 'Ativo'`) que não têm linha em `imedto_assinaturas` com `fim_em IS NULL`. Inativos não contam — não faz sentido alertar sobre tenant que o admin já desativou. | Foco em ação útil. |
| D15 | Todos os 4 endpoints respondem em < 500ms (p95). KPIs e crescimento mensal são contagens leves; alertas tem `LIMIT 10`; audit log paginado a 20 itens com índices existentes cobrindo os filtros. | Performance dia 1, sem necessidade de índices novos. |

---

## 6. Arquitetura proposta

### 6.1 Backend — handlers

Estrutura nova em `backend/src/Services/Imedto.Backend.Application/Admin/Dashboard/`:

```
Admin/Dashboard/
├── ObterKpisDashboardAdminQueryHandler.cs
├── ObterCrescimentoMensalDashboardAdminQueryHandler.cs
├── ObterAlertasDashboardAdminQueryHandler.cs
└── ListarAuditLogDashboardAdminQueryHandler.cs
```

Contracts em `backend/src/Services/Imedto.Backend.Contracts/Admin/Dashboard/`:
- `Queries/`: `ObterKpisDashboardQuery`, `ObterCrescimentoMensalDashboardQuery`, `ObterAlertasDashboardQuery`, `ListarAuditLogDashboardQuery`.
- `Queries/Results/`: `KpisDashboardDto`, `CrescimentoMensalPontoDto`, `AlertasDashboardDto` (`TrialsExpirando`, `SemAssinatura` — listas), `AuditLogItemDto`, `AuditLogPaginadoDto`.

Query repository em `backend/src/Services/Imedto.Backend.Infrastructure/Admin/`:
- `DashboardAdminQueryRepository.cs` implementando `IDashboardAdminQueryRepository` com 4 métodos. Dapper sobre `AppReadConnectionString`. Sem joins em `pacientes`, `prontuarios` — só metadados de tenant/admin.

Controller em `backend/src/Services/Imedto.Backend.API/Controllers/Admin/`:
- `AdminDashboardController.cs` com 4 endpoints:
  - `GET /api/admin/dashboard/kpis` → `KpisDashboardDto`
  - `GET /api/admin/dashboard/crescimento-mensal` → `IReadOnlyList<CrescimentoMensalPontoDto>` (12 itens fixos)
  - `GET /api/admin/dashboard/alertas` → `AlertasDashboardDto`
  - `GET /api/admin/dashboard/audit-log?acao=&adminId=&periodo=&pagina=&tamanhoPagina=` → `AuditLogPaginadoDto`
- Atributo `[Authorize(Policy = "ImedtoAdmin")]` no controller.
- Roteamento via `[ApiController]`, `[Route("api/admin/dashboard")]`.

Registro:
- 4 query handlers como `AddSingleton` em `Container.RegistrarHandlers` (consistente com `ListarAdminsQueryHandlers`).
- `DashboardAdminQueryRepository` como `AddSingleton` (usa `AppReadConnectionString`).
- Bus registra os 4 em `Container.RegistrarBuses`.

### 6.2 Frontend — composição

Refactor `frontend/src/modules/admin/views/AdminDashboard.vue`:
- Mantém `app-page` + `AppPageHeader` (W3-CA7..CA9).
- Compõe 4 componentes internos:

```
modules/admin/components/dashboard/
├── KpisGrid.vue
├── CrescimentoChart.vue
├── AlertasCard.vue
└── AuditLogFeed.vue
```

Cada componente:
- Possui estado próprio de loading/erro/dados.
- Consome `dashboardStore` (Pinia).
- Renderiza `AppCard` como contêiner.
- Estados: skeleton/spinner no loading, `AppEmptyState` quando vazio, mensagem genérica no erro (sem PII).

Service em `modules/admin/services/dashboardService.ts`:
- 4 funções: `obterKpis()`, `obterCrescimentoMensal()`, `obterAlertas()`, `listarAuditLog(filtros)`.
- Usa `adminApi.ts` (já existente).

Store em `modules/admin/stores/dashboardStore.ts`:
- `state`: `kpis`, `crescimento`, `alertas`, `auditLog`, mais flags de loading/erro por bloco.
- `actions`: `carregarKpis()`, `carregarCrescimento()`, `carregarAlertas()`, `carregarAuditLog(filtros)`.
- Cache: 60s por bloco (compara timestamp da última carga). Botão "Atualizar" força refresh ignorando cache.

### 6.3 Filtros do AuditLogFeed

- Acao: `AppSelect` populado de array constante no front (espelho do `ImedtoAdminAuditLogAcoes`). Manter consistência via comentário no front linkando ao arquivo do back; teste unitário no back exporta o array das constantes (já é a fonte de verdade — o front é UI).
- Admin: `AppSelect` populado por chamada a `/api/admin/admins` (já existe — `ListarAdminsQueryHandlers`) filtrando apenas ativos no front.
- Período: `AppFilterPills` com 5 presets. Default `7d`.
- Botão "Atualizar" recarrega `auditLog` mantendo filtros.
- Mudança em qualquer filtro reseta paginação para página 1.

### 6.4 Gráfico — implementação SVG mínima

`CrescimentoChart.vue` recebe `pontos: { mes: string /* "AAAA-MM" */, total: number }[]` (12 itens).
- Calcula `max = Math.max(...totais, 1)`.
- SVG `viewBox="0 0 600 200"`, polyline conectando pontos normalizados (`y = 180 - (total/max) * 160`).
- Eixo X: 12 labels (formato "MM/AA"), eixo Y opcional com 4 marcadores (0, max/3, 2*max/3, max arredondado).
- Cor da linha: `hsl(var(--primary))`. Área sombreada com `hsl(var(--primary) / 0.1)`.
- Hover: ponto destacado, tooltip nativo via `<title>` no `<circle>` ("MM/AAAA: N estabelecimentos").
- Responsivo via `preserveAspectRatio="xMidYMid meet"`.

Sem dependências novas. Se no futuro precisar de chart com mais flexibilidade, vira componente do DS em briefing separado.

### 6.5 Mapa de ações PT-BR (sugestão para o dev)

Em `AuditLogFeed.vue`:
```ts
const ACOES_PT: Record<string, string> = {
  LOGIN_OK: "Login realizado",
  LOGIN_FAIL: "Tentativa de login",
  LOGOUT: "Logout",
  CRIAR_ADMIN: "Admin criado",
  DESATIVAR_ADMIN: "Admin desativado",
  REATIVAR_ADMIN: "Admin reativado",
  RESETAR_SENHA_ADMIN: "Senha de admin resetada",
  ABRIR_DETALHE_TENANT: "Detalhe de tenant aberto",
  REVELAR_CPF_DONO: "CPF do dono revelado",
  RESETAR_TENANT: "Tenant resetado",
  CRIAR_PLANO: "Plano criado",
  ATUALIZAR_PLANO: "Plano atualizado",
  ATIVAR_PLANO: "Plano ativado",
  DESATIVAR_PLANO: "Plano desativado",
  EDITAR_PLANO: "Plano editado",
  TROCAR_PLANO: "Plano alterado em assinatura",
  ALTERAR_ASSINATURA: "Assinatura alterada",
  CONCEDER_GRATUIDADE: "Gratuidade concedida",
  ENCERRAR_ASSINATURA: "Assinatura encerrada",
  RESET_SENHA_PROPRIA: "Senha própria alterada",
  ATUALIZAR_CONFIG: "Configuração global atualizada",
  CRIAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário criado",
  ATUALIZAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário atualizado",
  INATIVAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário inativado",
  REATIVAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário reativado",
  CRIAR_VARIAVEL_PADRAO_SISTEMA: "Variável criada",
  ATUALIZAR_VARIAVEL_PADRAO_SISTEMA: "Variável atualizada",
  INATIVAR_VARIAVEL_PADRAO_SISTEMA: "Variável inativada",
  REATIVAR_VARIAVEL_PADRAO_SISTEMA: "Variável reativada",
  CRIAR_REGIAO_ANATOMICA: "Região anatômica criada",
  ATUALIZAR_REGIAO_ANATOMICA: "Região anatômica atualizada",
  INATIVAR_REGIAO_ANATOMICA: "Região anatômica inativada",
  EXCLUIR_REGIAO_ANATOMICA: "Região anatômica excluída",
};
const traduzir = (acao: string) => ACOES_PT[acao] ?? acao;
```

Dev fica livre para extrair em arquivo separado se preferir.

### 6.6 Decisão sobre auto-refresh

Auto-refresh foi avaliado e **rejeitado para MVP** (D9). Motivo: polling de 60s consome RDS desnecessariamente quando admin está em outra aba, e a Wave 6 ainda não tem dado suficiente para validar se há demanda real por "tempo real". Backlog: avaliar Server-Sent Events se a feature provar valor.

---

## 7. Critérios de aceite

### KPIs

**W6-CA1** (caminho feliz)
**Dado** que existem N estabelecimentos com `status = 'Ativo'`, M com `status = 'Inativo'`, X admins com `ativo = TRUE` e P assinaturas vigentes (fim_em IS NULL),
**Quando** o admin abre `/admin`,
**Então** os 4 cards `AppStatCard` mostram os valores corretos calculados a partir do banco em uma única request `GET /api/admin/dashboard/kpis`.

**W6-CA2** (estado de loading)
**Dado** que o endpoint `kpis` ainda está respondendo,
**Quando** a view carrega,
**Então** cada `AppStatCard` mostra um skeleton/placeholder visual (não travar a página).

**W6-CA3** (estado de erro)
**Dado** que o endpoint `kpis` retornou 500,
**Quando** a view carrega,
**Então** o bloco KPIs mostra mensagem genérica ("Não foi possível carregar os indicadores") e os outros 3 blocos continuam funcionando.

### Crescimento mensal

**W6-CA4** (12 pontos)
**Dado** o mês corrente,
**Quando** o endpoint `crescimento-mensal` é chamado,
**Então** retorna **exatamente 12 itens**, do mês corrente regredindo 11 meses, cada um com `mes` no formato `YYYY-MM` e `total` inteiro ≥ 0. Meses sem novos estabelecimentos vêm com `total = 0`.

**W6-CA5** (gráfico render)
**Dado** os 12 pontos,
**Quando** `CrescimentoChart.vue` renderiza,
**Então** o SVG mostra polyline com 12 vértices, eixo X com 12 labels "MM/AA" e tooltip nativo via `<title>` em cada vértice.

**W6-CA6** (estado vazio)
**Dado** que todos os 12 meses têm `total = 0`,
**Quando** o gráfico renderiza,
**Então** mostra a polyline rente ao eixo X mais mensagem discreta "Sem novos estabelecimentos no período."

### Alertas

**W6-CA7** (trials expirando)
**Dado** que existem K assinaturas com `gratuita = TRUE` e `fim_em BETWEEN NOW() AND NOW() + INTERVAL '7 days'`,
**Quando** o endpoint `alertas` é chamado,
**Então** retorna até 10 itens em `trialsExpirando` com `estabelecimentoId`, `nomeFantasia`, `donoNome`, `fimEm`, `diasRestantes`. Cada item no front linka via `<router-link :to="/admin/estabelecimentos/${id}">`.

**W6-CA8** (sem assinatura)
**Dado** que existem L estabelecimentos com `status = 'Ativo'` sem registro em `imedto_assinaturas` com `fim_em IS NULL`,
**Quando** o endpoint `alertas` é chamado,
**Então** retorna até 10 itens em `semAssinatura` com `estabelecimentoId`, `nomeFantasia`, `donoNome`, `criadoEm`, e o total absoluto em `semAssinaturaTotal` para mostrar "mostrando 10 de L".

**W6-CA9** (estado vazio positivo)
**Dado** que nenhum estabelecimento se enquadra,
**Quando** o bloco renderiza,
**Então** mostra `AppEmptyState` com mensagem positiva ("Nenhum trial expirando nos próximos 7 dias." / "Todos os estabelecimentos ativos têm assinatura vigente.").

### Audit log

**W6-CA10** (paginação 20/pg + ordenação)
**Dado** que existem > 20 linhas em `imedto_admin_audit_log` no período padrão (7d),
**Quando** o endpoint `audit-log` é chamado sem filtros,
**Então** retorna `itens` (≤ 20 ordenados por `criado_em DESC`), `total`, `pagina = 1`, `tamanhoPagina = 20`.

**W6-CA11** (filtro por ação)
**Dado** o usuário escolhe `CONCEDER_GRATUIDADE` no dropdown de ação,
**Quando** o endpoint é chamado com `?acao=CONCEDER_GRATUIDADE`,
**Então** retorna só linhas cujo `acao = 'CONCEDER_GRATUIDADE'`, paginação reseta para página 1, query usa índice `ix_imedto_admin_audit_log_acao_criado`.

**W6-CA12** (filtro por admin)
**Dado** o usuário escolhe um admin no dropdown,
**Quando** o endpoint é chamado com `?adminId={guid}`,
**Então** retorna só linhas cujo `admin_id = @adminId`, query usa índice `ix_imedto_admin_audit_log_admin_criado`.

**W6-CA13** (filtro por período)
**Dado** o usuário troca o pill de período para "30 dias",
**Quando** o endpoint é chamado com `?periodo=30d`,
**Então** retorna só linhas cujo `criado_em >= NOW() - INTERVAL '30 days'`. Presets: `hoje` = `>= DATE_TRUNC('day', NOW())`, `7d` = `NOW() - INTERVAL '7 days'`, `30d` = `NOW() - INTERVAL '30 days'`, `90d` = `NOW() - INTERVAL '90 days'`, `todos` = sem filtro de data.

**W6-CA14** (tradução de ação)
**Dado** uma linha com `acao = 'REVELAR_CPF_DONO'`,
**Quando** renderiza no feed,
**Então** mostra "CPF do dono revelado" (mapa PT-BR). Para ação não mapeada, mostra a constante crua.

**W6-CA15** (truncamento de motivo)
**Dado** uma linha com `motivo` > 100 caracteres,
**Quando** renderiza,
**Então** mostra os primeiros 100 chars + "…" e tooltip nativo (`title=`) com o motivo completo.

**W6-CA16** (tenant afetado)
**Dado** uma linha com `tenant_afetado_id NOT NULL`,
**Quando** renderiza,
**Então** mostra `nome_fantasia` do estabelecimento (via JOIN `LEFT JOIN public.estabelecimentos e ON e.id = al.tenant_afetado_id`). Quando `tenant_afetado_id IS NULL`, célula mostra "—".

**W6-CA17** (admin desativado)
**Dado** uma linha com `admin_id` cujo admin tem `ativo = FALSE`,
**Quando** renderiza,
**Então** mostra "Nome do admin (desativado)" em vez de bloquear a linha. Quando `admin_id IS NULL`, mostra "Sistema" ou "—".

**W6-CA18** (botão Atualizar)
**Dado** que o admin clicou em "Atualizar" no bloco audit log,
**Quando** clica,
**Então** chama o endpoint mantendo todos os filtros atuais, sem alterar paginação.

### RBAC + LGPD + audit do próprio dashboard

**W6-CA19** (RBAC)
**Dado** uma request sem cookie `imedto_admin_session` válido ou sem claim `role = ImedtoAdmin`,
**Quando** chama qualquer endpoint `/api/admin/dashboard/*`,
**Então** API responde 401/403 (consistente com outros controllers admin) e mensagem genérica.

**W6-CA20** (zero PII de paciente)
**Dado** qualquer endpoint do dashboard,
**Quando** o response é inspecionado,
**Então** não há nenhum campo de `pacientes`, `prontuarios`, `agendamentos`, `consultas` ou similares. Apenas metadados de `estabelecimentos`, `imedto_admins`, `imedto_assinaturas`, `imedto_planos`, `imedto_admin_audit_log`. CPF do dono NÃO é exibido em lugar nenhum do dashboard (alertas mostram só `donoNome`).

**W6-CA21** (mensagens de erro genéricas)
**Dado** falha em qualquer endpoint,
**Quando** retorna 4xx/5xx,
**Então** mensagem para o usuário é genérica ("Não foi possível carregar X"). Não vaza tenant_id, admin_id, nome de tabela ou stack trace.

**W6-CA22** (leitura do dashboard não audita)
**Dado** que o admin abre `/admin` ou clica em "Atualizar",
**Quando** os endpoints respondem,
**Então** **nenhuma linha é inserida em `imedto_admin_audit_log`** — consistente com Wave 1 CA16 que isenta leituras de tela do audit (só ações sensíveis com efeito de escrita ou exposição de dado sensível geram audit).

### Performance

**W6-CA23** (p95 < 500ms)
**Dado** o banco em produção com volume real,
**Quando** cada um dos 4 endpoints é chamado em condição normal,
**Então** p95 < 500ms (validar com 50 requisições no QA usando o ambiente local; em prod, validação visual + comportamento ao clicar).

**W6-CA24** (carregamento paralelo no front)
**Dado** que a view `AdminDashboard.vue` monta,
**Quando** o `onMounted` dispara,
**Então** os 4 endpoints são chamados em paralelo (`Promise.allSettled`) — falha de um não bloqueia os outros.

### Documentação viva

**W6-CA25** (Docs/ARQUITETURA.md)
**Dado** a entrega completa,
**Quando** o QA inspeciona,
**Então** `Docs/ARQUITETURA.md` ganha seção "Dashboard Admin" listando os 4 endpoints, o repositório Dapper compartilhado, e a regra de "leitura não audita".

**W6-CA26** (Docs/DESIGN.md — só se aplicável)
**Dado** que `CrescimentoChart.vue` é componente local do módulo admin (não foi promovido a `components/ui/`),
**Quando** o QA inspeciona,
**Então** `Docs/DESIGN.md` **não precisa ser atualizado**. Se o dev decidir promover `CrescimentoChart` ao DS, então deve adicionar entrada (decisão do dev — recomendação BA: manter local até haver segundo caso de uso).

---

## 8. Pontos de extensão futura (backlog implícito, não implementar agora)

- Export CSV do audit log com filtros aplicados (Wave 7).
- Comparativos histórico nos KPIs ("trials +25% vs mês anterior") — exige snapshot diário ou janela móvel.
- Dashboards customizáveis pelo admin (reordenar/ocultar widgets).
- Permissionamento granular por widget (admin "financeiro" vs "operacional").
- Auto-refresh via SSE quando o produto provar demanda.
- Métricas SaaS (MRR, churn, LTV, conversão trial→pago).
- Drill-down: clicar no card de "Trials" abre lista filtrada em `/admin/estabelecimentos?filtro=trial`.
- Gráficos adicionais (assinaturas pagas vs gratuitas ao longo do tempo, ações por tipo no audit).
- Promover `CrescimentoChart` a componente DS (`AppLineChart`) quando aparecer segundo caso de uso.

---

## 9. Riscos e mitigações

| Risco | Probabilidade | Mitigação |
|-------|---------------|-----------|
| Volume do `imedto_admin_audit_log` crescer rápido e tornar paginação lenta. | Média (em 6-12 meses pode atingir 100k+ linhas). | Índices existentes (`(acao, criado_em)`, `(admin_id, criado_em)`, `(criado_em)`) cobrem todos os filtros. `LIMIT 20 OFFSET N` em índice composto é O(log n + 20). Quando passar de 1M linhas, considerar keyset pagination (backlog). |
| SVG inline ficar feio comparado a chart.js. | Baixa (line chart simples renderiza bem em SVG). | Se o usuário reclamar visualmente, vira briefing separado para escolher lib leve (recomendação: `chart.js` via wrapper local OU `lightweight-charts`). |
| Endpoints de KPIs/alertas serem chamados em loop por bug de Vue. | Baixa (cache 60s no store mitiga). | Cache 60s no store + botão "Atualizar" explícito. |
| Dropdown de "Ação" no filtro desincronizar com constantes do back. | Média (constantes crescem com cada Wave). | Mapa PT-BR no front fica documentado em §6.5; quando back adicionar ação nova, dev atualiza front no mesmo PR. Fallback: ação não mapeada cai no `??` (mostra a constante crua, não quebra). |
| Endpoint `alertas` pode ficar pesado se `LIMIT 10` desaparecer numa refator. | Baixa. | CA8 explicitamente exige `LIMIT 10`. |
| `tenant_afetado_id` no audit log pode apontar para estabelecimento já deletado (hard delete) — JOIN retornaria NULL. | Baixíssima (não há hard delete de estabelecimento em produção). | `LEFT JOIN` no SQL + tratamento "Estabelecimento removido" no front quando JOIN retornar NULL apesar do id existir. |

---

## 10. Atualizações em `Docs/`

### `Docs/ARQUITETURA.md` — **OBRIGATÓRIO**

Adicionar subseção em "Backend — .NET 10 + CQRS + DDD" → "Bounded contexts ativos" (ou seção equivalente):

```md
### Dashboard Admin

`Application/Admin/Dashboard/` — 4 query handlers (singletons) servindo `/api/admin/dashboard/*`:
- `ObterKpisDashboardAdminQueryHandler` → KPIs (estabelecimentos, admins, trials, assinaturas).
- `ObterCrescimentoMensalDashboardAdminQueryHandler` → 12 pontos de novos estabelecimentos por mês.
- `ObterAlertasDashboardAdminQueryHandler` → trials expirando em 7d + estabelecimentos sem assinatura.
- `ListarAuditLogDashboardAdminQueryHandler` → feed paginado de `imedto_admin_audit_log` com filtros (ação, admin, período).

Read repository: `Infrastructure/Admin/DashboardAdminQueryRepository.cs` (Dapper sobre `AppReadConnectionString`). Sem joins em `pacientes`/`prontuarios` — só metadados de tenant/admin (CA20).

Política: `ImedtoAdmin` no controller. **Leitura do dashboard não gera linha em `imedto_admin_audit_log`** (Wave 1 CA16 — só ações sensíveis auditam).
```

### `Docs/DESIGN.md` — não atualizar

`CrescimentoChart` permanece componente local em `modules/admin/components/dashboard/`. Não promover ao DS até segundo caso de uso aparecer (§8). Se o dev decidir promover na implementação, atualiza junto.

### `Docs/LGPD.md` — não atualizar

Premissas já cobertas em Wave 1 (`imedto_admins`, `imedto_admin_audit_log`, política, mensagens genéricas). Nenhum padrão LGPD novo introduzido.

### `Docs/INFRA.md` e `Docs/COMANDOS.md` — não atualizar

Nenhum recurso AWS novo, nenhum comando novo, nenhuma migration.

---

## 11. Hand-off

**Próximo agente**: `imedto-developer`.

**Dependências**:
- Sem migration (`imedto-database` não precisa ser acionado).
- Schemas e índices já existem.
- Componentes DS reusados: `AppStatCard`, `AppCard`, `AppPageHeader`, `AppEmptyState`, `AppPagination`, `AppSelect`, `AppFilterPills`, `AppButton`, `AppBadge` (para "trial expirando em X dias"), `AppTabs` (opcional, se dev quiser tab "Trials" / "Sem assinatura" dentro de `AlertasCard`).
- Constantes de ação já existem em `ImedtoAdminAuditLogAcoes` (back) — front consome mapa estático espelho (§6.5).

**Sequência sugerida ao dev**:
1. Backend: contracts → query repository → 4 handlers → controller → registrar no `Container` → testes unitários dos handlers (mockando o repository).
2. Frontend: types em `dashboardService.ts` → service → store → 4 componentes → refactor da view raiz.
3. Atualizar `Docs/ARQUITETURA.md` com a seção "Dashboard Admin".
4. Smoke test local: cada bloco carrega independente, filtros funcionam, paginação funciona, alertas linkam.
5. Anunciar pronto ao QA.

**Após dev → `imedto-qa`** para validação de cada CA via chrome-devtools MCP + suíte automatizada. QA também valida que `Docs/ARQUITETURA.md` foi atualizado (W6-CA25).

**Próximos briefings sugeridos** (não para esta Wave):
- Wave 7: Export CSV do audit log + comparativos histórico de KPIs.
- MFA TOTP para admins (briefing separado já mapeado em Wave 1).
- Impersonate de tenant (briefing separado).
- Métricas SaaS avançadas (quando produto tiver dado suficiente).
