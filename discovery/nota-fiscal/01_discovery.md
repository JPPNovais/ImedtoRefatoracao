# Discovery — Nota Fiscal integrada com APIs do governo

> Status: **draft / discovery** — não é plano de execução. Objetivo é mapear o problema, opções, riscos e perguntas em aberto antes de cravar arquitetura.
> Autor: Claude (Opus 4.7) · Data: 2026-05-03

---

## 1. Contexto

O Imedto é uma plataforma SaaS multi-tenant para clínicas e profissionais de saúde. As clínicas faturam:

- **Serviços de saúde** (consultas, procedimentos, exames) — emissão de **NFS-e** (Nota Fiscal de Serviço Eletrônica), competência **municipal**.
- **Eventualmente produtos** (venda de cosméticos em clínicas estéticas, OPME em clínicas cirúrgicas) — emissão de **NF-e** (modelo 55), competência **estadual** via SEFAZ.
- **Cupom fiscal de varejo** (NFC-e modelo 65) — pouco provável no escopo, mas possível em clínicas estéticas com retail.

Hoje o legado **não emite nota fiscal**. A demanda é: a partir do agendamento finalizado / pagamento confirmado, gerar a NF correspondente, transmitir ao governo, devolver chave + PDF + XML, permitir cancelamento e consulta.

### Por que isso é mais complexo do que parece

1. **Não existe API única no governo.** NFS-e é municipal — cada um dos ~5.570 municípios escolhe seu próprio padrão (ABRASF v1, v2, GINFES, BETHA, ISSNet, etc.). Recentemente o Sistema Nacional NFS-e (padrão DPS/ADN) começou a unificar, mas a adoção é gradual e nem todos os municípios aderiram.
2. **NF-e (modelo 55)** tem padrão nacional via SEFAZ, mas exige **certificado digital A1/A3 (e-CNPJ)** por emissor, controle de **inutilização de numeração**, contingência (SVC/EPEC), eventos (CCe, cancelamento, manifestação), validação de schema XSD, assinatura XML.
3. **Multi-tenant** = cada clínica tem seu próprio CNPJ, seu certificado, seu município, seu regime tributário (Simples / Lucro Presumido / Lucro Real / MEI), suas alíquotas de ISS, sua série/RPS. Nada é "do Imedto" — é **do estabelecimento**.
4. **Conformidade fiscal não admite eventual.** Nota emitida com erro vira passivo (multa, glosa, rejeição na contabilidade do cliente). Reprocessar NFS-e com numeração já consumida é caro.

---

## 2. Escopo proposto (recorte mínimo viável)

Recomendação: **começar apenas com NFS-e**, pois é o que cobre 95% do faturamento de uma clínica de saúde.

### Em escopo (MVP)
- Emissão de NFS-e a partir de um **lançamento financeiro** (consulta paga, procedimento, etc.).
- Cancelamento de NFS-e dentro do prazo legal (varia por município, normalmente até 24h sem ônus).
- Consulta de status (autorizada, rejeitada, cancelada, em processamento).
- Download de PDF (DANFSE) e XML pelo usuário.
- Configuração de emissor por estabelecimento: CNPJ, regime, certificado, série, alíquota ISS, código de serviço (lista do município ou LC 116/2003).

### Fora de escopo (fase posterior, se houver demanda)
- NF-e modelo 55 (produtos).
- NFC-e modelo 65.
- Carta de correção, substituição de NF.
- Apuração tributária / livro fiscal / SPED.
- Integração com contador (export de XMLs em lote já cobre boa parte).

### Decisão a tomar com o produto
- **Toda clínica precisa emitir?** Algumas são MEI e usam recibo. Algumas usam contador externo. **Nem todo agendamento pago → NF**. Precisa ser **opt-in** por estabelecimento e por tipo de serviço.
- **Quem dispara a emissão?** Automática no pagamento confirmado? Manual via tela "lançamentos a faturar"? Lote no fim do dia? Recomendação inicial: **manual + opção de automatizar por configuração**, para não emitir nota por engano em fluxos que ainda estão sendo validados.

---

## 3. Caminhos de integração

### Opção A — Integração direta com APIs municipais

**Como funciona:** o Imedto fala SOAP/REST direto com o webservice do município, monta o XML, assina com o certificado da clínica, envia, processa retorno.

**Prós**
- Sem custo recorrente por NF.
- Controle total do fluxo, sem dependência de terceiro.
- Margem do produto não sofre.

