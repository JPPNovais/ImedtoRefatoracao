# PDF probatório do termo de consentimento aceito digitalmente (destravar stub 501)

**ID**: 2026-06-10_002
**Status**: Aprovado por usuário em 2026-06-10 (execução autônoma — decisões de produto fornecidas pelo orquestrador; ambiguidades residuais resolvidas no §4 "Decisões e assunções")
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P-M
**Áreas regressivas tocadas**: termos (leitura/geração de PDF), permissionamento (ação `termos`), LGPD (audit/minimização). Não toca: emissão/revogação de termo, fluxo de aceite público por token, upload manual de PDF assinado, prontuário, financeiro.

## 1. Contexto e motivação

O item 1.2 da [FASE_1_COMPLETUDE](../Docs/Roadmap/FASE_1_COMPLETUDE.md): o endpoint `GET /api/termos/{id}/pdf-gerado` retorna **501** (stub real, confirmado em `PacienteTermoController.cs:150-160`). O fluxo de aceite público por link já funciona — o paciente aceita pelo token, e o aggregate `TermoEmitido` registra a evidência (`AssinadoEm`, `IpAssinatura`, `UserAgentAssinatura`), além do `TermoEmitidoAcessoLog` (trilha pública: visualizou/aceitou/recusou) — mas **não existe um PDF probatório gerado pelo servidor** que materialize "o que foi aceito + a evidência do aceite". Hoje o front, quando o termo não tem PDF anexado manualmente, gera um PDF localmente via `useTermoPdf` (jsPDF) que **não inclui a evidência do aceite** (IP, data/hora, identificação) — ele monta só o snapshot + hash. O endpoint `pdf-gerado` nunca é chamado.

A demanda gera no servidor (QuestPDF) o **PDF probatório**: snapshot exato da versão aceita + bloco de evidência do aceite + marca d'água por status. Isso fecha o ciclo de validade jurídica do consentimento digital (argumento de venda em LGPD/saúde) e torna o documento consistente e auditável, independente do browser — espelhando a decisão já tomada para receita (briefing 2026-06-10_001).

## 2. Persona-alvo

- **Recepção / Profissional** com ação `termos.emitir`, e **Dono / Admin** com `termos.gerenciar_modelos` — qualquer um que abra os termos do paciente (o controller exige `[RequiresAcao("termos", "emitir")]` no GET).
- Momento da jornada: **pós-aceite** — o paciente já aceitou pelo link; o estabelecimento precisa do documento probatório para arquivo, auditoria ou resposta a um titular/órgão.
- Frequência: média — sob demanda quando se precisa comprovar o consentimento.

## 3. Escopo

**Inclui**:
- **Backend — destravar `GET /api/termos/{id}/pdf-gerado`**: substituir o 501 por geração real do PDF probatório via QuestPDF, espelhando a identidade institucional (mesmo helper de cabeçalho/logo/bloco-paciente/rodapé/Nunito do `QuestPdfReceitaService`). Retorna `FileContentResult` `application/pdf`.
- **Conteúdo do PDF** (ver §4 e §6): snapshot exato da versão aceita (`ConteudoSnapshotHtml`) + **bloco de evidência do aceite** + marca d'água por status (mesmas regras do `useTermoPdf` local: Assinado sutil "IMEDTO"; Revogado diagonal vermelho "REVOGADO"; Pendente "AGUARDANDO ASSINATURA").
- **Audit LGPD**: acesso ao PDF gerado é auditado via `ITermoAuditLogger.RegistrarAsync(..., acao: "termo-pdf-gerado", entidade: "TermoEmitido", entidadeId)`, seguindo o padrão de `ObterUrlPdfTermoQueryHandlers` (`"termo-pdf-baixou"`) e `ObterTermoEmitidoQueryHandlers` (`"termo-snapshot-visualizado"`).
- **Multi-tenant + RBAC**: filtro `estabelecimento_id` na leitura do termo; ação `termos` no controller (já presente no stub). Validação de existência do termo no tenant com mensagem genérica.
- **Front — `PacienteTermosTab.vue`**: o botão "Baixar PDF" passa a usar o **gerado pelo servidor** quando **não houver anexo manual** (`!t.temPdf`), substituindo a chamada atual ao `useTermoPdf` (jsPDF). Quando **houver** anexo manual (`t.temPdf`), o comportamento continua sendo baixar o anexo via presigned URL (`obterUrlPdf`).
- **Service do front**: novo método `pacienteTermoService.baixarPdfGerado(termoId)` que consome o endpoint como `Blob` (espelha `receitaService.baixarPdf`).

