# Central de Migração — Addendum 7: resolução de referências internas, campos compostos e matching tolerante de nome

**ID**: 2026-06-16_001
**Status**: Aprovado por usuário em 2026-06-16
**Autor**: imedto-business-analyst (investigação) + orquestrador (redação com decisões fechadas)
**Refere-se a**: `2026-06-15_001_central-de-migracao-mapeamento-por-ia.md` (e addendums _002 a _006)
**Tipo**: Gap incremental de mapeamento (caso não previsto) — não altera o escopo do épico
**Numeração de CAs**: a partir de **CA119** (addendum-6 vai até CA118)

---

## 1. Motivação (evidência de produção — Job #15, fase de testes)

O Job #15 (arquivo de um sistema clínico real) migrou **15/15 pacientes**, mas **os 7 agendamentos foram todos pulados**. Diagnóstico confirmado nos payloads materializados e no código:

1. **Referência por ID interno (5 agendamentos — "dados de agendamento insuficientes"):** no arquivo de origem, cada agendamento referencia paciente e profissional por **ID interno do sistema de origem** (`paciente_id`, `profissional_id`), não por nome. O prompt da IA instrui explicitamente a **ignorar IDs internos** (`AnthropicMapeadorDeMigracao.cs:277`), e a inferência roda **um bloco por vez** (`InferirMapaMigracaoJobHandler.cs:341`) — a IA nem enxerga o bloco `pacientes[]` ao mapear `agendamentos[]`. A carga (`CarregarOnda1JobHandler.ProcessarAgendamentoAsync`) resolve paciente/profissional **por nome** (`paciente_nome`, `profissional_nome`); sem esses campos → `MarcarPulado("dados de agendamento insuficientes")`.

2. **Campo composto data+hora (colisão silenciosa):** `AplicarDePara` (`MaterializarRegistrosCommandHandler.cs:258-275`) é um loop 1:1. Quando dois campos de origem (`data` e `hora`) mapeiam para o mesmo destino `data_hora`, a segunda escrita sobrescreve a primeira — sobra só a hora (`"08:00"`, sem a data).

3. **Matching de nome exato (2 agendamentos — "paciente não encontrado"):** os lookups comparam `LOWER(nome_completo) = LOWER(@Nome)` (`DapperPacienteMigracaoLookup.cs:63`) — exato, normaliza só caixa. Variações de acento/espaço não batem.

**Fato técnico decisivo:** `MaterializarRegistrosCommandHandler` já carrega **todos os blocos do arquivo em memória** antes de aplicar o de-para (`CarregarBlocosPorArquivoAsync`, ~`:84`/`:128-165`). Logo, o bloco-fonte (`pacientes[].id → nome`) está disponível no exato ponto onde a linha de agendamento é processada. A resolução de referência é **determinística e barata** ali — não exige tocar a IA.

---

## 2. Decisões de produto (fechadas)

| # | Decisão | Escolha |
|---|---------|---------|
| i | **Profissionais entram no escopo de criação?** | **NÃO.** A migração não cria profissionais (evita tocar auth/RBAC). O agendamento resolve o profissional **por nome contra os profissionais já cadastrados com vínculo ativo no tenant**; se não houver, pula com motivo claro. *(Confirmado pelo usuário em 2026-06-16.)* |
| ii | **Referência interna não resolvida no arquivo?** | **Pula o registro** com motivo específico ("referência não resolvida no arquivo"), distinto de "dados insuficientes". Não derruba o job. |
| iii | **Matching de nome: exato ou tolerante?** | **Tolerante determinístico**: compara sobre forma normalizada (sem acento via `imutable_unaccent`, espaços colapsados, `trim`, caixa unificada). **Igualdade**, não fuzzy/trigram — nunca vincular ao paciente errado é premissa LGPD/clínica. Ambiguidade (2+) ainda pula. |
| iv | **Onde resolver a referência interna?** | **Na materialização**, passo determinístico **após `AplicarDePara`** (heurística de convenção `*_id` → bloco correspondente do arquivo). A IA permanece intocada. O mesmo passo trata o **campo composto** (`data`+`hora` → `data_hora` concatenado). |

**Infra confirmada:** extensão `unaccent`, função `imutable_unaccent` e índice trigram `ix_pacientes_nome_completo_trgm` **já existem** no Postgres da EC2. O matching tolerante **não exige migration nova** para o unaccent. Acionar `imedto-database` apenas se a igualdade sobre forma normalizada exigir um índice btree de expressão para performance em arquivos grandes.

---

## 3. Escopo da mudança

**A. Materialização (`MaterializarRegistrosCommandHandler`):**
- Novo passo determinístico após `AplicarDePara`, com acesso a todos os blocos do arquivo:
  - **Resolução de referência interna:** para colunas de origem terminadas em `*_id` cujo prefixo casa com um bloco do arquivo (`paciente_id` → bloco `pacientes`/`paciente`), cruzar pelo `id` interno (usado **só em memória**, nunca persistido como chave) e injetar o `nome` resolvido no campo canônico correspondente (`paciente_nome`, `profissional_nome`).
  - **Combinação de campos compostos:** quando dois (ou mais) campos de origem mapeiam ao mesmo destino temporal, concatenar na ordem semântica (`"{data} {hora}"`) em vez de colidir.
- O ID de origem **nunca** é persistido como chave de negócio (preserva R-S9/D11 do épico).

