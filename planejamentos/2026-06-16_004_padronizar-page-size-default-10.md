# Padronizar tamanho de página padrão para 10 em todo o app

**ID**: 2026-06-16_004
**Status**: Aprovado por usuário em 2026-06-16
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (muitos pontos, mas cada um trivial — troca de literal `20` → `10`)
**Áreas regressivas tocadas**: paginação cross-cutting (Pacientes, Agendamentos, Financeiro, Inventário/Estoque, Notificações, Termos, Documentos/Acessos do paciente, Receitas, Atestados, Pedidos de exame, Exame físico, Prontuário/Evoluções, Orçamentos config, Dashboards Admin) + autocomplete de catálogo de procedimentos

> **Entrega frontend + backend. SEM schema. NÃO aciona `imedto-database`.** Defaults de tamanho de página são código puro — valores literais em contratos de query (`= 20`), parâmetros de controller (`[FromQuery] int tamanho = 20`), fallbacks de repositório e refs/services no front. Nenhuma migration, nenhuma coluna, nenhum índice. O dado em banco não muda; muda só quantos registros a primeira página carrega por padrão.

---

## 1. Contexto e motivação

Hoje o app é inconsistente no tamanho de página padrão: a grande maioria das listagens abre com **20 itens**, e a paginação nova do histórico de evoluções (briefing `2026-06-16_003`) já nasceu com **10**. O usuário decidiu **padronizar tudo em 10 como default** — listas mais curtas na primeira carga, scroll menor, percepção de leveza, e coerência visual entre telas. A decisão de produto já está fechada e validada; este briefing apenas materializa o inventário e os CAs.

Esta padronização é **complementar ao briefing `2026-06-16_003`** (paginação nova do histórico de evoluções, que já usa 10) e será implementada na **mesma sessão / mesmo push**. Não há conflito de arquivos: o 003 cria paginação onde não havia; o 004 ajusta defaults já existentes.

## 2. Persona-alvo

Todos os papéis do sistema (Recepção, Profissional, Dono/Admin, Financeiro) e o admin global — qualquer usuário que abre uma listagem paginada ou usa o autocomplete de busca de procedimentos. Frequência: contínua, em praticamente todas as telas de listagem do produto.

## 3. Escopo

**Inclui**:
- Trocar **todo default de tamanho de página = 20 → 10**, em **frontend E backend**, em todo o app.
- Contratos de Query do backend (propriedade com inicializador `= 20`).
- Parâmetros default de controllers (`[FromQuery] int tamanho = 20` / `tamanhoPagina = 20` / `limit = 20`).
- Fallbacks de tamanho em repositórios (ex.: `if (tamanho < 1 ...) tamanho = 20` → `10`).
- Refs, consts, services e stores do frontend que inicializam página em 20.
- **O teto do autocomplete de catálogo de procedimentos** (`BuscarProcedimentoCatalogoQuery.Limit = 20`, controller `limit = 20`, repositório `int limit = 20`) → **10**. O usuário confirmou explicitamente "também baixar para 10".
- **Comentário de documentação inline** no `AuditLogFeed.vue` ("Paginação: AppPagination 20/pg" → "10/pg") — para não deixar comentário mentindo sobre o código.
- **Testes (NUnit + Vitest)** cujo assert depende do default → atualizar para 10. Ver §7-CA-testes para o critério exato.

**Não inclui**:
- **`frontend/src/views/auth/OnboardingView.vue`** (linhas ~134/137): `tamanho: "Só eu"` e `const TAMANHOS = ["Só eu", "2 a 5", "6 a 20", "Mais de 20"]` são **porte da equipe da clínica**, NÃO paginação. **Não tocar.** (Confirmado por inspeção: também a linha 956 `<option value="20">20 minutos</option>` é duração de consulta — fora de escopo.)
- **Alteração do teto de clamp** (`if (... tamanho > 100) tamanho = 100`): o teto de **100 permanece**. Só o valor de **fallback** muda (20 → 10).
- **Adicionar paginação onde não existe** — isso é o briefing 003, não este.
- **Mudar o seletor de "itens por página" da UI** (caso exista em alguma tela) para outro conjunto de opções — só o default inicial muda; a lista de opções do seletor, se houver, permanece.
- **Schema, migration, índice** — nada disso.