**Não inclui** (explicitamente fora):
- Substituir o anexo manual: para termo com PDF anexado (papel escaneado / `PdfAnexado` assinado), o **anexo manual continua sendo o documento** — o gerado não o substitui (decisão de produto). O gerado só atende o caso "sem anexo manual".
- Remover o composable `useTermoPdf` do front imediatamente: ver §4 (decisão 7) — manter para o caso de "Gerar PDF para impressão" de termo **Pendente** (folha para assinar à mão), mas o "Baixar PDF" do caso sem-anexo passa a usar o servidor. (Liberdade do dev quanto a consolidar usos — ver §4.)
- Assinatura digital ICP do termo (item 1.3).
- Alterar o fluxo de aceite público, emissão, revogação ou upload manual.
- Qualquer mudança de schema (validado: ver §5).
- Expor o token de aceite completo em qualquer lugar do PDF (ver §4 — só os últimos 6 caracteres / hash).

## 4. Decisões e assunções (execução autônoma)

Decisões fornecidas pelo orquestrador, complementadas por defaults grounded no código:

1. **Conteúdo = snapshot exato da versão aceita + bloco de evidência + marca d'água por status.** (Fornecida.) O snapshot vem de `TermoEmitido.ConteudoSnapshotHtml` (imutável, já resolvido e sanitizado na emissão). O servidor renderiza esse HTML; **reusar a estratégia de "HTML → blocos textuais" do `useTermoPdf`** portada para C#/QuestPDF, ou um `StripHtml`/parser simples coerente — o objetivo é fidelidade ao texto aceito, não fidelidade de CSS.
2. **Bloco de evidência do aceite contém**: data/hora do aceite (`AssinadoEm`), identificação de quem aceitou **conforme registrado** (para `AceiteLink`: o paciente do termo — `nome_completo` do paciente; mais IP de origem `IpAssinatura` e user-agent `UserAgentAssinatura` quando presentes), **hash de integridade** do conteúdo (`HashIntegridade`, SHA-256) e **os últimos 6 caracteres do token** (`TokenAceite[^6..]`) — **nunca o token completo**. Dados do acesso registrados no `TermoEmitidoAcessoLog` (ação/IP/UA/quando do "aceitou") podem compor a evidência. (Fornecida.)
3. **Marca d'água por status** = mesmas regras do `useTermoPdf`: `Assinado` → "IMEDTO" sutil; `Revogado` → "REVOGADO" diagonal vermelho; `Pendente` → "AGUARDANDO ASSINATURA". (Fornecida.) **[Assunção] `Recusado` e `Expirado`**: tratá-los como "sem assinatura ativa" → marca d'água "IMEDTO" sutil + bloco de evidência indicando o status (Recusado: data/IP da recusa, que reusa `AssinadoEm`/`IpAssinatura`; Expirado: indicar que o link expirou sem aceite). Não inventar marca d'água nova para esses dois — o domínio só tinha regra explícita para Assinado/Revogado/Pendente.
4. **Anexo manual prevalece**: termo com PDF anexado → o anexo é o documento; o gerado não o substitui. (Fornecida.) Reflexo no front (decisão 6).
5. **Acesso ao PDF gerado é auditado**; multi-tenant + RBAC ação `termos`. (Fornecida.)
6. **Front — "Baixar PDF" usa o gerado quando não houver anexo manual.** (Fornecida.) Mapeamento concreto no `PacienteTermosTab.vue`:
   - `t.temPdf === true` → "Baixar PDF anexado" via `obterUrlPdf` (presigned). **Inalterado.**
   - `t.temPdf === false` → o botão de download passa a chamar `pacienteTermoService.baixarPdfGerado(t.id)` (servidor) em vez de `gerarPdfImpressao` (jsPDF local).
