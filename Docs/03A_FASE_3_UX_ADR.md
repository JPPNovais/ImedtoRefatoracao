# ADR — UX da Fase 3 (Receitas, Exame Físico, Procedimento Cirúrgico, Orçamento Completo)

**Data:** 2026-04-29
**Autor:** ux-designer
**Status:** Proposto

> Insumo de UX para `senior-software-engineer` (backend) e `ui-implementer` (frontend) implementarem os 4 fluxos clínicos da Fase 3 ([03_FASE_3_DOMINIO_CLINICO.md](03_FASE_3_DOMINIO_CLINICO.md)). Princípio geral: **paridade funcional 1:1 com legado, com migração para design system novo (`App*`)**. Não inventar fluxo. Quando houver lacuna ou simplificação, está marcada como "verificar com produto".

---

## 1. Receitas

### 1.1 Fluxo legado
Entrada via aba "Receitas" do prontuário ([ReceitasTabSection.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/receitas/ReceitasTabSection.vue)) → botão "Nova receita" abre **side panel** ([ReceitasDrawer.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/receitas/ReceitasDrawer.vue), 550px). Etapa 1: escolher tipo (`Simples` / `Controlada` via ToggleGroup) e, se controlada, tipo de notificação (A/B/C/Especial). Etapa 2: editor ([ReceitaEditor.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/receitas/ReceitaEditor.vue)) — máquina de estados `DRAFT → FINALIZED → CANCELED`, lista de itens medicáveis (medicamento, concentração, forma, quantidade, posologia, via, duração, instruções), favoritos do profissional (estrela + busca), observações com **autosave debounced 500ms**, finalização exige ≥1 item + confirm dialog, ações finais: Imprimir / PDF / Nova versão (clone).

**Manter:** máquina de estados explícita, drawer de 2 etapas, autocomplete de favoritos, autosave de observações, badges coloridos por tipo e status.

**UX antiga / friccional:** uso de `confirm()` nativo do browser, `alert()` para validação, ícones FontAwesome, classes Tailwind diretas em `bg-warning/10`. Texto "Nova versão" é ambíguo (clona a receita, não cria versão linkada — backend tem `Substituir(novaReceitaId)` mas legado não usa).

### 1.2 Recomendação
- Container: receita vive **dentro do prontuário**, não tem container próprio (é renderizada dentro de [ProntuarioView.vue](../frontend/src/views/pacientes/ProntuarioView.vue) que já usa `.app-page--wide`).
- Componentes: `AppDrawer` (~560px), `AppPillToggle` (Simples/Controlada e A/B/C/Especial), `AppField` + `AppInput`/`AppTextarea`/`AppSelect`, `AppButton`, `AppBadge` (status/tipo), `AppEmptyState` (sem medicamentos), `AppModal` para confirmações destrutivas (substituir `confirm()`).
- Lista paginada com `AppPagination` (≥20 receitas históricas é comum em paciente crônico).
- Busca de favoritos: usar [useDebouncedRef](../frontend/src/composables/useDebouncedRef.ts).
- Validação: backend retorna 422 (`BusinessException`); front exibe inline em `AppField :erro=""`.
- PDF: backend gera com `QuestPDF` (item 3.1). Front baixa blob — não duplicar layout PDF no front.

### 1.3 Decisão
**Drawer de 2 etapas (Tipo → Editor) reusando `AppDrawer`. Lista de receitas no prontuário usando `AppPagination`. Autosave de observações mantido. "Nova versão" renomear para "Duplicar" (cópia limpa) — se quiser versionamento real (Substituir), virar fluxo separado "Substituir esta receita" que requer motivo. Verificar com produto.**

---

## 2. Exame Físico

### 2.1 Fluxo legado
Aba "Exame físico" do prontuário. Estrutura em 3 partes:
1. **Dados gerais** ([DadosGeraisForm.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/exame-fisico/DadosGeraisForm.vue), 417 linhas) — peso, altura, IMC, PA, FC, FR, sat, temp.
2. **Body Map** ([BodyMapSvg.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/exame-fisico/BodyMapSvg.vue)): SVG path-based de corpo humano (frente + costas), variantes M/F, hover destaca grupo de membro, click abre [RegionSelectorPopup.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/exame-fisico/RegionSelectorPopup.vue) — Dialog com navegação hierárquica (breadcrumb → sub-regiões → drill-down), seleção múltipla com lateralidade D/E/Bilateral.
3. **Cards por região** ([RegionExamCard.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/exame-fisico/RegionExamCard.vue)) — `Collapsible` com texto livre, achados, observações.
4. **Timeline** ([ExameFisicoTimeline.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/exame-fisico/ExameFisicoTimeline.vue)) — histórico do paciente.

**Atenção:** o schema da Fase 3 ([03_FASE_3_DOMINIO_CLINICO.md §3.2](03_FASE_3_DOMINIO_CLINICO.md)) é **mais simples** que o legado: `regiao_codigo` string + `severidade` enum, sem hierarquia/lateralidade explícita. **Verificar com produto:** preservar hierarquia/lateralidade do legado (backend precisa de `regiao_pai_codigo`, `lateralidade`) ou simplificar (perde fidelidade clínica)?

