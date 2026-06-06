---
name: project-pool-variaveis-autocomplete
description: Pool de variáveis conectado ao prontuário — autocomplete + criação automática ao salvar evolução (briefing 2026-06-05_001)
metadata:
  type: project
---

Briefing 2026-06-05_001 + addendum (Expectativa fora desta entrega).

**O que foi feito:**
- Enum `TipoVariavelPool`: removidos Droga e AtividadeFisica; mantidos Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa.
- `NormalizadorPool` (Domain): normalização canônica trim + lower + sem diacríticos (100% C#, sem unaccent).
- `IProntuarioVariavelPoolRepository`: novo método `ListarAtivosPorTipo` + `ExisteOutraComMesmoNome` agora considera padrão-sistema (AND considera `EhPadraoSistema=true OR estabelecimento_id=@X`).
- `PoolExtratorEvolucao` (Application): extrai os 5 campos mapeados do ConteudoJson e cria inéditos no pool. Falha-suave (nunca quebra o salvamento).
- `RegistrarEvolucaoCommandHandler`: injeta `PoolExtratorEvolucao`, chama após salvar evolução.
- Mensagem 422 "Tipo inválido" atualizada para listar os 6 tipos válidos.
- Front: `TipoVariavelPool` e `TIPOS_VARIAVEL_POOL` sem Droga/AtividadeFisica. `ListasVariaveisTab.vue` e `VariaveisGlobaisFormView/ListView.vue` atualizados.
- `AppAutocompleteCriavel` criado no design system (typeahead string, v-model string, filtro client-side).
- `SecaoHistoriaPregressa.vue` e `SecaoHistoriaFamiliar.vue`: campos nome/parentesco usam AppAutocompleteCriavel. Pool carregado uma vez no onMounted.

**Status:** backend 0 erros, 1375 testes (1298 pass + 77 skip). Frontend build verde, 458 testes (52 suites). Aguarda: (1) migration imedto-database DELETE tipos removidos; (2) commit pelo imedto-qa.

**Carona de UI:** `EstabelecimentoView.vue` tem mudança pré-existente (tabs refactor) não relacionada a este briefing — incluir em commit separado.

**Why:** pool estava desconectado dos campos do prontuário desde a criação. Esta feature fecha o ciclo: profissional vê sugestões ao digitar, valores inéditos viram pool automaticamente ao salvar.

**How to apply:** dedup usa NormalizadorPool.Normalizar — qualquer nova comparação de nome no pool deve usar esse helper.
