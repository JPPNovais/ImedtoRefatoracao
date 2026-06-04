# Especialidade por vínculo/estabelecimento — fallback invertido + edição inline

**ID**: 2026-06-03_004
**Status**: Aprovado por usuário em 2026-06-03
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento, prontuário (PDFs/termos), relatório (lista de equipe e seletores)

## 1. Contexto e motivação

Um profissional pode atuar em N estabelecimentos com **especialidades diferentes** em cada (ex.: "Clínico Geral" numa clínica e "Acupuntura" noutra). Hoje o sistema trata a especialidade como um atributo **global do cadastro profissional** (`profissionais.especialidade`), o que faz com que a mesma especialidade apareça em todos os estabelecimentos onde o profissional está vinculado — não reflete a realidade do multi-vínculo.

**Causa raiz confirmada** (inspeção de código):
- Já existe o campo de vínculo `vinculo_profissional_estabelecimento.especialidade_convidada` (mapeado no domínio como `VinculoProfissionalEstabelecimento.EspecialidadeConvidada`, `string`, setter `protected`, normalizado a 200 chars), preenchido no convite e na reativação como convite.
- Porém, nos dois pontos de leitura de lista o COALESCE está **invertido em relação à decisão de produto**: `COALESCE(p.especialidade, v.especialidade_convidada)` — o cadastro global vence e a especialidade do vínculo só é usada quando o global está nulo. Resultado: o que foi definido no convite por estabelecimento é ofuscado pelo cadastro global.
- Nos termos/PDFs clínicos a variável `{{profissional.especialidade}}` lê **apenas** `p.especialidade` (cadastro global), ignorando o vínculo por completo.
- Não há, hoje, forma de **editar** a especialidade do vínculo sem refazer o convite (reconvite) — atrito operacional para o Dono que precisa corrigir/ajustar.

Evidência (linhas confirmadas em código no momento do briefing):
- `VinculoQueryRepository.cs:42` — lista interna de equipe: `COALESCE(p.especialidade, v.especialidade_convidada)`.
- `VinculoQueryRepository.cs:101` — lista pública/seletores (DTO minimizado LGPD): `COALESCE(p.especialidade, v.especialidade_convidada)`.
- `TermoResolverDeVariaveis.cs:130` — resolver de variáveis dos termos/PDFs: lê só `p.especialidade`.

## 2. Decisão de produto registrada

**A especialidade é um atributo do vínculo (profissional × estabelecimento), com fallback para o cadastro global.**

- A regra canônica de leitura passa a ser: **especialidade efetiva = `COALESCE(v.especialidade_convidada, p.especialidade)`** — o que está no vínculo vence; se o vínculo não tiver especialidade definida, cai para o cadastro global do profissional.
- A especialidade do vínculo vale **em todos os lugares** onde a especialidade do profissional é exibida ou impressa **no contexto daquele estabelecimento**: lista interna de equipe, seletores (agenda/prontuário/orçamento) e **PDFs/termos clínicos** (decisão B1).
- O **Dono do estabelecimento** pode **editar continuamente** a especialidade do vínculo direto no modal de detalhes do profissional, sem refazer o convite (decisão A2). Grava em `v.especialidade_convidada`.
- O **Dono** (linha sintética, sem `vinculoId`) não tem vínculo formal e portanto **não tem especialidade de vínculo editável** — para ele a especialidade segue sendo o cadastro global (`p.especialidade`), comportamento mantido sem alteração.

## 3. Persona-alvo

Dono do estabelecimento, na gestão de equipe (tela de Equipe → modal de detalhes do profissional). Uso pontual/eventual (ao cadastrar ou corrigir a atuação de um profissional na clínica). Leitura impacta também recepção/profissional (lista e seletores) e qualquer geração de termo/PDF clínico.

## 4. Escopo

**Inclui**:
- Inverter o COALESCE para `COALESCE(v.especialidade_convidada, p.especialidade)` em:
  - `VinculoQueryRepository.cs:42` (lista interna de equipe — `ListarProfissionaisDoEstabelecimento`).
  - `VinculoQueryRepository.cs:101` (lista pública/seletores — `ListarProfissionaisPublicoDoEstabelecimento`).
