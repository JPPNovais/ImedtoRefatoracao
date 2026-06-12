# Termo de consentimento físico-primeiro: remover aceite por link, anexar foto/PDF assinado (com conversão de foto → PDF) e emitir+anexar pela evolução

**ID**: 2026-06-12_002
**Status**: Aprovado por usuário em 2026-06-12 (decisões de produto fornecidas pelo orquestrador; ambiguidades residuais resolvidas no §4)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: termos (emissão / aceite / anexo / PDF probatório / e-mail), prontuário (evolução, anexos, audit), permissionamento (`termos.emitir` / `prontuario`), LGPD (audit, minimização, magic bytes), rota pública anônima (`/termos/aceite/:token`), schema (migração de transição de registros legados — aciona `imedto-database`).

## 1. Contexto e motivação

O fluxo de aceite digital por **link público** (`AssinaturaTipo.AceiteLink`, "Fase 4") foi construído mas, na prática da clínica, o termo de consentimento é assinado **fisicamente** — o paciente está presente, assina o papel (ou o tablet), e a recepção/profissional digitaliza/fotografa o documento assinado. O link por e-mail introduz fricção (paciente não abre, link expira, suporte tem que reenviar), risco jurídico/LGPD adicional (endpoint anônimo, token na URL, rate-limit, enumeração) e superfície de manutenção que não paga o próprio custo. **Decisão de produto: remover por completo o aceite por link** e consolidar o fluxo em torno do documento físico assinado.

Hoje, anexar o comprovante exige um **PDF** (`AnexarPdfTermoCommandHandler` aceita só `application/pdf`). Na recepção, o mais natural é **fotografar** (frente e verso) com o celular. Faltam dois recursos:
1. **Aceitar foto** (JPG/PNG) e **convertê-la em PDF no backend** (multi-página: frente + verso) — entregando ao final um único documento probatório consistente com o fluxo já existente de `PdfAnexado` (1 arquivo, `PdfUrl`/`PdfHash`).
2. **Emitir e anexar dentro da evolução do prontuário** — quando o profissional registra a evolução, ele escolhe o modelo de termo, emite o snapshot, sobe a foto/PDF assinado e marca como `Assinado` na mesma jornada, com o documento visível tanto na **aba de Termos do paciente** quanto na **timeline da evolução**.

Esta demanda fecha o ciclo "termo físico assinado → arquivado digitalmente → auditável" e remove a dívida do aceite por link.

## 2. Persona-alvo

- **Recepção / Profissional** (ação `termos.emitir`): emite o termo e anexa o documento assinado pela **aba de Termos** do paciente (jornada pós-atendimento ou administrativa).
- **Profissional** (ação `prontuario` + `termos.emitir`): emite + anexa o termo **dentro da evolução** do prontuário, durante/ao final do atendimento — momento em que o paciente assina presencialmente.
- **Dono / Admin** (`termos.gerenciar_modelos`): gere os modelos e revoga termos assinados (inalterado nesta demanda, exceto remoção do aceite por link).
- Frequência: alta — todo procedimento que exige consentimento gera um termo físico assinado.

## 3. Escopo

**Inclui**:

**A) Remoção total do aceite por link (`AceiteLink`)**
- **Backend** — remover: `TermoPublicoController.cs` (endpoints anônimos `GET/POST /api/publico/termos/aceite/{token}`), `ObterTermoPublicoPorTokenQueryHandler` + `ObterTermoPublicoPorTokenQuery` + `TermoPublicoDto`, `RegistrarRespostaPublicaTermoCommandHandler` + `RegistrarRespostaPublicaTermoCommand`, `EnviarEmailTermoLinkEventHandler`, `ReenviarLinkTermoCommandHandler` + `ReenviarLinkTermoCommand` + endpoint `POST /api/termos/{id}/reenviar-link`, métodos de domínio `RegistrarAceitePublico`, `RegistrarRecusaPublica`, `MarcarReenvioLinkEmail`, `Expirar` em `TermoEmitido`, o rate-limit policy `termos-publico`, a geração de `TokenAceite`/`TokenExpiraEm`/TTL na emissão, e o `NotificarEmissorTermoRespondidoEventHandler` (notifica o emissor sobre resposta pública — sem aceite público, perde o gatilho).
- **Frontend** — remover: `AceiteTermoPublicoView.vue` (+ teste), a rota `/termos/aceite/:token`, `termoAceitePublicoService` (`montarUrlAceitePublico`), a opção "Enviar link de aceite" e todo o passo 4 (tela do link gerado + copiar) do `EmitirTermoModal.vue`, o sub-bloco de canal e-mail/cópia, o botão "reenviar link" no `PacienteTermosTab.vue`. O wizard passa a ter **um único caminho de assinatura: documento físico assinado** (foto ou PDF).
- **Enum `AssinaturaTipo`**: `AceiteLink` é **removido** do domínio para termos novos. Como há registros legados persistidos com `assinatura_tipo = 'AceiteLink'`, a leitura/mapeamento desse valor histórico **deve continuar funcionando** (ver R6 e §5 — política de transição). Para emissão nova, só `PdfAnexado` (renomeável conceitualmente para "documento físico", mas **manter o valor `PdfAnexado` no schema** para não migrar histórico — ver §4 decisão 6).
- **Enum `StatusTermoEmitido`**: `Recusado` e `Expirado` **permanecem no enum** (compat de leitura do histórico já materializado), mas **deixam de ser estados alcançáveis** para termos novos. Transição válida nova: `Pendente → Assinado → Revogado` (ver R7).

