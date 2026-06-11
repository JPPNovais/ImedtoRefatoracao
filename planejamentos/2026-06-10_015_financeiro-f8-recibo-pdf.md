# Financeiro F8 — Recibo de pagamento (PDF interno, sem valor fiscal)

**ID**: 2026-06-10_015
**Status**: Aprovado por usuário em 2026-06-10 (execução autônoma — decisões de produto fornecidas pelo orquestrador; ambiguidades residuais resolvidas no §4 "Decisões e assunções")
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P (S — fase pequena: 1 ação, 1 endpoint, 1 PDF reusando pipeline existente, 1 coluna)
**Áreas regressivas tocadas**: financeiro (`Pagamento` ganha `recibo_emitido_em`; aba Financeiro do paciente F2 e `PaymentModal` F1 ganham ação "Emitir recibo"), relatório/PDF (novo gerador server-side reusando pipeline). **Não toca**: estoque, prontuário clínico, agenda, NFS-e (F9).

> **Fonte de verdade da visão**: `Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md` §1.7 (Recibo ≠ NF), §2.1 (recibo não é entidade nova; flag `recibo_emitido_em?` decidida aqui) e §F8 (IN/OUT/CAs). Este briefing é a fonte de verdade da **execução da F8** — imutável; gap vira addendum.
> **Fundação**: F1 (`planejamentos/2026-06-10_009`) entregou `Cobranca`/`Pagamento`, `PaymentModal.vue`, `cobrancaService.ts`/`cobrancaStore.ts`, `CobrancaController`, permissões `financeiro_paciente.{ver,registrar}`. F2 (`planejamentos/2026-06-10_010`) entregou a aba Financeiro do paciente, `EstornoPagamento`/`estorno_pagamentos`, e o audit de acesso por paciente via `paciente_acesso_log`. **Não reabrir** decisões de F1/F2.
> **Padrão de PDF server-side**: `planejamentos/2026-06-10_001_pdf-receita-servidor.md` — geração no servidor com QuestPDF, fonte **Nunito** embarcada no assembly, cabeçalho institucional com dados do estabelecimento (logo/iniciais), bloco do paciente, rodapé. **Reusar fielmente** a mesma fundação (`usePdfHeader.ts` no front é a referência visual do cabeçalho; o gerador server-side de receita é o molde de implementação).

## 1. Contexto e motivação

Hoje, depois que o paciente paga (`Pagamento` quitado, F1/F2), não há como entregar a ele um **comprovante**. O plano mestre (§5 q4, resolvida) decidiu: emitir um **recibo em PDF interno**, sem valor fiscal, gerado pelo próprio Imedto. Não é nota fiscal — NFS-e é documento fiscal via gateway (F9), em bounded context próprio. O recibo é só um comprovante operacional de que aquele pagamento aconteceu. A persona (recepção/financeiro) precisa disso no balcão, na hora de fechar o atendimento.

A F8 é pequena e independente: reusa o pipeline de PDF que já existe (receita, termo probatório), não cria agregado novo, não cria fluxo de cobrança novo — apenas materializa um `Pagamento` já registrado num PDF.

## 2. Persona-alvo

- **Recepção / Financeiro / Dono** (papéis com acesso à aba Financeiro do paciente — gate `financeiro_paciente.ver`).
- Momento da jornada: **pós-pagamento** — a cobrança já tem um `Pagamento` quitado; o operador quer entregar o comprovante ao paciente, imediatamente após registrar o pagamento (porta `PaymentModal`) ou depois, revisitando a aba Financeiro (porta aba).
- Frequência: alta na recepção de clínica com volume de particular.

## 3. Escopo

**Inclui**:
- **Ação "Emitir recibo"** sobre um `Pagamento` **quitado e não estornado**, nas **duas portas**:
  1. **`PaymentModal` (F1)** — no estado "pago" pós-quitação (ação já prevista no protótipo).
  2. **Aba Financeiro do paciente (F2)** — uma ação por linha de `Pagamento` no histórico.
- **Endpoint de PDF no backend** que gera o recibo **on-the-fly** a partir do `Pagamento` (+ dados da `Cobranca`/paciente/estabelecimento), reusando o pipeline de PDF server-side (QuestPDF + Nunito + cabeçalho institucional).
- **Conteúdo do recibo**: cabeçalho do estabelecimento (padrão institucional já usado nos outros PDFs); nome do paciente; valor pago; forma de pagamento; nº de parcelas; data do pagamento; referência da cobrança (origem + descrição); quem registrou o pagamento; **rótulo explícito** "RECIBO — documento sem valor fiscal".
- **Flag de audit `recibo_emitido_em` (timestamptz NULL) em `pagamentos`** — registra a 1ª emissão para rastreio (ver §4 decisão 4 e §5).
- **Audit LGPD de acesso**: a emissão registra acesso financeiro do paciente no `paciente_acesso_log` (mesmo padrão da aba Financeiro, F2 — `Leitura`/exportação de comprovante).
- **Multi-tenant** falha-fechada (filtro `estabelecimento_id`).

