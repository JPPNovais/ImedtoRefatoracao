# Admin Global — Wave 2: Destravar Mutações, Configs, Catálogos Globais e Redesign

**ID**: 2026-05-30_002
**Data**: 2026-05-30
**Autor**: imedto-business-analyst
**Status**: Aprovado para implementação
**Estimativa de esforço**: G (5-7 dias úteis)
**Áreas regressivas tocadas**: módulo admin global (todas as views da Wave 1), trial de assinatura, modelo de prontuário (escopo tenant — nova aba "Templates do sistema"), variável pool (escopo tenant — nova aba), regiões anatômicas (catálogo seed promovido a entidade editável)

**Refere-se a**: [`planejamentos/2026-05-30_001_admin-global-mvp.md`](./2026-05-30_001_admin-global-mvp.md) (Wave 1, entregue em produção no commit `3d602e6`)

**Agentes downstream esperados (em ordem)**:
1. `imedto-database` — 3 tabelas globais novas + seeds de `imedto_config` + índices
2. `imedto-developer` — implementação **sequencial por frente** (1 → 2 → 3 → 4) por dependência entre elas
3. `imedto-qa` — quality gate único, fecha pipeline

---

## 1. Contexto e motivação

A Wave 1 ([`2026-05-30_001_admin-global-mvp.md`](./2026-05-30_001_admin-global-mvp.md)) entregou o esqueleto do módulo admin global: layout isolado, autenticação dedicada, listagem de estabelecimentos, planos, administradores e a tabela `imedto_config` vazia.

Feedback literal do usuário após testar Wave 1 em produção (commit `3d602e6`):

> "eu nao estou conseguindo fazer nada a nao ser visualizar os estabelecimentos, planos e administradores, eu preciso poder alterar os planos dos estabelecimentos (principalmente nessa fase de testes). tenho q poder mudar algumas configurações como quantos dias o estabelecimento pode ficar no plano gratuito de teste, devo poder controlar também a parte de modelos padroes de prontuario do sistema, pull de variaveis padroes também, etc, assim como no que ta do legado. outro detalhe é que quero que o design fique semelhante o que é da ferramenta"

Discovery do BA identificou **4 frentes**:

1. **Frente 1 — Bug de integração herdado (Tipo A)**: os componentes `AssinaturaCard.vue`, `TrocarPlanoModal.vue`, `ConcederGratuidadeModal.vue`, `EncerrarAssinaturaModal.vue` foram entregues em `frontend/src/modules/admin/components/assinaturas/` na Wave 1, mas nenhuma view os importa. A view `EstabelecimentoDetalheView.vue` não montou o card. Resultado: feature de mutação de assinatura existe no código mas não é alcançável pela UI.

2. **Frente 2 — Configurações globais inexistentes**: tabela `imedto_config` foi criada na Wave 1 sem CRUD UI nem leitor. Crítico: `IniciarTrialAoCriarEstabelecimentoHandler.cs:21` tem `TimeSpan.FromDays(14)` hardcoded — mudar configuração via UI sem refatorar este handler resultaria em zero efeito real, o que invalida a feature.

3. **Frente 3 — Catálogos globais (modelos prontuário, variáveis pool, regiões anatômicas)**: hoje todos esses recursos existem **apenas no escopo de cada estabelecimento**. O admin global precisa manter catálogos do sistema que os tenants possam **importar** (modelo de cópia, não live-link), preservando autonomia de quem já personalizou. Regiões anatômicas têm seed em produção (commit `cabdf31`) mas não são entidade editável — promover.

4. **Frente 4 — Redesign visual**: `AdminLayout.vue` hoje usa hex próprios e não reusa o design system. O usuário pediu paridade visual com o app principal, mantendo um sinal discreto de que se trata da área admin.

A Wave 2 fecha a UX da Wave 1 e habilita o admin global a operar de fato durante a fase de testes (mudança de plano em estabelecimento, controle de trial, gestão de catálogos do sistema).

---

## 2. Persona-alvo

- **Admin global do Imedto** (única persona da pipeline admin). Frequência: diária durante fase de testes; semanal em regime normal.
- Cenários de uso típicos:
  - Trocar plano de um estabelecimento que pediu upgrade/downgrade.
  - Conceder gratuidade a um estabelecimento parceiro (com motivo registrado).
  - Estender trial de 14 para 30 dias para uma campanha.
  - Criar/editar template de prontuário "Anamnese Clínica Geral" no nível do sistema, que todos os tenants podem importar.
  - Promover uma variável pool comum (ex: "Sintomas frequentes") para que clínicas novas tenham material inicial.

**Personas indiretas (afetadas pela Wave 2)**:
- Donos e profissionais de clínicas — verão uma nova aba "Templates do sistema" em telas de modelo de prontuário e variável pool. Podem importar ou ignorar.

---

## 3. Escopo

### Inclui (Frente 1 — Integração de assinatura)
- Importar e montar `AssinaturaCard` em `EstabelecimentoDetalheView`.
- Wire-up dos modais `TrocarPlanoModal`, `ConcederGratuidadeModal`, `EncerrarAssinaturaModal`.
- Refresh da view após cada mutação.
- Timeline de histórico de assinaturas dentro do card.

