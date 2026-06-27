# Addendum — Upload diferido de Anexos/Fotos na consulta atual ("pending" → persiste no salvar)

## Refere-se a: 2026-06-27_002_upload-anexos-e-fotos-do-paciente.md

**ID**: 2026-06-27_002 (addendum)
**Status**: Aprovado por usuário em 2026-06-27
**Autor**: imedto-business-analyst
**Origem**: Spec gap Tipo B escalado pelo `imedto-qa` (CA1/R5 do 002 inviáveis no fluxo real)
**Estimativa de esforço adicional**: M
**Áreas regressivas tocadas**: prontuário (consulta atual / salvar evolução), seções Anexos/Fotos, drawer de detalhe

> O briefing original **permanece intocado**. Este addendum **substitui o fluxo presumido em R5/CA1** do 002 (que assumia "anexar depois de salvar a evolução, com `evolucaoId` já existente") pelo fluxo de **upload diferido**. Todas as demais regras e CAs do 002 (tipos, 2MB, redimensionamento 1600/0.8, defense-in-depth de 3 níveis, prévia read-only, whitelist Office, TTL de foto, multi-tenant, audit, consumo da regra de acesso do 001) **continuam valendo sem alteração**.

---

## 1. Por que o fluxo original não funciona (raiz do gap)

Investigação confirmou (caminhos reais):

1. **`salvarEvolucao()` finaliza a consulta** (`frontend/src/views/pacientes/ProntuarioView.vue:317-395`): captura o `evolucaoId` do `registrarEvolucao` (linhas 338-342, grava em `evolucaoIdAtual`), e em seguida chama `inicializarFormEvolucao()` (linha 386 — que **reseta `evolucaoIdAtual` para `null`**, linha 272) e seta `abaAtiva = "anteriores"` (linha 388 — **navega para fora da consulta atual**). Ou seja: assim que salva, o contexto da consulta atual é limpo e o usuário sai dela.

2. **`ProntuarioEvolucao` é imutável após criação** (sem métodos de alteração — "para corrigir, cria-se nova evolução"). Logo, **não dá** para criar a evolução como rascunho cedo e atualizar o `ConteudoJson` no momento do salvar.

3. Os componentes `SecaoAnexos.vue`/`SecaoFotosPaciente.vue` (já existem em `frontend/src/components/prontuario/secoes/`) hoje só fazem **upload imediato gated por `evolucaoId`** (`temContexto = !!pacienteId && !!evolucaoId`) e mostram o aviso **"Salve a evolução primeiro para habilitar anexos."**. Na consulta atual ainda não há `evolucaoId`, então o upload **nunca destrava** — e por isso o QA bateu Tipo A duas vezes. A raiz é de **produto/fluxo**, não de código.

## 2. Decisão do usuário (direta)

> "Tem a seção de anexo/foto na evolução do prontuário; quando ele quiser adicionar, ele usa essa parte **durante** o atendimento."

Anexar **durante o atendimento**, na própria seção da evolução em montagem. **Não** é "salvar primeiro e voltar depois".

## 3. Fluxo correto — UPLOAD DIFERIDO ("pending" → persiste no salvar)

**Durante a consulta atual (sem `evolucaoId` ainda):**
- Nas seções Anexos/Fotos, o médico **adiciona arquivos que ficam PENDENTES no front** (preview local via `object URL`/`URL.createObjectURL`). Foto já é **redimensionada a 1600/0.8 no momento da seleção** (não no salvar). Tipo e limite de 2MB são validados no front no momento da seleção (espelho do back — regras do 002).
- O médico pode **remover** um pendente antes de salvar.
- Nenhum upload ao S3 acontece nessa fase.

**Ao clicar "Salvar evolução" (`salvarEvolucao()`):**
1. Cria a evolução (`registrarEvolucao`) → obtém `evolucaoId` (já é capturado hoje, linhas 338-342).
2. **Antes de finalizar/navegar**, sobe os anexos/fotos pendentes vinculados a esse `evolucaoId`, com o `Marcador` correto, reusando `prontuarioService.uploadAnexoComMarcador(pacienteId, arquivo, "anexo" | "foto-paciente", evolucaoId)` (já existe).
3. **Só então** finaliza (`inicializarFormEvolucao()` + `abaAtiva = "anteriores"`), como hoje.