## 4. Regras de negócio

- **R1 — Default canônico = 10.** Toda listagem paginada do app abre, por padrão (sem o cliente passar `tamanho`/`tamanhoPagina`/`limit` explícito), com **10 itens**. Mora em: contrato de Query (back) + ref/service (front). Validada em: back (default do contrato/controller é a fonte da verdade) + front (default do ref/service espelha o back). O front nunca deve depender de um default diferente do back para a mesma tela.
- **R2 — Teto preservado.** O clamp de máximo permanece **100**. Apenas o valor de fallback quando `tamanho < 1` (ou inválido) muda de 20 para 10. Mora em: repositórios com clamp (`NotificacaoQueryRepository`, `TermoQueryRepository`). Validada em: back.
- **R3 — Autocomplete de catálogo limita a 10.** A busca incremental de procedimentos do catálogo retorna **no máximo 10** resultados por padrão. Mora em: `BuscarProcedimentoCatalogoQuery.Limit`, `CatalogoController` (`limit = 20`→`10`), `ProcedimentoCatalogoQueryRepository` (`int limit = 20`→`10`). Validada em: back. (Front não passa `limit` explícito hoje — herda o default.)
- **R4 — Onboarding intocado.** O porte de equipe do onboarding ("Só eu" / "2 a 5" / "6 a 20" / "Mais de 20") **não é paginação** e permanece exatamente como está. Mora em: `OnboardingView.vue`. Validada em: revisão de diff (o arquivo não deve aparecer no diff).
- **R5 — Comentários coerentes.** Qualquer comentário de código que documente o tamanho de página (ex.: "20/pg" no cabeçalho de `AuditLogFeed.vue`) é atualizado junto com o valor. Documentação parada vira documentação errada.

## 5. Modelo de dados

**Nenhuma mudança.** Sem tabela, sem coluna, sem índice, sem migration. Não aciona `imedto-database`. Multi-tenant, audit e LGPD não são afetados — o número de registros por página não altera filtro de tenant, escopo de acesso nem conteúdo do DTO.

## 6. Inventário completo de pontos a alterar (20 → 10)

> Verificado por grep em 2026-06-16. O dev deve **reproduzir os greps abaixo** antes de implementar para garantir que a lista está completa/atual (arquivos podem ter mudado). Os greps são a rede de segurança; a lista abaixo é o mapa.
>
> - Backend: `grep -rnE '=\s*20\b' backend/src --include='*.cs' | grep -iE 'tamanho|pagina|pagesize|limit'`
> - Frontend: `grep -rniE '(ref\(\s*20\s*\)|tamanho\s*[:=]\s*20|pageSize\s*[:=]\s*20|tamanhoPagina\s*[:=]\s*20|\?\?\s*20)' frontend/src --include='*.vue' --include='*.ts'`
> - Confirmação do autocomplete: `grep -rnE 'limit\s*=\s*20|Limit\s*\{\s*get;\s*set;\s*\}\s*=\s*20' backend/src`

### 6.1 Backend — Contratos de Query (`= 20` no inicializador da propriedade)

