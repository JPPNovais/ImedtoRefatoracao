---
name: exame-fisico-passo-lateralidade-membro
description: Decisão de produto no mapa corporal — lado escolhido 1x por membro; "Ambos" = 1 entrada bilateral (não mais 2 D+E)
metadata:
  type: project
---

Briefing `2026-06-08_001_passo-lateralidade-membro-no-mapa-corporal.md` move a escolha de lado (Direito/Esquerdo/Ambos) para o PRIMEIRO passo do `RegionSelectorPopup.vue`, exclusivo de membros (sup/inf, ant/post). Botões D/E/Bilateral POR sub-região somem no fluxo de membro.

**Why:** fricção clínica — o profissional decidia o lado uma vez mas re-escolhia D/E em cada sub-região; e "Ambos" duplicava registros (2 cards D+E via `idEsquerdoDe`), poluindo a lista de regiões examinadas.

**How to apply:**
- "Ambos" agora gera UMA entrada `lateralidade: 'bilateral'` por sub-região, `regiao_id` canônico = base direita (`dirBase`). Isso MUDA o comportamento histórico de `confirmar()`. Não restaurar a expansão D+E sem novo briefing.
- Base muda por lado: Direito→`dirBase`, Esquerdo→`esquBase`, Ambos→`dirBase`. Antes `regiaoClicada` era fixado em `dirBase` no `onRegiaoClicada` de `SecaoExameFisico.vue`.
- Escopo é frontend-only: campo `lateralidade` já existe em `RegiaoAnatomicaSelecionada` e persiste via `exameFisicoService`. Demandas futuras de lateralidade no exame físico NÃO precisam de migration/backend por padrão.
- `RegionExamCard.vue` já renderiza badge "Bilateral".
- Decisões R5 (id canônico = direita) e R6 (Voltar→passo de lado, Cancelar fecha) marcadas como DEFAULT revisável.
- Reusa [[apppilltoggle-design-system]] para o seletor de lado (e quita dívida de doc em Docs/DESIGN.md).
