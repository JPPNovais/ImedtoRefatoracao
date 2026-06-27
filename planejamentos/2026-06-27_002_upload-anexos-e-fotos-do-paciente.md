# Upload de Anexos e Fotos do paciente nas seções do prontuário (com segurança de arquivos em 3 níveis)

**ID**: 2026-06-27_002
**Status**: Aprovado por usuário em 2026-06-27
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: prontuário (seções/builder), anexos, storage S3, infra (whitelist MIME + TTL de foto), LGPD

> **Depende de** `2026-06-27_001_confidencialidade-evolucao-autor-ou-dono.md` — este briefing **consome** a regra de acesso autor-ou-dono lá definida (anexos/fotos só vistos pelo autor da evolução vinculada ou pelo Dono). Implementar o 001 primeiro.

---

## 1. Contexto e motivação

Hoje, no builder de modelo de prontuário, as seções **Anexos** (`anexos`) e **Fotos do paciente** (`fotos-paciente`) caem no fallback genérico e são renderizadas como **textarea de texto** — o profissional só consegue *descrever* o que anexaria, não anexar de fato. A demanda transforma essas duas seções em **upload real de arquivos**, reusando a infraestrutura de anexos que já existe (entidade, endpoints, storage, services, util de redimensionamento). É feature **majoritariamente frontend** — o backend está quase pronto; o que falta é a **regra de acesso** (vinda do Briefing 001), a **whitelist de MIME para Office**, o **limite de 2MB para anexos** e o **endurecimento do TTL da URL de foto**.

Adicionalmente, o usuário elevou a **segurança de arquivos clínicos** a requisito crítico: fotos de paciente/procedimento (pré/pós-op) **não podem vazar de forma alguma**. A autorização "autor-ou-dono" não pode viver só no banco — precisa ser imposta em **defense-in-depth de 3 níveis: S3 + Backend + Frontend**, porque o S3 sozinho não conhece "autor-ou-dono"; quem decide é o backend ao emitir a presigned URL, e o S3 garante que sem essa URL nada é acessível.

## 2. Persona-alvo

- **Profissional**: durante a consulta atual (ou ao revisar uma evolução sua), anexa exames/laudos (Anexos) e registra fotos clínicas pré/pós-op (Fotos do paciente). Só vê/baixa o que ele mesmo anexou (ou tudo, se Dono — regra do 001).
- **Dono**: vê/baixa todos os anexos e fotos do paciente.
- **Admin/Dono configurando modelo**: vê a **prévia read-only** da seção no builder (com exemplos fictícios, sem backend).

Momento da jornada: atendimento (consulta atual) e pós-consulta (revisão da própria evolução).

## 3. Escopo

**Inclui**:

- **Seção Anexos** (`anexos`): upload de arquivos. Tipos aceitos: **PDF**, **imagens** (JPG/PNG/WEBP) e **documentos Office** (Word: `doc`/`docx`; Excel: `xls`/`xlsx`). **Limite ~2MB por arquivo**. Lista com nome/tipo/tamanho + download (URL assinada on-demand) + remover (soft-delete).
- **Seção Fotos do paciente** (`fotos-paciente`): upload de **apenas imagens**, **redimensionadas no envio** para **lado maior ~1600px, JPEG qualidade ~0.8** via `imageUtils.redimensionarImagem(arquivo, 1600, 0.8)`. Grid de thumbnails + visualizar.
- **Dois componentes Vue novos**: `SecaoAnexos.vue` e `SecaoFotosPaciente.vue` em `frontend/src/components/prontuario/secoes/`, seguindo o contrato de seção (`modelValue` objeto + `readOnly` + emit `update:modelValue`), registrados no dispatcher `SecaoProntuario.vue` (substituindo o fallback textarea dessas duas chaves).
- **Prévia read-only no builder**: ambos os componentes, em `readOnly`, mostram **exemplos fictícios sem chamar backend e sem upload ativo** (padrão do briefing 2026-06-26_001). Ajustar `EXEMPLOS_SECAO_MODELO` para `anexos` e `fotos-paciente` virarem **objeto de exemplo** (hoje são string).
- **Backend — whitelist MIME Office**: adicionar `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `application/vnd.ms-excel`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` à `MimeTypesPermitidos`. **Inclui tarefa de deploy/infra** (ver §5 e §10 — o valor de produção precisa carregar a nova lista).
- **Backend — limite 2MB para Anexos**: validação específica da seção Anexos (front + back), distinta do limite global de 50MB.
- **Infra/Segurança S3 — endurecimento do TTL de foto**: `S3FotoStorageService` usa TTL hardcoded de **86400s (24h)** — sensível demais para foto clínica. Reduzir para **~300s (5min)**, alinhando com `S3AnexoStorageService`, e tornar configurável (`StorageOptions`). Confirmar Block Public Access + SSE nos buckets (via `imedto-database`/MCP AWS).
- **Consumo da regra de acesso do 001**: anexos/fotos só listados/baixados pelo autor da evolução vinculada ou pelo Dono; órfão (`evolucao_id IS NULL`) = quem fez o upload (`criado_por_usuario_id`) ou o Dono.
- **Atualização de `Docs/INFRA.md` e `Docs/DESIGN.md`**.