**B. Carga (`CarregarOnda1JobHandler` + lookups Dapper):**
- Matching tolerante determinístico de paciente/profissional por nome (normalização nos dois lados).
- Motivos de rejeição/pulo específicos e sem PII (ver CAs).

**Fora de escopo (não-objetivos):** criar profissionais/contas/vínculos; matching fuzzy/trigram; mudar o prompt da IA ou o design de 1 chamada/bloco da inferência; migrar entidades novas.

---

## 4. Critérios de aceite (Dado / Quando / Então)

- **CA119 (resolução ID→nome dentro do arquivo):** Dado um arquivo com `agendamentos[]` cujas linhas têm `paciente_id` e um bloco `pacientes[]` com `id`+`nome`, Quando a materialização processa o agendamento, Então o payload canônico recebe `paciente_nome` = o `nome` da linha de `pacientes` cujo `id == agendamento.paciente_id`, e a carga vincula o agendamento ao paciente. O `id` interno nunca é persistido como chave.

- **CA120 (referência não resolvida → pula com motivo específico):** Dado um agendamento com `paciente_id` sem correspondente em `pacientes[]`, Quando materializa/carrega, Então o registro é **pulado** com motivo "referência de paciente não resolvida no arquivo" (distinto de "dados insuficientes"), o job **não falha**, e a linha consta no relatório.

- **CA121 (campo composto data+hora):** Dado dois campos de origem (`data`, `hora`) mapeados ambos para `data_hora`, Quando materializa, Então `data_hora` = `"{data} {hora}"` (não a colisão), e a carga parseia para um `DateTime` válido.

- **CA122 (composto com parte ausente):** Dado um campo composto onde só `hora` existe e `data` está vazia/ausente, Quando materializa, Então o agendamento é pulado com motivo "data/hora incompleta" — não grava `data_hora` inválido.

- **CA123 (tolerância determinística de nome):** Dado `paciente_nome` = "João da Silva" (acento, espaços extras) e um paciente cadastrado como "Joao da  Silva", Quando resolve por nome, Então o paciente é encontrado (comparação sobre forma normalizada: sem acento, espaços colapsados, caixa unificada).

- **CA124 (ambiguidade ainda pula):** Dado dois pacientes no tenant cujo nome normalizado é idêntico, Quando resolve por nome, Então retorna ambíguo → pula com motivo "paciente ambíguo por nome", **nunca** vincula ao primeiro arbitrariamente.

- **CA125 (profissional fora de escopo — resolve só existentes):** Dado um agendamento cujo `profissional_nome` foi resolvido mas **não existe** profissional com esse nome via vínculo ativo no tenant, Quando carrega, Então o agendamento é pulado com motivo "profissional não encontrado no estabelecimento" — nenhum profissional é criado, nenhum padrão é atribuído.

- **CA126 (profissional existente vincula):** Dado um agendamento cujo `profissional_nome` (resolvido + normalizado) casa com um profissional de vínculo ativo no tenant, Quando carrega, Então o agendamento é criado e vinculado a esse `profissional_usuario_id`.

- **CA127 (multi-tenant):** Dado o job do estabelecimento A, Quando resolve paciente/profissional por nome, Então a busca filtra `estabelecimento_id = A` (e vínculo ativo, para profissional); homônimo no estabelecimento B nunca é vinculado.

- **CA128 (sem PII em log):** Dado qualquer falha de resolução, Quando o motivo é registrado, Então a mensagem é genérica/categórica e **não contém** nome/CPF/telefone (espelha o padrão de `MarcarPulado`).

- **CA129 (não regride o caminho com nome direto):** Dado um arquivo cujo agendamento já traz `paciente_nome`/`profissional_nome` diretamente, Quando materializa, Então o comportamento atual é preservado — a resolução de referência só atua quando o nome direto está ausente.

- **CA130 (regressão e2e — Job #15 verde):** Dado o arquivo do Job #15 (5 com `*_id`, 2 com nome variante, composto data+hora), Quando reprocessa do zero, Então nenhum agendamento cai mais em "dados de agendamento insuficientes" por referência não resolvida; os que têm paciente+profissional resolvíveis migram, e os demais pulam com motivo **específico** (não genérico) verificável.

---

## 5. Documentação viva a atualizar (mesma entrega)

- **`Docs/ARQUITETURA.md`** (bounded context de Migração): documentar o **passo de resolução determinística de referências internas na materialização** (cruzamento ID-origem→nome em memória, nunca persistido como chave) e a combinação de campos compostos. Padrão novo do pipeline.
- **`Docs/INFRA.md` / `imedto-database`**: somente se a igualdade sobre forma normalizada exigir índice btree de expressão (`imutable_unaccent(lower(nome_completo))`). `unaccent`/`imutable_unaccent` já existem — sem migration para a extensão.
- **`Docs/LGPD.md`**: sem mudança esperada (mantém mensagens genéricas, sem PII). Revisitar só se algum motivo novo precisar de redação.

---

## 6. Validação (premissa não-negociável — CLAUDE.md §Regras dos Pipelines)

Smoke **local** obrigatório **antes** do push: subir backend local (`./dev.sh`), reprocessar o arquivo do Job #15 isoladamente (parar o backend de prod durante o teste, fase de testes permite) e confirmar nos logs **locais** que os agendamentos migram/pulam com os motivos esperados. Validar em produção pós-deploy é proibido como etapa de validação.