**Tratamento de erro (evolução salva, upload falha):** **não regredir a evolução** — avisar com toast `"info"`, espelhando o padrão já existente de exame físico/regiões em `ProntuarioView.vue` (linhas 365-368: `catch` → `notificar(..., "info")`, evolução permanece salva). Os object URLs dos pendentes são revogados (`URL.revokeObjectURL`) ao final, com ou sem sucesso, para não vazar memória.

**Remoção do gating obsoleto:** remover o aviso **"Salve a evolução primeiro"** e o gating de upload por `evolucaoId === null` na **consulta atual** — o motivo dele deixa de existir (os arquivos ficam pendentes, não dependem mais de `evolucaoId` prévio). Em uma evolução **já existente** (ex.: futura adição via drawer, se entrar em escopo), o upload continua imediato e gated normalmente.

**Reuso (não recriar componentes):** `SecaoAnexos.vue`/`SecaoFotosPaciente.vue` **são reaproveitados** — muda só a **orquestração**: passam a suportar uma **lista de itens pendentes** (sem id de servidor, com preview local) **além dos já persistidos**, e a coordenação do "persistir no salvar" é orquestrada por `ProntuarioView.vue` (que detém o gatilho do salvar e o `evolucaoId` recém-criado). Decisão de como a lista de pendentes transita (via `v-model`/`modelValue` da seção vs. um store/ref na view) fica como **liberdade técnica do dev**, desde que: (a) a foto seja redimensionada na seleção; (b) o pendente apareça com preview; (c) o salvar persista todos os pendentes com `evolucaoId` + `Marcador`.

## 4. Revisar evolução salva (ver/baixar) — escopo mínimo

Ao abrir uma **evolução já salva** no `EvolucaoDetalheDrawer.vue` (hoje renderiza só texto via `formatarSecaoLegivel`; já recebe `pacienteId` e `evolucao.id`; já chama `prontuarioService.listarTermosDaEvolucao` — tem o padrão de acesso pronto), o **autor** daquela evolução **ou o Dono** deve poder **ver/baixar** os anexos e fotos vinculados a ela:
- Listar via `listarAnexos(pacienteId, evolucao.id)` (filtrando `Marcador` "anexo" vs "foto-paciente"), **consumindo o gating do Briefing 001** (autor-ou-dono; quem não é autor/Dono recebe lista vazia/negação).
- Anexos: lista com download (URL assinada on-demand, descartável). Fotos: grid de thumbnails + visualizar.

**Fora de escopo deste addendum:** **adicionar** novos anexos/fotos a uma evolução **já salva** pelo drawer. O foco do usuário é adicionar **durante** o atendimento; ver/baixar depois é o complemento natural e suficiente agora. (Se surgir a necessidade de "adicionar depois", vira novo addendum/briefing.)

## 5. Regras de negócio (substituem/complementam R5 do 002)

- **R5' (substitui R5)** — **Upload diferido na consulta atual**: na consulta atual, anexos/fotos ficam **pendentes no front** (preview local; foto redimensionada a 1600/0.8 na seleção; tipo/2MB validados na seleção). Só são enviados ao S3 **no `salvarEvolucao()`**, após a criação da evolução e antes de finalizar/navegar, vinculados ao `evolucaoId` recém-criado, com `Marcador` "anexo"/"foto-paciente" via `uploadAnexoComMarcador`. Mora em: **Front (`ProntuarioView.vue` orquestra + seções gerenciam pendentes)**. *A evolução **não** é criada cedo/como rascunho (entidade imutável).*

- **R11 (novo)** — **Falha-suave de upload pós-save**: se a evolução salvar mas um ou mais uploads pendentes falharem, a evolução **não regride**; o usuário é avisado por toast `"info"` (espelha o padrão de exame físico/regiões, `ProntuarioView.vue:365-368`). Pendentes que subiram com sucesso ficam persistidos; os que falharam são reportados de forma genérica (sem PII). Object URLs revogados ao final. Mora em: **Front**.