**Não inclui**:

- A **regra de acesso autor-ou-dono em si** (gating nas queries de anexo) — é do Briefing 001. Aqui apenas **consumimos** e adicionamos os CAs de segurança que comprovam a postura defense-in-depth.
- Anexar arquivo a um documento clínico (receita/atestado) — fora de escopo.
- Conversão de Office para PDF, preview inline de Word/Excel — fora de escopo (Office é download apenas).
- Câmera nativa mobile para fotos — este briefing é da **web** (`frontend/`); se o app mobile precisar das seções, vira briefing mobile próprio.

## 4. Regras de negócio

- **R1 — Tipos aceitos por seção**: **Anexos** aceita PDF + imagens (JPEG/PNG/WEBP) + Office (doc/docx/xls/xlsx). **Fotos** aceita **apenas imagens** (após redimensionar, vira sempre `image/jpeg`). Validado no **front** (UX: bloqueia seleção/avisa) **e no back** (`AdicionarAnexoCommandHandler` via `MimeTypesPermitidos` — fonte da verdade). Mora em: **Handler (whitelist) + Front (espelho)**.

- **R2 — Limite de tamanho**: **Anexos = ~2MB por arquivo**. **Fotos** após redimensionamento a 1600px/0.8 ficam <1MB (2MB cobre com folga). Validado no **front** (antes do upload) **e no back**. Como o limite global é 50MB, o 2MB específico da seção precisa ser aplicado: no front sempre; no back, decidir o ponto (validação no handler/command para a origem "anexo"/"foto", ou um limite por `Marcador`). Mora em: **Handler + Front**. *Liberdade técnica do dev sobre o ponto exato de aplicação do 2MB no back, desde que rejeite >2MB com 422 genérico.*

- **R3 — Redimensionamento de foto**: toda imagem da seção Fotos passa por `redimensionarImagem(arquivo, 1600, 0.8)` **no front, antes do upload**. O resultado é `image/jpeg`. Mora em: **Front** (`imageUtils.ts`, já existe; hoje default é 512/0.85 — passar 1600/0.8 explicitamente).

- **R4 — Distinção de origem via `Marcador`**: Anexos e Fotos **reusam a mesma entidade `ProntuarioAnexo`**. O campo `Marcador` (string livre, já existe) distingue a seção de origem — valores propostos: `"anexo"` e `"foto-paciente"`. Listagem de cada seção filtra pelo `Marcador` correspondente. Mora em: **Command (grava Marcador) + Query (filtra por Marcador) + Front (passa origem)**. *Confirmar/documentar: hoje `listarAnexos` não filtra por Marcador — avaliar adicionar filtro ou separar no front. Decisão registrada para o dev.*