**Não inclui** (explicitamente fora):
- **NFS-e / qualquer documento fiscal** → F9 (bounded context `Faturamento`, via gateway).
- **Recibo de cobrança ainda não paga** — recibo é de **pagamento** quitado, nunca de cobrança aberta.
- **Recibo de pagamento estornado** — bloqueado (ver §4 decisão 1).
- **Envio do recibo por e-mail / WhatsApp** — só geração/download na sessão.
- **Recibo agregado de múltiplos pagamentos** (1 recibo = 1 `Pagamento`).
- Redesenhar o pipeline de PDF — reusar o existente; ajuste de layout só se um CA falhar.

## 4. Decisões e assunções (execução autônoma)

1. **Pagamento estornado NÃO emite recibo → 422.** O simples e seguro: um `Pagamento` que tem `EstornoPagamento` associado (F2) não representa mais um pagamento válido; emitir recibo dele induziria o paciente a erro. **Decisão: bloquear com 422** ("Pagamento estornado não pode gerar recibo."), e no front a ação fica **oculta/desabilitada** em pagamento estornado. Rejeitada a alternativa "emitir com tarja ESTORNADO" — adiciona superfície visual e risco de o paciente reter um comprovante que parece válido. (Regra de negócio no backend.)
2. **RBAC = `financeiro_paciente.ver`.** Emitir recibo é **leitura/comprovante** de algo que já aconteceu (o pagamento já foi registrado por quem tinha `financeiro_paciente.registrar`). Quem pode **ver** a aba financeira pode emitir o comprovante. Não exige `registrar` (não cria nem altera valor). (Fornecida/confirmada.)
3. **Sem PII clínica no recibo.** CID/diagnóstico/medicação **nunca** aparecem. A **descrição da cobrança** (ex.: nome do procedimento, vinda da `Cobranca.descricao`) é dado financeiro/identificação de serviço, **não** diagnóstico — é aceitável no recibo. Caso a descrição de uma cobrança de procedimento contenha o **nome do procedimento**, é permitido; nome de procedimento ≠ diagnóstico. (Registrada.)
4. **Coluna `recibo_emitido_em` (timestamptz NULL) em `pagamentos` — SIM.** O plano mestre §2.1 deixou a decisão de schema para este briefing. **Decisão: criar a coluna** — é barata (1 `ALTER ADD COLUMN NULL`, sem default, sem backfill) e dá rastreio direto de "este pagamento já teve recibo emitido e quando". Gravada na **1ª emissão** (idempotente: emitir de novo não sobrescreve o timestamp original; reemissões não bloqueiam). Não substitui o audit de acesso do `paciente_acesso_log` — são camadas distintas (flag = fato no agregado financeiro; log = trilha de acesso a dado do paciente). Rejeitada a alternativa "só audit por log" — perde o rastreio no próprio `Pagamento` e dificulta um futuro "reimprimir 2ª via" com indicação visual.
5. **PDF gerado on-the-fly, sem armazenar arquivo.** Como receita: o endpoint gera os bytes a cada chamada. Sem S3, sem persistir o PDF. (Espelha o pipeline existente.)
6. **Audit de acesso reusa o padrão da F2** (`paciente_acesso_log`, tipo `Leitura` — emitir comprovante é acesso a dado financeiro do paciente), **best-effort** (falha do log não bloqueia a emissão), como nos demais acessos. (Assunção grounded no padrão F2/receita.)
7. **Nome do arquivo sem PII**: `recibo-{pagamentoId}.pdf` no `Content-Disposition` do servidor — minimização LGPD (sem nome/CPF do paciente no header nem em log). (Espelha 2026-06-10_001 decisão 11.)
8. **Reemissão livre.** Não há limite de quantas vezes o recibo pode ser baixado; cada emissão registra acesso no `paciente_acesso_log`; a flag `recibo_emitido_em` guarda só a **primeira**. (Operação real: 2ª via é comum.)

## 5. Modelo de dados

**1 alteração de schema** (aciona `imedto-database`):

```sql
ALTER TABLE pagamentos ADD COLUMN IF NOT EXISTS recibo_emitido_em timestamptz NULL;
```