**B) Anexar foto (JPG/PNG) com conversão para PDF no backend**
- Estender o fluxo de anexo (`AnexarPdfTermoCommandHandler` / endpoint `POST /api/termos/{id}/pdf`) para aceitar, além de `application/pdf`, **`image/jpeg` e `image/png`** (uma ou mais imagens — frente e verso).
- Quando o upload for imagem(ns): o **backend converte para PDF multi-página** (1 imagem = 1 página; frente+verso = PDF de 2 páginas) via **QuestPDF** (já na Infra), calcula o **SHA-256 do PDF resultante**, e segue exatamente o fluxo `PdfAnexado` existente: upload do PDF no S3 → `termo.AnexarPdf(pdfUrl, pdfHash)` → status `Assinado`.
- Quando o upload já for PDF: comportamento atual inalterado (sem conversão).
- **Validação de magic bytes real** por tipo, defense-in-depth: PDF (`%PDF-` / `\x25\x50\x44\x46`), JPG (`\xFF\xD8\xFF`), PNG (`\x89PNG\r\n\x1A\n`). MIME declarado deve bater com magic bytes; divergência → 422 genérico.
- `StorageOptions.MimeTypesPermitidos` já contém `application/pdf`, `image/png`, `image/jpeg` — **confirmar e reusar** (sem mudança necessária; ver §4 decisão 8).
- **HEIC fica explicitamente fora** (backlog) — formato nativo do iPhone exige decoder adicional. Upload HEIC → 422 com mensagem orientando converter para JPG/PNG.

**C) Emitir + anexar pela evolução do prontuário**
- No fluxo de registro de evolução do prontuário (`ProntuarioView.vue` / passo de evolução), o atendente pode: escolher o modelo de termo (reusando o **passo de seleção de modelo do `EmitirTermoModal`**), emitir o `TermoEmitido` (snapshot imutável), subir a foto/PDF assinado e marcá-lo como `Assinado`, **com `EvolucaoId` registrada no vínculo**.
- O documento fica visível em **dois lugares**: na **aba de Termos** do paciente (fluxo existente) **e** na **timeline/detalhe da evolução** — via criação de um `ProntuarioAnexo` com `EvolucaoId` apontando para o **mesmo objeto S3** do PDF do termo (não duplica o binário no storage; registra o vínculo de leitura). Ver R9 e §4 decisão 9.
- Continua disponível também pela aba de termos avulsa (os dois lugares coexistem).

**Não inclui** (explicitamente fora):
- Suporte a **HEIC** (backlog declarado).
- Assinatura digital ICP / e-CPF do termo.
- Alterar o **PDF probatório gerado** (`GET /api/termos/{id}/pdf-gerado`, briefing 2026-06-10_002) — continua funcionando para termos `Assinado`/`Revogado`/`Pendente` sem anexo; o bloco de evidência que cita "aceite por link" deve ser ajustado para não referenciar mais o token/IP de aceite público (que deixam de ser preenchidos em termos novos) — ver R8.
- Migrar o valor `PdfAnexado` do schema para um novo nome (mantém-se por compat — §4 decisão 6).
- Permitir anexar documento a termo que **não** esteja `Pendente` (regra de `AnexarPdf` inalterada: só pendente, e só uma vez).
- OCR / extração de texto da foto.

## 4. Decisões e assunções