### Inclui (Frente 2 — Configurações globais)
- CRUD UI de `imedto_config` agrupado por seção.
- Seeds default das 8 chaves listadas em §6.
- Helper `IConfigGlobalReader` injetável no backend (com cache em memória curto).
- Refator de `IniciarTrialAoCriarEstabelecimentoHandler` para ler `trial.dias_padrao` via reader (fallback 14 se ausente/inválido).
- Refator do tempo de sessão admin (hoje hardcoded 15 min) para ler `seguranca.tempo_sessao_admin_min`.
- Tipagem de valor por chave (numérico, texto, email, toggle).

### Inclui (Frente 3 — Catálogos globais)
- 3 entidades novas SEM `estabelecimento_id`:
  - `imedto_modelo_prontuario_global`
  - `imedto_variavel_pool_global`
  - `imedto_regiao_anatomica_global` (migra o seed existente para tabela editável)
- CRUD admin completo das 3 entidades (criar, listar, editar, desativar soft).
- Endpoint `POST /api/.../importar-do-global/{id}` no escopo TENANT que copia a entidade global para a tabela do tenant (cópia independente, editável, sem live-link).
- Nova aba "Templates do sistema" nas telas de modelo de prontuário e variável pool dos tenants, com botão "Importar para minha clínica".

### Inclui (Frente 4 — Redesign)
- Refazer `AdminLayout.vue` para reusar `AppSidebar` + `AppTopBar` do design system.
- Badge "Admin" no topbar (próximo ao logo).
- Faixa de 2px na borda superior do topbar com `hsl(var(--warning))` (ou token equivalente de acento).
- Todas as views admin (login, dashboard, lista admins, lista estabelecimentos, detalhe estabelecimento, lista planos, form plano + novas telas Wave 2) usando `AppCard`, `AppPageHeader`, `AppButton`, etc.
- Zero hex code próprio em CSS scoped do módulo admin.
- Cross-cutting: motivo ≥10 chars em toda mutação; audit em `imedto_admin_audit_log`; multi-tenant em mensagens genéricas.

### Não inclui (Wave 3+)
- Impersonate (admin "logar como" tenant).
- MFA TOTP no login admin.
- RBAC granular (papéis admin diferenciados — todos são "ImedtoAdmin").
- IP allow-list.
- Billing real (cobrança/integração com gateway).
- Logs centralizados (CloudWatch/Datadog/etc).
- Dashboard admin com métricas globais.
- Fila de envio Resend / fila SMTP real.
- Edição/sincronização live entre catálogo global e cópia do tenant.

---

## 4. Decisões de produto (rodada de perguntas)

| # | Pergunta | Resposta do usuário |
|---|---|---|
| 1 | Wave 2 monolítica ou fatiada? | **B — Tudo numa Wave 2 só (5-7 dias)** |
| 2 | Configs: conjunto enxuto, SaaS típico ou comunicação? | **B + C — Conjunto enxuto + SaaS típico unificado (8 chaves)** |
| 3 | Catálogos: quais entidades + modelo de cópia? | **B — Modelos prontuário + Variáveis pool + Regiões anatômicas, modelo cópia (tenant importa)** |
| 4 | Redesign: nível de paridade? | **C — Idêntico ao app principal + badge "Admin" + faixa 2px de acento** |

---

## 5. Regras de negócio

### Mutação de assinatura (Frente 1)
- **R1**: Trocar plano só vale para estabelecimento com assinatura ativa. Mora em: `Domain.Estabelecimento.Assinatura` + handler. Validada em back+front.
- **R2**: Conceder gratuidade exige motivo ≥10 chars. 422 se ausente. Mora em: handler + audit. Validada em back+front (front desabilita botão).
- **R3**: Encerrar assinatura é soft (gera linha de histórico marcando data de fim). Mora em: handler. Validada em back.

### Configurações globais (Frente 2)
- **R4**: Toda config tem `chave` (texto único, formato `secao.nome`), `valor` (texto), `tipo` (enum: `numerico`, `texto`, `email`, `toggle`), `secao`, `descricao`, `atualizado_em`, `atualizado_por_admin_id`. Mora em: `imedto_config` (DB) + `Domain.Admin.ConfigGlobal`.
- **R5**: Mudar config dispara audit obrigatório (motivo ≥10 chars). Mora em: handler. Validada em back+front.
- **R6**: Config nova **não afeta entidades já criadas** — só afeta entidades criadas dali em diante. Aplica-se em particular a `trial.dias_padrao` (mudou de 14 para 30 → trials novos têm 30 dias; trials existentes mantêm 14). Mora em: handlers que consomem config (lêem no momento da criação, não recalculam retroativo).
- **R7**: Valor inválido por tipo (ex: `trial.dias_padrao = -5`, `sistema.email_suporte = "abc"`) → 422 com mensagem clara. Mora em: validator de comando. Validada em back+front.
- **R8**: Helper `IConfigGlobalReader` (back) faz cache em memória com TTL curto (60s) para evitar hit DB em cada criação de estabelecimento. Mora em: infra/Admin. Invalidação manual após update.