- **R5 — Vínculo com a evolução (`EvolucaoId`)**: o anexo/foto precisa de `EvolucaoId` para o gating de acesso (001) funcionar. **Em consulta atual, a evolução é criada ANTES do upload** (espelhando o fluxo do termo de consentimento: a investigação confirmou que o termo cria a evolução primeiro e passa `EvolucaoId` na emissão — não há "vincular depois"). Assim, todo anexo/foto novo nasce com `evolucao_id` real, e em produção os únicos `null` serão **legados** (visíveis a quem fez o upload `criado_por_usuario_id` ou ao Dono, pela regra R7 do 001). A seção lista os anexos/fotos por `evolucaoId` (da evolução existente, ou da que está sendo montada na consulta atual). Mora em: **Front (orquestra ordem) + Command (recebe EvolucaoId)**. *O dev confirma o gatilho exato de criação da evolução na consulta atual (provável reuso do `EvolucaoIdCriada` retornado por `RegistrarEvolucaoCommandHandler`, como o termo já faz). Se a ordem "criar evolução antes do upload" exigir mudança não-trivial no fluxo da consulta atual, o dev **para e reporta** (possível spec gap).*

- **R6 — Segurança de arquivos em 3 níveis (defense-in-depth)** — *crítico, fotos não podem vazar*:
  - **Nível S3**: buckets `BucketFotos` e `BucketAnexosProntuario` com **Block Public Access ON**, **sem ACL/policy pública**, **SSE** (AES256 já é default do bucket no upload), **path isolado por tenant** (já existe: `est_{id}/paciente_{id}/...`). Objeto **inacessível** por URL sem assinatura (403 do S3). TTL da presigned URL de **foto reduzido para ~5min**. Mora em: **Infra (bucket) + S3FotoStorageService (TTL)**.
  - **Nível Backend**: presigned URL de anexo/foto **só é emitida após validar autor-ou-dono** (regra do 001, em `ObterUrlAnexoQueryHandlers`/`ObterUrlsAnexosQueryHandler`); negação = "não encontrado" genérico; cada emissão auditada (`Exportacao`/`Leitura`). Backend **nunca** expõe listagem direta do bucket — só metadados via DTO + URL on-demand. Mora em: **Handler/Query (001)**.
  - **Nível Frontend**: URL assinada buscada **on-demand e descartável** — **nunca** persistida em store/`localStorage`/Pinia, **nunca** logada; thumbnail/preview só renderiza se autorizado (espelho do back); a prévia do builder usa **só imagens fictícias** (zero S3). Mora em: **Front**.

- **R7 — Prévia read-only sem backend**: em `readOnly` (builder), `SecaoAnexos`/`SecaoFotosPaciente` mostram exemplos fictícios estáticos, **sem** request ao backend, **sem** botões de upload ativos, **sem** S3. Espelha o padrão do briefing 2026-06-26_001 (dispatcher real `SecaoProntuario` em `:read-only="true"` com `EXEMPLOS_SECAO_MODELO[chave]`). Mora em: **Front**.

- **R8 — Soft-delete na remoção**: remover anexo/foto usa o soft-delete LGPD já existente (`ProntuarioAnexo.MarcarComoDeletado`) — blob retido conforme política, registro marcado `deletado_em`. Mora em: **Command/Handler de remoção (avaliar se já existe; se não, é item para o dev — pode acionar fluxo existente de arquivar/deletar)**. *Confirmar existência de endpoint de remoção; a investigação encontrou `Arquivar`/`MarcarComoDeletado` no domínio mas não um handler/rota de remoção exposto — registrar para o dev.*

## 5. Modelo de dados

**Sem tabela nova. Sem coluna nova.** Reusa `ProntuarioAnexo` (entidade, tabela `prontuario_anexos`) integralmente. Campo `Marcador` (já existe, nullable, string livre) passa a carregar `"anexo"` ou `"foto-paciente"`.

