# Addendum — Redução de escopo: Expectativa fora desta entrega

## Refere-se a: 2026-06-05_001_pool-variaveis-autocomplete-prontuario.md

**ID**: 2026-06-05_001 (addendum)
**Status**: Aprovado pelo dono do produto em 2026-06-05 (decisão de redução de escopo na revisão do briefing)
**Documento imutável** — o briefing original NÃO é editado. Próximas mudanças entram como `-addendum-2.md`.
**Autor**: imedto-business-analyst

---

## Motivo da mudança

Na revisão do briefing original, apurou-se que **não existe campo de Expectativa no prontuário hoje**: o campo de Procedimentos (`procedimentos[].descricao` em `SecaoProcedimentosIndicados.vue`) é "procedimento indicado" — conceito clínico distinto de "o que o paciente espera do tratamento". Criar uma subseção nova de Expectativa exigiria uma decisão de produto sobre o campo (formato, comportamento, persistência) que o dono prefere tomar depois, com mais informação.

Decisão do dono: **tirar Expectativa do escopo desta entrega.** Cabear apenas os 5 tipos que já têm campo correspondente no prontuário. Expectativa permanece como tipo válido do enum para uso futuro, mas sem cableamento de campo agora.

Esta entrega passa a ser, portanto, mais simples e cirúrgica: zero mudança em Procedimentos.

---

## O que muda (sobrescreve os pontos correspondentes do briefing original)

1. **Tipos efetivamente cabeados nesta entrega = 5**: Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar. Cada um recebe autocomplete + criação automática ao salvar evolução, exatamente como descrito no briefing original. **Expectativa NÃO é cabeada** (sem autocomplete, sem criação automática). Sobrescreve o "6 tipos vinculados a campo do prontuário" do §3 (Escopo → Inclui).

2. **CANCELADO o item de §6 (UX e fluxo) "Procedimentos / Expectativa"** que mandava criar a subseção "Expectativas do paciente" em `SecaoProcedimentosIndicados.vue`. **NENHUMA mudança em Procedimentos nesta entrega.** O campo `procedimentos[].descricao` continua exatamente como está: texto livre, sem autocomplete, sem criação de pool. Nenhuma chave nova (`expectativas[]` ou equivalente) é criada no `ConteudoJson`. O dev/QA ignoram qualquer instrução do §6 relativa a Expectativa.

3. **CANCELADO o CA15** ("Expectativa — subseção em Procedimentos") do §7. O dev não implementa e o QA não valida o CA15. Permanece registrado no briefing original apenas por imutabilidade — está revogado por este addendum.

4. **Ajuste do §4 (Mapeamento tipo → seção → campo)**: a linha de **Expectativa** passa a valer como **"FORA DESTA ENTREGA — sem campo definido ainda"**. As 5 linhas restantes (Alergia, Medicamento, Cirurgia, Doenca, RelacaoFamiliar) permanecem válidas e inalteradas. A "Observação técnica para o dev" logo abaixo da tabela do §4 (distinção entre `procedimentos[].descricao` e Expectativa) permanece útil como contexto, mas a ação dela (criar campo de Expectativa) está cancelada pelo item 2.

5. **Expectativa PERMANECE no enum `TipoVariavelPool`.** NÃO é removida. Diferente de `Droga` e `AtividadeFisica` (que continuam sendo removidos do enum, do banco e do admin, conforme o briefing original §3/§5/CA12/CA13), Expectativa segue sendo um **tipo válido** — apenas sem campo no prontuário por enquanto. Consequência direta:
   - A lista de tipos VÁLIDOS após a entrega continua sendo **6**: Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa.
   - O §9 "Os 6 tipos válidos finais" permanece correto e inalterado.
   - **CA13 permanece válido sem alteração**: a mensagem 422 "Tipo inválido" continua listando esses mesmos 6 tipos. Expectativa segue aceito por API/admin como tipo, mesmo sem campo de prontuário.
   - Apenas `Droga` e `AtividadeFisica` saem do enum/banco/admin (CA12 e CA13 inalterados quanto a esses dois).

6. **Handler de extração ao salvar evolução varre apenas os 5 tipos cabeados** (Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar). **NÃO procura chave de Expectativa** — ela não existe no `ConteudoJson`. Sobrescreve a parte do §6 ("Fluxo de salvar") e da R3/§8 que pressupunha extração de uma chave de Expectativa.

---

## O que permanece válido e inalterado (não reescrever)

Todo o restante do briefing original continua valendo integralmente:

- Criação automática de itens do pool **do estabelecimento** ao salvar evolução, para valores inéditos dos 5 campos cabeados.
- Permissão dupla: criação automática via evolução para qualquer profissional (sem `ModelosProntuario`); CRUD manual continua exigindo `ModelosProntuario`.
- Dedup robusta (trim + case-insensitive + insensível a acento) reusando padrão-sistema sem copiar para o estabelecimento; mesma normalização no CRUD manual (R5).
- Isolamento multi-tenant dos itens não-padrão.
- LGPD: só `nome` vira item; campos livres (`observacao`, `dose`, `frequencia`, `motivo`, `ano`, `observacoes`, `comentario`, `doencas` dos parentes) jamais.
- Remoção de `Droga` e `AtividadeFisica` do enum/banco/admin (migration idempotente).
- Componente de autocomplete novo (typeahead por string), reusando `variavelPoolService.listar` e `VariavelPoolQueryRepository.Listar`.
- RelacaoFamiliar na História familiar (semeadura dos parentescos fixos como padrão-sistema — handoff ao admin).
- **CAs 1–14 e 16 permanecem válidos e devem ser validados pelo QA.** Apenas o **CA15 está cancelado** (item 3).
- Atualização de documentação (§10): `Docs/ARQUITETURA.md`, `Docs/DESIGN.md`, `Docs/LGPD.md` continuam sendo atualizados no mesmo ciclo — apenas sem qualquer menção a campo/subseção de Expectativa no prontuário. Expectativa segue mencionada apenas como tipo válido do enum, sem campo.

---

## Resumo executável para dev/QA

- Cabear: **Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar** (5).
- Não tocar em Procedimentos. Não criar subseção de Expectativa. Ignorar CA15.
- Remover do enum/banco/admin: **só Droga e AtividadeFisica**.
- Tipos válidos finais (enum / mensagem 422): **6** (os 5 cabeados + Expectativa, esta sem campo).
- CAs ativos: **CA1–CA14 e CA16**. CA15 cancelado.