- Em `TermoResolverDeVariaveis.cs` (~linha 130, query do `ProfissionalResolver`): a variável `{{profissional.especialidade}}` passa a usar `COALESCE(v.especialidade_convidada, p.especialidade)` — o JOIN com `vinculo_profissional_estabelecimento` (filtrado por `estabelecimento_id` e `status='Ativo'`) **já existe** nessa query, então `v.especialidade_convidada` está disponível sem novo JOIN.
- Edição inline da especialidade do vínculo no `ProfissionalDetalhesModal.vue`, gravando em `v.especialidade_convidada`:
  - **Domínio**: novo método mutador em `VinculoProfissionalEstabelecimento` (ex.: `AtualizarEspecialidade(string especialidade)`) que normaliza a 200 chars (reusar `NormalizarTexto`) e seta `EspecialidadeConvidada`. Permitir valor vazio/nulo (limpar → volta a usar o fallback global).
  - **Command + Handler**: novo `AlterarEspecialidadeDoVinculoCommand` + handler, espelhando 1:1 o padrão de `AlterarModeloPermissaoDoVinculoCommandHandler` (busca falha-fechada por `ObterPorIdNoEstabelecimentoOuNulo(vinculoId, estabelecimentoId)`, RBAC Dono via `estab.DonoUsuarioId != UsuarioSolicitanteId`, `Salvar`).
  - **Endpoint**: novo `PUT /api/estabelecimento/profissionais/{vinculoId:long}/especialidade`, espelhando o `PUT .../{vinculoId}/modelo-permissao` existente em `ModeloPermissaoController.cs:90` (ou controller equivalente de vínculo).
  - **Front**: service (`vinculoService` ou equivalente) + campo editável no modal, visível/habilitado **apenas para o Dono** e **apenas para linhas com `vinculoId != null`** (oculto para a linha sintética do próprio Dono).

**Não inclui**:
- Migration de schema. **Confirmado: nenhuma migration necessária** — `vinculo_profissional_estabelecimento.especialidade_convidada` já existe e está mapeado no domínio. Só haverá novo método de domínio + command/handler + endpoint + UI.
- Alterar a especialidade global do cadastro profissional (`profissionais.especialidade`) — fora de escopo; permanece como fallback.
- Especialidade editável para a linha do Dono (ele não tem vínculo formal).
- Histórico/versionamento de mudanças de especialidade do vínculo (não pedido).

## 5. Regras de negócio

- **R1 — Fallback canônico**: especialidade efetiva = `COALESCE(v.especialidade_convidada, p.especialidade)`. Mora em: Query (Dapper) nos 3 pontos listados. Validada em: back (resultado da query) + front (exibe o que o back devolver, sem recalcular).
- **R2 — RBAC de edição**: somente o Dono do estabelecimento edita a especialidade do vínculo. Mora em: Handler (espelha `AlterarModeloPermissaoDoVinculoCommandHandler` — `estab.DonoUsuarioId != command.UsuarioSolicitanteId` → `BusinessException`). Validada em: back (422/erro de negócio) + front (campo oculto/desabilitado quando não-Dono, reusando o computed `ehDono`/guarda de permissão já existente no modal).
- **R3 — Multi-tenant**: o vínculo é buscado por `ObterPorIdNoEstabelecimentoOuNulo(vinculoId, estabelecimentoId)`; vínculo de outro tenant → "Vínculo não encontrado." (mensagem genérica). Mora em: Handler/Repositório (falha-fechada). Validada em: back.
- **R4 — Normalização**: especialidade normalizada a 200 chars (reusar `NormalizarTexto`); string vazia → nulo (limpa o campo e faz cair no fallback global). Mora em: Domain (`VinculoProfissionalEstabelecimento.AtualizarEspecialidade`). Validada em: back.
- **R5 — Dono não tem vínculo editável**: linha com `vinculoId == null` (Dono sintético) não expõe o campo editável. Mora em: Front (guarda `vinculoId != null`, mesmo padrão de `atribuirAoVinculo`/`podeReativar`). Validada em: front.

## 6. Modelo de dados

