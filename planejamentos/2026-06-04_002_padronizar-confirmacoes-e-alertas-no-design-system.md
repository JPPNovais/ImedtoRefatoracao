# Padronizar confirmações e alertas no Design System (eliminar `confirm()`/`alert()` nativos)

**ID**: 2026-06-04_002
**Status**: Aprovado por usuário em 2026-06-04 (escopo "padronizar o app inteiro" fechado pelo dono do produto)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (mudança mecânica e repetitiva em ~15 arquivos, baixo risco lógico, mas volume + verificação por CA)
**Áreas regressivas tocadas**: estoque, financeiro, agenda, orçamento, pacientes, prontuário (modelos), permissionamento (convites/vínculos) — toque é só de UX, sem mudança de regra de negócio

## 1. Contexto e motivação

O app ainda usa diálogos nativos do browser (`window.confirm()` e `window.alert()`) em ~15 telas. Isso quebra a consistência visual do produto (premissa central do `Docs/DESIGN.md`: "parecer parte do mesmo software"), trava a thread do browser, não é estilizável, não respeita o tema, é hostil em mobile e contorna o design system. O DS já resolveu o problema: `AppConfirmDialog` (confirmação) e `AppToast` (feedback) já são o padrão em ~18 telas — notavelmente todo o módulo `orcamento/config/*` e, nesta sessão, `PapelEditorModal.vue` e `ProfissionalDetalhesModal.vue`, que servem de precedente. A demanda crava esse padrão no app inteiro e elimina o anti-padrão de vez.

**Evidência (grep em `frontend/src/`, 2026-06-04)**: 16 ocorrências de `confirm(` em 15 arquivos + 14 ocorrências de `alert(` em 11 arquivos. Zero `prompt()`. Zero `window.*` bare (todos são `confirm(`/`alert(` globais). Os arquivos de `orcamento/config/*` aparecem só em comentários — já migrados, não tocar.

## 2. Persona-alvo

Todos os operadores internos do app (recepção, profissional, financeiro, dono/admin), em qualquer estabelecimento, sempre que executam uma ação destrutiva ou recebem feedback de operação. Frequência alta e transversal — é UX cross-cutting, não feature de um módulo.

## 3. Escopo

**Inclui**:
- Substituir TODO `confirm()` nativo por `AppConfirmDialog` nas 15 telas pendentes (16 ocorrências).
- Substituir TODO `alert()` nativo por feedback via `AppToast` (padrão `notificar()` local) nas telas pendentes (14 ocorrências).
- Cravar a regra canônica (regra 1 e 2 da seção 4) no `Docs/DESIGN.md`.

**Não inclui**:
- Refatorar para um composable global de toast/confirm. O padrão canônico do projeto é estado local por componente (ver seção 4) — manter consistência com as ~18 telas já migradas; criar abstração nova é scope creep e diverge do precedente. (Registrar como item de backlog separado se o dono quiser unificar depois.)
- Mudar QUALQUER regra de negócio, endpoint, payload, ordem de operação ou texto semântico das mensagens.
- Re-migrar os arquivos `orcamento/config/*`, `PapelEditorModal.vue`, `ProfissionalDetalhesModal.vue` (já feitos — são referência).
- Migrar `notificacoesStore` ou qualquer feature de notificação in-app (é domínio diferente — ver nota em seção 4).

## 4. Regras de negócio (padrão canônico do Design System)

> **Atenção semântica — dois padrões distintos, nunca confundir.** `AppConfirmDialog` é para *perguntar antes de agir*. `AppToast` é para *avisar depois de agir*. Erro de operação NUNCA vira `AppConfirmDialog`; é sempre toast `error`.

- **R1 — Confirmação de ação destrutiva/irreversível** (excluir, inativar, cancelar, remover, recusar, baixar pagamento, converter): substitui `confirm()` por `AppConfirmDialog`. Mora em: Front (UX). A regra de negócio do que a ação faz continua no back. Variante:
  - `variante="danger"` para excluir/inativar/cancelar/remover/recusar/baixar (destrutivo ou de impacto financeiro irreversível).
  - `variante="primary"` para ação não-destrutiva que ainda merece confirmação (ex.: "Converter orçamento em cirurgia" em `OrcamentoDetalheView.vue` — é uma transição de estado, não uma destruição).