### 2.2 Recomendação
- Container: dentro do prontuário (sem `.app-page` próprio).
- Tabs internas: "Dados gerais" / "Mapa corporal" / "Histórico" (`ProntuarioTabs` já existe).
- BodyMap: **portar 1:1 SVG paths + assets `corpo-bg-masculino.jpg` / `corpo-bg-feminino.jpg`**. Trocar `Dialog` legado por `AppModal` com slot de breadcrumb. Trocar `Checkbox` por primitiva equivalente do `App*` (criar `AppCheckbox` no DS — não existe ainda).
- Card de região → componente novo `ExameFisicoRegiaoCard` em `components/prontuario/exame-fisico/` (não vai ao DS — é específico do domínio).
- Severidade: usar `AppPillToggle` (Normal/Leve/Alterado/Crítico) com cores semânticas (success/warning/error/destructive).

**Decisão sobre BodyMap:**
- (A) **Portar 1:1** como `components/prontuario/exame-fisico/BodyMapSvg.vue` (não DS — uso único). Mais rápido, mantém fidelidade. ✅ **Recomendado.**
- (B) Reescrever do zero — quebra fidelidade clínica.
- (C) `AppBodyMap` no DS — só vale se houver outro lugar pedindo (não há).

### 2.3 Decisão
**Portar 1:1 (opção A). Manter hierarquia + lateralidade no backend (estender schema com `regiao_pai_codigo varchar` e `lateralidade varchar(10)` em `exame_fisico_regioes`). Tabs internas Dados Gerais / Mapa / Histórico. Criar `AppCheckbox` no DS (faltante) para evoluir form.**

---

## 3. Procedimento Cirúrgico

### 3.1 Fluxo legado
4 sub-formulários separados em `medical-record/components/`: [DescricaoCirurgicaSection.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/DescricaoCirurgicaSection.vue), [EquipeCirurgicaSection.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/EquipeCirurgicaSection.vue), [FichaAnestesicaSection.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/FichaAnestesicaSection.vue), [EvolucaoPosOperatoriaSection.vue](../ReferenciaLegado/Imedto/src/modules/medical-record/components/EvolucaoPosOperatoriaSection.vue). No legado são seções soltas dentro de uma evolução de prontuário — não há "página de procedimento cirúrgico" como entidade própria. O backend novo modela como **aggregate raiz** (`procedimentos_cirurgicos`) com lifecycle (Planejado → Confirmado → Realizado).

### 3.2 Recomendação
- Container: rota dedicada `/pacientes/:id/cirurgias/:cirurgiaId` com `.app-page--wide` (`AppPageHeader` "Procedimento cirúrgico — [cirurgia_principal]" + status badge + botões contextuais por estado).
- **Tabs verticais (ou laterais em desktop) > steps**: cirurgião retorna nas 4 seções em momentos diferentes (planejamento, intra-op, pós-op). Steps forçariam ordem que não corresponde ao workflow real.
- Tabs: `Descrição` | `Equipe` | `Ficha anestésica` | `Evolução pós-op`.
- Estado "Planejado" → só Descrição + Equipe editáveis. "Realizado" → todas editáveis. "Cancelado" → readonly + motivo.
- Integração agenda: quando agendamento com `procedimento_cirurgico_id` é marcado "concluído", emitir `ProcedimentoConfirmadoEvent` (já no plano) e oferecer CTA "Registrar realização" no card do agendamento.

### 3.3 Decisão
**Rota dedicada com tabs internas (`AppPageHeader` + `ProntuarioTabs` reutilizado para tabs). Status como state machine com botões contextuais. Listagem em `/cirurgias` com `AppPagination` filtrável por status/data/profissional. Verificar com produto: ficha anestésica é JSONB rico — definir schema fechado antes da implementação.**

---

## 4. Orçamento Completo

### 4.1 Fluxo legado
Single-page com seções verticais ([BudgetFormTab.vue](../ReferenciaLegado/Imedto/src/modules/budgets/components/tabs/BudgetFormTab.vue)): Paciente → Cirurgias → Equipes especializadas → Implantes → Profissionais → Local → Totais + Formas de pagamento. Cada seção é card colapsável. Totais ([BudgetTotaisSection.vue](../ReferenciaLegado/Imedto/src/modules/budgets/components/form/BudgetTotaisSection.vue)) recalcula em real-time. Múltiplas formas de pagamento somam para um subtotal — soma deve bater com total - desconto + juros.

**Bem feito:** seções colapsáveis, cálculo reativo, painel lateral com totais sempre visível.

**Friccional:** sem indicação visual de integridade (soma das formas vs. total). Sem progresso (usuário não sabe quanto falta).