- Tabela: `vinculo_profissional_estabelecimento`. Coluna usada: `especialidade_convidada` (**já existe**, sem alteração de schema).
- Vínculo multi-tenant: toda leitura/escrita filtra `estabelecimento_id`.
- LGPD: especialidade é dado profissional/operacional (não é dado sensível de saúde de paciente). Não há PII de paciente envolvida. Audit dedicado não é obrigatório (ver R6 de CA LGPD); seguir o padrão da edição de modelo de permissão (que não cria audit dedicado).

## 7. UX e fluxo

Modal `ProfissionalDetalhesModal.vue`, aba/seção de dados do profissional:
- Hoje a especialidade aparece como leitura (`profissional.especialidade`) no cabeçalho (`.ph-spec`) e no bloco "Dado" (label "Especialidade").
- Adicionar, **para o Dono e quando `vinculoId != null`**, um campo editável de especialidade (input de texto, design system `AppInput` ou equivalente já usado no modal), com botão Salvar reaproveitando o padrão de `atribuirAoVinculo` (loading no botão, fechamento/refresh otimista).
- Estados: loading no salvar; erro → mensagem genérica do back; campo vazio é válido (limpa → cai pro fallback global, exibição volta a mostrar `p.especialidade`).
- Para não-Dono e para a linha do Dono sintético: campo permanece **somente leitura** (comportamento atual).
- Mobile-ready: reusar layout responsivo já existente no modal.

## 8. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — leitura com vínculo)**: Dado um profissional com `especialidade_convidada = "Acupuntura"` no estabelecimento E e `profissionais.especialidade = "Clínico Geral"`, Quando a lista interna de equipe de E é carregada, Então a especialidade exibida para esse profissional é "Acupuntura".

- **CA2 (fallback / regressão)**: Dado um profissional com `especialidade_convidada` NULL e `profissionais.especialidade = "Clínico Geral"`, Quando a lista interna e a lista pública/seletores de E são carregadas, Então a especialidade exibida é "Clínico Geral" (fallback ao cadastro global preservado).

- **CA3 (lista pública/seletores)**: Dado o cenário do CA1, Quando um seletor de profissional (agenda/prontuário/orçamento) consome `ListarProfissionaisPublicoDoEstabelecimento` de E, Então a especialidade exibida é a do vínculo ("Acupuntura"), e o DTO continua minimizado (sem e-mail, sem modelo de permissão, sem vinculoId).

- **CA4 (PDF/termo clínico — decisão B1)**: Dado um termo clínico gerado no estabelecimento E para um profissional com `especialidade_convidada = "Acupuntura"`, Quando `{{profissional.especialidade}}` é resolvido, Então o valor impresso é "Acupuntura"; e Dado `especialidade_convidada` NULL, Então imprime o cadastro global.

- **CA5 (edição inline — caminho feliz)**: Dado o Dono de E no modal de um profissional vinculado (`vinculoId != null`), Quando ele informa "Dermatologia" e salva, Então `PUT /api/estabelecimento/profissionais/{vinculoId}/especialidade` retorna sucesso, `v.especialidade_convidada` passa a "Dermatologia", e a lista/seletores/PDF de E passam a exibir "Dermatologia".

- **CA6 (edição — limpar campo)**: Dado um vínculo com `especialidade_convidada = "Dermatologia"`, Quando o Dono salva o campo vazio, Então `especialidade_convidada` fica NULL e a exibição volta a mostrar `profissionais.especialidade` (fallback).

- **CA7 (RBAC)**: Dado um usuário com papel diferente de Dono em E (ex.: Recepção ou Profissional), Quando ele chama `PUT .../{vinculoId}/especialidade`, Então recebe erro de negócio (422 `BusinessException` "Apenas o dono...") e, no front, o campo editável de especialidade fica oculto/desabilitado (somente leitura).

- **CA8 (multi-tenant)**: Dado o Dono do estabelecimento B, Quando ele chama `PUT .../{vinculoId}/especialidade` com um `vinculoId` pertencente ao estabelecimento A, Então recebe "Vínculo não encontrado." (mensagem genérica), nada é alterado, e nenhum dado do tenant A é revelado.