- **Sem default, sem backfill, sem índice** — coluna de rastreio, lida/escrita 1:1 pelo agregado `Pagamento`. Pagamentos antigos ficam `NULL` (nunca emitiram recibo) — correto.
- **Multi-tenant**: nenhuma coluna de tenant nova — `pagamentos` é filha de `cobrancas`, que já carrega `estabelecimento_id`; toda leitura passa por `Cobranca` filtrada por tenant (F1/F2).
- **Audit**: reusa `paciente_acesso_log` (já existe, F2) — sem tabela nova.
- **Domínio**: `Pagamento` (em `Domain/Cobrancas/`) ganha o campo `ReciboEmitidoEm` e um método `RegistrarEmissaoRecibo()` que (a) valida que o pagamento **não está estornado** (lança `BusinessException` se estiver) e (b) seta `ReciboEmitidoEm` apenas se ainda `NULL`. A regra de "estornado → bloqueia" mora no Domain (espelhada no front como UX).

> Observação ao DB agent: confirmar nome real da tabela (`pagamentos`) e que a coluna não conflita. `ALTER ... ADD COLUMN ... NULL` é não-bloqueante no Postgres (sem rewrite). Sem trigger/function — regra no backend.

## 6. UX e fluxo

**Reuso visual obrigatório**: o cabeçalho do PDF segue `usePdfHeader.ts` (referência) e o molde do gerador de receita server-side (Nunito, logo/iniciais do estabelecimento, rodapé). Layout do recibo: cabeçalho institucional → título "RECIBO" + subtítulo/rótulo **"Documento sem valor fiscal"** → bloco do paciente (nome) → bloco do pagamento (valor pago em destaque, forma, parcelas, data) → referência da cobrança (origem + descrição) → "Registrado por: {nome de quem registrou}" → rodapé.

**Porta 1 — `PaymentModal.vue` (F1), estado pago**: a ação "Emitir recibo" (já prevista no protótipo) chama o endpoint, recebe o `Blob`, dispara download (padrão `<a download>` + `URL.createObjectURL`/`revokeObjectURL`, reusando o utilitário de download de blob já usado no PDF de receita). Botão em `:loading`/`:disabled` durante o fetch.

**Porta 2 — Aba Financeiro do paciente (F2)**: cada linha de `Pagamento` **quitado e não estornado** no histórico ganha a ação "Emitir recibo" (ícone `fa-file-pdf` ou `fa-receipt`, coerente com o set). Em pagamento **estornado** (linha riscada/atenuada da F2), a ação **não aparece**.

**Estados**:
- **Loading por ação**: botão desabilitado + spinner durante o fetch do blob.
- **Estornado**: ação oculta no front; se mesmo assim chamado, 422 com mensagem genérica.
- **Erro de rede/422/500**: toast genérico `e?.response?.data?.mensagem ?? "Erro ao emitir o recibo."` (sem PII).
- **Sucesso**: download inicia; toast discreto opcional ("Recibo gerado.").
- **Vazio**: cobrança sem pagamento quitado não exibe a ação (não há o que recibar).

**Mobile-ready**: a ação acompanha o layout responsivo já existente da aba/modal.

## 7. Critérios de aceite (testáveis)