**Contras**
- ~5.570 municípios, cada um com seu padrão. Mesmo dentro do "ABRASF v2", há variações de namespace, de campo opcional/obrigatório, de regra de cálculo de ISS retido.
- Manutenção contínua: cada município muda layout sem aviso. É um time inteiro só para isso em empresas que escolheram esse caminho.
- Gestão de certificado digital A1/A3 do cliente (custódia, expiração, troca, revogação).
- Contingência, retry, idempotência, fila — tudo do nosso lado.
- Sandbox/homologação varia por município (alguns nem têm).

**Veredito:** inviável como ponto de partida. Pode ser caminho de longo prazo para os top-20 municípios mais frequentes (concentração 80/20), nunca para a cauda.

### Opção B — Provedor/gateway de NFS-e (BPaaS fiscal)

**Como funciona:** o Imedto fala uma API REST única e o provedor traduz para o município de destino, gerencia certificado, faz retry, devolve XML/PDF normalizados.

**Players relevantes do mercado brasileiro:**
- **NFE.io** — tem API moderna, bem documentada, foco em devs. Cobre boa parte dos municípios via ABRASF + integrações específicas. Pricing por NF emitida.
- **eNotas / Gyra+** — bastante usado em SaaS. Modelo similar.
- **Focus NFe** — muito popular, API REST limpa, pricing competitivo. Cobre NFS-e, NF-e, NFC-e, MDF-e.
- **PlugNotas** — concorrente direto da Focus, modelo similar.
- **Migrate (Tecnospeed)** — provedor de white-label para SaaS. Mais enterprise, contrato mensal.
- **Sistema Nacional NFS-e (Receita Federal)** — gratuito, mas só atende municípios que aderiram ao padrão DPS/ADN. Cobertura ainda parcial em 2026.

**Prós**
- Time-to-market rápido — semanas vs. meses.
- Cobertura municipal terceirizada.
- Custódia de certificado digital frequentemente é deles (vantagem operacional + responsabilidade).
- SLA, suporte, sandbox.

**Contras**
- Custo por NF (R$ 0,10 a R$ 0,80 dependendo do volume — precisa entrar no cálculo de unit economics).
- Vendor lock-in. Se o provedor cair, ninguém emite nota.
- Latência adicional (≈300-1500ms a mais que direto).
- Precisamos de uma camada de abstração para trocar de provedor sem reescrever tudo.

**Veredito:** caminho recomendado para o MVP. Decisão tática: começar com **um único provedor** mas isolar atrás de uma interface (`INfsEmissao`) para que trocar seja questão de outra implementação, não de reescrita.

### Opção C — Sistema Nacional NFS-e (Receita Federal) direto

**Como funciona:** padrão único nacional (DPS — Declaração de Prestação de Serviços + ADN — Ambiente de Dados Nacional). Gerido pela RFB. API REST moderna, certificado e-CNPJ.

**Prós**
- Gratuito.
- Padrão único (em tese).
- Modernização do ecossistema fiscal.

**Contras**
- Adoção parcial — em 2026, ainda há muitos municípios em transição ou fora.
- Para municípios não aderentes, precisa do caminho A ou B mesmo.

**Veredito:** **não substitui** B no curto prazo. Pode complementar — fluxo "tenta nacional → fallback provedor" — mas adiciona complexidade que o MVP não precisa.

---

## 4. Recomendação de arquitetura (para discussão)

### 4.1 Bounded context novo: `Faturamento`

Faturamento é um domínio próprio. **Não** misturar com Agendamento ou Financeiro.

- **Agregados:**
  - `LancamentoFaturavel` — representa um serviço prestado e pago, candidato a virar NF. Origem: agendamento concluído, procedimento avulso, pacote.
  - `NotaFiscal` — agregado raiz. Estado-máquina explícita: `Rascunho → EmTransmissao → Autorizada → Cancelada / Rejeitada`.
  - `EmissorFiscal` — configuração por estabelecimento (CNPJ, regime, certificado, série, código de serviço padrão, alíquotas).
- **Domain events:**
  - `NotaFiscalSolicitada` (handler chama o gateway).
  - `NotaFiscalAutorizada` (handler atualiza lançamento, dispara notificação ao paciente).
  - `NotaFiscalRejeitada` (handler atualiza lançamento, notifica clínica).
  - `NotaFiscalCancelada`.
- **Idempotência:** `LancamentoFaturavel` só pode ter **uma** NF ativa. Reemissão exige cancelar a anterior.

### 4.2 Camada de gateway

```
INfsEmissaoGateway
  Task<EmissaoResult> EmitirAsync(EmissaoRequest req, CancellationToken ct);
  Task<CancelamentoResult> CancelarAsync(string chaveOuId, MotivoCancelamento motivo, CancellationToken ct);
  Task<ConsultaResult> ConsultarAsync(string chaveOuId, CancellationToken ct);
```