1. **Aceite por link: remoção total, não ocultação.** (Fornecida — diverge da recomendação inicial de "esconder".) Todo o código de aceite público, e-mail de link, reenvio, recusa e expiração sai do produto.
2. **Foto → PDF no backend, multi-página, sem HEIC.** (Fornecida.) JPG/PNG embutidos em PDF via QuestPDF; frente+verso = 2 páginas; SHA-256 do PDF resultante; reusa o fluxo `PdfAnexado` (1 arquivo, `PdfUrl`/`PdfHash`). HEIC é backlog.
3. **Evolução emite + anexa no mesmo fluxo.** (Fornecida.) Documento visível na aba de Termos **e** na timeline da evolução; reusa o passo de seleção de modelo do `EmitirTermoModal`.
4. **Audit nos dois trilhos.** (Fornecida.) Anexo pela aba de termos → `termo_audit_log` (ação `termo-pdf-anexado`, já existe). Anexo pela evolução → **também** `IProntuarioAcessoLogService.RegistrarAsync(..., TipoAcessoProntuario.Escrita)` (porque toca o prontuário). Ambos os audits são best-effort (não bloqueiam o fluxo principal).
5. **[Decisão] Destino dos `AceiteLink` legados pendentes (PONTO CRÍTICO).** Termos legados com `assinatura_tipo = 'AceiteLink'` e `status = 'Pendente'` ficarão com link morto (endpoint removido). **Regra de transição** (executada por **migration** do `imedto-database`): marcar todos os `AceiteLink` **`Pendente`** como **`Expirado`** (status terminal já existente, semanticamente correto: "o caminho de aceite não está mais disponível"). Termos `AceiteLink` já em estado terminal (`Assinado`, `Recusado`, `Revogado`) **permanecem intocados** — são histórico jurídico imutável e continuam legíveis. **Não** apagar registros. A migration também pode (opcional, liberdade do DB) limpar `token_aceite`/`token_expira_em` dos `Pendente` migrados para `Expirado`, já que o token perde função (não-PII, mas segredo morto) — ver §5.
6. **[Decisão] Manter o valor `PdfAnexado` no schema.** Não renomear `assinatura_tipo` nem o enum no banco — termos novos continuam gravados como `PdfAnexado` (agora significa "documento físico assinado: foto convertida ou PDF"). Renomear exigiria migrar histórico sem ganho de produto. No **front**, o wizard não expõe mais a escolha de tipo (só há um caminho), então o usuário nunca vê o termo "PdfAnexado".
7. **[Decisão] Estados `Recusado`/`Expirado` permanecem no enum** apenas para compat de leitura do histórico (incluindo os `Pendente` migrados para `Expirado` em D5). Para termos novos, são inalcançáveis. A documentação do enum e do aggregate deve registrar isso explicitamente. Transição nova: `Pendente → Assinado → Revogado`.
8. **[Assunção] PDF probatório (briefing 2026-06-10_002) — ajuste mínimo.** O bloco de evidência que hoje cita IP/UA/token de aceite **público** deve degradar para termos novos (que não têm aceite por link): para um termo `Assinado` por documento físico, a evidência passa a ser "Assinado em {AssinadoEm} mediante documento físico anexado (hash do documento: {PdfHash[..16]}…)". Para termos legados `Assinado` por link, manter o que já existe (degradação graciosa por null-check já prevista naquele briefing). **Não** reescrever o serviço — ajustar o bloco de evidência para não pressupor token/IP de aceite. Liberdade do dev quanto à granularidade do texto, desde que (a) nenhum token completo apareça e (b) termo físico exiba o hash do PDF anexado como evidência de integridade.
9. **[Decisão] Vínculo evolução ↔ documento sem duplicar binário.** O PDF do termo vive em **um único objeto S3** (bucket de termos). A visibilidade na timeline da evolução é feita registrando um `ProntuarioAnexo` (que já tem `EvolucaoId`) **apontando para o mesmo `storage_path`** do PDF do termo — ou, se o `ProntuarioAnexo` exigir path próprio, registrar o vínculo de forma que a timeline liste o documento sem re-upload. **Liberdade do dev/db** para a forma concreta do vínculo (coluna `evolucao_id` em `termos_emitidos`, ou linha em `prontuario_anexos` referenciando o mesmo S3 path), desde que: (a) o binário não seja duplicado no S3; (b) o documento apareça na timeline da evolução E na aba de termos; (c) multi-tenant e audit sejam respeitados. Recomendação: adicionar `evolucao_id BIGINT NULL` em `termos_emitidos` (FK para a evolução, mesmo tenant) + a timeline da evolução lê os termos vinculados — menor superfície que materializar um `ProntuarioAnexo` espelho. **Decisão final da forma do vínculo é do `imedto-database` em conjunto com o `imedto-developer`** (registrar no PR). O que é não-negociável: os CAs C1–C4.
10. **[Assunção] Conversão imagem→PDF.** QuestPDF compõe um documento A4 com cada imagem centralizada/escalada (`fit` preservando proporção, margem mínima), 1 imagem por página, na ordem de envio (frente, depois verso). O nome do arquivo final no S3 segue o padrão atual (`termos/est_{id}/{termoId}_{guid}.pdf`) — sem PII. O hash é do PDF resultante (não das imagens). Se o upload for uma única imagem, gera PDF de 1 página.
11. **[Assunção] Limite de tamanho/quantidade de imagens.** Reusar o limite de 10 MB do `AnexarPdfTermoCommandHandler` aplicado **ao total** das imagens enviadas; máximo de **frente+verso = 2 imagens** no MVP (rótulos "frente" e "verso" no front). Liberdade do dev para permitir N imagens se o esforço for trivial, mas o default é 2. Imagem isolada também é aceita (1 página).
12. **[Assunção] Mensagens genéricas e sem PII** em todos os 422 (tipo inválido, magic bytes divergentes, HEIC, tamanho), sem ecoar nome do arquivo nem conteúdo. Nome do arquivo de origem do usuário não é persistido em coluna nem em log (o S3 path é gerado por GUID).

