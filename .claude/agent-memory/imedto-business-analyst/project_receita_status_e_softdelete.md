---
name: receita-status-e-softdelete
description: Modelo de status de Receita e infra de soft-delete já existente — base para descarte de rascunho vs cancelamento de emitida.
metadata:
  type: project
---

Receita tem 4 status (StatusReceita.cs): Rascunho, Emitida, Cancelada, Substituida.

Distinção de produto (definida pelo dono em 2026-06-03):
- RASCUNHO = documento que nunca existiu clinicamente → deve ser DESCARTÁVEL.
- EMITIDA = documento clínico real → só CANCELÁVEL (vira Cancelada, nada se apaga; retificação, não exclusão).

Regra clínica imutável: `Receita.Cancelar(motivo)` (Receita.cs:267-280) só aceita Status==Emitida. NÃO afrouxar.

Infra de soft-delete JÁ EXISTE e está pronta para reuso (não precisa de imedto-database):
- Domain `Receita.MarcarComoDeletado(Guid usuarioId)` (Receita.cs:299-307) — seta DeletadoEm + DeletadoPorUsuarioId, idempotência (throws se já deletada).
- Colunas `deletado_em` / `deletado_por_usuario_id` já existem na tabela.
- Read-side `ReceitaQueryRepository` já filtra `deletado_em IS NULL` AND `estabelecimento_id` em todas as queries → soft-deleted some da lista automaticamente, multi-tenant safe.

Lacuna (em 2026-06-03): não há endpoint nem método de service `descartar` expondo o soft-delete. Front reusa botão "Cancelar receita" no rodapé do rascunho (ReceitasPacienteTab.vue:779-780) → erro "Apenas receitas emitidas podem ser canceladas" + acúmulo de rascunhos vazios.

**Why:** entender a separação descarte (rascunho) vs cancelamento (emitida) é a chave de qualquer demanda futura em Receitas.
**How to apply:** descarte de rascunho deve usar MarcarComoDeletado (soft-delete), não Cancelar. Cancelar é só para Emitida.
