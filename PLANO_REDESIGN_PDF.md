# Plano — Redesign dos PDFs (Receita, Prontuário, Relatório)

**Etapa 1 do pipeline** — refinamento de requisitos pelo `clinica-qa-specialist`.
**Data:** 2026-05-16.
**Próximo agente:** `fullstack-clinica-senior`.

---

## 0. Escopo

Adaptar 3 geradores de PDF ao design extraído de **/tmp/design-extracted/imedto/project/**:

| Documento | Onde vive hoje | Continua onde |
|---|---|---|
| **Receita** | Backend — `QuestPdfReceitaService.cs` (QuestPDF) | Backend (decisão do usuário) |
| **Prontuário** | Frontend — `useProntuarioPdf.ts` (jsPDF + autotable) | Frontend |
| **Relatório** (Faturamento + Agendamentos) | Frontend — `useRelatorioPdf.ts` (jsPDF + autotable) | Frontend |

**Fora do escopo:** novo modal de pré-visualização React do mock (`PrintPreview.jsx`), editor inline de medicamentos, exportação CSV (já funciona).

---

## 1. Especificação visual consolidada

### 1.1 Folha A4

| Atributo | Valor |
|---|---|
| Tamanho | 210 × 297 mm (A4) |
| Padding (impressão real) | **14 mm topo · 18 mm laterais · 22 mm rodapé** (de `@media print` do mock) |
| Cor primária / "ink" | `#1a2440` (azul-marinho institucional) — usado em texto, cabeçalho, círculos, linha de assinatura |
| Cor primária — alternativa para `h1/h2/h3` | `hsl(218 60% 28%)` ≈ **`#1d3557`** |
| Cor controle especial | **`#dc2626`** (vermelho) |
| Cor secundária texto | `#475569` (slate-600) |
| Cor mute / labels | `#94a3b8` / `#64748b` |
| Fundo bloco paciente | `#f8fafc` borda `#e2e8f0` raio 6 px |
| Fonte | Inter no mock — **decisão: usar Nunito** (ver §2.2) |
| Tamanho base | 11,5 px (≈ 9 pt no print) · linha 1,5 |
| Tamanho cabeçalho clínica (h1) | 22 px · weight 700 · letter-spacing −0,3 px |
| Tamanho título doc (h2) | 15 px · weight 700 · letter-spacing 1 px · UPPERCASE |
| Tamanho módulo (h3) | 12 px · weight 700 · UPPERCASE · letter-spacing 0,4 px |
| Marca d'água | "IMEDTO" 140 px weight 800 letter-spacing 8 px · cor `rgba(26,36,64,0.025)` · `rotate(-25deg)` · `z-index 0` |
| Linha divisória header | **3 px double** `hsl(218 60% 28%)` (vermelho em controle especial) |
| Linha assinatura | 1,5 px solid `#1a2440` · 80 % da largura · centralizada |

### 1.2 Cabeçalho institucional (`sheet-head`)

Grid 2 colunas (1fr 1.2fr), 2 linhas:
```
+------------------+------------------+
| LOGO + nome      | endereço / cnpj  |
| Clínica Imedto   | telefone / email |
+------------------+------------------+
| TÍTULO DOC       | subtítulo (data) |
+------------------+------------------+
```
Bordas: linha fina `#e2e8f0` separa a linha do título-doc; linha dupla 3 px embaixo separa o cabeçalho do corpo.

### 1.3 Bloco do paciente (`sheet-patient`)

Card com fundo `#f8fafc`, borda `#e2e8f0`, raio 6 px, padding 12 × 14 px.
**Linha 1** (4 colunas, ratios 2 : 1 : 1 : 1): Paciente · Idade · Sexo · Tipo sang.
**Linha 2** (4 colunas iguais, separada por borda tracejada): CPF · Nascimento · Convênio · Telefone.
Label em uppercase 9 px cinza; valor 11,5 px texto cheio.

### 1.4 Receita — lista de medicamentos (`rx-print-item`)

Cada item é uma linha com:
- Círculo numerado 30 × 30 px, fundo azul-marinho `#1a2440` (vermelho em controle especial), texto branco bold 13 px.
- Bloco à direita com:
  - Linha 1: **Medicamento** (14 px bold) + chip de dose (bg `#e0e7ff`, texto azul, 12 px) + chip de via (borda 1px `#cbd5e1`, 10 px).
  - Linha 2: ícone ⏰ frequência · ícone 📅 duração (11 px).
  - Linha 3: texto orientação em itálico mute (10 px).
- Borda esquerda 3 px azul (vermelho em controle especial).
- Fundo `#fafbfc`, raio 0 6 6 0.

Variante controle especial adiciona caixa amarela `receita-2via` no final ("1ª via Farmácia · 2ª via Paciente").

### 1.5 Prontuário — módulos (`mp-section`)

- Título h3 uppercase azul-marinho com borda inferior 1 px `#cbd5e1`.
- Corpo: texto justificado, lista, grade SOAP (`dt`/`dd`), grade kv, vitais em 5 colunas (`mp-vitals`).
- `page-break-inside: avoid` por seção.

### 1.6 Rodapé (`sheet-foot`)

Grid 2 colunas (1fr 1fr), alinhado ao fundo da folha (`margin-top:auto`):
- **Esquerda — assinatura**: linha 80%, **nome** 11,5 px bold, **CRM/UF** 10 px slate, **selo verde** (#16a34a) "Assinado digitalmente · ICP-Brasil · SHA-256 ABCD...".
- **Direita — meta**: 3 linhas 9 px cinza claro (`#94a3b8`) com "Emitido em …", "Página N de M", site do estabelecimento.

---

## 2. Decisões tomadas

### 2.1 Relatório — qual layout aplicar (ponto 1 do briefing)

**DECISÃO:** Aplicar o **mesmo cabeçalho institucional + marca d'água + rodapé com paginação** do design, e adaptar o corpo para tabelas/KPIs. O bloco de paciente vira **bloco de "período" e "totais"** (mesma estética: card azul-claro, labels uppercase, valores destacados).

- Estrutura do corpo:
  - **Hero numérico** (`mp-vitals` reaproveitado visualmente): 4-5 cartões com os KPIs principais (Total receita / Total despesa / Saldo / Qtd. agendamentos / % no-show).
  - **Tabela** (autotable) com header em azul-marinho `#1a2440`, sem bordas pesadas.
  - **Empty state** quando dados vazios — caixa pontilhada (estilo `rxp-empty`) com mensagem "Sem dados no período selecionado".
- O rodapé do relatório **não** mostra assinatura — só meta (gerado em, página, site). A coluna da assinatura vira um aviso discreto "Documento de gestão — não vale como comprovante fiscal".

### 2.2 Fonte tipográfica (ponto 2)

**DECISÃO:** Usar **Nunito** em todos os PDFs.

- Razão: já é a fonte do produto (`--font-sans` em `colors_and_type.css`) e Inter é só do CSS de print do mock. Manter consistência produto ↔ documento impresso.
- **Backend (QuestPDF)**: embedar `Nunito-Regular.ttf`, `Nunito-Bold.ttf`, `Nunito-SemiBold.ttf` em `backend/src/Services/Imedto.Backend.API/Resources/Fonts/` (ou Infrastructure) e registrar via `QuestPDF.Fluent.FontManager.RegisterFontType`.
- **Frontend (jsPDF)**: usar `doc.addFont()` carregando a fonte base64. Como custa ~150 KB adicional no bundle, **lazy-load junto com o jsPDF**. Fallback: se font não carregar, cai em helvetica (já é o default jsPDF).

### 2.3 Logo da clínica (ponto 3)

**DECISÃO:** Usar o campo **`Estabelecimento.FotoUrl`** já existente como logo do PDF. Fallback explícito:
- Se `FotoUrl` for null → renderizar um **placeholder textual** com as iniciais do `NomeFantasia` num círculo azul-marinho (cor `#1a2440`, texto branco bold).
- **Sem** o ícone-coração SVG do mock (é genérico e não representa nenhuma clínica real).
- **Frontend**: baixar a imagem via fetch + converter pra dataURL antes de chamar `doc.addImage` (jsPDF não aceita URL remota direto). Cache da imagem por sessão.
- **Backend (QuestPDF)**: baixar bytes via `HttpClient` (timeout 3 s) ou — preferível — passar o **caminho/key do S3** e usar `IFotoStorageService` se já existir um método de leitura; se não, ler bytes diretamente pelo SDK do S3 com cache em memória por estabelecimento (TTL 5 min). Se falhar, renderiza placeholder.

### 2.4 Dados do estabelecimento (ponto 4)

**Gap:** o `Estabelecimento` **não tem** os campos `Email`, `Site`, `Tagline`, `Cidade`/`CEP` separados — só `Endereco` (string única) e `Telefone`.

**DECISÃO:** Renderizar **só o que existe** no domínio atual; **não pedir migration agora**.

| Campo do mock | Como resolver |
|---|---|
| `clinic.name` | `Estabelecimento.NomeFantasia` |
| `clinic.tagline` | **Omitir** (não existe). Fica espaço em branco abaixo do nome — mock fica visualmente OK. |
| `clinic.cnpj` | `Estabelecimento.Cnpj` (formatar `XX.XXX.XXX/XXXX-XX` no render). Se null → omitir linha. |
| `clinic.address` + `clinic.city` | `Estabelecimento.Endereco` (uma única linha; quebra automática se passar do width). |
| `clinic.phone` | `Estabelecimento.Telefone` (formatar `(XX) XXXXX-XXXX`). |
| `clinic.email` / `clinic.site` | **Omitir** (não existe). |

**Backlog (não desta entrega):** migration adicionando `email`, `site`, `tagline`, `cidade`, `cep` em `estabelecimentos`. Quando vier, o template já estará preparado para preencher essas linhas — basta passar o dado novo.

### 2.5 Assinatura digital ICP-Brasil (ponto 5)

**Gap:** o sistema **não tem** ICP-Brasil — `AssinaturaDigitalStatus = NaoAssinada` por default em toda receita emitida.

**DECISÃO:** Mostrar o bloco de assinatura **condicionalmente** baseado em `Receita.AssinaturaDigitalStatus`:

| Status | O que renderiza no rodapé |
|---|---|
| `NaoAssinada` (atual default) | Linha 80% + nome + CRM/CRO + aviso cinza **"Assine manualmente no espaço acima"** (texto pequeno, sem cor verde, sem cadeado). |
| `Assinada` (futura integração) | Linha + nome + CRM + selo verde "Assinado digitalmente · ICP-Brasil · SHA-256 …" (igual ao mock). |
| `Cancelada` | Selo vermelho "Assinatura cancelada". |

- Prontuário: sempre o mesmo aviso "Assine manualmente" (não há integração).
- Relatório: **sem** bloco de assinatura — substituído por aviso "Documento de gestão".

Isso evita exibir uma falsa afirmação de ICP-Brasil quando não há assinatura — que seria infração regulatória e LGPD/CFM.

### 2.6 Variantes de Receita (ponto 6)

**Gap:** o mock distingue **3 variantes** (Comum / Uso Contínuo / Controle Especial). O backend distingue **4 tipos regulatórios** (Comum / Controlada / Antibiotico / Especial). Não existe flag `Continua` no aggregate.

**DECISÃO:** Mapear assim no PDF (consistente com a regulação):

| `Receita.Tipo` | Layout visual do PDF | Título |
|---|---|---|
| `Comum` | Padrão azul-marinho | "RECEITA MÉDICA" |
| `Antibiotico` | Padrão azul-marinho (mesmo do mock comum) — texto rodapé adiciona "Reter na farmácia (RDC 471/2021)" | "RECEITUÁRIO DE ANTIBIÓTICO" |
| `Controlada` | **Variante vermelha** (`sheet-receita-controlled`) — todas as cores trocam para `#dc2626` + caixa amarela "1ª via Farmácia · 2ª via Paciente" | "RECEITUÁRIO DE CONTROLE ESPECIAL — NOTIFICAÇÃO {A/B/C/Especial}" |
| `Especial` | Padrão azul-marinho | "RECEITUÁRIO ESPECIAL" |

**"Uso Contínuo" do mock fica como gap de produto** — não tem entidade que represente isso hoje. Backlog: flag `boolean UsoContinuo` em `Receita` (migration + comando + checkbox no front). Não bloqueia esta entrega.

### 2.7 Marca d'água "IMEDTO" diagonal (ponto 7)

**DECISÃO:** Em **todos** os 3 documentos (Receita, Prontuário, Relatório), **todas** as variantes, sempre presente, opacidade 0,025, rotação −25°.

- Backend QuestPDF: `page.Foreground()` com texto rotacionado.
- Frontend jsPDF: `doc.saveGraphicsState()` + `doc.setGState(GState({opacity:0.025}))` + `doc.text("IMEDTO", x, y, {angle: -25})`. Aplicar em **toda página** (loop após `doc.addPage()`).

Substituir o atual texto "RASCUNHO"/"CANCELADA" do `QuestPdfReceitaService` por:
- **Rascunho** → marca d'água "RASCUNHO" 96 px cinza `#cbd5e1` (mantém aviso visual sem usar a marca IMEDTO sutil).
- **Cancelada** → marca d'água "CANCELADA" 96 px vermelho transparente.
- **Substituida** → marca d'água "SUBSTITUÍDA".
- **Emitida** → marca d'água sutil "IMEDTO".

### 2.8 LGPD (ponto 8)

**DECISÃO:** Manter a exposição atual de PII do paciente nas receitas e prontuários — **paciente é o destinatário do documento**, então CPF/Nascimento/Telefone/Convênio são esperados (e exigidos por ANVISA em receita controlada).

Reforços:
- **Relatório** **não pode** trazer PII de paciente individual no template padrão. Conferir se `FaturamentoCategoria` e `RelatorioAgendamentos` contêm nomes/CPFs — se sim, manter agregação por categoria/status (já é o caso) e nunca por paciente.
- **Audit trail**: a geração de PDF de receita já é auditável via emissão; reforçar **log de download de prontuário** (quem baixou, quando, qual paciente) — se não existir, criar evento `ProntuarioPdfBaixadoEvent` (item de backlog se já existir similar).
- **Não logar** payload completo do PDF em log do backend — apenas `receitaId` + `estabelecimentoId`.
- **CPF formatado com máscara** (`XXX.XXX.XXX-XX`) já formatada — não mostrar dígitos crus.

---

## 3. Cenários funcionais a cobrir

### 3.1 Receita

| # | Cenário | Esperado |
|---|---|---|
| R1 | Receita Comum, 1 medicamento, estabelecimento completo (logo+CNPJ+endereço+telefone) | Folha azul-marinho, header completo, 1 item numerado, rodapé com aviso "Assine manualmente" |
| R2 | Receita Comum, 5 medicamentos (texto longo que ultrapassa página) | Quebra de página correta (item não corta no meio — `page-break-inside: avoid`), rodapé aparece em todas as páginas, marca d'água em todas |
| R3 | Receita Controlada (Notificação B), 2 itens | **Layout vermelho** (header borda vermelha, círculos vermelhos, chips vermelhos), caixa amarela "1ª via/2ª via", subtítulo com validade de 30 dias |
| R4 | Receita Antibiótico | Layout azul, título "RECEITUÁRIO DE ANTIBIÓTICO", rodapé com aviso "Reter na farmácia" |
| R5 | Receita em **Rascunho** | Marca d'água "RASCUNHO" sobreposta, demais elementos normais, rodapé sem nome/CRM (não emitida) |
| R6 | Receita **Cancelada** | Marca d'água "CANCELADA" vermelha + razão do cancelamento no rodapé |
| R7 | Estabelecimento **sem `FotoUrl`** | Placeholder com iniciais do `NomeFantasia` em círculo azul |
| R8 | Estabelecimento **sem CNPJ** / sem endereço | Linha omitida (sem mostrar "null" ou "—") |
| R9 | Observação longa (2000 caracteres) | Não quebra layout, quebra para nova página se necessário |
| R10 | Configuração tem `CabecalhoHtml` customizado | Texto extraído (stripped) aparece **acima** do bloco institucional como nota livre — não substitui o cabeçalho do design |

### 3.2 Prontuário

| # | Cenário | Esperado |
|---|---|---|
| P1 | Prontuário com 3 evoluções, todas com módulos preenchidos | 1 página por evolução, módulos com h3 uppercase, separação por linha tracejada |
| P2 | Prontuário **sem evoluções** | Empty state centralizado: "Nenhuma evolução registrada para este paciente." |
| P3 | Evolução com módulo SOAP completo | Grade `dt`/`dd` com labels "S — Subjetivo", etc. |
| P4 | Evolução com módulo de Vitais (PA, FC, FR, Temp, SatO₂, Peso, Altura) | Grade 5 colunas com label uppercase + valor bold + unidade pequena. IMC calculado automaticamente quando peso+altura preenchidos |
| P5 | Evolução com CID | Lista com `code` em mono-font destacado fundo azul-claro |
| P6 | Texto livre longo (5000 chars) que ultrapassa página | Quebra correta, sem cortar palavra; nova página com mesmo cabeçalho/rodapé |
| P7 | Estabelecimento sem logo/CNPJ | Fallbacks iguais §R7-R8 |

### 3.3 Relatório

| # | Cenário | Esperado |
|---|---|---|
| F1 | Faturamento com receitas+despesas no período | Hero com 4 KPIs (Receita / Despesa / Saldo / Qtd categorias). Tabela com header azul-marinho. Footer com totais bold |
| F2 | Faturamento **vazio** no período | Empty state pontilhado: "Sem dados no período selecionado" |
| F3 | Faturamento com >50 categorias | Múltiplas páginas, cabeçalho repete, marca d'água em todas |
| A1 | Agendamentos com status + por-dia | Hero com total, tabela por status + tabela por dia |
| A2 | Agendamentos **vazio** | Empty state |
| RX | Relatório **não tem** assinatura | Substituído por "Documento de gestão · não vale como comprovante fiscal" |

### 3.4 Fallbacks comuns

- **Sem `FotoUrl`** → placeholder com iniciais.
- **Sem CNPJ** → omitir linha.
- **Sem `Endereco`** → omitir linha.
- **Sem `Telefone`** → omitir linha.
- **Sem nada (estabelecimento recém-criado)** → mostrar só `NomeFantasia` + placeholder, sem quebrar layout.
- **Carga de fonte Nunito falha** → cai pra helvetica/Arial silenciosamente.

---

## 4. Critérios de aceite (gate para etapa 3 — QA)

Para cada um dos 3 PDFs:

1. **Visual** — comparado lado-a-lado com o mock (`/tmp/design-extracted/imedto/project/components/PrintPreview.jsx` renderizado), o PDF gerado deve ter:
   - Mesmo cabeçalho (logo + nome + tagline*omit* / endereço / CNPJ / telefone / título doc / subtítulo).
   - Mesmo bloco de paciente (receita+prontuário) ou bloco de período (relatório).
   - Mesma marca d'água diagonal IMEDTO sutil.
   - Mesmo rodapé (assinatura condicional, paginação, site*omit*).
2. **Variantes funcionam**:
   - Receita Comum / Antibiótico → azul-marinho.
   - Receita Controlada → vermelho com caixa amarela 1ª/2ª via.
   - Status Rascunho / Cancelada → marca d'água adequada.
3. **Build/CI passa**:
   - `dotnet build Imedto.Backend.sln` sem warnings novos.
   - `cd frontend && npm run build` (vue-tsc + vite) sem erros.
4. **Testes unitários novos**:
   - **Backend**: `QuestPdfReceitaServiceTests` cobrindo (a) Comum 1 item, (b) Controlada com notificação, (c) Cancelada, (d) sem FotoUrl, (e) bytes > 0 e mime válido. Não validar pixel-a-pixel — validar contagem de páginas, presença de strings no PDF (extrair texto com `PdfPig` ou similar).
   - **Frontend**: `useProntuarioPdf.spec.ts` e `useRelatorioPdf.spec.ts` cobrindo geração sem crash com (a) dados vazios, (b) dados longos, (c) estabelecimento mock sem logo. Validar via `doc.output('arraybuffer').byteLength > 0`.
5. **Performance**:
   - Receita 1 página: < 500 ms no backend (medir em teste).
   - Prontuário 5 evoluções: < 1,5 s no frontend.
   - Bundle frontend: lazy-load mantido. Nunito embarcada adiciona ≤ 200 KB ao chunk lazy (não ao bundle inicial).
6. **LGPD**:
   - Nenhum log do backend contém nome de paciente, CPF, ou nome de medicamento.
   - Relatório não exibe paciente individualizado.
7. **Multi-tenant**:
   - PDF de receita só carrega se `r.estabelecimento_id = @EstabelecimentoId` (já é assim — não regredir).
   - Logo carregado é só do estabelecimento da sessão atual.

---

## 5. Lista de mudanças por arquivo (orientação para o fullstack)

**Não é prescritivo — o fullstack pode reorganizar; é o mapa de impacto.**

### Backend

- `backend/src/Services/Imedto.Backend.Infrastructure/Receitas/QuestPdfReceitaService.cs` — reescrever `GerarPdf` aplicando o novo layout. Manter assinatura pública do `IReceitaPdfService.GerarAsync`. Acrescentar carregamento de dados de `Estabelecimento` ao SQL de leitura (NomeFantasia, Cnpj, Telefone, Endereco, FotoUrl).
- `backend/src/Services/Imedto.Backend.API/Resources/Fonts/Nunito-*.ttf` — adicionar fontes (3 arquivos: Regular, SemiBold, Bold).
- Registrar fontes em `QuestPdfReceitaService` (static init) via `FontManager.RegisterFontType`.
- `backend/src/Tests/Imedto.Backend.Test/Receitas/QuestPdfReceitaServiceTests.cs` — novos testes.

### Frontend

- `frontend/src/composables/useProntuarioPdf.ts` — reescrever a função `gerarPdf` aplicando o novo layout (header institucional, bloco paciente, módulos com h3 uppercase, rodapé).
- `frontend/src/composables/useRelatorioPdf.ts` — reescrever `gerarFaturamentoPdf` e `gerarAgendamentosPdf` com header institucional, hero de KPIs, tabela com novo estilo, rodapé sem assinatura.
- `frontend/src/composables/usePdfHeader.ts` (**novo**) — helper compartilhado que desenha header institucional + marca d'água + rodapé. Reusado pelos 2 composables.
- `frontend/src/assets/fonts/nunito-base64.ts` (**novo**) — fontes Nunito em base64, importadas dinamicamente.
- Stores/serviços: adicionar método em `estabelecimentoService` (ou reusar o existente) para buscar dados do estabelecimento da sessão se ainda não estiverem disponíveis em memória.
- Testes: `useProntuarioPdf.spec.ts`, `useRelatorioPdf.spec.ts`.

### Banco / SSM / DNS

- **Nenhuma mudança** (sem migration, sem novo secret, sem nova rota).

---

## 6. Perguntas para o usuário (precisam de resposta antes da etapa 2)

> Nenhuma é bloqueante — o fullstack pode começar com as suposições abaixo. Mas é melhor confirmar para evitar retrabalho:

1. **Tagline da clínica** — o design mostra "Cardiologia · Medicina Diagnóstica" abaixo do nome. Hoje não existe esse campo. **Confirmar omitir** (suposição padrão) ou pedir migration agora?
2. **Nunito vs Inter** — confirmar Nunito (consistência com o produto) — minha recomendação. Se o usuário tem preferência forte por Inter (fidelidade ao mock), trocar.
3. **Texto da orientação por item da receita** (linha 3 do `rx-print-item`) — no mock vem fixo *"Tomar conforme orientação médica. Não interromper o tratamento sem consultar o médico."*. Confirmar se é **sempre esse texto fixo** ou se deve usar `ItemReceita.Observacao` quando preenchida.
4. **Texto sob a assinatura quando `NaoAssinada`** — sugiro **"Assine manualmente no espaço acima"** em cinza pequeno. Confirmar a frase exata.
5. **Validade no subtítulo (receita controlada)** — mock diz "válida por 30 dias". Backend calcula `ValidadeAte`. Mostrar **a data efetiva** ("válida até DD/MM/AAAA") em vez de "30 dias"? **Recomendo data efetiva** (mais informativa).
6. **"Uso contínuo"** — confirmar que fica como **backlog futuro** (precisa de migration + UI). Hoje o front não tem campo para marcar.

---

## 7. Itens explicitamente fora do escopo

- Migration para `email`, `site`, `tagline`, `cidade`, `cep` em `estabelecimentos`.
- Flag `UsoContinuo` em `Receita` (e UI correspondente).
- Integração ICP-Brasil real (selo verde só aparece quando `AssinaturaDigitalStatus == Assinada`, que nunca acontece no fluxo atual).
- Novo modal de pré-visualização React (mock `PrintPreview.jsx`) — o produto continua oferecendo "Baixar PDF" direto.
- Editor inline de medicamentos do mock (`receita-editor`).
- Reescrita do `useProntuarioPdf.capturarImagemPdf` (html2canvas) — usado em outra rota e fora do escopo.

---

## 8. Sinal para o `fullstack-clinica-senior`

**LUZ VERDE para iniciar a etapa 2** com as decisões deste plano. Sugestão de ordem de implementação:

1. Helper `usePdfHeader` no frontend (compartilhado).
2. `useProntuarioPdf` reescrita + testes.
3. `useRelatorioPdf` reescrita + testes.
4. `QuestPdfReceitaService` reescrita + testes.
5. Smoke test manual gerando 1 PDF de cada variante.
6. Build CI + commit único.

Se alguma das perguntas §6 surgir como bloqueio, parar e pedir resposta — não inventar dado de domínio.