**Índice**: o JOIN/filtro por `evolucao_id` (gating do 001) e o filtro por `Marcador` na listagem podem pedir índice — **avaliação fica no Briefing 001** (`prontuario_anexos (evolucao_id)`). Se o filtro por `Marcador` virar quente, o `imedto-database` avalia índice composto. **Provavelmente não precisa de DB agent exclusivo para este briefing** — só se o DB agent (já acionado pelo 001) precisar incluir o índice de anexos.

**Config (`StorageOptions`)**:
- `MimeTypesPermitidos`: adicionar os 4 MIME types Office.
- Novo campo configurável para TTL de foto (ex.: `TtlSignedUrlFotosMinutos`, default 5) — ou reusar `TtlSignedUrlMinutos`. *Decisão de naming do dev.*

**LGPD**: foto clínica = dado de saúde sensível (Art. 11). Audit de escrita no upload (já existe). Minimização: DTO de anexo só com campos da tela.

## 6. UX e fluxo

**`SecaoAnexos.vue`** (modo edição):
- Botão "Adicionar anexo" → file picker (`accept` = PDF + imagens + Office). Valida tipo/tamanho no front (espelho do back) antes de enviar.
- Lista: cada item com ícone por tipo, **nome**, **tipo**, **tamanho**, ação **baixar** (busca URL assinada on-demand, abre/baixa, descarta a URL) e **remover** (confirmação → soft-delete).
- Estados: **vazio** (`AppEmptyState` "Nenhum anexo"), **enviando** (loading no item/botão), **erro** (tipo inválido / >2MB / falha de upload — mensagem genérica).

**`SecaoFotosPaciente.vue`** (modo edição):
- Botão "Adicionar foto" → file picker (`accept` = imagens). Aplica `redimensionarImagem(arquivo,1600,0.8)` → upload.
- **Grid de thumbnails** (busca URLs assinadas on-demand para render; descarta). Clicar → visualizar (lightbox/modal). Remover (confirmação → soft-delete).
- Estados: **vazio** (`AppEmptyState`), **enviando** (placeholder/loading no grid), **erro** (não-imagem / falha — genérico).

**Modo `readOnly` (prévia do builder)** — ambos:
- Mostram **exemplos fictícios estáticos** (sem upload, sem S3, sem request). Ex.: Anexos → 2 itens fake ("laudo-ultrassom.pdf", "hemograma.pdf"); Fotos → 2 thumbnails fake (imagens locais/placeholder embutidas). `EXEMPLOS_SECAO_MODELO['anexos']` e `['fotos-paciente']` viram **objeto** com esses exemplos.

**Design system**: reusar componentes de `frontend/src/components/ui/` (AppButton, AppEmptyState, AppModal, etc.). `AppPhotoUpload.vue` existente é **avatar único** (não grid) — **não** reusar direto para a grade de fotos; avaliar extrair/criar um componente de grade de fotos se reutilizável (decisão do dev; se for reutilizável, vai pro design system e atualiza `Docs/DESIGN.md`). Tipografia via tokens (CLAUDE.md §5).

## 7. Critérios de aceite (testáveis)

- **CA1 (anexo — tipo válido)**: Dado o Dr. A na seção Anexos da sua evolução, Quando envia um PDF de 1MB, Então o upload conclui, o item aparece na lista com nome/tipo/tamanho, e o anexo é gravado com `Marcador="anexo"` e `EvolucaoId` da evolução atual.

- **CA2 (anexo — Office aceito)**: Dado o Dr. A, Quando envia um `.docx` (≤2MB), Então o upload conclui (MIME `...wordprocessingml.document` aceito pela whitelist). Idem `.doc`, `.xls`, `.xlsx`.

- **CA3 (anexo — tipo inválido rejeitado)**: Dado o Dr. A, Quando tenta enviar um `.zip` ou `.exe`, Então o front bloqueia/avisa antes do envio, e se forçado, o back retorna **422 genérico** ("tipo de arquivo não permitido"), sem PII.

