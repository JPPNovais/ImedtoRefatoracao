---
name: pool-variaveis-vinculo-prontuario
description: Como o pool de variáveis se conecta (ou não) aos campos do prontuário, dedup, escopo e os 2 tipos removidos
metadata:
  type: project
---

Vínculo pool ↔ prontuário especificado no briefing `planejamentos/2026-06-05_001_pool-variaveis-autocomplete-prontuario.md` + addendum `..._-addendum.md` (redução de escopo 2026-06-05: Expectativa fora desta entrega).

**Estado pré-briefing**: o pool (`ProntuarioVariavelPool` / tabela `prontuario_variaveis_pool`) tinha CRUD completo mas NÃO estava conectado a nada — campos do prontuário eram `AppInput` de texto puro.

**Decisões fechadas com o dono:**
- Criação de item do pool acontece AO SALVAR A EVOLUÇÃO (extração no `RegistrarEvolucaoCommandHandler`), transacional. Qualquer profissional do estabelecimento gera item assim — NÃO exige `PermissoesExtras.ModelosProntuario`. O CRUD manual (`ListasVariaveisTab`, só Dono) continua exigindo `ModelosProntuario`. Duas portas separadas.
- Dedup canônica: trim + case-insensitive + insensível a acento. Reusa padrão-sistema (eh_padrao_sistema=true, estabelecimento_id NULL) sem copiar para o estabelecimento. Itens não-padrão são exclusivos por estabelecimento_id.
- LGPD: só o campo `nome` vira item do pool. Campos livres (observacao, dose, frequencia, motivo, ano, observacoes, comentario) NUNCA viram pool — podem conter PII.

**Mapeamento tipo → campo (5 CABEADOS nesta entrega após addendum)**: Alergia/Medicamento/Cirurgia/Doenca → HPP (`SecaoHistoriaPregressa.vue`, campo `nome`); RelacaoFamiliar → `SecaoHistoriaFamiliar.vue` campo `parentes[].parentesco` (era AppSelect fixo `PARENTESCOS`). **Expectativa FORA desta entrega** (addendum 2026-06-05): NÃO existe campo de Expectativa hoje (`procedimentos[].descricao` é procedimento indicado, conceito distinto). NÃO criar subseção; NÃO tocar em Procedimentos; CA15 cancelado; handler de extração NÃO procura chave de Expectativa. O dono decide o campo depois.

**REMOVIDOS** (do enum, banco, admin, front): SÓ `Droga` e `AtividadeFisica` — não tinham campo no prontuário. **Expectativa NÃO é removida** — permanece tipo válido do enum para uso futuro, apenas sem campo no prontuário. Tipos válidos finais (enum / mensagem 422 CA13): 6 = Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa. Cabeados de fato: 5 (sem Expectativa). CUIDADO: a HISTÓRIA SOCIAL tem campos `atividadeFisicaNivel/Obs` e drogas que são texto livre da anamnese — NÃO são pool, não tocar.

**Notas técnicas**: `tipo` é armazenado como string (HasConversion<string>, max 20) → remover enum não exige mudança de coluna, só DELETE das linhas. `ExisteOutraComMesmoNome` hoje só faz ToLower e só compara dentro do mesmo estabelecimento (ignora padrão-sistema) — precisa evoluir. `AppSelectComCriacao.vue` é select por ID, NÃO serve como autocomplete (campos guardam string `nome`); precisa de typeahead novo. Listagem do autocomplete reusa `VariavelPoolQueryRepository.Listar` (já filtra padrão OR tenant + tipo + ativo).