- **R2 — Feedback de erro/sucesso de operação**: substitui `alert()` por `AppToast` via função local `notificar()`. Mora em: Front (UX). Toast `error` para falha, `success` para sucesso. NUNCA usar `AppConfirmDialog` para erro.
- **R3 — Mensagem de erro do backend é a fonte da verdade**: o toast de erro usa `e?.response?.data?.mensagem ?? "<fallback PT-BR específico da ação>"`, exatamente como o `alert()` atual já faz. Preservar o texto de fallback existente palavra por palavra. (Premissa do projeto: 422 do `BusinessException` é a verdade; front é UX.)
- **R4 — Preservação semântica**: o texto e a semântica de cada mensagem PT-BR (confirmação e erro) são preservados. É permitido apenas o ajuste mínimo de pontuação/aspas tipográficas para caber no template do dialog (ex.: aspas `"..."` → `"..."`), sem mudar o sentido. A interpolação de nome/descrição do alvo é mantida.
- **R5 — Comportamento de negócio idêntico**: a ação só dispara após o `@confirmar`; cancelar/fechar o dialog não executa nada (igual ao `if (!confirm(...)) return` de hoje). Nenhuma mudança em ordem de chamadas, payload ou efeito colateral.

**Forma canônica EXATA de disparar toast e confirmação** (extraída de `src/components/orcamento/config/ProcedimentosTab.vue`, que é o gabarito):

```ts
// no <script setup>, importar do design system:
import { /* ...demais... */ AppToast, AppConfirmDialog } from "@/components/ui"

// --- TOAST (substitui alert) ---
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
// uso: notificar(e?.response?.data?.mensagem ?? "Erro ao inativar.", "error")
//      notificar("Procedimento inativado.", "success")

// --- CONFIRMAÇÃO (substitui confirm) ---
const confirmacao = ref<{ aberto: boolean, alvo: TipoDoAlvo | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})
function abrirConfirmacao(item: TipoDoAlvo) {
    confirmacao.value = { aberto: true, alvo: item, executando: false }
}
async function executarAcao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await store.acao(alvo.id)              // mesma chamada de negócio de hoje
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Mensagem de sucesso.", "success")
        // ...recarregar lista se hoje recarrega...
    } catch (e: any) {
        confirmacao.value.executando = false   // mantém o dialog aberto no erro
        notificar(e?.response?.data?.mensagem ?? "Falha ao ...", "error")
    }
}
```

```vue
<!-- no <template>, ao final do componente -->
<AppConfirmDialog
    v-model:aberto="confirmacao.aberto"
    titulo="Inativar categoria?"
    :mensagem="confirmacao.alvo ? `Deseja inativar “${confirmacao.alvo.nome}”?` : ''"
    confirmar-rotulo="Inativar"
    variante="danger"
    :executando="confirmacao.executando"
    @confirmar="executarAcao"
/>

<AppToast
    v-if="toast"
    :mensagem="toast.mensagem"
    :variante="toast.variante"
    @fechar="toast = null"
/>
```

**API real dos componentes (validada lendo o código):**
- `AppConfirmDialog` props: `aberto` (boolean, via `v-model:aberto`), `titulo` (string, obrigatório), `mensagem?` (string), `confirmarRotulo?`, `cancelarRotulo?`, `variante?: "danger" | "primary"` (default = danger no botão), `icone?`, `executando?` (boolean → loading no botão + bloqueia fechar). Emits: `update:aberto`, `confirmar`, `cancelar`.
- `AppToast` props: `mensagem` (string, obrigatório), `variante?: "info" | "success" | "error"` (default info), `duracao?` (ms, default 3500). Emit: `fechar` (dispara sozinho após `duracao`; caller faz `toast = null`). Renderiza via `<Teleport to="body">` — pode ficar no fim do template sem se preocupar com posição.

> **Nota de domínio (não confundir):** `notificacoesStore` é a feature de *notificações in-app* (sino/lista de notificações do usuário), NÃO o mecanismo de toast. O padrão canônico de toast é o estado local `toast` + `notificar()` por componente. Não substituir `alert()` por `notificacoesStore`.

## 5. Modelo de dados

Nenhuma mudança. Sem tabela, coluna, índice, migration ou audit. Mudança é 100% de camada de apresentação (front). Sem impacto multi-tenant novo (as queries/comandos por trás das ações já filtram `estabelecimento_id` e não são tocados).

## 6. UX e fluxo