### Catálogos globais (Frente 3)
- **R9**: Modelo prontuário global, variável pool global e região anatômica global NÃO têm `estabelecimento_id`. Inserir registro nessas tabelas com esse campo é erro de schema (constraint não existe). Mora em: schema + Domain (sem `EstabelecimentoId` nas entidades globais).
- **R10**: Soft-delete via flag `ativo` (não DELETE físico). Listagem default oculta inativos; flag `incluir_inativos=true` mostra. Mora em: Query + repo. Espelhada no front.
- **R11**: "Importar para minha clínica" cria **cópia independente** na tabela do tenant (com `estabelecimento_id` preenchido). Editar o global depois NÃO altera as cópias. Mora em: handler `ImportarModeloGlobalCommandHandler` (e equivalentes). Front exibe aviso "Esta é uma cópia editável independente" no botão.
- **R12**: Tenant pode importar o mesmo global N vezes (cada importação é cópia nova com timestamps próprios). Mora em: handler. Sem deduplicação por ID global.
- **R13**: Editar catálogo global exige motivo ≥10 chars (audit). Mora em: handler. Validada em back+front.

### Redesign (Frente 4)
- **R14**: Módulo admin continua fisicamente isolado em `frontend/src/modules/admin/`. Redesign **não move arquivos do módulo** — só troca `AdminLayout` e estilos das views para reusar componentes do design system. Mora em: convenção arquitetural.
- **R15**: Zero hex code em CSS scoped do módulo admin pós-redesign. Usa apenas `hsl(var(--token))` ou utility classes. Mora em: convenção. Validada em revisão visual + grep automático no QA.

### Cross-cutting
- **R16**: Toda mutação (config, catálogo, assinatura) é gravada em `imedto_admin_audit_log` com `{admin_id, action, entity_type, entity_id, motivo, payload_snapshot, ip, user_agent, timestamp}`. Mora em: middleware/decorator de comandos admin.
- **R17**: Acesso a `/api/admin/*` exige policy `ImedtoAdmin`. Usuário comum → 403. Mora em: auth pipeline. Inalterado.
- **R18**: Mensagens de erro genéricas para multi-tenant (sem revelar existência de tenant alheio). Mora em: controllers/handlers. Inalterado.

---

## 6. Modelo de dados

> Detalhamento de tipos/índices/constraints/seeds é responsabilidade do `imedto-database`. Esta seção fixa as entidades e campos essenciais.

### Tabelas novas

**`imedto_modelo_prontuario_global`**
- `id` (uuid, PK)
- `nome` (texto, not null)
- `descricao` (texto, nullable)
- `conteudo_json` (jsonb — estrutura do modelo: campos, ordem, defaults)
- `ativo` (bool, default `true`)
- `criado_em` (timestamptz)
- `criado_por_admin_id` (uuid, FK → `imedto_admin`)
- `atualizado_em` (timestamptz)
- `atualizado_por_admin_id` (uuid, FK → `imedto_admin`)

**`imedto_variavel_pool_global`**
- `id` (uuid, PK)
- `nome` (texto, not null)
- `tipo` (enum: `lista`, `texto_livre`, `numero`, `data` — alinhado ao tipo do legado)
- `valores_json` (jsonb — para tipo `lista`)
- `descricao` (texto, nullable)
- `ativo` (bool, default `true`)
- `criado_em`, `criado_por_admin_id`, `atualizado_em`, `atualizado_por_admin_id`

**`imedto_regiao_anatomica_global`**
- `id` (uuid, PK)
- `nome` (texto, not null, único quando `ativo = true`)
- `sinonimos` (text[], default `'{}'`)
- `sistema_corporal` (texto — ex: "musculoesquelético", "tegumentar")
- `ativo` (bool, default `true`)
- `criado_em`, `atualizado_em`
- **Migration**: popular tabela com os dados de seed atuais (commit `cabdf31`). Após popular, refatorar consumidores para apontarem para esta tabela.

### Tabela existente

**`imedto_config`** (criada Wave 1, agora ganha seeds)

Seeds default das 8 chaves (DB agent insere via migration idempotente):

| chave | tipo | valor default | seção | descrição |
|---|---|---|---|---|
| `trial.dias_padrao` | numerico | `14` | Trial | Dias do trial inicial ao criar novo estabelecimento |
| `trial.limite_profissionais` | numerico | `5` | Trial | Máximo de profissionais permitidos no trial |
| `assinatura.dias_aviso_expiracao` | numerico | `7` | Assinatura | Quantos dias antes da expiração começar a avisar |
| `sistema.email_suporte` | email | `suporte@imedto.com` | Sistema | E-mail exibido para usuários em casos de erro |
| `comunicacao.smtp_remetente` | email | `noreply@imedto.com` | Comunicação | Remetente padrão de e-mails transacionais |
| `comunicacao.from_padrao` | texto | `Imedto` | Comunicação | Nome de exibição em e-mails |
| `seguranca.tempo_sessao_admin_min` | numerico | `15` | Segurança | Tempo de inatividade até logout do admin (min) |
| `feature_flags.exemplo` | toggle | `false` | Feature Flags | Flag de exemplo para validar pipeline de feature flag |

**Campos novos em `imedto_config`** (se ainda não tiver — DB agent confere):
- `tipo` (enum: `numerico`, `texto`, `email`, `toggle`)
- `secao` (texto — agrupa na UI)
- `descricao` (texto — explica para o admin o que a chave faz)

### Tabelas existentes (sem mudança de schema)