Implementações: `FocusNfeGateway`, `NfeIoGateway`, `PlugNotasGateway`. Selecionada por configuração do estabelecimento (alguns podem usar provedor próprio do contador) ou config global do Imedto.

### 4.3 Resiliência

- **Outbox pattern** para a chamada externa. Salvar a `NotaFiscal` em estado `Rascunho` na mesma transação do `LancamentoFaturavel`, e enfileirar o evento `NotaFiscalSolicitada`. Worker assíncrono lê e dispara o gateway. Garante: se o gateway estiver fora, a UI já confirmou para o usuário e o sistema retentará.
- **Polling de status** para NFs que ficam em "EmProcessamento" por mais de N segundos (alguns municípios são lentos).
- **Webhook do provedor** (Focus, NFE.io, PlugNotas suportam) → endpoint público autenticado por HMAC para receber atualização sem polling.
- **Circuit breaker + retry exponencial** nas chamadas ao gateway.
- **Fila de DLQ** para falhas que precisam de intervenção humana (ex: regime fiscal mudou, certificado expirou).

### 4.4 Custódia de certificado digital

- Certificado **A1** (arquivo `.pfx`) — permite custódia em servidor.
- Certificado **A3** (token físico) — não permite custódia remota; descartar do escopo. Clínica que só tem A3 precisa migrar para A1 (custo ~R$ 200/ano).
- **Quem guarda?**
  - Se o provedor (Focus, PlugNotas) custodiar — melhor para nós (responsabilidade legal, segurança operacional). Subimos o `.pfx` para a API deles uma vez, eles assinam.
  - Se o Imedto custodiar — precisa de KMS (AWS KMS, Vault) com criptografia em repouso, audit trail de uso, alerta de expiração (certificados duram 12 meses), rotação. **Não recomendado para o MVP.**
- Decisão: **delegar custódia ao provedor**. Reduz superfície de ataque e responsabilidade LGPD/fiscal nossa.

### 4.5 LGPD

- NF carrega CPF/CNPJ do tomador, valor, descrição do serviço. Dado pessoal sensível **não está** na NFS-e por padrão (não descrevemos a doença/procedimento na descrição — usar texto genérico tipo "Consulta médica" ou código TUSS quando aplicável, **nunca CID/diagnóstico**).
- **Política do produto a definir:** o que vai no campo "Discriminação dos Serviços"? Recomendação fortíssima: nunca expor diagnóstico/CID. Usar `Consulta em <especialidade>` ou descrição cadastrada no procedimento.
- XMLs/PDFs ficam em Storage com RLS por estabelecimento. Audit trail de download obrigatório.
- Endpoint de exportação LGPD do paciente já existe — incluir as NFs onde ele é tomador.

---

## 5. Modelo de dados (esboço)

```
emissor_fiscal
  id, estabelecimento_id, cnpj, razao_social, regime_tributario,
  inscricao_municipal, codigo_servico_padrao, aliquota_iss,
  certificado_id (referencia para gateway), serie_rps, proximo_rps,
  ambiente (homologacao | producao), gateway (focus | nfe_io | ...)

lancamento_faturavel
  id, estabelecimento_id, agendamento_id?, paciente_id, profissional_id,
  valor_bruto, valor_liquido, descricao, data_competencia,
  status (pendente | faturado | dispensado), nota_fiscal_id?

nota_fiscal
  id, estabelecimento_id, lancamento_id, emissor_id,
  numero, serie, chave_acesso, codigo_verificacao,
  tomador_cpf_cnpj, tomador_nome, tomador_email,
  valor_servicos, valor_iss, aliquota,
  status (rascunho | em_transmissao | autorizada | rejeitada | cancelada),
  motivo_rejeicao, data_emissao, data_autorizacao, data_cancelamento,
  xml_storage_key, pdf_storage_key,
  gateway_id_externo, created_at, updated_at

nota_fiscal_evento
  id, nota_fiscal_id, tipo (solicitacao | autorizacao | rejeicao | cancelamento | consulta),
  payload_json, created_at, usuario_id?
```

Tudo com filtro `estabelecimento_id` (multi-tenant, RLS espelhada).

---

## 6. Fluxo do MVP