- **Confirmação**: clicar a ação destrutiva abre o `AppConfirmDialog` (modal centralizado, `largura="sm"`, do DS) em vez do popup nativo. Botão de cancelar (secondary) + botão de confirmar (danger/primary). Enquanto a request está em voo, `executando=true` → botão de confirmar com spinner e fechar bloqueado, evitando duplo disparo.
- **Feedback**: sucesso/erro aparece como `AppToast` (pílula flutuante bottom-center, some em 3.5s) em vez do `alert()` bloqueante.
- **Estados**: loading (botão `executando`), erro (toast `error`, dialog permanece aberto para retry), sucesso (toast `success`, dialog fecha). Componentes 100% do design system, já responsivos/mobile-ready.
- **Reuso**: zero HTML/CSS novo. Apenas importa `AppConfirmDialog`/`AppToast` de `@/components/ui` e replica o padrão local.

## 7. Critérios de aceite (testáveis)

> Os CAs de inventário (CA1–CA3) são verificáveis por grep retornando zero — o QA pode automatizar. Os CAs por módulo (CA8+) validam que cada ação manteve comportamento.

- **CA1** (erradicação de `confirm`): Dado o diretório `frontend/src/`, Quando se executa `grep -rn "confirm(" src --include="*.vue" --include="*.ts"` e se filtram os falsos-positivos (`confirmar`, `confirmacao`, `confirmDialog`, `@confirmar`, `confirmar-rotulo`, `BusinessException`/comentários), Então o resultado é **zero** ocorrências de `confirm(` nativo de chamada.
- **CA2** (erradicação de `alert`): Dado o diretório `frontend/src/`, Quando se executa `grep -rn "alert(" src --include="*.vue" --include="*.ts"`, Então o resultado é **zero** ocorrências de `alert(` nativo (apenas `alerta`/`alertas`/`Alert` de domínio podem permanecer — nenhum é chamada de função).
- **CA3** (zero `window.*` nativo): Dado `frontend/src/`, Quando se executa `grep -rn "window\.confirm\|window\.alert\|window\.prompt" src --include="*.vue" --include="*.ts"`, Então só restam ocorrências em comentários de documentação, **zero** chamadas.
- **CA4** (caminho feliz — confirmação): Dado qualquer uma das telas migradas, Quando o usuário clica numa ação destrutiva, Então abre `AppConfirmDialog` (modal do DS), e a request de negócio só dispara após `@confirmar` — clicar Cancelar/fechar não executa nada.
- **CA5** (feedback de erro usa mensagem do back): Dado que uma operação falha com 422, Quando o catch é acionado, Então aparece `AppToast` `variante="error"` com `e?.response?.data?.mensagem` e, na ausência, o fallback PT-BR específico da ação (preservado palavra por palavra do `alert()` original).
- **CA6** (feedback de sucesso): Dado que uma operação destrutiva conclui com 2xx, Quando termina, Então aparece `AppToast` `variante="success"` com mensagem PT-BR coerente e o dialog fecha.
- **CA7** (não-regressão de negócio): Dado cada ação migrada, Quando executada e confirmada, Então o efeito de negócio (request, payload, recarga de lista, navegação) é idêntico ao comportamento anterior — só o mecanismo de UX mudou.
- **CA8** (mensagens preservadas): Dado cada confirmação migrada, Quando o dialog é exibido, Então o texto/semântica da mensagem PT-BR é preservado (incl. interpolação de nome/descrição do alvo); apenas pontuação/aspas tipográficas podem ser ajustadas.
- **CA9** (variante correta): Dado ação destrutiva (excluir/inativar/cancelar/remover/recusar/baixar), Quando o dialog abre, Então `variante="danger"`; Dado ação não-destrutiva (converter orçamento em cirurgia), Então `variante="primary"`.
- **CA10** (LGPD — exclusão de paciente robusta): Dado `PacientesView.vue`, Quando o usuário aciona excluir paciente, Então o `AppConfirmDialog` usa `variante="danger"`, título e mensagem deixam explícito que a ação é **irreversível** (preservando "Esta ação é irreversível."), e a exclusão só ocorre após `@confirmar`. Nenhuma PII adicional além do nome (já exibido hoje) é introduzida na mensagem.
- **CA11** (sem duplo disparo): Dado uma confirmação cuja request está em voo, Quando o usuário clica novamente, Então o botão de confirmar está em estado `executando` (loading) e o dialog não pode ser fechado, impedindo a segunda chamada.
- **CA12** (build e testes): Dado o app após a migração, Quando se roda `npm run build` e a suíte de testes, Então passam sem erro de tipo/import e sem regressão (ver `Docs/COMANDOS.md`).
- **CA13** (documentação viva): Dado `Docs/DESIGN.md`, Quando a entrega conclui, Então existe a seção "Confirmações e feedback (AppConfirmDialog / AppToast)" com a regra R1/R2 e a forma canônica de disparo (texto pronto na seção 10 deste briefing).
- **CA14** (multi-tenant intacto): Dado que nenhuma query/comando é tocado, Quando se revisa o diff, Então não há alteração em camada de dados/handler — o filtro `estabelecimento_id` existente permanece inalterado (verificação por inspeção do diff: só arquivos `.vue` de view/componente são tocados, exceto `Docs/DESIGN.md`).