- `imedto_admin_audit_log` — ganha novos `action`/`entity_type` mas estrutura inalterada.
- `modelo_prontuario` (tenant) — ganha consumidor novo (endpoint importar) mas sem mudança de schema.
- `variavel_pool` (tenant) — idem.
- `regiao_anatomica` (tenant ou seed?) — `imedto-database` confere se hoje é seed global ou já é por tenant. Não precisa migrar para tenant agora.

### Índices estratégicos sugeridos
- `imedto_modelo_prontuario_global(ativo, nome)` para listagem default.
- `imedto_variavel_pool_global(ativo, tipo)` idem.
- `imedto_regiao_anatomica_global(ativo, sistema_corporal, nome)` idem.
- `imedto_config(secao, chave)` já deve existir; senão, criar.

---

## 7. UX e fluxo

### Frente 1 — Detalhe do estabelecimento
- `EstabelecimentoDetalheView` ganha bloco "Assinatura" usando `AppCard`.
- Card mostra: plano vigente, status (ativo/trial/encerrado), data de início, próxima cobrança/expiração.
- Ações no card (`AppButton`): "Trocar plano", "Conceder gratuidade", "Encerrar assinatura".
- Cada ação abre modal já existente (importar e wirar).
- Após submit bem-sucedido: fechar modal, refresh do card (e do `EstabelecimentoDetalheView`), toast de sucesso.
- Erro: toast genérico + manter modal aberto com mensagem ao lado do campo (se 422 estruturado).
- Timeline de histórico embaixo: lista cronológica de eventos (criação → trocas → gratuidade → encerramento).

### Frente 2 — Página de configurações
- Nova entrada na sidebar admin: "Configurações" (ícone engrenagem).
- View `ConfiguracoesView`: layout em seções colapsáveis (Trial / Assinatura / Sistema / Comunicação / Segurança / Feature Flags).
- Cada chave é um `AppCard` ou linha de form com:
  - Label (= chave + descrição em tooltip)
  - Input tipado conforme `tipo` (number / text / email / toggle)
  - Botão "Salvar" no nível da chave (não no nível da página — reduz blast radius)
  - Modal de confirmação ao clicar "Salvar": pede `motivo ≥10 chars` antes de enviar
- Estado vazio: nunca acontece (seeds garantem 8 chaves no boot).
- Erro: 422 → mensagem inline; 500 → toast genérico.

### Frente 3 — Catálogos globais (admin)
- Nova seção na sidebar admin: "Catálogos" (subitens: "Modelos de prontuário", "Variáveis pool", "Regiões anatômicas").
- Cada subitem tem view tipo CRUD:
  - Lista paginada (usa `AppPagination`) com filtro de ativo/inativo.
  - Botão "+ Novo" abre `AppDrawer` (ou modal) com form.
  - Linha tem ações "Editar" e "Desativar" (ou "Reativar").
  - Toda mutação pede motivo em modal de confirmação.
- Variáveis pool e regiões anatômicas: forms mais simples (uma variável = um campo + valores).
- Modelos prontuário: form mais complexo (JSON estruturado de campos). Sugestão: começar com textarea JSON validado; editor visual fica para Wave 3+.

### Frente 3 — Catálogos globais (tenant — aba "Templates do sistema")
- Telas existentes de tenant em "Modelos de prontuário" e "Variáveis pool" ganham uma **segunda aba**: "Templates do sistema".
- Aba mostra lista de catálogos globais ativos (paginada).
- Cada item tem botão "Importar para minha clínica".
- Ao clicar: modal de confirmação com texto "Será criada uma cópia editável independente. Alterações no catálogo do sistema feitas depois NÃO afetam esta cópia. Confirma?"
- Confirmado → cópia criada → toast "Importado com sucesso" → navega/refresh para a aba "Meus modelos" com o novo item destacado.
- Sem aba "Templates do sistema" para Regiões Anatômicas neste momento (tenant não tem CRUD próprio de região anatômica; só consome o seed/global diretamente — DB agent confere).

### Frente 4 — Redesign
- `AdminLayout` passa a renderizar `AppSidebar` + `AppTopBar` do design system, com slot/prop para customização.
- `AppTopBar` recebe:
  - Slot esquerdo: logo + badge "Admin" (chip pequeno, cor `hsl(var(--warning))` ou tom suave).
  - Slot direito: avatar do admin + dropdown de logout.
  - Borda superior de 2px: cor de acento (variável `--admin-accent` derivada de `--warning`).
- Sidebar: mesmas seções, mas usando o `AppSidebar` (consistência visual).
- Todas as views (Wave 1 e Wave 2) usam `AppCard`, `AppPageHeader`, `AppButton`, `AppInput`, etc.
- Tokens HSL ou utility classes — zero hex.

### Estados obrigatórios em todas as listagens
- Loading: skeleton via componente do DS.
- Vazio: `AppEmptyState` com mensagem específica ("Nenhum modelo de prontuário global ainda. Clique em + Novo para criar o primeiro.").
- Erro: `AppErrorState` com botão "Tentar novamente".

### Atalhos / Performance
- Debounce ~300ms em campos de busca via `useDebouncedRef`.
- Paginação server-side em listas (default 20 por página).

---

## 8. Critérios de Aceite (testáveis)

### Frente 1 — Integração de mutação de assinatura