| Arquivo | Propriedade |
|---|---|
| `Contracts/Atestados/Queries/AtestadoQueries.cs` | `TamanhoPagina = 20` |
| `Contracts/PedidosExame/Queries/PedidoExameQueries.cs` | `TamanhoPagina = 20` |
| `Contracts/Pacientes/Queries/ListarPacientesQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Pacientes/Queries/ListarDocumentosDoPacienteQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Pacientes/Queries/ListarAcessosDoPacienteQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Receitas/Queries/ListarReceitasDoPacienteQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Admin/Dashboard/Queries/ListarAuditLogDashboardQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Termos/Queries/ListarModelosTermoQuery.cs` | `Tamanho = 20` |
| `Contracts/Financeiro/Queries/ListarLancamentosQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Financeiro/Queries/ListarExtratoQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Prontuarios/Queries/ExameFisicoQueries.cs` | `Tamanho = 20` |
| `Contracts/Prontuarios/Queries/ListarEvolucoesProntuarioPacienteQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Agendamentos/Queries/ListarListaEsperaQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Agendamentos/Queries/ListarAgendamentosQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Notificacoes/Queries/ListarNotificacoesQuery.cs` | `Tamanho = 20` |
| `Contracts/Inventario/Cadastros/Queries/CadastrosQueries.cs` | `TamanhoPagina = 20` (**4 ocorrências** — linhas ~12, 21, 30, 39) |
| `Contracts/Inventario/Queries/ListarItensInventarioQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Inventario/Queries/ListarMovimentacoesQuery.cs` | `TamanhoPagina = 20` |
| `Contracts/Catalogo/Queries/BuscarProcedimentoCatalogoQuery.cs` | **`Limit = 20`** (autocomplete — R3) |

### 6.2 Backend — Controllers (`[FromQuery] ... = 20`)

| Arquivo | Parâmetro |
|---|---|
| `Controllers/InventarioController.cs` | `tamanho = 20` (**2** — linhas ~38, 119) |
| `Controllers/CadastrosEstoqueController.cs` | `tamanho = 20` (**4** — linhas ~48, 131, 212, 305) |
| `Controllers/NotificacaoController.cs` | `tamanho = 20` |
| `Controllers/CatalogoController.cs` | **`limit = 20`** (autocomplete — R3) |
| `Controllers/ExameFisicoController.cs` | `tamanho = 20` |
| `Controllers/ReceitaController.cs` | `tamanho = 20` |
| `Controllers/PacienteController.cs` | `tamanho = 20` (**3** — linhas ~45, 214, 244) |
| `Controllers/AtestadoController.cs` | `tamanho = 20` |
| `Controllers/PedidoExameController.cs` | `tamanho = 20` |
| `Controllers/ProntuarioController.cs` | `tamanho = 20` |
| `Controllers/TermoModeloController.cs` | `tamanho = 20` |
| `Controllers/FinanceiroController.cs` | `tamanho = 20` (**2** — linhas ~41, 335) |
| `Controllers/AgendamentoController.cs` | `tamanho = 20` (**2** — linhas ~41, 225) |
| `Controllers/Admin/AdminDashboardController.cs` | `tamanhoPagina = 20` |
| `Controllers/Admin/AdminVariaveisPoolGlobaisController.cs` | `tamanhoPagina = 20` |
| `Controllers/Admin/AdminModelosProntuarioGlobaisController.cs` | `tamanhoPagina = 20` |
| `Controllers/Admin/AdminModelosPermissaoGlobaisController.cs` | `tamanhoPagina = 20` |

### 6.3 Backend — Repositórios (fallback / parâmetro default)

| Arquivo | Trecho | Ação |
|---|---|---|
| `Infrastructure/Database/Repositories/NotificacaoQueryRepository.cs` | `if (tamanho < 1 \|\| tamanho > 100) tamanho = 20;` | fallback **20 → 10**; teto `> 100` **mantido** |
| `Infrastructure/Database/Repositories/TermoQueryRepository.cs` | `if (tamanho < 1) tamanho = 20;` (linha seguinte `if (tamanho > 100) tamanho = 100;`) | fallback **20 → 10**; clamp `> 100` **mantido** |
| `Infrastructure/Database/Repositories/ProcedimentoCatalogoQueryRepository.cs` | `Buscar(..., int limit = 20)` | **20 → 10** (autocomplete — R3) |