## 8. Inventário rastreável (cada item é um checkbox para o developer e o QA)

> Os arquivos `orcamento/config/*`, `PapelEditorModal.vue`, `ProfissionalDetalhesModal.vue` JÁ estão migrados — **referência, não re-fazer.**

| # | Arquivo | confirm | alert | Tipo de ação | Variante |
|---|---------|:------:|:-----:|--------------|----------|
| 1 | `src/components/estoque/cadastros/CadastroCategoriasTab.vue` | 1 (L70) | 2 (L75,84) | inativar/reativar | danger |
| 2 | `src/components/estoque/cadastros/CadastroProdutosTab.vue` | 1 (L264) | 1 (L269) | inativar | danger |
| 3 | `src/components/estoque/cadastros/CadastroFornecedoresTab.vue` | 1 (L99) | 2 (L104,113) | inativar/reativar | danger |
| 4 | `src/components/estoque/cadastros/CadastroLocaisTab.vue` | 1 (L76) | 2 (L81,90) | inativar/reativar | danger |
| 5 | `src/components/estoque/cadastros/CadastroFabricantesTab.vue` | 1 (L71) | 2 (L76,85) | inativar/reativar | danger |
| 6 | `src/components/estabelecimento/ListasVariaveisTab.vue` | 1 (L116) | 0 | excluir item de lista | danger |
| 7 | `src/views/pacientes/PacientesView.vue` | 1 (L117) | 0 | excluir paciente (LGPD) | danger |
| 8 | `src/views/profissionais/MeusConvitesView.vue` | 1 (L37) | 0 | recusar convite | danger |
| 9 | `src/views/profissionais/SolicitarVinculoView.vue` | 1 (L73) | 0 | cancelar solicitação | danger |
| 10 | `src/views/inventario/InventarioView.vue` | 1 (L203) | 1 (L210) | inativar | danger |
| 11 | `src/views/equipe/EquipeView.vue` | 1 (L252) | 0 | cancelar convite | danger |
| 12 | `src/views/financeiro/FinanceiroView.vue` | 2 (L108,118) | 2 (L113,123) | baixar pago / cancelar | danger |
| 13 | `src/views/agenda/AgendaView.vue` | 1 (L479) | 0 | remover da lista de espera | danger |
| 14 | `src/views/orcamentos/OrcamentoDetalheView.vue` | 1 (L66) | 0 | **converter em cirurgia** | **primary** |
| 15 | `src/views/configuracoes/ModelosProntuarioView.vue` | 1 (L100) | 2 (L92,108) | excluir modelo (+ salvar) | danger |

**Totais: 16 `confirm()` + 14 `alert()`** = 30 ocorrências nativas a erradicar. (Números de linha são referência do grep de 2026-06-04; o developer deve localizar pela chamada, não pela linha exata.)

> Nota item 15: o `alert(L92)` é erro de **salvar** modelo (sucesso/erro), não destrutivo — vira toast (sem dialog). O `confirm(L100)`+`alert(L108)` é o fluxo de **excluir** modelo (dialog danger + toast erro).
> Nota item 12: são duas ações distintas (baixar como pago / cancelar lançamento) — cada uma precisa do seu próprio estado de confirmação (ou um estado com discriminador de ação), sem misturar os dois fluxos.

## 9. Riscos e dependências

- **Risco de regressão de comportamento**: a tradução `if (!confirm) return` → fluxo assíncrono com dialog inverte o controle (de síncrono bloqueante para evento `@confirmar`). O developer deve garantir que toda a lógica que vinha *depois* do `confirm()` migre para `executarAcao()`/`@confirmar`, sem perder nenhum passo (recarga de lista, fechar drawer, navegação). CA7 cobre isso.
- **Risco em FinanceiroView (item 12)**: duas confirmações no mesmo componente — não compartilhar um único `confirmacao.alvo` ambíguo. Usar discriminador de ação ou dois estados.
- **Risco em ModelosProntuarioView (item 15)**: separar o toast de *salvar* (não-destrutivo) do dialog de *excluir*. Não envolver o salvar num dialog.
- **Dependência**: nenhuma de back/db. Tudo front + `Docs/DESIGN.md`.
- **Áreas regressivas a vigiar no QA**: financeiro (baixa de pagamento é sensível), pacientes (exclusão irreversível — LGPD), estoque (inativar/reativar tem par de mensagens).