- **CA4 (anexo — >2MB rejeitado)**: Dado o Dr. A, Quando tenta enviar um PDF de 3MB na seção Anexos, Então o front rejeita com mensagem clara e, se forçado, o back retorna **422 genérico** — o arquivo **não** é gravado nem sobe ao S3.

- **CA5 (foto — só imagem)**: Dado o Dr. A na seção Fotos, Quando tenta enviar um PDF, Então é rejeitado (apenas imagens). Quando envia um JPG/PNG/WEBP, Então é aceito.

- **CA6 (foto — redimensionamento aplicado)**: Dado o Dr. A, Quando envia uma foto de 4000×3000px (≈6MB), Então o front a redimensiona para lado maior ~1600px / JPEG ~0.8 **antes** do upload, o arquivo que sobe é `image/jpeg` e <1MB, e o anexo grava `Marcador="foto-paciente"`.

- **CA7 (download via URL assinada)**: Dado o Dr. A (autor) na lista de anexos, Quando clica em baixar, Então o front busca a URL assinada on-demand, o arquivo abre/baixa, e a URL **não** é persistida em store/localStorage nem logada (verificável: após o uso, a URL não está em estado/Network log retido).

- **CA8 (remoção — soft-delete)**: Dado o Dr. A, Quando remove um anexo/foto (com confirmação), Então o item some da lista, o registro fica com `deletado_em` preenchido (soft-delete), e deixa de ser listado/baixável.

- **CA9 (prévia read-only no builder — sem backend)**: Dado um Dono/admin no builder de modelo, Quando abre a prévia das seções Anexos e Fotos, Então vê exemplos fictícios (itens/thumbnails fake) **sem nenhuma requisição** ao backend nem ao S3 (Network limpo) e **sem** botões de upload ativos.

- **CA10 (render nos dois lugares via dispatcher)**: Dado o dispatcher `SecaoProntuario.vue`, Quando a chave é `anexos` ou `fotos-paciente`, Então renderiza `SecaoAnexos`/`SecaoFotosPaciente` (não mais o fallback textarea), tanto no prontuário (editável) quanto na prévia (read-only).

— **Segurança de arquivos (defense-in-depth) — obrigatórios:**

- **CA11 (isolamento — Dr.B negado)**: Dado o Dr. B (Profissional), Quando tenta obter a URL de uma foto/anexo vinculado a evolução do Dr. A, Então recebe **"não encontrado" genérico**, **nenhuma** presigned URL é emitida, e nada no S3 fica acessível para ele. (Consome regra do 001.)

- **CA12 (Dono obtém qualquer URL)**: Dado o Dono, Quando obtém a URL de qualquer foto/anexo do paciente, Então recebe a URL assinada e baixa com sucesso.

- **CA13 (TTL curto de foto)**: Dado o Dono/autor que obteve a URL assinada de uma foto, Quando a URL expira, Então expira em **minutos** (~5min, `Expires` da presigned), **não** em 24h — verificável no parâmetro `X-Amz-Expires`/comportamento de expiração.

- **CA14 (objeto não-público no S3)**: Dado o caminho de um objeto no bucket de fotos/anexos, Quando se acessa a URL **sem assinatura** (sem query de presign), Então o S3 retorna **403** (Block Public Access ON, sem policy pública).

- **CA15 (front não guarda a URL)**: Dado o front após renderizar/baixar com a URL assinada, Quando se inspeciona store/Pinia/localStorage, Então a URL assinada **não** está persistida (descartável) e não foi logada no console.

- **CA16 (órfão — Dono ou uploader)**: Dado um anexo/foto com `evolucao_id IS NULL`, Quando o Dr. A que **fez o upload** (`criado_por_usuario_id == Dr. A`) tenta obter a URL, Então **obtém**; Quando o Dr. B (não-uploader, não-Dono) tenta, Então é **negado** (genérico); Quando o Dono tenta, Então **obtém**. (Consome 001/R7/CA6.)

— **Transversais:**