## 5. Modelo de dados

**Aciona `imedto-database`** — esta demanda tem migration de transição + (provável) coluna de vínculo.

- **`public.termos_emitidos`** (`TermoEmitido`):
  - **Migration de transição (D5)**: `UPDATE termos_emitidos SET status = 'Expirado', atualizado_em = now() WHERE assinatura_tipo = 'AceiteLink' AND status = 'Pendente';` Opcionalmente, `token_aceite = NULL, token_expira_em = NULL` nas mesmas linhas (segredo morto). **Idempotente** (rodar duas vezes não muda nada além da 1ª). Registros `AceiteLink` terminais (`Assinado`/`Recusado`/`Revogado`) **não** são tocados.
  - **Vínculo com evolução (D9, recomendação)**: nova coluna `evolucao_id BIGINT NULL` + FK para a tabela de evoluções no **mesmo `estabelecimento_id`** + índice `(evolucao_id)` para a leitura da timeline. **Forma final decidida por `imedto-database`** (ver §4 D9): coluna em `termos_emitidos` (recomendado) **ou** materializar via `prontuario_anexos`. Multi-tenant: a FK e toda query de leitura filtram `estabelecimento_id`.
  - **Colunas que perdem uso para termos novos** (não remover — compat histórico): `token_aceite`, `token_expira_em` (continuam preenchidas só no histórico; nulas em termos novos). Não dropar colunas nesta entrega (drop é migração arriscada sem ganho).
- **`public.termo_emitido_acesso_log`** (trilha pública do aceite por link): **deixa de receber escrita** (a fonte — aceite/recusa/visualização pública — é removida). **Não dropar a tabela** nesta entrega (histórico de acessos públicos já registrados é dado de auditoria LGPD). Marcar como legada na doc; drop é backlog separado.
- **`public.termo_audit_log`** — destino do audit de anexo (`termo-pdf-anexado`, já existe). Inalterado.
- **`public.prontuario_anexos`** (`ProntuarioAnexo`, tem `evolucao_id`) — eventual destino do espelho de visibilidade na timeline (se a forma escolhida for materializar anexo; ver D9). Reuso de schema existente — sem coluna nova nele.
- **`public.prontuario_acesso_log`** — audit de escrita ao anexar pela evolução (reuso de `IProntuarioAcessoLogService`). Sem schema novo.
- **`public.pacientes`, `public.estabelecimentos`** — leitura para cabeçalho do PDF probatório (inalterado).

**Índices**: `(evolucao_id)` se a coluna de vínculo for adotada. Leitura de termos por `paciente_id + estabelecimento_id` e por `id + estabelecimento_id` já coberta.

**Sem nova audit table.**

## 6. UX e fluxo

**Wizard de emissão (`EmitirTermoModal.vue`) — simplificado:**
- **Passo 1 — Modelo**: inalterado (cards filtráveis por categoria + busca).
- **Passo 2 — Preview**: inalterado (variáveis resolvidas client-side).
- **Passo 3 — Confirmar e anexar documento assinado**: o card de escolha de tipo de assinatura é **removido** (não há mais "link"). O passo passa a oferecer: **"Anexar documento assinado"** com duas sub-opções de origem — **(a) Foto (frente/verso)** via `input[type=file] accept="image/jpeg,image/png"` (até 2 imagens, rótulos "Frente" e "Verso") e **(b) PDF** via `accept="application/pdf"`. Pré-visualização das imagens selecionadas (thumbnails) antes de confirmar. Ao confirmar: emite o termo (`Pendente`) e na sequência sobe o documento (conversão no backend se foto), resultando em `Assinado`. Alternativamente, manter o fluxo em duas etapas (emitir Pendente → anexar pela lista) é aceitável se reduzir complexidade — **liberdade do dev**, desde que o caminho "emitir + anexar foto" exista no MVP (CA-B1).
- **Passo 4 (tela do link)**: **removido** por completo.

**Aba de Termos do paciente (`PacienteTermosTab.vue`):**
- Botão **"Reenviar link"**: removido.
- Botão de **anexar**: passa a aceitar foto **ou** PDF (mesmo `input`, `accept="image/jpeg,image/png,application/pdf"`). Para foto, permite selecionar frente+verso.
- Demais ações (baixar PDF anexado, baixar PDF gerado, revogar, visualizar no drawer) inalteradas. O badge de status não exibe mais "Aguardando aceite (link)" para termos novos — só `Pendente` (aguardando anexo) / `Assinado` / `Revogado`. Termos legados podem exibir `Expirado`/`Recusado` (read-only, histórico).

**Evolução do prontuário (`ProntuarioView.vue` / passo de evolução):**
- Ação **"Emitir termo de consentimento"** dentro do contexto da evolução: abre o seletor de modelo (reuso do passo 1 do `EmitirTermoModal`), emite e anexa o documento físico, vinculando `EvolucaoId`. Estados loading/erro/sucesso seguem o padrão da view.
- O documento aparece na **timeline da evolução** (`EvolucaoTimelineItem` / `EvolucaoDetalheDrawer`) como item de documento clicável (baixa o PDF) **e** na aba de Termos.