- **W2-CA1** (caminho feliz visualização)
  Dado um admin logado e um estabelecimento com assinatura ativa,
  Quando acessa `EstabelecimentoDetalheView`,
  Então vê o `AssinaturaCard` com plano vigente, status, datas e os botões "Trocar plano", "Conceder gratuidade", "Encerrar assinatura".

- **W2-CA2** (trocar plano)
  Dado um estabelecimento com plano A,
  Quando o admin clica "Trocar plano", seleciona plano B no modal, informa motivo ≥10 chars e confirma,
  Então o endpoint é chamado, retorna 200, modal fecha, card atualiza mostrando plano B, e uma linha aparece em `imedto_admin_audit_log` com `action=AssinaturaPlanoTrocado` e o motivo.

- **W2-CA3** (conceder gratuidade)
  Dado um estabelecimento parceiro,
  Quando o admin clica "Conceder gratuidade", informa motivo ≥10 chars e confirma,
  Então o endpoint é chamado, retorna 200, card atualiza e audit registra `action=AssinaturaGratuidadeConcedida`.

- **W2-CA4** (timeline)
  Dado um estabelecimento com histórico de assinaturas,
  Quando o admin abre a view de detalhe,
  Então a timeline dentro do `AssinaturaCard` lista os eventos em ordem cronológica (criação, trocas, gratuidade, encerramento), cada um com data e motivo.

- **W2-CA5** (motivo curto bloqueia)
  Dado o modal "Conceder gratuidade" aberto,
  Quando o admin informa motivo com menos de 10 chars e tenta confirmar,
  Então o front desabilita o botão de confirmar; se forçar via API, recebe 422 com mensagem "Motivo deve ter ao menos 10 caracteres."

### Frente 2 — Configurações globais

- **W2-CA6** (listagem agrupada)
  Dado o admin acessa `/admin/configuracoes`,
  Quando a página carrega,
  Então `GET /api/admin/configs` retorna 200 com as 8 chaves agrupadas em 6 seções (Trial, Assinatura, Sistema, Comunicação, Segurança, Feature Flags) e a UI renderiza cada seção colapsável.

- **W2-CA7** (atualização com motivo)
  Dado a chave `trial.dias_padrao`,
  Quando o admin altera para `30`, informa motivo ≥10 chars e clica Salvar,
  Então `PUT /api/admin/configs/trial.dias_padrao` retorna 200, audit registra `action=ConfigAtualizada` com payload `{antes: 14, depois: 30}` e a UI exibe toast de sucesso.

- **W2-CA8** (efeito real — novo estabelecimento)
  Dado `trial.dias_padrao = 30` recém-salvo,
  Quando um novo estabelecimento é criado (fluxo de onboarding normal),
  Então o trial criado tem 30 dias (não 14), comprovado por consulta no banco e linha em `imedto_admin_audit_log` ou log de criação.

- **W2-CA9** (sem efeito retroativo)
  Dado um estabelecimento criado quando `trial.dias_padrao = 14`,
  Quando o admin altera depois para `30`,
  Então o trial do estabelecimento existente permanece com 14 dias.

- **W2-CA10** (tempo de sessão admin lê config)
  Dado `seguranca.tempo_sessao_admin_min = 30` recém-salvo,
  Quando um admin novo loga,
  Então sua sessão expira após 30 minutos de inatividade (não 15).

- **W2-CA11** (motivo curto bloqueia config)
  Dado a tela de configurações,
  Quando o admin tenta salvar com motivo <10 chars,
  Então o front bloqueia; se forçar via API, recebe 422 com mensagem clara.

- **W2-CA12** (valor inválido por tipo)
  Dado a chave `trial.dias_padrao` (tipo `numerico`),
  Quando o admin envia valor `-5` ou `abc`,
  Então recebe 422 com mensagem "Valor inválido para chave numérica" (ou equivalente).

- **W2-CA13** (e-mail inválido)
  Dado a chave `sistema.email_suporte` (tipo `email`),
  Quando o admin envia `nao-eh-email`,
  Então recebe 422 com mensagem específica.

### Frente 3 — Catálogos globais (admin)

- **W2-CA14** (criar modelo global)
  Dado o admin em `/admin/catalogos/modelos-prontuario`,
  Quando clica "+ Novo", preenche nome + conteúdo JSON válido + motivo ≥10 chars e confirma,
  Então `POST /api/admin/catalogos/modelos-prontuario` retorna 201, item aparece na lista e audit registra `action=ModeloProntuarioGlobalCriado`.

- **W2-CA15** (listar paginado)
  Dado existem 25 modelos globais ativos,
  Quando o admin abre a listagem com `page_size=20`,
  Então recebe a primeira página com 20 itens, paginação mostra "1 de 2" e o segundo clique traz 5.

- **W2-CA16** (editar modelo global)
  Dado um modelo global existente,
  Quando o admin edita nome ou conteúdo, informa motivo ≥10 chars e confirma,
  Então retorna 200 e audit registra `action=ModeloProntuarioGlobalAtualizado` com `payload_snapshot` contendo antes/depois.

- **W2-CA17** (desativar modelo global — soft)
  Dado um modelo global ativo,
  Quando o admin desativa,
  Então `ativo = false` no banco (sem DELETE), listagem default não mostra, mas `?incluir_inativos=true` mostra.

- **W2-CA18-20** (variáveis pool globais)
  Idem CA14-17 para `imedto_variavel_pool_global`: criar, listar, editar, desativar.