- **CA118 (caminho feliz — recibo de pagamento quitado)**: Dado um `Pagamento` quitado e não estornado, Quando o usuário clica em "Emitir recibo", Então o endpoint retorna `200` com `Content-Type: application/pdf` e o front dispara o download de um PDF válido renderizado com a identidade institucional (Nunito, cabeçalho do estabelecimento) contendo: nome do paciente, valor pago, forma de pagamento, parcelas, data do pagamento, referência da cobrança (origem + descrição), quem registrou, e o rótulo "RECIBO — documento sem valor fiscal".
- **CA119 (PDF gerado no backend, não no front)**: Dado a emissão do recibo, Quando inspecionado o fluxo, Então os bytes do PDF são produzidos no servidor (pipeline QuestPDF reusado) e o front apenas baixa o blob — **nenhuma** geração de PDF do recibo no front (jsPDF/cliente).
- **CA120 (estornado → 422 + ação oculta)**: Dado um `Pagamento` que possui `EstornoPagamento`, Quando o endpoint de recibo é chamado para ele, Então responde `422` com mensagem genérica ("Pagamento estornado não pode gerar recibo.") e **nenhum** byte de PDF é retornado; E no front a ação "Emitir recibo" **não é renderizada** para esse pagamento.
- **CA121 (só pagamento existente do tenant)**: Dado um `Pagamento` inexistente ou de cobrança sem pagamento quitado, Quando o endpoint é chamado, Então responde `404`/"não encontrado" genérico, sem vazar dados.
- **CA122 (duas portas)**: Dado o `PaymentModal` no estado pago **e** uma linha de pagamento quitado na aba Financeiro do paciente, Quando o usuário aciona "Emitir recibo" em cada uma, Então **ambas** geram o mesmo recibo do mesmo `Pagamento` (mesmo endpoint, mesmo conteúdo) — sem lógica duplicada.
- **CA123 (RBAC — `financeiro_paciente.ver`)**: Dado um usuário com `financeiro_paciente.ver`, Quando emite recibo, Então recebe `200`; E Dado um usuário **sem** `financeiro_paciente.ver`, Quando chama o endpoint, Então recebe `403` e a ação não é alcançável no front (a aba financeira já é restrita por essa permissão).
- **CA124 (multi-tenant falha-fechada)**: Dado um usuário do estabelecimento B, Quando chama o endpoint de recibo para um `Pagamento` de uma `Cobranca` do estabelecimento A (id direto na rota), Então recebe `404`/"não encontrado" genérico, **nenhum** dado de A vaza e **nada** é logado com PII; sem tenant claim, o repositório retorna vazio/throws.
- **CA125 (LGPD — sem PII clínica no recibo)**: Dado o recibo gerado, Quando inspecionado o PDF, Então ele **não** contém CID, diagnóstico nem medicação; a descrição da cobrança (ex.: nome do procedimento) é permitida como dado financeiro; e o nome do arquivo no `Content-Disposition` é `recibo-{pagamentoId}.pdf` (sem nome/CPF do paciente).
- **CA126 (LGPD — sem PII em log)**: Dado a emissão do recibo, Quando inspecionados os logs do servidor, Então nenhum log contém nome, CPF, valor ou descrição do paciente/cobrança.
- **CA127 (audit de acesso financeiro)**: Dado a emissão bem-sucedida de um recibo, Quando ocorre, Então é registrado **um** acesso em `paciente_acesso_log` com `{ paciente_id, usuario_id = solicitante, estabelecimento_id, tipo_acesso = Leitura }` (mesmo padrão da aba Financeiro F2); E Dado que a gravação do audit falhe, Quando o recibo é emitido, Então o usuário ainda recebe o `200` (audit best-effort, não bloqueia).
- **CA128 (flag `recibo_emitido_em`)**: Dado um `Pagamento` com `recibo_emitido_em = NULL`, Quando o recibo é emitido pela 1ª vez, Então `recibo_emitido_em` é gravado com o timestamp da emissão; E Quando o recibo é emitido novamente (2ª via), Então o `recibo_emitido_em` original **não** é sobrescrito e a emissão conclui com `200`.
- **CA129 (estados de carregamento)**: Dado o clique em "Emitir recibo" (qualquer porta), Quando o blob está sendo buscado, Então o botão fica desabilitado/com indicador de carregamento e volta ao normal ao concluir (sucesso ou erro), exibindo toast genérico em caso de erro.
- **CA130 (doc viva)**: Dado a entrega da F8, Quando concluída, Então `Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md` (status da F8 + decisão de schema `recibo_emitido_em`) e `Docs/LGPD.md` (emissão de recibo gera acesso `Leitura` em `paciente_acesso_log`; recibo sem PII clínica) foram atualizados conforme §10.

## 8. Riscos e dependências

- **Confundir recibo com nota fiscal** — risco de produto. Mitigação: rótulo explícito "RECIBO — documento sem valor fiscal" no PDF (CA118) e cópia da UI não usar "nota"/"fiscal". NFS-e é F9.
- **Detecção de "estornado"** — a regra depende de saber se o `Pagamento` tem `EstornoPagamento` (F2). Garantir que a query/agregado carrega esse vínculo antes de gerar (CA120). Reusar o mesmo critério que a F2 já usa para riscar a linha do pagamento estornado.
- **Reuso fiel do pipeline de PDF** — não recriar gerador; estender/clonar o molde de receita (QuestPDF + Nunito + cabeçalho). Se o gerador de receita estiver acoplado a `receita`, extrair só o cabeçalho/fundação compartilhável; não duplicar a fonte embarcada nem o layout do header.
- **Dependências**: **F1** (`Pagamento`, `PaymentModal`, `CobrancaController`, permissões) e **F2** (aba Financeiro, `EstornoPagamento`, audit `paciente_acesso_log`) — ambas pré-requisito, já no escopo. Schema → `imedto-database`: **1 `ALTER` em `pagamentos`** (`recibo_emitido_em`). Nenhuma tabela nova. **Não** depende de F4/F5/F9.