**Estados**: loading por ação (`acaoEmAndamentoId`), erro via toast genérico (`e?.response?.data?.mensagem ?? "Erro ao anexar documento."`), vazio (`AppEmptyState` "Nenhum termo emitido"), conversão de foto exibe loading enquanto o backend processa.

**Mobile-ready**: o `input[type=file]` com `accept="image/*"` aciona a câmera no celular (caso de uso central — recepção fotografando o termo assinado). Garantir que a captura por câmera funcione.

## 7. Critérios de aceite (testáveis)

### Remoção do aceite por link (A)

- **CA-A1 (endpoints públicos removidos)**: Dado o roteamento da API após a entrega, Quando se chama `GET` ou `POST /api/publico/termos/aceite/{token}` ou `POST /api/termos/{id}/reenviar-link`, Então a rota **não existe** (404 de rota / endpoint ausente) — e um `grep` por `TermoPublicoController`, `RegistrarRespostaPublicaTermoCommand`, `ReenviarLinkTermoCommand`, `EnviarEmailTermoLinkEventHandler` no backend retorna vazio.
- **CA-A2 (rota e view públicas removidas no front)**: Dado o roteador do frontend, Quando se navega para `/termos/aceite/<qualquer-token>`, Então a rota não resolve para `AceiteTermoPublicoView` (componente e rota removidos); `grep` por `AceiteTermoPublicoView`, `montarUrlAceitePublico`, `termoAceitePublicoService` retorna vazio.
- **CA-A3 (wizard sem opção de link)**: Dado o `EmitirTermoModal` aberto no passo de confirmação, Quando o usuário visualiza as opções, Então **não** há opção "Enviar link de aceite", nem sub-bloco de canal e-mail/cópia, nem passo 4 de "link gerado"; o único caminho é anexar documento assinado (foto ou PDF).
- **CA-A4 (domínio sem métodos de aceite público)**: Dado o aggregate `TermoEmitido` após a entrega, Quando se inspeciona a classe, Então `RegistrarAceitePublico`, `RegistrarRecusaPublica`, `MarcarReenvioLinkEmail` e `Expirar` (como ação pública) foram removidos; a emissão **não** gera mais `TokenAceite`/`TokenExpiraEm` para termos novos; transição válida documentada é `Pendente → Assinado → Revogado`.
- **CA-A5 (transição de legados pendentes — migration)**: Dado um termo legado com `assinatura_tipo = 'AceiteLink'` e `status = 'Pendente'`, Quando a migration de transição roda, Então seu status passa a `Expirado` e `atualizado_em` é atualizado; um termo legado `AceiteLink` com status `Assinado`/`Recusado`/`Revogado` **não** é alterado; nenhum registro é deletado.
- **CA-A6 (migration idempotente)**: Dado que a migration de transição rode duas vezes, Quando a segunda execução ocorre, Então nenhuma linha adicional é alterada (já não há mais `AceiteLink` `Pendente`).
- **CA-A7 (leitura de histórico legado intacta)**: Dado um termo legado `AceiteLink` já `Assinado` (com IP/UA/token de aceite público registrados), Quando ele é listado na aba de termos e o PDF probatório é gerado, Então é exibido/gerado sem erro, preservando a evidência histórica do aceite por link (degradação graciosa por null-check).

### Anexo de foto com conversão para PDF (B)

- **CA-B1 (caminho feliz — foto frente+verso → PDF de 2 páginas)**: Dado um termo `Pendente`, Quando o usuário anexa duas imagens JPG (frente e verso) válidas, Então o backend converte para um PDF de **2 páginas** (1 imagem por página, na ordem enviada), calcula o SHA-256 do PDF, sobe no S3, e o termo passa a `Assinado` com `PdfUrl`/`PdfHash` preenchidos; baixar "PDF anexado" devolve o PDF de 2 páginas.
- **CA-B2 (imagem isolada → PDF de 1 página)**: Dado um termo `Pendente`, Quando o usuário anexa uma única imagem PNG válida, Então o backend gera um PDF de 1 página e marca o termo como `Assinado`.
- **CA-B3 (PDF direto — sem conversão)**: Dado um termo `Pendente`, Quando o usuário anexa um `application/pdf` válido, Então o comportamento atual é preservado (sem conversão; hash do PDF original) e o termo passa a `Assinado`.
- **CA-B4 (magic bytes — JPG/PNG/PDF reais)**: Dado um arquivo cujo MIME declarado é `image/jpeg` mas cujos magic bytes **não** são `\xFF\xD8\xFF`, Quando o upload é tentado, Então o backend rejeita com 422 genérico ("Arquivo não é uma imagem/PDF válido.") sem persistir nada; o mesmo vale para PNG (`\x89PNG…`) e PDF (`%PDF-`).
- **CA-B5 (HEIC rejeitado com orientação)**: Dado um arquivo HEIC (magic bytes `ftypheic`/`ftypheix`/`ftypmif1`), Quando o upload é tentado, Então recebe 422 com mensagem orientando converter para JPG ou PNG; nada é persistido.
- **CA-B6 (limite de tamanho)**: Dado um conjunto de imagens cujo total excede 10 MB, Quando o upload é tentado, Então recebe 422 ("Tamanho do documento excede 10 MB.") e nada é persistido.
- **CA-B7 (MIME whitelist no back é fonte da verdade)**: Dado um upload com MIME fora da whitelist (`StorageOptions.MimeTypesPermitidos` / tipos aceitos no handler), Quando tentado, Então é rejeitado no backend mesmo que o front não tenha validado (espelho back+front; back é a verdade).
- **CA-B8 (anexo só em Pendente, uma vez)**: Dado um termo já `Assinado`, Quando se tenta anexar outro documento, Então recebe 422 ("Só é possível anexar em termos pendentes." / "já possui documento anexado.") — regra de `AnexarPdf` inalterada.