- **W2-CA21-23** (regiões anatômicas globais)
  Idem CA14-17 para `imedto_regiao_anatomica_global`: criar, listar, editar, desativar. Adicional: seed do commit `cabdf31` está disponível na listagem após a migration de dados.

### Frente 3 — Catálogos globais (tenant)

- **W2-CA24** (aba "Templates do sistema" — modelos)
  Dado um usuário tenant em "Modelos de prontuário",
  Quando troca para a aba "Templates do sistema",
  Então `GET /api/modelos-prontuario/globais` retorna apenas modelos globais com `ativo=true`, listados paginados.

- **W2-CA25** (importar para a clínica)
  Dado a aba "Templates do sistema" mostrando um modelo global X,
  Quando o usuário clica "Importar para minha clínica" e confirma o aviso,
  Então `POST /api/modelos-prontuario/importar-do-global/{id}` retorna 201, uma nova linha aparece em `modelo_prontuario` com `estabelecimento_id = tenant_atual` e os campos copiados de X, e a UI navega para "Meus modelos" destacando o item.

- **W2-CA26** (cópia independente — sem live-link)
  Dado um tenant que importou o modelo global X,
  Quando o admin edita o global X depois,
  Então a cópia do tenant **não muda**. O tenant também pode editar sua cópia livremente sem afetar o global.

- **W2-CA27** (importação múltipla)
  Dado o tenant já importou X uma vez,
  Quando importa X novamente,
  Então uma segunda cópia é criada (sem deduplicação) com timestamps próprios.

- **W2-CA28** (idem aba variáveis pool)
  Análogo a CA24-27 para variáveis pool: aba "Templates do sistema" + importar.

### Frente 4 — Redesign

- **W2-CA29** (layout admin usa DS)
  Dado o admin em qualquer view do módulo,
  Quando inspeciona o DOM,
  Então `AdminLayout` renderiza `AppSidebar` e `AppTopBar` (componentes do design system).

- **W2-CA30** (badge "Admin" visível)
  Dado qualquer view do módulo admin,
  Quando renderizada,
  Então existe um chip/badge com texto "Admin" próximo ao logo no topbar.

- **W2-CA31** (faixa de acento)
  Dado qualquer view do módulo admin,
  Quando inspecionada,
  Então o topbar tem uma borda superior de 2px com cor derivada de `hsl(var(--warning))` (ou token de acento equivalente).

- **W2-CA32** (views usam componentes DS)
  Dado qualquer view admin (login, dashboard, lista admins, lista estabelecimentos, detalhe estabelecimento, lista planos, form plano, configurações, catálogos),
  Quando inspecionada,
  Então usa `AppCard`, `AppPageHeader`, `AppButton`, `AppInput` etc — não tem componentes ad-hoc duplicando o que existe no DS.

- **W2-CA33** (zero hex no módulo admin)
  Dado o módulo `frontend/src/modules/admin/`,
  Quando rodado `grep -rE "#[0-9a-fA-F]{3,6}" frontend/src/modules/admin/ --include="*.vue" --include="*.css" --include="*.scss"` em CSS scoped ou inline styles,
  Então não há matches (apenas tokens HSL ou utility classes).

- **W2-CA34** (zero regressão Wave 1)
  Dado as views da Wave 1 (login, dashboard, lista admins, lista estabelecimentos, detalhe estabelecimento, lista planos, form plano),
  Quando navegadas após o redesign,
  Então todos os fluxos da Wave 1 continuam funcionando (criar admin, criar plano, listar estabelecimentos, abrir detalhe).

### Cross-cutting

- **W2-CA35** (motivo obrigatório em toda mutação)
  Dado qualquer endpoint de mutação `/api/admin/configs/*` ou `/api/admin/catalogos/*` ou `/api/admin/estabelecimentos/{id}/assinatura/*`,
  Quando chamado sem motivo ou com motivo <10 chars,
  Então retorna 422 com mensagem clara.

- **W2-CA36** (audit completo)
  Dado qualquer mutação admin (config, catálogo, assinatura),
  Quando bem-sucedida,
  Então uma linha aparece em `imedto_admin_audit_log` com `{admin_id, action, entity_type, entity_id, motivo, payload_snapshot (antes/depois quando aplicável), ip, user_agent, timestamp}`.

- **W2-CA37** (RBAC mantido)
  Dado um usuário comum (não admin),
  Quando chama qualquer endpoint `/api/admin/*`,
  Então recebe 403.

- **W2-CA38** (multi-tenant em catálogos globais)
  Dado o schema das tabelas globais,
  Quando inspecionado (`\d imedto_modelo_prontuario_global` etc),
  Então **não há coluna `estabelecimento_id`** nas 3 tabelas globais. Tentar inserir registro com esse campo via SQL falha por coluna inexistente.

- **W2-CA39** (mensagens sem PII)
  Dado qualquer erro 4xx/5xx em endpoints admin,
  Quando inspecionada a resposta,
  Então não contém PII de paciente, e-mail completo de admin ou tenant alheio (apenas IDs e mensagens genéricas).