### 4.2 Recomendação
- Container: `.app-page--wide`.
- **Single page com seções colapsáveis > wizard.** Orçamento não tem ordem rígida (corretor de seguro pula entre seções); wizard penaliza edições. Wizard é melhor para fluxo guiado de criação **única vez** — não é o caso aqui.
- Layout 2 colunas em desktop (≥1024px): coluna esquerda = seções colapsáveis (`AppCard` com header clicável); coluna direita sticky = `Resumo` com totais + integridade ("Falta R$ 230 para completar"). Em mobile, resumo vira footer fixo com CTA.
- Integridade visual: badge `success` quando soma == total, `warning` quando não. Botão "Salvar" desabilitado se diferença > 0,01.
- **Reuso com orçamento simples**: mesma view com prop `tipo: 'Simples' | 'Cirurgico'` que esconde seções Equipe/Implantes/Procedimento. **Verificar com produto:** alguns clínicos pedem fluxos visualmente distintos para evitar confusão. Recomendação: uma view só com toggle, mantendo URL diferenciada (`/orcamentos/novo?tipo=cirurgico`).

### 4.3 Decisão
**Single page com seções colapsáveis em 2 colunas desktop, resumo sticky com indicador de integridade. Mesma view para Simples/Cirúrgico com prop `tipo`. Validação backend (422) quando soma das formas ≠ total. Cálculos no aggregate, front só exibe.**

---

## 5. Acessibilidade (WCAG 2.1 AA — checklist mínimo)

- **Foco visível**: todos componentes `App*` usam `--ring` HSL. Confirmar em modal/drawer (foco volta ao trigger ao fechar).
- **Labels associadas**: todo input via `AppField` (já associa `<label for>` automaticamente).
- **Erros lidos por SR**: `AppField :erro=""` deve renderizar com `role="alert"` ou `aria-live="polite"`. **Verificar implementação atual.**
- **BodyMap**: SVG hotspots precisam de `<title>` por região + alternativa textual (lista navegável por teclado). Sem isso, leitor de tela não consegue selecionar região. **Bloqueante para AA.**
- **Modal de seleção de região**: `Dialog` legado tem `DialogDescription class="sr-only"` — bom. Replicar em `AppModal`.
- **Contraste**: tokens HSL atuais passam AA (verificado em `--primary` 254/56/38 sobre branco = 8.2:1).
- **Touch targets**: 44×44px mínimo. SVG hotspots de membros pequenos (mão, pé) podem ficar abaixo — adicionar lista textual paralela como fallback para mobile.
- **Reduced motion**: respeitar `prefers-reduced-motion` em transições do BodyMap (hover highlight) e drawer.

---

## 6. Riscos / Trade-offs

| Risco | Impacto | Mitigação |
|---|---|---|
| Portar BodyMap 1:1 leva ~1-2 dias; reescrever ~4-5. | Schedule | **Portar 1:1.** Refinar com feedback clínico depois. |
| Schema novo do exame físico é mais simples que o legado (sem hierarquia/lateralidade). | Fidelidade clínica | Estender schema para preservar (recomendado). Validar com clínico. |
| `AppCheckbox` não existe no DS. | Bloqueia exame físico (popup de seleção). | Criar `AppCheckbox` no DS antes de iniciar 3.2. |
| `AppModal` pode não ter slot de breadcrumb. | Selector de região | Confirmar API; se faltar, estender ou aceitar header customizado. |
| Substituição de `confirm()`/`alert()` nativos por `AppModal` em 4 fluxos. | Esforço | Criar helper `useConfirm()` composable que retorna promise — reduz boilerplate. |
| Orçamento simples e completo na mesma view podem virar Frankenstein. | Componentização | Extrair seções como componentes `<Budget*Section>` reusáveis; toggle só liga/desliga. |
| Ficha anestésica é JSONB livre — risco de inconsistência. | Dado clínico não-estruturado | Definir schema fechado (técnica, drogas com dose, intercorrências) antes de codar. |
| BodyMap acessibilidade (SVG sem `<title>`). | WCAG AA | Mandatório adicionar `<title>` + lista textual. |

---

## 7. Componentes que faltam no DS (criar antes da Fase 3)

1. `AppCheckbox` — bloqueia exame físico.
2. `AppTabs` (ou confirmar reuso de `ProntuarioTabs` em outros contextos — virar genérico). Cirurgia precisa.
3. `AppCollapsible` (ou `AppCard` com prop `colapsavel`) — seções de orçamento.
4. `useConfirm()` composable — substitui `confirm()` nativo nos 4 fluxos.

Esses 4 itens devem ser entregues no início da Wave 4 (frontend) **antes** das telas dos domínios. Caso contrário, cada agente reinventa scoped.

---

## 8. Recomendações ao implementador

- **Backend (`senior-software-engineer`)**: validar todas as regras que o front mostra (soma de formas, ≥1 item em receita, ≥1 cirurgião, severidade enum). Front é UX; 422 do back é fonte da verdade.
- **Frontend (`ui-implementer`)**: nada de Tailwind direto em cores — só tokens HSL. Antes de criar componente novo, conferir [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md). Buscas que vão à API: `useDebouncedRef`. Listas: `AppPagination`. Modais destrutivos: `AppModal` (não `confirm()`).
- **Ambos**: marcações "verificar com produto" desta ADR são bloqueantes — pingar produto/cliente antes de implementar.