### Emitir + anexar pela evolução (C)

- **CA-C1 (emitir + anexar na evolução)**: Dado um profissional registrando uma evolução, Quando ele escolhe um modelo de termo, emite e anexa a foto/PDF assinado, Então um `TermoEmitido` é criado com snapshot imutável, status `Assinado`, documento no S3, e o vínculo com a evolução (`EvolucaoId`) é registrado.
- **CA-C2 (visível nos dois lugares)**: Dado um termo emitido pela evolução, Quando o usuário abre a **aba de Termos** do paciente **e** a **timeline/detalhe da evolução**, Então o documento aparece em ambos, clicável para download, apontando para o **mesmo** objeto no S3 (binário não duplicado).
- **CA-C3 (snapshot fiel também na evolução)**: Dado um termo emitido pela evolução na versão N de um modelo posteriormente editado, Quando o documento é consultado, Então o snapshot corresponde à versão N (imutabilidade preservada — mesma regra do aggregate).
- **CA-C4 (reuso do seletor de modelo)**: Dado o fluxo de emissão pela evolução, Quando o seletor de modelo é exibido, Então ele reusa o passo de seleção do `EmitirTermoModal` (sem duplicar a lógica de listagem/filtro/preview).

### Multi-tenant, RBAC, LGPD, estados, performance

- **CA-MT1 (multi-tenant no anexo/download)**: Dado um usuário autenticado no estabelecimento B, Quando tenta anexar ou baixar o documento de um termo do estabelecimento A (`POST /api/termos/{id}/pdf` ou `GET .../pdf`), Então recebe erro genérico ("Termo não encontrado.") e nada do tenant A vaza; repositório falha-fechada sem tenant claim.
- **CA-MT2 (multi-tenant no vínculo da evolução)**: Dado o vínculo termo↔evolução, Quando a timeline da evolução lista os termos, Então só lista termos do **mesmo `estabelecimento_id`**; a FK/coluna de vínculo respeita o tenant.
- **CA-RBAC1 (anexar exige `termos.emitir`)**: Dado um usuário sem a ação `termos.emitir`, Quando chama `POST /api/termos/{id}/pdf`, Então recebe 403; e no front o botão de anexar não é alcançável.
- **CA-RBAC2 (evolução exige `prontuario`)**: Dado um usuário sem permissão de prontuário, Quando tenta emitir/anexar termo pela evolução, Então é bloqueado no back (403) e a ação fica oculta no front.
- **CA-LGPD1 (sem PII no nome do arquivo nem em log)**: Dado o documento anexado, Quando o `storage_path` e os logs do servidor são inspecionados, Então o path é gerado por GUID (`termos/est_{id}/{termoId}_{guid}.pdf`), sem nome/CPF do paciente, e nenhum log contém conteúdo do termo, o binário, nome do arquivo de origem ou hash em texto livre de log de aplicação.
- **CA-LGPD2 (audit no anexo pela aba)**: Dado um anexo bem-sucedido pela aba de termos, Quando concluído, Então é registrada 1 linha em `termo_audit_log` via `ITermoAuditLogger` com `{ estabelecimento_id, usuario_id, acao = "termo-pdf-anexado", entidade = "TermoEmitido", entidade_id }`.
- **CA-LGPD3 (audit no anexo pela evolução)**: Dado um anexo bem-sucedido pela evolução, Quando concluído, Então **além** do `termo_audit_log`, é registrada 1 linha de escrita em `prontuario_acesso_log` via `IProntuarioAcessoLogService.RegistrarAsync(..., TipoAcessoProntuario.Escrita)`.
- **CA-LGPD4 (audit best-effort)**: Dado que a gravação de qualquer um dos audits falhe, Quando o anexo é processado, Então o documento ainda é anexado com sucesso (audit nunca bloqueia).
- **CA-EST1 (estados de UI)**: Dado o upload em andamento (incluindo conversão de foto no backend), Quando o usuário aguarda, Então a ação fica em loading e o botão desabilitado; erro mostra toast genérico; sucesso atualiza a lista/timeline.
- **CA-EST2 (mensagem genérica em 422)**: Dado qualquer 422 (tipo inválido, magic bytes, HEIC, tamanho), Quando exibido, Então a mensagem é genérica e **não** contém PII nem nome do arquivo.
- **CA-REG1 (regressão — PDF probatório)**: Dado o endpoint `GET /api/termos/{id}/pdf-gerado` (briefing 2026-06-10_002), Quando gerado para um termo `Assinado` por documento físico, Então o documento é produzido sem erro, com o bloco de evidência ajustado para "documento físico anexado" (hash do PDF), **sem** referenciar token/IP de aceite público; e para termo legado `Assinado` por link, a evidência histórica é preservada.
- **CA-REG2 (regressão — fluxos remanescentes)**: Dado os fluxos de termos que permanecem (listar, visualizar no drawer, emitir, anexar PDF, baixar PDF anexado/gerado, revogar), Quando a feature é entregue, Então todos funcionam inalterados; nenhum resíduo de aceite por link quebra build, testes ou navegação.