- **R12 (novo)** — **Remoção do gating obsoleto na consulta atual**: o aviso "Salve a evolução primeiro" e o bloqueio por `evolucaoId === null` são removidos **no contexto da consulta atual** (deixam de fazer sentido). Em evolução já existente, comportamento de upload imediato/gated permanece. Mora em: **Front (`SecaoAnexos`/`SecaoFotosPaciente`)**.

- **R13 (novo)** — **Ver/baixar no drawer de evolução salva**: `EvolucaoDetalheDrawer.vue` lista (ver/baixar) os anexos/fotos da evolução via `listarAnexos(pacienteId, evolucao.id)`, **consumindo o gating autor-ou-dono do Briefing 001**. Não adiciona novos arquivos. Mora em: **Front (drawer) + Back (gating do 001)**.

## 6. Critérios de aceite (continuam do 002; CA1 reescrito; novos a partir de CA22)

- **CA1' (reescreve CA1 do 002 — upload diferido na consulta atual)**: Dado o Dr. A montando a consulta atual (sem evolução salva ainda), Quando adiciona um anexo PDF na seção Anexos, Então o arquivo **fica pendente** com preview local, **nenhuma** requisição de upload ao S3 é feita nesse momento, e **não** aparece o aviso "Salve a evolução primeiro".

- **CA22 (pendente aparece com preview antes de salvar)**: Dado o Dr. A na consulta atual, Quando seleciona uma foto na seção Fotos, Então ela é **redimensionada a 1600px/JPEG 0.8 na seleção**, aparece como **thumbnail de preview** (object URL local) na grade, sem upload ao S3.

- **CA23 (remover pendente antes de salvar)**: Dado um anexo/foto pendente ainda não salvo, Quando o Dr. A o remove, Então some da lista de pendentes, seu object URL é revogado, e ao salvar a evolução ele **não** é enviado.

- **CA24 (persistência no salvar)**: Dado o Dr. A com 1 anexo e 2 fotos pendentes, Quando clica em "Salvar evolução", Então: (1) a evolução é criada e retorna `evolucaoId`; (2) os 3 pendentes são enviados ao S3 vinculados a esse `evolucaoId`, com `Marcador` "anexo"/"foto-paciente" correto; (3) só então o form é limpo e navega para "anteriores".

- **CA25 (ordem — persistir antes de finalizar)**: Dado o salvar com pendentes, Quando a persistência dos uploads ainda não terminou, Então a finalização (`inicializarFormEvolucao` + troca de aba) **só ocorre após** a tentativa de upload dos pendentes — não antes (senão o `evolucaoId` é perdido no reset).

- **CA26 (falha-suave de upload pós-save)**: Dado que a evolução salvou mas o upload de 1 pendente falha (ex.: rede), Quando o erro ocorre, Então a evolução **permanece salva** (não regride), o usuário vê um toast `"info"` genérico (sem PII) informando que algum anexo/foto falhou, e os pendentes que subiram ficam persistidos.

- **CA27 (sem gating obsoleto)**: Dado o Dr. A na consulta atual, Quando abre as seções Anexos/Fotos, Então **não** existe o bloqueio "Salve a evolução primeiro" nem botão de upload desabilitado por `evolucaoId` null — ele pode adicionar pendentes livremente.

- **CA28 (ver/baixar no drawer — autor)**: Dado o Dr. A (autor) abrindo uma evolução salva no `EvolucaoDetalheDrawer`, Quando o drawer carrega, Então lista os anexos/fotos daquela evolução (via `listarAnexos(pacienteId, evolucao.id)`), permite baixar anexo (URL assinada descartável) e visualizar foto (thumbnail).

- **CA29 (ver/baixar no drawer — isolamento, consome 001)**: Dado o Dr. B (não-autor, não-Dono) abrindo a mesma evolução, Quando o drawer carrega (se ele sequer alcançar essa evolução — pela regra do 001 ele não a vê na timeline), Então **não** recebe os anexos/fotos do Dr. A (lista vazia/negação genérica) — o gating do 001 é a fonte da verdade.