7. **[Assunção] Destino do `useTermoPdf` (jsPDF)**: manter o composable para o caso de **gerar a folha de impressão de um termo Pendente do tipo `PdfAnexado`** (o emissor imprime, colhe assinatura física e reanexa) — esse é um caso de "documento de trabalho", não probatório, e o fluxo `onEmitido` já o usa para `download` logo após emitir um `PdfAnexado`. **Liberdade do dev**: se concluir que o servidor cobre bem também esse caso (Pendente com marca "AGUARDANDO ASSINATURA"), pode redirecionar também a folha de impressão para o servidor e reduzir o uso do jsPDF — desde que (a) emitir um `PdfAnexado` continue oferecendo o PDF para impressão imediata e (b) nenhum CA regrida. O default mais simples (menor diff) é trocar **apenas** o "Baixar PDF" do caso sem-anexo, conforme decisão 6, e deixar o jsPDF nos demais usos.
8. **[Assunção] Termo Pendente pode gerar PDF probatório?** Sim, mas com marca d'água "AGUARDANDO ASSINATURA" e **bloco de evidência vazio/indicando "ainda não assinado"** (sem `AssinadoEm`/IP). O endpoint não bloqueia por status — gera o documento coerente com o estado. (Espelha o `useTermoPdf`, que gera para qualquer status.) Não há regra de produto pedindo 422 por status aqui (diferente da receita Rascunho, porque termo Pendente já é um documento emitido com snapshot fixado; rascunho de receita não é).
9. **[Assunção] Onde mora a geração**: novo serviço `ITermoPdfGeradoService` + impl `QuestPdfTermoService` em `Imedto.Backend.Infrastructure/Termos/`, espelhando a estrutura de `QuestPdfReceitaService` (mesmas constantes de cor sincronizadas com `PDF_THEME`, registro de fontes Nunito, download best-effort da logo, leitura Dapper). O controller injeta o serviço e chama no `ObterPdfGerado`. Alternativa rejeitada: reaproveitar `QuestPdfReceitaService` (acopla dois aggregates num serviço). **Reuso obrigatório dos helpers comuns** (cabeçalho/logo/bloco-paciente/marca-d'água) — extrair para um helper compartilhado se reduzir duplicação; liberdade do dev (ver §9).
10. **[Assunção] Dados necessários para o PDF**: termo (`ObterPorIdComSnapshot` já devolve snapshot + hash + IP/UA via `TermoEmitidoDetalheDto`), dados do paciente (nome/CPF/nascimento/gênero/telefone — para o bloco de paciente do cabeçalho), dados do estabelecimento (logo/nome/CNPJ/endereço/telefone). **[Assunção]** A trilha do `TermoEmitidoAcessoLog` (registro "aceitou") é **opcional** no bloco de evidência: se for trivial ler (o `TermoEmitidoRepository` já tem `SalvarAcessoLog`; pode não haver método de leitura), inclui-se "registrado em {data} via link" a partir do log; se exigir novo método de query repo, **usar a evidência já presente no aggregate** (`AssinadoEm`/`IpAssinatura`/`UserAgentAssinatura`) é suficiente para o probatório — não criar query nova só para isso nesta entrega. Default: usar a evidência do aggregate; o log público é um plus se barato.
11. **[Assunção] Nome do arquivo**: `termo-{id}.pdf` no `Content-Disposition` do servidor (sem PII — minimização LGPD), espelhando a decisão de receita (`receita-{id}.pdf`).
12. **[Assunção] Multi-tenant na query**: a leitura do termo filtra `estabelecimento_id = @EstabelecimentoId`; termo de outro tenant → `null` → `BusinessException("Termo não encontrado.")` (genérica). O paciente é resolvido pelo `paciente_id` do termo no mesmo tenant.

## 5. Modelo de dados

**Schema NÃO muda.** Validado na investigação:
- `public.termos_emitidos` (aggregate `TermoEmitido`) — colunas usadas: `id, paciente_id, estabelecimento_id, termo_modelo_id, versao_modelo, conteudo_snapshot_html, conteudo_snapshot_texto, status, assinatura_tipo, assinado_em, ip_assinatura, user_agent_assinatura, hash_integridade, pdf_url, token_aceite, revogado_em, revogado_motivo, emitido_por_usuario_id, criado_em`. Tudo já mapeado e exposto via `TermoEmitidoDetalheDto`.
- `public.termo_emitido_acesso_log` (`TermoEmitidoAcessoLog`) — trilha pública (IP/UA/ação/quando). Leitura **opcional** (ver decisão 10).
- `public.termo_audit_log` — destino do audit de acesso ao PDF (via `ITermoAuditLogger`, já existe).
- `public.pacientes`, `public.estabelecimentos`, `public.usuarios` — para bloco de paciente/cabeçalho institucional.

**Índices**: a leitura é por `id + estabelecimento_id` (PK + tenant) do termo e por `id` do paciente/estabelecimento — todos cobertos por índices existentes (mesma rota de `ObterPorIdComSnapshot` / `ObterTermoEmitidoQuery`). **Não cria coluna, não cria índice.** Não acionar `imedto-database`.

**Sem novo audit table** — reusa `ITermoAuditLogger` (`termo_audit_log`).

## 6. UX e fluxo

**PDF probatório (servidor)** — estrutura visual, espelhando `QuestPdfReceitaService` + regras do `useTermoPdf`:
1. **Cabeçalho institucional**: logo do estabelecimento (ou placeholder de iniciais), nome/CNPJ, contato; linha dupla; título `TERMO DE CONSENTIMENTO — {CATEGORIA}` + subtítulo `Modelo: {titulo} (v{versao})`.
2. **Bloco do paciente**: nome, idade, sexo, CPF, nascimento, telefone (mesmo card do helper compartilhado).
3. **Metadados curtos**: `Emitido em {criado_em}` · `Emitido por: {nome do emissor}` (se disponível) · `ID #{id}`.
4. **Corpo**: snapshot HTML renderizado como texto rico simples (parágrafos, títulos, listas) — fidelidade ao texto aceito.
5. **Bloco de evidência do aceite** (novo, destacado — ex.: card cinza/borda):
   - **Assinado**: "Aceito digitalmente em {assinado_em}" · "Por: {nome do paciente}" · "IP de origem: {ip_assinatura}" (se houver) · "Dispositivo: {user_agent}" (se houver, truncado) · "Identificador do aceite: …{últimos 6 chars do token}" · "Hash de integridade (SHA-256): {hash_integridade}".
   - **Pendente**: "Documento ainda não assinado — aguardando aceite." (sem IP/data de aceite).
   - **Recusado**: "Recusado em {assinado_em}" + IP, se houver.
   - **Expirado**: "Link de aceite expirou sem assinatura."
   - **Revogado**: além do acima (estado pré-revogação), bloco de revogação em vermelho: "REVOGADO EM {revogado_em}" + "Motivo: {revogado_motivo}".
6. **Marca d'água** por status (decisão 3).
7. **Rodapé**: assinatura/nota coerente com status (Pendente: "Assine no espaço acima e devolva…"; Assinado: "Aceito digitalmente — evidência registrada acima."; etc.) + `Hash: {hash[..16]}…` + página x de y + "Emitido em {agora}".

**Front (`PacienteTermosTab.vue`)** — coluna "Ações" da tabela:
- Botão **"Baixar PDF anexado"** (`fa-download`, `v-if="t.temPdf"`) → `baixarPdfAnexado` (presigned). **Inalterado.**
- Botão **"Baixar PDF"** (`v-else`, hoje `fa-print` chamando `gerarPdfImpressao`) → passa a chamar uma função que baixa o **gerado do servidor** via `pacienteTermoService.baixarPdfGerado(t.id)` (blob → download). Ícone sugerido `fa-file-pdf`/`fa-download` (dev decide; pode manter `fa-print` se o significado "obter PDF" se mantiver claro). Estado `:disabled="acaoEmAndamentoId === t.id"` durante a chamada (padrão já existente na tabela).
- **Drawer de visualização** (`TermoVisualizacaoDrawer`): o evento `@gerar-pdf` (`drawerGerarPdf`) hoje usa `useTermoPdf`. **[Assunção]** alinhar o drawer ao mesmo critério: se o termo não tem anexo, "Gerar PDF" baixa o gerado do servidor; se tem anexo, baixa o anexo. Liberdade do dev para manter o drawer no jsPDF se o esforço de alinhamento for desproporcional — **mas o "Baixar PDF" da tabela (caso sem-anexo) é obrigatório no servidor** (CA1). Registrar a escolha.

**Estados**:
- **Loading por linha**: `acaoEmAndamentoId === t.id` desabilita o botão durante o fetch (padrão já existente).
- **Erro**: toast genérico `e?.response?.data?.mensagem ?? "Erro ao gerar o PDF."` (sem PII).
- **Multi-tenant/404**: mensagem genérica "Termo não encontrado." vinda do backend.

**Mobile-ready**: a tabela já é responsiva no padrão da view; o botão acompanha.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — PDF gerado de termo assinado sem anexo)**: Dado um termo `Assinado` por link (`AceiteLink`, `temPdf = false`), Quando o usuário clica em "Baixar PDF" na aba de termos, Então `GET /api/termos/{id}/pdf-gerado` retorna `200` `application/pdf` e o front baixa um PDF com cabeçalho institucional, snapshot do termo, bloco de evidência (data/hora do aceite, identificação, hash, últimos 6 caracteres do token) e marca d'água "IMEDTO" sutil.
- **CA2 (snapshot fiel à versão aceita)**: Dado um termo emitido na versão N de um modelo e o modelo posteriormente editado para a versão N+1, Quando o PDF gerado é produzido, Então o conteúdo do corpo corresponde ao `conteudo_snapshot_html` da emissão (versão N), **não** à versão atual do modelo; e o subtítulo indica `v{N}`.
- **CA3 (evidência — token nunca completo)**: Dado o PDF gerado de um termo assinado por link, Quando o bloco de evidência é inspecionado, Então aparece apenas o **hash** e os **últimos 6 caracteres** do token de aceite — o token completo **não** aparece em lugar nenhum do PDF.
- **CA4 (marca d'água por status)**: Dado três termos nos status `Assinado`, `Revogado` e `Pendente`, Quando cada PDF é gerado, Então as marcas d'água são, respectivamente, "IMEDTO" sutil, "REVOGADO" diagonal vermelho e "AGUARDANDO ASSINATURA"; o termo `Revogado` exibe ainda o bloco "REVOGADO EM {data}" + motivo.
- **CA5 (Pendente gera sem evidência de aceite)**: Dado um termo `Pendente`, Quando o PDF é gerado, Então o documento é produzido com marca d'água "AGUARDANDO ASSINATURA" e o bloco de evidência indica "ainda não assinado" (sem IP/data de aceite), sem erro.
- **CA6 (anexo manual prevalece no front)**: Dado um termo com PDF anexado manualmente (`temPdf = true`), Quando o usuário clica no botão de download, Então o front baixa o **anexo** via presigned URL (`obterUrlPdf`) — **não** chama `pdf-gerado`; o gerado não substitui o anexo.
- **CA7 (audit LGPD no acesso ao PDF gerado)**: Dado um acesso bem-sucedido a `GET /api/termos/{id}/pdf-gerado`, Quando concluído, Então é registrada uma linha em `termo_audit_log` via `ITermoAuditLogger` com `{ estabelecimento_id, usuario_id = solicitante, acao = "termo-pdf-gerado", entidade = "TermoEmitido", entidade_id = id }`.
- **CA8 (audit best-effort)**: Dado que a gravação do audit falhe, Quando o PDF gerado é solicitado, Então o usuário ainda recebe o `200` com o PDF (o audit nunca bloqueia o fluxo — `ITermoAuditLogger` não lança).
- **CA9 (multi-tenant)**: Dado um usuário autenticado no estabelecimento B, Quando chama `GET /api/termos/{id}/pdf-gerado` para um termo do estabelecimento A, Então recebe erro genérico ("Termo não encontrado.") e nenhum conteúdo do termo de A vaza; nada é logado com PII.
- **CA10 (RBAC)**: Dado um usuário sem a ação `termos`, Quando chama `GET /api/termos/{id}/pdf-gerado`, Então recebe `403`; e no front o botão de download de termo não é alcançável (a aba de termos já é gated por `termos.emitir`/`gerenciar_modelos`).
- **CA11 (LGPD — minimização no transporte)**: Dado o download do PDF gerado, Quando o `Content-Disposition` e os logs do servidor são inspecionados, Então o nome do arquivo é `termo-{id}.pdf` (sem nome/CPF do paciente) e nenhum log contém o conteúdo do termo, IP do paciente, token ou hash em texto de log.
- **CA12 (501 eliminado)**: Dado `grep` por `501`/`NotImplemented` no `PacienteTermoController`, Quando executado após a entrega, Então o `ObterPdfGerado` **não** retorna mais 501 (stub eliminado).
- **CA13 (identidade institucional consistente)**: Dado o PDF gerado, Quando comparado ao PDF de receita do servidor, Então usa a mesma identidade visual base (fonte Nunito, cabeçalho com logo do estabelecimento/placeholder, bloco do paciente, rodapé com página x de y).
- **CA14 (estado de loading no front)**: Dado o clique no botão de download do PDF gerado, Quando o blob está sendo buscado, Então o botão da linha fica desabilitado (`acaoEmAndamentoId === t.id`) e volta ao normal ao concluir (sucesso ou erro).
- **CA15 (regressão — fluxos existentes)**: Dado os fluxos atuais de termos (listar, visualizar no drawer, emitir, anexar PDF manual, copiar/reenviar link, revogar), Quando a feature é entregue, Então todos continuam funcionando inalterados; o `useTermoPdf` permanece operante nos usos que não foram redirecionados ao servidor.

## 8. Riscos e dependências

- **Fidelidade do render HTML→PDF**: o snapshot é HTML; QuestPDF não renderiza HTML/CSS nativamente. Reusar a abordagem "HTML → blocos textuais" do `useTermoPdf` portada para C# garante fidelidade ao **texto** aceito (parágrafos, títulos, listas) — não à formatação CSS. Validar que termos com listas/headings saem legíveis (CA1/CA2). Risco de termo com HTML complexo perder formatação fina — aceitável para um probatório (o hash garante integridade do conteúdo original).
- **Evidência incompleta para termos antigos**: termos assinados antes do registro de IP/UA podem ter `IpAssinatura`/`UserAgentAssinatura` nulos. O bloco de evidência deve degradar (omitir o campo nulo, manter data/hash/token-parcial). CA3 cobre o caso com dados; o dev trata nulos sem quebrar.
- **Duplicação de código de PDF**: `QuestPdfTermoService` espelha muito de `QuestPdfReceitaService` (cores, fontes, cabeçalho, bloco paciente, marca d'água). **Risco de copy-paste divergente.** Mitigação: extrair os helpers comuns (cabeçalho institucional, bloco do paciente, registro de fontes Nunito, download da logo, marca d'água parametrizável) para um utilitário compartilhado em `Infrastructure` e ambos os serviços consumirem — ver §9 (liberdade técnica com forte recomendação de reuso).
- **DI e fontes**: o registro de fontes Nunito hoje é `static`/idempotente dentro de `QuestPdfReceitaService`. Garantir que o serviço de termo não re-registre conflitando — se extrair para helper compartilhado, o registro fica num único lugar. Confirmar `QuestPDF.Settings.License = Community` (já configurado).
- **Token parcial**: cuidado para nunca logar/expor o token completo (CA3/CA11). O `TokenAceite` é segredo de fluxo público.
- **Front — não quebrar o caso com anexo**: a troca do "Baixar PDF" deve respeitar `temPdf` (CA6) — só o caso sem-anexo vai para o servidor.
- **Dependência**: nenhuma externa. Independente do item 1.3 (assinatura ICP).

## 9. Observações para execução

**Não-negociável**:
- Eliminar o 501 do `ObterPdfGerado` (CA12).
- Snapshot fiel à versão aceita (CA2) — ler `conteudo_snapshot_html`, nunca a versão atual do modelo.
- Token **nunca** completo no PDF — só hash + últimos 6 caracteres (CA3/CA11).
- Marca d'água por status conforme regras do `useTermoPdf` (CA4/CA5).
- Anexo manual prevalece no front (CA6).
- Audit de acesso ao PDF gerado via `ITermoAuditLogger`, best-effort (CA7/CA8).
- Multi-tenant + RBAC (CA9/CA10).
- Sem PII no nome do arquivo do servidor nem em logs (CA11).

**Liberdade técnica (dev decide)** — com recomendações:
- **Fortemente recomendado**: extrair helpers comuns de PDF (cabeçalho institucional, bloco do paciente, fontes Nunito, logo best-effort, marca d'água parametrizável) compartilhados entre `QuestPdfReceitaService` e o novo `QuestPdfTermoService`, em vez de duplicar. Se o esforço de refator for desproporcional ao prazo, duplicar é tolerável **desde que** as constantes de cor permaneçam sincronizadas com `PDF_THEME` e o resultado visual seja consistente (CA13) — mas registrar a dívida.
- Parser HTML→blocos: portar a lógica do `useTermoPdf` ou usar `StripHtml` + heurística de parágrafos; o critério é legibilidade do texto (CA1/CA2).
- Destino do `useTermoPdf` no front (manter para folha de impressão de Pendente vs redirecionar tudo ao servidor) — decisão 7. Default mínimo: trocar só o "Baixar PDF" do caso sem-anexo.
- Incluir ou não a trilha do `TermoEmitidoAcessoLog` no bloco de evidência (decisão 10) — default: usar a evidência do aggregate.
- Ícone/rótulo do botão no front.

**Reuso obrigatório (grep antes de criar)**:
- `QuestPdfReceitaService` (modelo de estrutura + constantes de cor + fontes Nunito + logo best-effort).
- `ITermoAuditLogger` (padrão de `ObterUrlPdfTermoQueryHandlers` / `ObterTermoEmitidoQueryHandlers`).
- `ITermoEmitidoQueryRepository.ObterPorIdComSnapshot` / `TermoEmitidoDetalheDto` (snapshot + evidência já disponíveis).
- `useTermoPdf` (regras de marca d'água — portar, não reinventar).
- `receitaService.baixarPdf` (modelo do novo `pacienteTermoService.baixarPdfGerado`).
- Padrão de download de blob no front (alinhar com o que for definido no briefing 2026-06-10_001).

**Aciona `imedto-database`**: não — schema não muda; índices de leitura por id+tenant já existem.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar, na seção de leitura/serviços (read-side / Infrastructure), uma nota curta sobre o **padrão de geração de PDF server-side via QuestPDF** agora aplicado a dois documentos (receita e termo), citando os serviços `QuestPdfReceitaService` e `QuestPdfTermoService` (e o helper compartilhado, se o dev extrair). Mudança incremental, cirúrgica.
- **`Docs/LGPD.md`** — adicionar uma linha registrando que **a geração do PDF probatório de termo é auditada** em `termo_audit_log` (ação `termo-pdf-gerado`) e que o PDF expõe apenas hash + últimos 6 caracteres do token (nunca o token completo) — minimização de segredo. Incremental.
- **`Docs/DESIGN.md`** — sem alteração (reusa AppButton e padrão de ação existente; nenhum componente novo de design system no front).
- **`Docs/INFRA.md` / `Docs/COMANDOS.md`** — sem alteração (sem recurso novo de infra, sem script/migration novo; QuestPDF e fontes Nunito já embarcadas no assembly de Infrastructure).

## 11. Critério de pronto (Definition of Done)

- Todos os CA1–CA15 verdes (validados por QA via análise de código + suíte automatizada; validação visual do PDF fica para o usuário em produção, conforme limitação de browser no sandbox).
- `grep 501` no `PacienteTermoController` = vazio para o `pdf-gerado` (CA12).
- Suíte de testes do backend cobre: snapshot fiel (CA2), token nunca completo (CA3), marca d'água por status (CA4/CA5), audit registrado (CA7), multi-tenant (CA9).
- Front: "Baixar PDF" do caso sem-anexo usa o servidor; caso com-anexo inalterado (CA1/CA6/CA14).
- `Docs/ARQUITETURA.md` e `Docs/LGPD.md` atualizados.
- Sem mudança de schema; sem migration nova.