- **CA17 (multi-tenant)**: Dado um usuário do estabelecimento B, Quando tenta obter URL de anexo/foto de paciente do estabelecimento A, Então recebe "não encontrado" genérico e nada é logado com PII do tenant A.

- **CA18 (audit LGPD)**: Dado o upload de uma foto/anexo, Quando ocorre, Então uma linha de **escrita** é registrada em `prontuario_acesso_log`; Dado o download (emissão de URL), Quando ocorre, Então é auditado — sem PII na mensagem/log.

- **CA19 (whitelist em produção)**: Dado o deploy, Quando um `.docx` é enviado **em produção**, Então é aceito — ou seja, a nova whitelist Office está efetivamente carregada no ambiente de produção (não só local). *Validado pelo QA conforme a estratégia de config escolhida em §10.*

- **CA20 (documentação viva)**: Dado o merge, Quando se consulta `Docs/INFRA.md` e `Docs/DESIGN.md`, Então refletem (a) whitelist MIME atualizada + TTL de foto 5min + onde a whitelist é configurada em prod, e (b) os componentes `SecaoAnexos`/`SecaoFotosPaciente` e o padrão de prévia.

- **CA21 (gate tipográfico)**: Dado o build, Quando roda `npm run check:typography -- --ci`, Então passa sem novos literais de `font-size`/`font-weight`.

## 8. Riscos e dependências

- **Dependência forte**: Briefing 001 (regra de acesso). Sem ele, os CAs de isolamento (CA11/CA16) não passam. **Implementar 001 antes.**
- **Risco — whitelist Office só local**: a investigação mostrou que `MimeTypesPermitidos` vive em `appsettings.json` + defaults da classe `StorageOptions`; o `docker-compose.yml` de prod **só** sobrescreve `Storage__Region` e os 2 buckets — **não há** parâmetro `/imedto/dev/storage/*` no SSM hoje. Logo, adicionar Office só no `appsettings.Development.json` funciona local e **quebra/não-habilita em prod**. Ver §10 para a estratégia correta. CA19 cobre.
- **Risco — TTL de foto hardcoded**: `S3FotoStorageService.TtlPresignedUrlSegundos = 86_400` é constante; reduzir e tornar configurável. CA13.
- **Risco — ordem evolução×upload na consulta atual**: se criar a evolução antes do upload exigir refactor não-trivial do fluxo da consulta atual, pode virar spec gap (R5 manda o dev parar e reportar).
- **Risco — `AppPhotoUpload` não serve para grade**: é avatar único; não forçar reuso. Possível componente novo de grade (design system).
- **Áreas regressivas**: prontuário/seções, anexos, storage, infra. O builder (prévia) já entregue (2026-06-26_001) não pode regredir.

## 9. Observações para execução

**Reuso (não duplicar)**:
- Entidade `ProntuarioAnexo` + `Registrar`/`Arquivar`/`MarcarComoDeletado` (domínio).
- Endpoints existentes em `ProntuarioAnexoController`: POST `/anexos` (multipart) e `/anexos/base64`, GET `/anexos` (filtro `evolucaoId`), POST `/anexos/urls` (batch), GET `/anexos/{id}/url`.
- `AdicionarAnexoCommandHandler` (validação tamanho/MIME, sanitização de nome, path por tenant, audit).
- Storage: `IAnexoStorageService`/`IFotoStorageService` + providers S3; `StorageOptions`.
- Front: `prontuarioService.uploadAnexo(pacienteId, arquivo, evolucaoId?)` / `listarAnexos(pacienteId, evolucaoId?)` / `obterUrlAnexo(pacienteId, anexoId)` (não prefixar `/api` — `httpClient` já tem baseURL `/api`). Util `redimensionarImagem` (`frontend/src/services/imageUtils.ts`).
- Padrão de prévia: dispatcher `SecaoProntuario.vue` em `:read-only="true"` + `EXEMPLOS_SECAO_MODELO` (briefing 2026-06-26_001).