## 10. Observações para execução

**Não-negociável:**
- Seguir a forma canônica EXATA da seção 4 (gabarito `ProcedimentosTab.vue`). Não inventar composable global, não criar componente novo, não usar `notificacoesStore` para toast.
- Preservar mensagens PT-BR e fallback de erro palavra por palavra (R3/R4/R8... CA5/CA8).
- `AppConfirmDialog` e `AppToast` importados de `@/components/ui` (barrel `index.ts`).
- Atualizar `Docs/DESIGN.md` **antes** de despachar para o QA, com o texto abaixo.

**Liberdade técnica:**
- Nome das funções locais (`abrir...`/`executar...`), discriminador de ação no FinanceiroView, e tipagem do `alvo` ficam a critério do developer, desde que sigam o padrão.

**Sugestão de FASES (execução incremental, mas 1 único push ao fim — política do projeto):**
- **Fase 1 — Estoque** (itens 1–5 + 10 `InventarioView`): 6 arquivos, padrão idêntico (inativar/reativar). Faz o grosso e firma o padrão.
- **Fase 2 — Views financeiro/agenda/orçamento** (itens 12, 13, 14): inclui o caso `primary` (converter cirurgia) e o caso de duas confirmações (financeiro).
- **Fase 3 — Pacientes/profissionais/config/estabelecimento** (itens 6, 7, 8, 9, 11, 15): inclui LGPD (pacientes) e separação salvar/excluir (modelos).
- Ao fim das 3 fases: rodar CA1–CA3 (grep zero), `npm run build` + testes, atualizar `Docs/DESIGN.md`, então QA → 1 push.

**Texto pronto para `Docs/DESIGN.md`** (inserir como nova subseção dentro de "## Experiência consistente em todo o site", logo após o bullet "Botões de ação em tabelas"):

```md
- **Confirmações e feedback (AppConfirmDialog / AppToast)**: o app NÃO usa `window.confirm()`/`window.alert()` nativos — eles quebram o tema, travam a thread e são hostis em mobile. Dois padrões distintos do design system, nunca confundir:
  - **Confirmar antes de agir** (excluir, inativar, cancelar, remover, recusar, baixar pagamento, converter): usar [`AppConfirmDialog`](../frontend/src/components/ui/AppConfirmDialog.vue) com `v-model:aberto`, `titulo`, `:mensagem`, `confirmar-rotulo`, `:executando` e `@confirmar`. `variante="danger"` para ação destrutiva/irreversível; `variante="primary"` para transição de estado não-destrutiva (ex.: converter orçamento em cirurgia). A request só dispara no `@confirmar`; enquanto está em voo, `:executando="true"` mostra loading no botão e bloqueia fechar (evita duplo disparo).
  - **Avisar depois de agir** (sucesso/erro de operação): usar [`AppToast`](../frontend/src/components/ui/AppToast.vue), nunca `AppConfirmDialog` para erro. Padrão canônico: estado local `const toast = ref<{ mensagem, variante } | null>(null)` + função `notificar(mensagem, variante)`, renderizado com `<AppToast v-if="toast" ... @fechar="toast = null" />` (ele se auto-fecha em ~3.5s via Teleport). Toast de erro usa a mensagem do backend: `notificar(e?.response?.data?.mensagem ?? "Falha ao ...", "error")` — o 422 do `BusinessException` é a fonte da verdade. `notificacoesStore` é a feature de notificações in-app (sino), NÃO o mecanismo de toast.
  - Gabarito de referência completo: [`ProcedimentosTab.vue`](../frontend/src/components/orcamento/config/ProcedimentosTab.vue).
```

## 11. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar a subseção "Confirmações e feedback (AppConfirmDialog / AppToast)" em "## Experiência consistente em todo o site" (texto pronto na seção 10). É a parte cross-cutting que justifica o briefing: crava a regra de UX para todo o app e impede o anti-padrão de voltar.
- Nenhum outro doc. Sem mudança de arquitetura/infra/LGPD-dado/comandos (nenhum dado pessoal novo, nenhum endpoint novo, nenhuma migration).