## 9. Observações para execução

**Não-negociável**:
- PDF gerado **no backend** reusando o pipeline existente (QuestPDF + Nunito + cabeçalho institucional) — nunca no front (CA118/CA119).
- Pagamento estornado → **422** no backend, regra no Domain (`Pagamento`), espelhada como ação oculta no front (CA120).
- RBAC `financeiro_paciente.ver` no endpoint (CA123); multi-tenant falha-fechada (CA124).
- Sem PII clínica no recibo; nome de arquivo sem PII; sem PII em log (CA125/CA126).
- Audit de acesso reusando `paciente_acesso_log`/`Leitura`, best-effort (CA127).
- Rótulo "RECIBO — documento sem valor fiscal" visível no PDF.

**Liberdade técnica (dev/db decide)**:
- Nome EF do método de domínio (`RegistrarEmissaoRecibo`/equivalente) e do gerador (`ReciboPagamentoPdfService`/`IReciboPdfService`).
- Forma de compartilhar o cabeçalho institucional entre receita e recibo (helper extraído vs. clone mínimo) — desde que não duplique fonte/layout.
- Rota do endpoint (sugestão: `GET /api/cobrancas/pagamentos/{pagamentoId}/recibo` ou similar coerente com `CobrancaController`).
- Ícone/posição exata da ação nas duas portas, desde que coerente com o set e o protótipo.
- Mecânica de download do blob no front — **reusar** o utilitário já usado no PDF de receita.

**Reuso obrigatório (grep antes de criar)**:
- Gerador de PDF server-side de receita (QuestPDF) + `usePdfHeader.ts` (referência de cabeçalho).
- `paciente_acesso_log` / serviço de audit de acesso da F2 (`Leitura`).
- `CobrancaController` / repositório de `Cobranca`/`Pagamento` da F1/F2 (não criar leitura paralela).
- Utilitário de download de blob do front (PDF de receita).
- `cobrancaService.ts`/`cobrancaStore.ts` (estender, não duplicar).

**Aciona `imedto-database`**: **sim** — 1 `ALTER TABLE pagamentos ADD COLUMN recibo_emitido_em timestamptz NULL` (idempotente, sem backfill, sem índice).

## 10. Atualização de documentação

- **`Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md`** — marcar status da **F8** e cravar a decisão de schema deixada em aberto no §2.1: a flag `recibo_emitido_em` **foi adotada** (coluna `timestamptz NULL` em `pagamentos`, gravada na 1ª emissão). Linha de status; mudança cirúrgica.
- **`Docs/LGPD.md`** — na seção de audit/acesso a dado do paciente: registrar que a **emissão de recibo de pagamento** gera acesso `Leitura` em `paciente_acesso_log` (junto com o acesso à aba Financeiro da F2) e que o recibo **não** contém PII clínica (CID/diagnóstico), só dado financeiro/identificação, com nome de arquivo sem PII. Mudança incremental.
- **`Docs/ARQUITETURA.md`** — sem alteração estrutural obrigatória. O padrão de PDF server-side (QuestPDF) e a área de domínio Cobranças já constam (F1/F2). Adicionar **uma linha** apenas se o dev formalizar um cabeçalho/fundação de PDF compartilhada entre receita e recibo (novo serviço reutilizável). Decisão do dev no momento; o QA valida coerência.
- **`Docs/DESIGN.md`** — sem alteração (reusa AppButton + padrão de ação/download já existentes; nenhum componente novo de design system).
- **`Docs/INFRA.md` / `Docs/COMANDOS.md`** — sem alteração (sem recurso novo de infra; a migration segue o fluxo padrão já documentado).

## 11. Critério de pronto (Definition of Done)

- Todos os CA118–CA130 verdes (validados por QA via análise de código + suíte automatizada; validação visual do PDF fica para o usuário em produção, conforme limitação de browser no sandbox).
- Suíte de backend cobre: estornado → 422 (CA120), pagamento inexistente/sem quitação → 404 (CA121), multi-tenant (CA124), audit inserido por emissão (CA127), flag gravada só na 1ª emissão (CA128).
- Front: ação "Emitir recibo" presente nas duas portas (`PaymentModal` pago + linha da aba Financeiro) com estado de loading, oculta em pagamento estornado.
- Migration `ALTER pagamentos ADD recibo_emitido_em` aplicada via fluxo padrão.
- `Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md` e `Docs/LGPD.md` atualizados.