**Novo (a construir)**:
- `SecaoAnexos.vue` e `SecaoFotosPaciente.vue` (+ registro no dispatcher, substituindo o fallback textarea das chaves `anexos`/`fotos-paciente`).
- Entradas objeto em `EXEMPLOS_SECAO_MODELO` para `anexos` e `fotos-paciente` (hoje string).
- Whitelist Office na `MimeTypesPermitidos` (back) + **tarefa de config de produção** (§10).
- Limite 2MB específico de Anexos (front + back).
- TTL de foto 5min configurável (`S3FotoStorageService` + `StorageOptions`).
- Filtro/uso do `Marcador` ("anexo" vs "foto-paciente") na listagem.
- Confirmar/expor endpoint de remoção (soft-delete) se ainda não houver rota — registrar para o dev.

**Não-negociável**:
- Defense-in-depth de 3 níveis (R6). Foto clínica não vaza: TTL curto + presign só após autor-ou-dono + front descartável + bucket privado.
- Validação de tipo/tamanho espelhada front+back; back é a fonte da verdade (422 genérico).
- Prévia read-only: zero S3, zero request.

**Coordenação com o 001**: os handlers de anexo (`ListarAnexos...`, `ObterUrlAnexo...`, `ObterUrlsAnexos...`) são tocados pelos dois briefings — a **regra de acesso** vem do 001, o **uso para upload/listagem das seções** vem daqui. Sequenciar: 001 primeiro (gating), depois 002 (seções consomem). Evitar conflito de merge implementando em ordem.

## 10. Atualização de documentação

- **`Docs/INFRA.md` §Storage** — **atualizar**:
  - Tabela de buckets: TTL da presigned de **fotos passa de 24h para 5min** (alinhar com anexos).
  - Lista de `MimeTypesPermitidos`: acrescentar os 4 tipos Office (doc/docx/xls/xlsx).
  - **Documentar a estratégia de config de produção da whitelist**: hoje `MimeTypesPermitidos` **não está no SSM** nem no `docker-compose.yml` (só `Region` e buckets são sobrescritos); o valor efetivo em prod vem do `appsettings.json` embutido na imagem. **Decisão do dev/DB agent** (registrar no doc qual foi escolhida):
    - (a) editar `appsettings.json` (deploya com a imagem via CI/CD — mais simples, redeploy aplica), **ou**
    - (b) adicionar `Storage__MimeTypesPermitidos__N` no `deploy/docker-compose.yml` (e, se quiser rotacionar via SSM, criar `/imedto/dev/storage/...` + mapear no `pull-secrets.sh`).
  - Reforçar Block Public Access + SSE + sem policy pública nos dois buckets (defense-in-depth nível S3).
- **`Docs/DESIGN.md`** — **adicionar/atualizar** na seção de seções do prontuário: os componentes `SecaoAnexos.vue` e `SecaoFotosPaciente.vue` (chaves `anexos`/`fotos-paciente`), o contrato (`modelValue` objeto + `readOnly`), e a nota de que a prévia read-only mostra exemplos fictícios sem S3 (estende o padrão do briefing 2026-06-26_001). Se um componente de grade de fotos reutilizável for criado, registrá-lo no design system.
- **`Docs/LGPD.md`** — **opcional/incremental**: referenciar que fotos/anexos consomem a regra autor-ou-dono (001) e a postura de TTL curto + URL descartável (1-2 linhas; o detalhe do gating mora na seção criada pelo 001). Se o dev preferir consolidar, pode ficar só no 001 — registrar a escolha.

> **Necessita `imedto-database`?** Para **este** briefing, provavelmente **não** de forma exclusiva — não há tabela/coluna nova. O índice de `prontuario_anexos (evolucao_id)` e o eventual índice por `Marcador` entram no escopo do DB agent já acionado pelo **Briefing 001**. O **endurecimento do bucket S3** (Block Public Access/SSE/policy) pode ser executado pelo `imedto-database` via MCP AWS (é infra de storage), ou confirmado como já-configurado — registrar no INFRA.md.