### 6.4 Frontend — refs / consts (`ref(20)` / `= 20`)

| Arquivo | Ref |
|---|---|
| `components/orcamento/config/ValoresProfissionalTab.vue` | `tamanho = ref(20)` |
| `components/orcamento/config/ProcedimentosTab.vue` | `tamanho = ref(20)` |
| `components/orcamento/config/PacotesTab.vue` | `tamanho = ref(20)` |
| `components/orcamento/config/ProdutosTab.vue` | `tamanho = ref(20)` |
| `components/orcamento/config/EquipeTab.vue` | `tamanho = ref(20)` |
| `components/orcamento/config/AnestesistasTab.vue` | `tamanho = ref(20)` |
| `components/estoque/cadastros/CadastroLocaisTab.vue` | `tamanho = ref(20)` |
| `components/estoque/cadastros/CadastroFabricantesTab.vue` | `tamanho = ref(20)` |
| `components/estoque/cadastros/CadastroCategoriasTab.vue` | `tamanho = ref(20)` |
| `components/estoque/cadastros/CadastroProdutosTab.vue` | `tamanho = ref(20)` |
| `components/estoque/cadastros/CadastroFornecedoresTab.vue` | `tamanho = ref(20)` |
| `components/termos/TermosPainelEmbutido.vue` | `tamanho = ref(20)` |
| `views/configuracoes/TermosListaView.vue` | `tamanho = ref(20)` |
| `modules/admin/stores/modelosGlobaisStore.ts` | `tamanho = ref(20)` |
| `modules/admin/stores/variaveisGlobaisStore.ts` | `tamanho = ref(20)` |
| `modules/admin/stores/permissoesGlobaisStore.ts` | `tamanho = ref(20)` |
| `modules/admin/components/dashboard/AuditLogFeed.vue` | `tamanhoPagina = 20` (linha ~70) **+ comentário "20/pg" no cabeçalho (linha ~7) → "10/pg"** (R5) |
| `views/pacientes/PacientesView.vue` | `tamanho = ref(20)` |
| `views/pacientes/PacienteDetalheView.vue` | `tamDocumentos = ref(20)` (linha ~83) **e** `tamAcessos = ref(20)` (linha ~210) |
| `views/notificacoes/NotificacoesView.vue` | `tamanho = ref(20)` |
| `views/orcamentos/OrcamentoListaView.vue` | `tamanho = ref(20)` |
| `views/inventario/InventarioView.vue` | `tamanhoItens = ref(20)` (linha ~30) **e** `tamanhoMovs = ref(20)` (linha ~41) |
| `views/financeiro/tabs/VisaoGeralTab.vue` | `tamanho = ref(20)` |

### 6.5 Frontend — services / stores (default `?? 20` / `= 20`)

| Arquivo | Trecho |
|---|---|
| `services/pacienteService.ts` | `listar(busca?, pagina = 1, tamanho = 20)` → `tamanho = 10` |
| `services/documentoService.ts` | `tamanho: params.tamanho ?? 20` → `?? 10` |
| `services/acessoService.ts` | `tamanho: params.tamanho ?? 20` → `?? 10` |
| `services/termoModeloService.ts` | `tamanho: filtros.tamanho ?? 20` → `?? 10` |
| `modules/admin/services/dashboardService.ts` | `tamanhoPagina: filtros.tamanhoPagina ?? 20` → `?? 10` |
| `stores/notificacoesStore.ts` | `tamanho: params.tamanho ?? 20` → `?? 10` |

### 6.6 Testes — atualizar quando o assert depende do default

> **Critério (decisão fechada):** atualizar para **10** os asserts em que o teste verifica o **default** (valor de `tamanho`/`TamanhoPagina`/`tamanhoPagina` esperado quando o input **não passa** o valor explicitamente). Testes que passam `20` como **input explícito** (testando outra coisa, não o default) **podem permanecer 20** — o dev decide caso a caso. O critério de sucesso é a suíte verde, não "trocar todo 20".