- **CA30 (ver/baixar no drawer — Dono)**: Dado o Dono abrindo qualquer evolução salva, Quando o drawer carrega, Então vê/baixa todos os anexos/fotos daquela evolução.

- **CA31 (regressão — consulta atual do próprio médico)**: Dado o Dr. A que salvou a evolução com pendentes, Quando reabre a evolução salva, Então os anexos/fotos persistidos estão lá com `evolucaoId` correto e visíveis a ele (autor) — confirmando que o vínculo `evolucaoId` foi gravado no salvar.

- **CA32 (memória — object URLs revogados)**: Dado o ciclo de adicionar pendentes e salvar (ou cancelar/sair), Quando o fluxo termina, Então os `object URL` dos previews são revogados (`URL.revokeObjectURL`), sem vazamento de memória.

> **Permanecem válidos sem alteração** do 002: CA2-CA10 (tipos/2MB/redimensionamento/download/soft-delete/prévia/dispatcher), CA11-CA16 (defense-in-depth + isolamento + órfão Dono-ou-uploader), CA17-CA21 (multi-tenant, audit, whitelist em prod, doc viva, gate tipográfico). A **prévia read-only** (CA9) continua mostrando exemplos fictícios estáticos — pendentes só existem em modo edição, não na prévia.

## 7. Riscos e dependências

- **Risco — perder o `evolucaoId` no reset**: `inicializarFormEvolucao()` zera `evolucaoIdAtual`; se a finalização rodar antes de persistir os pendentes, o vínculo se perde. Mitigado por CA25 (ordem estrita: persistir → depois finalizar).
- **Risco — upload parcial**: alguns pendentes sobem, outros falham. Decisão: **não** transacionar/rollback (evolução imutável já existe; melhor manter o que subiu e avisar). CA26.
- **Risco — memória/preview**: muitos pendentes grandes em memória. Fotos já vêm redimensionadas (<1MB); revogar object URLs. CA32.
- **Dependência**: Briefing 001 (gating autor-ou-dono) para CA28-CA30. **Ordem mantida**: 001 → 002 (+ este addendum).
- **Sem schema novo**: `ProntuarioAnexo` já cobre (`EvolucaoId`, `Marcador`, `CriadoPorUsuarioId`). **Confirmado: não aciona `imedto-database`** por conta deste addendum.

## 8. Observações para execução

- **Reuso**: `uploadAnexoComMarcador` (já existe, assinatura `(pacienteId, arquivo, "anexo"|"foto-paciente", evolucaoId?)`); `listarAnexos(pacienteId, evolucaoId?)` (seções filtram por `Marcador`); `redimensionarImagem(arquivo,1600,0.8)`; padrão de toast `"info"` best-effort de `ProntuarioView.vue`. `EvolucaoDetalheDrawer` já tem `pacienteId` + `evolucao.id` e já consome service por evolução (`listarTermosDaEvolucao`).
- **Novo**: lista de pendentes (preview local) nas seções; orquestração "persistir pendentes no salvar" em `ProntuarioView.vue`; remoção do gating "Salve primeiro" na consulta atual; bloco de ver/baixar no drawer.
- **Não-negociável**: ordem persistir-antes-de-finalizar (CA25); falha-suave sem regredir evolução (CA26); foto redimensionada na seleção (CA22); reusar componentes existentes (não recriar).
- **Liberdade técnica**: estrutura de dados dos pendentes e como transitam entre seção e view; se o salvar persiste em série ou paralelo (respeitando que Npgsql não faz queries paralelas na mesma conexão — mas aqui são requests HTTP independentes, ok paralelizar no front com `Promise.allSettled` para capturar falhas parciais).
- **Documentação viva**: este addendum **não** muda o que o 002 já registrou para `Docs/INFRA.md`/`Docs/DESIGN.md`. Acrescentar em `Docs/DESIGN.md` (seção das seções do prontuário) uma nota curta: as seções Anexos/Fotos operam em **modo pendente** na consulta atual (upload diferido no salvar) e em **modo imediato/gated** quando há `evolucaoId`; e o `EvolucaoDetalheDrawer` exibe ver/baixar dos anexos/fotos da evolução. Mudança incremental.