- **CA9 (estado — Dono sintético)**: Dado a linha do próprio Dono (status="Dono", `vinculoId == null`) no modal, Quando ela é exibida, Então o campo editável de especialidade não aparece (somente leitura do cadastro global), pois o Dono não tem vínculo formal.

- **CA10 (estado — vínculo convidado/inativo)**: Dado um vínculo em status "Convidado" e outro em "Inativo" em E, Quando o Dono abre o modal de cada um, Então a edição de especialidade continua disponível ao Dono (a especialidade é atributo do vínculo, independente do status), e a leitura na lista interna reflete `COALESCE(v.especialidade_convidada, p.especialidade)`.

- **CA11 (LGPD — mensagem genérica)**: Dado qualquer erro de validação/autorização no endpoint de especialidade, Quando o back responde, Então a mensagem é genérica e não contém PII (nome, e-mail, registro) nem revela existência de vínculo de outro tenant.

- **CA12 (normalização)**: Dado o Dono salvando uma especialidade com mais de 200 caracteres ou com espaços nas pontas, Quando o domínio processa, Então o valor é truncado/normalizado a 200 chars (reuso de `NormalizarTexto`), consistente com o comportamento do convite.

## 9. Riscos e dependências

- **Regressão de exibição**: inverter o COALESCE muda o valor exibido para todo profissional que tenha `especialidade_convidada` preenchida E `profissionais.especialidade` também preenchida. Antes o global vencia; agora o vínculo vence. Isso é intencional (decisão de produto), mas é mudança observável em telas existentes — QA deve validar que profissionais com ambos preenchidos passam a mostrar o do vínculo.
- **PDFs/termos clínicos**: mudança em documento impresso — validar visualmente que a especialidade impressa corresponde ao vínculo do tenant do termo.
- **Defense-in-depth no TermoResolver**: o JOIN com `vinculo ... status='Ativo'` já existe; confirmar que para termo de profissional sem vínculo Ativo no tenant o `ProfissionalResolver` continua nulo (comportamento atual preservado).
- **Dependência de padrão**: edição inline depende de espelhar fielmente `AlterarModeloPermissaoDoVinculoCommandHandler` (RBAC + multi-tenant + falha-fechada).

## 10. Observações para execução

- **Não-negociável**: RBAC Dono e multi-tenant falha-fechada idênticos ao handler de modelo de permissão; mensagens genéricas; espelho back+front da trava de Dono.
- **Reuso obrigatório**: `NormalizarTexto` no domínio; padrão de command/handler/endpoint de `AlterarModeloPermissao*`; no front reusar o computed `ehDono` e a guarda `vinculoId != null` já existentes no modal; design system (`AppInput`/`AppButton`) — nada de HTML/CSS novo se já houver componente.
- **Liberdade técnica**: nome exato do método de domínio, do command e da rota (sugeridos acima como `AtualizarEspecialidade` / `AlterarEspecialidadeDoVinculoCommand` / `.../{vinculoId}/especialidade`); dev pode ajustar para coerência com convenções vigentes.
- **Sem migration**: confirmar com `imedto-database` apenas se houver dúvida; coluna `especialidade_convidada` já existe e está mapeada — não acionar o agente de DB salvo imprevisto.

## 11. Atualização de documentação

- **`Docs/ARQUITETURA.md`** (ou doc de regras de domínio de Vínculos/Equipe, se existir): registrar a **regra cross-cutting** "especialidade efetiva do profissional é por vínculo/estabelecimento, com fallback para o cadastro global: `COALESCE(v.especialidade_convidada, p.especialidade)`", listando os pontos canônicos de leitura (lista interna, lista pública/seletores, resolver de termos) para evitar regressão futura ao COALESCE invertido. Mudança **incremental/cirúrgica** — adicionar nota na seção de Vínculos/Equipe, sem reescrever o doc.
- **`Docs/LGPD.md`**: não requer atualização — especialidade é dado profissional/operacional, sem novo tipo de PII de paciente nem novo endpoint expondo PII.
- Demais docs (`DESIGN.md`, `INFRA.md`, `COMANDOS.md`): sem alteração — a UI reusa componentes existentes do design system, não há recurso de infra novo nem comando novo.