| Arquivo | Observação |
|---|---|
| `backend/.../Application/Documentos/ListarDocumentosDoPacienteQueryHandlerTests.cs` | `TamanhoPagina = 20` em 3 pontos — revisar quais refletem o default |
| `backend/.../Application/Financeiro/ListarExtratoQueryHandlerTests.cs` | `TamanhoPagina = 20` em 4 pontos — idem |
| `backend/.../Application/Acessos/ListarAcessosDoPacienteQueryHandlerTests.cs` | `TamanhoPagina = 20` em 3 pontos — idem |
| `frontend/src/services/termoModeloService.test.ts` | asserts `tamanho: 20` (linhas ~27, 39) que dependem do default → 10 |
| `frontend/src/services/documentoService.test.ts` | asserts `tamanhoPagina: 20` / `tamanho: 20` (linhas ~17, 30, 83) que dependem do default → 10 |

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — Pacientes):** Dado um estabelecimento com mais de 10 pacientes, Quando o usuário abre a lista de Pacientes sem alterar o seletor de página, Então a primeira página exibe exatamente **10** pacientes e o `AppPagination` calcula o total de páginas com base em tamanho 10.
- **CA2 (Agendamentos):** Dado mais de 10 agendamentos no período, Quando a listagem de agendamentos carrega, Então a primeira página traz **10** itens (request com `tamanho=10` ou sem `tamanho`, herdando o default 10 do back).
- **CA3 (Financeiro — extrato/lançamentos):** Dado mais de 10 lançamentos, Quando a aba financeira (extrato/visão geral) carrega, Então a primeira página traz **10** itens.
- **CA4 (Inventário — itens e movimentações):** Dado mais de 10 itens de inventário e mais de 10 movimentações, Quando a `InventarioView` carrega cada aba, Então **itens** e **movimentações** abrem com **10** cada (`tamanhoItens` e `tamanhoMovs` em 10).
- **CA5 (Notificações):** Dado mais de 10 notificações, Quando o painel/lista de notificações carrega, Então a primeira página traz **10**, e o fallback do `NotificacaoQueryRepository` para tamanho inválido também é **10** (não mais 20).
- **CA6 (Termos):** Dado mais de 10 modelos de termo, Quando `TermosListaView` / `TermosPainelEmbutido` carregam, Então a primeira página traz **10**, e o fallback de `TermoQueryRepository` para `tamanho < 1` é **10**.
- **CA7 (Documentos e Acessos do paciente):** Dado um paciente com mais de 10 documentos e mais de 10 acessos registrados, Quando a `PacienteDetalheView` carrega as seções de documentos e de acessos, Então cada uma abre com **10** itens (`tamDocumentos = 10`, `tamAcessos = 10`).
- **CA8 (Dashboards Admin):** Dado mais de 10 registros, Quando o `AuditLogFeed` e as listagens admin (variáveis/modelos/permissões globais) carregam, Então cada uma abre com **10** itens; **e** o comentário de cabeçalho do `AuditLogFeed.vue` diz "10/pg" (não "20/pg").
- **CA9 (Autocomplete de catálogo de procedimentos — R3):** Dado um termo de busca que casa com mais de 10 procedimentos no catálogo, Quando o usuário digita no autocomplete e o `GET` de busca dispara sem `limit` explícito, Então a resposta traz **no máximo 10** resultados (não 20).
- **CA10 (Onboarding NÃO afetado — R4):** Dado o fluxo de onboarding, Quando o usuário escolhe o porte da equipe, Então as opções permanecem "Só eu" / "2 a 5" / "6 a 20" / "Mais de 20" intactas, e o arquivo `OnboardingView.vue` **não aparece** no diff desta entrega.
- **CA11 (teto de clamp preservado — R2):** Dado um cliente que passa `tamanho=500`, Quando a query atinge o repositório com clamp, Então o tamanho é limitado a **100** (teto inalterado); e dado `tamanho` inválido/ausente, o fallback aplicado é **10**.
- **CA12 (espelho front+back):** Dado qualquer tela do inventário acima, Quando o front dispara a request sem `tamanho` explícito, Então o tamanho efetivo da primeira página é **10** tanto pela ausência (default do back) quanto pelo default do front — front e back concordam em 10 para a mesma tela.
- **CA-testes (suíte verde):** Dado a suíte automatizada (**NUnit + Vitest**), Quando roda após as alterações, Então **passa 100%**: os asserts que verificavam o default 20 foram atualizados para 10; asserts que passam 20 como input explícito (não testando o default) podem permanecer. `npm run build`, `npm run type-check` (typecheck do front) e o build do backend concluem sem erro.
- **CA-tipografia/lint:** Dado o gate de qualidade, Quando o QA roda lint + `npm run check:typography -- --ci`, Então não há regressão introduzida por esta entrega (mudança é só de literal numérico de paginação, sem CSS).
- **CA-sem-migration:** Dado o diff completo da entrega, Quando o QA inspeciona, Então **não há** arquivo novo em `db/migrations/`, nenhuma alteração de schema/EF, e `imedto-database` **não foi acionado**.