## 8. Riscos e dependências

- **Migration de transição em produção (PONTO CRÍTICO)**: o `UPDATE` dos `AceiteLink` `Pendente` → `Expirado` é irreversível na prática (não há fluxo de "des-expirar"). Risco baixo (esses termos já estavam mortos sem o endpoint), mas a migration deve ser **idempotente** e **não tocar** terminais. Validar contagem de linhas afetadas antes/depois (`imedto-database`).
- **Remoção ampla cria órfãos de compilação**: remover `TermoPublicoController`, handlers, queries, DTOs, eventos e métodos de domínio vai gerar referências quebradas (DI, testes, imports). O dev deve remover em cascata e rodar build+testes até verde. Testes existentes de aceite público (`RegistrarRespostaPublicaTermoCommandHandlerTests`, `ReenviarLinkTermoCommandHandlerTests`, `AceiteTermoPublicoView.test.ts`) devem ser **removidos** (não adaptados — a feature morreu).
- **Conversão imagem→PDF com QuestPDF**: validar orientação/escala de fotos grandes (celular gera 4000×3000px) — `fit` preservando proporção, sem estourar a página. Risco de PDF pesado se a imagem não for recomprimida; aceitável no MVP (limite de 10 MB protege). Liberdade do dev para recomprimir a imagem antes de embutir, se necessário.
- **HEIC silencioso**: iPhones podem enviar HEIC com extensão `.jpg` — por isso a validação por **magic bytes** (não por extensão/MIME declarado) é não-negociável (CA-B4/B5).
- **Vínculo evolução↔termo (forma a decidir)**: a escolha entre coluna `evolucao_id` em `termos_emitidos` vs. materializar `ProntuarioAnexo` espelho afeta a query da timeline. Decisão conjunta dev+db; CAs C1–C4 são o contrato.
- **PDF probatório acoplado à evidência de link**: o ajuste do bloco de evidência (D8/CA-REG1) deve preservar termos legados — cuidado para não quebrar a geração de PDF de termos antigos.
- **`termo_emitido_acesso_log` órfã de escrita**: a tabela deixa de receber inserts. Não dropar agora (audit histórico). Documentar como legada.

## 9. Observações para execução

**Não-negociável**:
- Remoção **total** do aceite por link (back + front + domínio + rota + testes da feature morta) — CA-A1..A4.
- Migration de transição idempotente; legados terminais intocados; nada deletado — CA-A5/A6/A7.
- Conversão foto→PDF multi-página no backend, SHA-256 do PDF resultante, reuso do fluxo `PdfAnexado` — CA-B1/B2/B3.
- Validação por **magic bytes reais** (JPG/PNG/PDF), HEIC rejeitado — CA-B4/B5. Back é fonte da verdade — CA-B7.
- Emitir+anexar pela evolução com `EvolucaoId`, visível nos dois lugares sem duplicar binário — CA-C1/C2.
- Audit nos dois trilhos, best-effort — CA-LGPD2/3/4. Multi-tenant + RBAC — CA-MT1/2, CA-RBAC1/2. Sem PII em path/log — CA-LGPD1.
- Manter valor `PdfAnexado` no schema; manter `Recusado`/`Expirado` no enum (compat) — §4 D6/D7.

**Liberdade técnica (dev/db decidem, registrar no PR)**:
- Forma do vínculo evolução↔termo (coluna `evolucao_id` recomendada vs. `ProntuarioAnexo` espelho) — §4 D9.
- Emitir+anexar foto em uma etapa (wizard) vs. duas etapas (emitir Pendente → anexar pela lista) — desde que o caminho exista (CA-B1).
- Recompressão da imagem antes de embutir no PDF; layout da página (margem, fit).
- N imagens vs. fixo em 2 (frente/verso) — default 2.
- Granularidade do texto do bloco de evidência ajustado (CA-REG1), respeitando "sem token completo" e "hash do PDF como evidência".
- Limpeza opcional de `token_aceite`/`token_expira_em` dos legados migrados.