- **W2-CA40** (docs atualizados)
  Dado o PR final da Wave 2,
  Quando o QA revisa,
  Então:
  - `Docs/ARQUITETURA.md` tem nova seção "Configurações Globais (`imedto_config`)" com explicação do helper `IConfigGlobalReader`, cache, padrão de leitura por consumidor.
  - `Docs/ARQUITETURA.md` tem nova seção "Catálogos Globais" com explicação do modelo de cópia (tenant importa).
  - `Docs/DESIGN.md` tem nota sobre o módulo admin reusando design system + badge "Admin" + faixa de acento.

---

## 9. Arquitetura proposta (alto nível)

### Frente 1
- **Sem mudança arquitetural**. Apenas importar e wirar componentes existentes em `EstabelecimentoDetalheView`.

### Frente 2
- **Backend**:
  - `Domain.Admin.ConfigGlobal` (entidade) — value object `ConfigChave`, `ConfigValor`, validador por `tipo`.
  - `Admin.Configs.AtualizarConfigCommandHandler` — recebe `chave`, `valor`, `motivo`, valida tipo, persiste, dispara audit.
  - `Admin.Configs.ListarConfigsQuery` — agrupa por seção.
  - **Helper novo**: `IConfigGlobalReader` (injeção em handlers) — método `T Get<T>(string chave, T fallback)`. Implementação com `IMemoryCache` (TTL 60s).
  - **Refator**: `IniciarTrialAoCriarEstabelecimentoHandler.cs:21` passa a chamar `_configReader.Get<int>("trial.dias_padrao", 14)`.
  - **Refator**: tempo de sessão admin (hoje 15 min hardcoded) lê `seguranca.tempo_sessao_admin_min`.
- **Frontend**:
  - Store Pinia `useConfigGlobalStore` com `loadAll()` e `updateOne(chave, valor, motivo)`.
  - View `ConfiguracoesView.vue` com seções colapsáveis.

### Frente 3
- **Backend**:
  - 3 novos bounded contexts (ou subáreas dentro de `Admin.Catalogos`):
    - `Admin.Catalogos.ModeloProntuarioGlobal` (Domain + Handlers + Queries)
    - `Admin.Catalogos.VariavelPoolGlobal` (Domain + Handlers + Queries)
    - `Admin.Catalogos.RegiaoAnatomicaGlobal` (Domain + Handlers + Queries)
  - Cada um com CRUD: `Criar`, `Atualizar`, `Desativar`, `Reativar`, `Listar`, `ObterPorId`.
  - **Endpoint novo no escopo TENANT** (não admin): `POST /api/modelos-prontuario/importar-do-global/{idGlobal}` e `POST /api/variaveis-pool/importar-do-global/{idGlobal}`. Cada um é um handler que:
    1. Busca a entidade global pelo id (sem filtro de tenant — é global mesmo).
    2. Cria nova entidade no escopo tenant copiando os campos.
    3. Audit no `imedto_admin_audit_log`? Não — audit admin é para admin. Audit tenant é o usual do tenant.
- **Frontend**:
  - Admin: 3 views CRUD em `frontend/src/modules/admin/views/catalogos/`.
  - Tenant: aba "Templates do sistema" em telas existentes (sem novo módulo — extensão das telas atuais de modelo de prontuário e variável pool).

### Frente 4
- **Frontend**:
  - Refator de `frontend/src/modules/admin/layouts/AdminLayout.vue` para reusar `AppSidebar` + `AppTopBar` (importados de `frontend/src/components/ui/` ou onde estão no DS).
  - Customização via props/slots: badge "Admin", faixa 2px de acento.
  - Refator das views Wave 1 e Wave 2 para usar `AppCard`, `AppPageHeader`, `AppButton`, `AppInput`.
  - Variável CSS `--admin-accent: hsl(var(--warning))` (ou criar token novo se justificar).

---

## 10. Riscos e mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Redesign quebra testes da Wave 1 | Média | Alto | CA W2-CA34 valida regressão; QA percorre fluxo Wave 1 inteiro antes do commit; rodar suíte vitest completa. |
| Tenant espera atualização live do global | Média | Médio | UI mostra aviso explícito no botão "Importar"; documentação tenant-facing futura (Wave 3+) reforça. Texto curto no modal de confirmação. |
| Config aplicada retroativamente por engano | Baixa | Alto | Handlers leem config no momento da criação (não em recalcs). CA W2-CA9 valida. |
| Cache do `IConfigGlobalReader` serve valor stale | Baixa | Médio | TTL curto (60s); invalidação manual após `Atualizar` via método explícito no reader. |
| Migration de regiões anatômicas com dados duplica seed | Média | Médio | DB agent valida idempotência: se `imedto_regiao_anatomica_global` tem dados, não reinsere. Refator de consumidores depois da migration. |
| Importar mesmo global N vezes confunde tenant | Baixa | Baixo | Tenant fica responsável; UI mostra timestamps na lista "Meus modelos". Wave 3+ pode adicionar deduplicação opcional. |
| Editor JSON cru para modelo prontuário gera erro de sintaxe | Alta | Médio | Validar JSON no front antes de enviar; backend valida estrutura mínima e retorna 422 claro. Editor visual fica para Wave 3+. |
| Topbar com badge + faixa fica visualmente carregado | Baixa | Baixo | QA valida com captura visual; usuário aprovou conceito; ajustes finos em Wave 3+ se feedback adicional. |

---

## 11. Observações para execução