## 8. Riscos e dependências

- **Risco baixo, abrangência alta.** A mudança é mecânica (literal `20` → `10`), mas toca ~30 arquivos de back e ~30 de front. O risco real é **omissão** (esquecer um ponto) ou **falso positivo** (trocar um `20` que não é paginação). Mitigação: os greps de §6 são a rede de segurança; revisar cada ocorrência no contexto antes de trocar.
- **Falso positivo conhecido a evitar:** `OnboardingView.vue` (porte de equipe, linhas 134/137; e `<option value="20">20 minutos</option>` na linha ~956). **Não tocar.** Qualquer outro `20` que não seja tamanho de página (duração, padding, dimensão, ano, etc.) fica fora.
- **Dependência de sessão:** implementar junto com `2026-06-16_003` (paginação do histórico de evoluções, que já usa 10) num **único push**, conforme regra "1 push por sessão". Sem conflito de arquivos entre os dois briefings.
- **Áreas regressivas a vigiar:** qualquer tela com `AppPagination` cujo cálculo de "total de páginas" derive do tamanho — confirmar que o total de páginas recalcula corretamente com 10. Seletores de "itens por página" da UI (se existirem) devem continuar funcionando; só o valor inicial muda.

## 9. Observações para execução

- **Não-negociável:** front e back concordam no default 10 para a mesma tela (R1). O back (contrato/controller) é a fonte da verdade; o front espelha.
- **Não-negociável:** teto de clamp permanece 100 (R2); só o fallback muda.
- **Não-negociável:** `OnboardingView.vue` fora do diff (R4).
- **Liberdade técnica do dev:** decidir, caso a caso nos testes, o que é assert-de-default (atualiza para 10) vs. input-explícito (pode manter 20). Critério de sucesso é suíte verde, não "zerar todo 20".
- **Reuso:** nenhum componente novo, nenhum endpoint novo, nenhum DTO novo. É ajuste de default em pontos existentes. Não criar abstração de "page size config" — seria scope creep; a decisão é trocar literais.

## 10. Atualização de documentação

**Nenhum doc em `Docs/` precisa ser atualizado.** A mudança não altera arquitetura, infra, design system, LGPD nem comando recorrente — é só o valor default de paginação, que não está documentado como contrato em `Docs/`. (O único "doc" tocado é o **comentário inline** "20/pg" → "10/pg" no `AuditLogFeed.vue`, já coberto por R5/CA8 — não é doc de `Docs/`.)