**Reuso obrigatório (grep antes de criar)**:
- `AnexarPdfTermoCommandHandler` (magic bytes, hash, upload, `AnexarPdf` → `Assinado`) — estender, não duplicar.
- `ITermoPdfStorageService` / `S3TermoPdfStorageService` (upload no bucket de termos).
- `AdicionarAnexoCommandHandler` + `ProntuarioAnexo` (padrão `EvolucaoId`, `IProntuarioAcessoLogService.Escrita`, sanitização de path) — modelo para o trilho da evolução.
- `EmitirTermoModal` passo 1 (seleção de modelo) — reuso no fluxo da evolução (CA-C4).
- `QuestPDF` (já na Infra; usado por `QuestPdfReceitaService`/`QuestPdfTermoService`) — para compor o PDF a partir de imagens.
- `ITermoAuditLogger` (`termo-pdf-anexado`).
- `StorageOptions.MimeTypesPermitidos` (já tem pdf/png/jpeg — confirmar; sem mudança esperada).

**Aciona `imedto-database`**: **sim** — migration de transição dos `AceiteLink` `Pendente` → `Expirado` (idempotente) + (provável) coluna `evolucao_id BIGINT NULL` com FK multi-tenant e índice em `termos_emitidos`. Forma final do vínculo decidida em conjunto com o dev.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — (a) na seção de termos/serviços, registrar que o **aceite por link foi removido** (controller público, query por token, e-mail de link, reenvio, recusa, expiração pública), que `AssinaturaTipo` para termos novos é só documento físico (valor `PdfAnexado` mantido no schema), e a transição de estados nova `Pendente → Assinado → Revogado`; (b) registrar o **fluxo de anexo com conversão foto→PDF** (QuestPDF compõe PDF multi-página a partir de JPG/PNG; SHA-256 do PDF resultante; reusa `AnexarPdf`); (c) registrar o **vínculo termo↔evolução** (forma adotada — `evolucao_id` em `termos_emitidos` ou anexo espelho) e que o documento aparece na timeline da evolução e na aba de termos sem duplicar binário; (d) atualizar a nota do PDF probatório (briefing 2026-06-10_002) indicando que o bloco de evidência degrada para "documento físico" em termos novos. Remover/ajustar a menção a `AceiteTermoPublicoView` como espelho do fluxo público de agendamento (linha ~539) — o espelho de termos deixou de existir; o de agendamento permanece.
- **`Docs/LGPD.md`** — (a) atualizar a seção do PDF probatório (briefing 2026-06-10_002) para o bloco de evidência de termo físico (hash do PDF, sem token/IP de aceite público); (b) adicionar nota registrando que **o aceite por link público de termo foi removido**, que `termo_emitido_acesso_log` passa a ser **tabela legada** (sem novas escritas; histórico preservado, não dropada) e que o relatório de acessos ao titular (briefing 2026-06-10_007) já a excluía do MVP; (c) registrar o **audit do anexo pela evolução** em `prontuario_acesso_log` (`Escrita`, best-effort), além do `termo_audit_log` (`termo-pdf-anexado`); (d) registrar a aceitação de **JPG/PNG convertidos para PDF**, com validação por magic bytes e nome de arquivo sem PII (GUID). Incremental/cirúrgica.
- **`Docs/DESIGN.md`** — sem componente novo de design system (reusa `AppModal`, `AppButton`, `input[type=file]`, padrões de ação existentes). Se o dev criar um componente reutilizável de captura/preview de foto (frente/verso) no design system, registrá-lo aqui — caso contrário, "nenhuma alteração".
- **`Docs/INFRA.md` / `Docs/COMANDOS.md`** — sem alteração (QuestPDF já embarcado; sem recurso de infra novo; a migration segue o fluxo já documentado de `db/migrations/`).

## 11. Critério de pronto (Definition of Done)

- CA-A1..A7, CA-B1..B8, CA-C1..C4, CA-MT1/2, CA-RBAC1/2, CA-LGPD1..4, CA-EST1/2, CA-REG1/2 verdes (validados por QA via código + suíte + smoke local; validação visual do PDF convertido e da câmera mobile fica para o usuário em produção, conforme limitação do sandbox).
- `grep` por `TermoPublicoController`, `AceiteTermoPublicoView`, `RegistrarRespostaPublicaTermoCommand`, `ReenviarLinkTermoCommand`, `EnviarEmailTermoLinkEventHandler`, `montarUrlAceitePublico` = vazio.
- Build + testes do backend e do frontend verdes (testes da feature morta removidos).
- Migration de transição aplicada e idempotente (validação de contagem por `imedto-database`).
- `Docs/ARQUITETURA.md` e `Docs/LGPD.md` atualizados.