### Ordem **obrigatória** de execução (dependências)
1. **`imedto-database`** primeiro:
   - Migration que cria as 3 tabelas globais + popula `imedto_regiao_anatomica_global` com o seed atual.
   - Migration que insere seeds em `imedto_config` (8 chaves) — idempotente (se já existem, não duplica).
   - Validação de schema multi-tenant (ausência de `estabelecimento_id` nas 3 tabelas globais).
2. **`imedto-developer`** depois, **sequencial por frente** (não paralelo — frentes dependem entre si):
   - **Frente 1** primeiro (fix rápido, destrava UX imediatamente; sem dependência de DB ou outras frentes).
   - **Frente 2** segundo (cria helper `IConfigGlobalReader` + refatora handlers que vão consumir). Frente 4 depende de saber as telas finais para redesenhar.
   - **Frente 3** terceiro (3 sub-CRUDs admin + 2 endpoints tenant + 2 abas tenant). Mais volumoso. Pode ser quebrado em commits internos.
   - **Frente 4** por último (redesign cobre Wave 1 + tudo entregue nas Frentes 1, 2, 3). Vir por último evita retrabalho.
3. **`imedto-qa`** valida CAs em ordem (Frente 1 → 2 → 3 → 4 → cross-cutting), commita uma vez ao final com referência ao briefing e push único.

### Restrições técnicas
- Módulo admin continua isolado fisicamente em `frontend/src/modules/admin/` — redesign não move arquivos do módulo, só troca dependências de componentes.
- Endpoints admin permanecem em `/api/admin/*` com policy `ImedtoAdmin`.
- Mutações tenant (importar do global) usam autenticação tenant normal (NÃO `ImedtoAdmin`).
- Helper `IConfigGlobalReader` é singleton com cache; não usar `IDbConnection` direto em handlers para ler config.
- Editor de JSON do modelo prontuário: começar simples (textarea + validador). Editor visual fica para Wave 3+.

### Liberdade técnica do dev
- Forma de cache do `IConfigGlobalReader` (memória, lazy, etc) — dev decide.
- Estrutura interna do JSON do `conteudo_json` no modelo prontuário — dev decide com base no que o tenant `modelo_prontuario` espera hoje (cópia precisa ser compatível).
- Implementação do toggle de seção colapsável em `ConfiguracoesView` — dev decide.

---

## 12. Atualização de documentação (a fazer durante a implementação, valida no QA)

> **Importante**: O BA NÃO atualiza `Docs/` neste momento. O `imedto-developer` atualiza durante a implementação. O `imedto-qa` valida no fechamento.

- **`Docs/ARQUITETURA.md`** — adicionar:
  - Seção "Configurações Globais (`imedto_config`)": explicação do schema, helper `IConfigGlobalReader`, padrão de cache, regra de não-retroatividade, espelho back+front de validação de tipo.
  - Seção "Catálogos Globais": as 3 tabelas, princípio "modelo de cópia, sem live-link", endpoint tenant `POST .../importar-do-global/{id}`, regra de soft-delete.
- **`Docs/DESIGN.md`** — adicionar nota:
  - Módulo admin reusa `AppSidebar` + `AppTopBar` do design system com badge "Admin" e faixa 2px de acento no topbar.
  - Zero hex code em CSS scoped do módulo admin (apenas tokens HSL ou utility classes) — princípio convencional.
- **`Docs/LGPD.md`** — revisar (sem mudança esperada; catálogos globais não têm PII; configs não vazam tenant data).
- **`Docs/COMANDOS.md`** — sem mudança esperada (npm scripts e fluxo de migration inalterados).
- **`Docs/INFRA.md`** — sem mudança esperada (sem novo recurso AWS / SSM / IAM).

---

## 13. Pontos de extensão futura (Wave 3+)

- Impersonate (admin "entrar como" tenant para suporte).
- MFA TOTP no login admin.
- RBAC granular dentro de admin (papéis: admin pleno, suporte read-only, financeiro).
- IP allow-list para área admin.
- Billing real (gateway de pagamento, geração de fatura, conciliação).
- Dashboard admin com métricas globais (MRR, churn, estabelecimentos ativos, etc).
- Logs centralizados (CloudWatch / Datadog).
- Fila de envio Resend / fila SMTP real.
- Editor visual de modelo de prontuário (substitui textarea JSON).
- Feature flags: schema mais rico (segmentação por tenant, por plano, gradual rollout).
- "Sincronizar com global" — opção para tenant trazer atualizações do global mantendo as edições locais (merge consciente).
- Catálogo global de CIDs, TUSS, medicamentos.

---

## 14. Hand-off

- **Próximo agente**: `imedto-database`
- **Entregáveis esperados do DB**:
  1. Migration EF + SQL idempotente em `db/migrations/` criando as 3 tabelas globais (sem `estabelecimento_id`).
  2. Migration de dados populando `imedto_regiao_anatomica_global` com o seed atual.
  3. Migration idempotente preenchendo seeds default em `imedto_config` (8 chaves listadas em §6).
  4. Índices estratégicos sugeridos em §6.
  5. Validação via psql/MCP que schema das 3 tabelas globais não tem `estabelecimento_id`.
- **Depois**: `imedto-developer` na ordem **sequencial** Frente 1 → 2 → 3 → 4.
- **Por último**: `imedto-qa` valida 40 CAs + cross-cutting + docs.