1. Usuário marca consulta como concluída + paga → cria `LancamentoFaturavel` (status `pendente`).
2. Tela "Lançamentos a faturar" lista pendentes do estabelecimento. Usuário seleciona N → "Emitir NF".
3. Backend: para cada lançamento, cria `NotaFiscal` rascunho, enfileira evento `NotaFiscalSolicitada`. Retorna 202 ao usuário ("emissão em andamento").
4. Worker consome evento → chama `INfsEmissaoGateway.EmitirAsync` → atualiza status para `EmTransmissao` → recebe resposta síncrona OU webhook → atualiza para `Autorizada` ou `Rejeitada`.
5. Frontend faz polling em `GET /api/notas-fiscais?status=em_transmissao` ou recebe via Realtime do Supabase (dado público para o tenant).
6. Autorizada → e-mail ao tomador com PDF anexado (configurável). Botão "Baixar XML/PDF" disponível na lista.
7. Rejeitada → mostra motivo + botão "Corrigir e reemitir".
8. Cancelamento → ação "Cancelar NF" pede motivo, dispara fluxo de cancelamento no gateway, atualiza status.

---

## 7. Riscos e mitigações

| Risco | Impacto | Mitigação |
|---|---|---|
| Gateway externo fica fora do ar | Clínica não emite nota | Outbox + retry + alerta operacional. Documentar SLA do provedor. |
| Município muda layout sem aviso | NF rejeitada em massa | Provedor absorve isso. Monitor de taxa de rejeição por município. |
| Certificado da clínica expira | Toda emissão falha | Alerta D-30/D-15/D-7 antes da expiração. Gateway costuma ter isso pronto. |
| Numeração de RPS gap/duplicação | Multa fiscal | Numeração centralizada no `EmissorFiscal` com lock pessimista. **Nunca** gerar RPS no front. |
| Dado de paciente vaza na descrição da NF | Incidente LGPD | Texto da descrição vem de campo controlado, validado no backend; nunca diagnóstico. |
| Reemissão duplicada (usuário clica 2x) | NF dupla = passivo fiscal | Idempotência via `lancamento_id` + status check antes de chamar gateway. |
| Cliente cancela NF fora do prazo legal | Cancelamento rejeitado | Backend valida prazo antes de chamar gateway; UI mostra "fora do prazo" como erro de negócio (422). |
| Custo do gateway corrói margem | Unit economics ruim | Negociar volume após validar adoção. Modelar custo no business case. |

---

## 8. Perguntas em aberto (precisam de produto/cliente)

1. **Quais municípios são prioritários?** Os top 5-10 onde a base de clientes está concentrada definem o gateway.
2. **MEI / Simples / Lucro Presumido** — qual a distribuição da base? Afeta complexidade tributária.
3. **Cobramos extra?** NF é feature paga? Inclusa no plano? Repassamos custo do gateway? Define se vamos atrás do menor preço por NF ou do melhor SLA.
4. **Há clínicas com contador que já emite por fora?** Provavelmente sim. Precisamos de fluxo "marcado como faturado externamente" para não obrigar ninguém.
5. **NF-e (produto) entra quando?** Há clínicas estéticas vendendo cosméticos hoje. Se sim, multiplicar o esforço por ~2x.
6. **Conta digital própria vs. PJ do cliente** — quem é o emissor? Sempre o estabelecimento. Mas se tiver split de pagamento (Imedto Pay futuro?), há cenário de **emissão pelo Imedto** com nota de comissão. Não escopar agora.
7. **Compliance do contador** — precisamos exportar XMLs em lote (ZIP por mês)? É padrão de mercado, mas confirmar.

---

## 9. Próximos passos sugeridos

1. **Validação com 3-5 clientes-âncora**: hoje como vocês emitem? Provedor? Manual? Contador? Quanto pagam? — antes de qualquer linha de código.
2. **Levantamento dos municípios da base** (consulta no banco atual: distinct cidade dos estabelecimentos ativos, ordenado por volume de agendamentos).
3. **POC com 1 provedor** (sugestão: Focus NFe pelo custo + documentação) emitindo NFS-e em homologação para o município com maior volume.
4. **Definição de produto:** opt-in, configuração mínima viável, UX da tela de lançamentos a faturar, política de descrição de serviço (LGPD).
5. **ADR formal** após POC definindo gateway escolhido, modelo de custódia, abstração `INfsEmissaoGateway`.
6. Entrar como **Fase nova** no `Docs/00_PLANO_MIGRACAO.md` (provavelmente Fase 6 ou superior — não bloqueia hardening atual).

---

## 10. O que **não** vamos fazer agora

- Implementar.
- Escolher provedor sem POC.
- Tratar NF-e e NFC-e.
- Construir UI ou DTOs.
- Mexer no domínio existente para "preparar terreno" (premissa CLAUDE.md §3 — sem refactor especulativo).

Este documento é insumo de decisão. Próxima ação esperada: revisão do produto e dos clientes-âncora, depois ADR.
