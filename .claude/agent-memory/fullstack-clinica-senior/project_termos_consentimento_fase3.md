---
name: project-termos-consentimento-fase3
description: Fase 3 dos Termos de Consentimento (2026-05-19) — aba do paciente + emissão + PDF gerado no front + upload manual.
metadata:
  type: project
---

Fase 3 dos Termos de Consentimento entregue em 2026-05-19 (não commitado, aguardando QA). Substituiu o empty state "em breve" da aba `termos` em `PacienteDetalheView.vue` pela implementação completa.

**Decisões arquiteturais (fora ou em divergência leve com o briefing):**

1. **Geração de PDF roda 100% no front** via novo composable `useTermoPdf` (jsPDF + `usePdfHeader`), espelhando o padrão de `useAtestadoPdf`/`useProntuarioPdf`. O endpoint backend `GET /api/termos/{id}/pdf-gerado` continua como stub 501 — não foi removido nem implementado.
   - **Why:** consistência com os outros PDFs (Receita/Atestado/Prontuário) e zero custo de servidor pra gerar.
   - **How to apply:** se a Fase 4 precisar de PDF "com selo de assinatura digital", aí sim vale implementar no back.

2. **Sem upload automático após emitir** (briefing seção 3 sugeriu; descartado). Front baixa PDF "AGUARDANDO ASSINATURA" e o termo fica em `Status = Pendente`. Recepcionista anexa o PDF assinado via botão na lista, e só aí vira `Assinado`.
   - **Why:** o aggregate `TermoEmitido.AnexarPdf` exige `Status == Pendente` e muda pra `Assinado`. Upload automático faria todo termo virar "Assinado" sem o paciente ter assinado de fato — quebra integridade e auditoria LGPD.
   - **How to apply:** **NUNCA** fazer upload automático nesse fluxo. Se quiser PDF pré-emissão pra impressão, gerar no front + baixar — sem chamar `POST /api/termos/{id}/pdf`.

3. **Sem paginação na lista da aba** (briefing pediu `AppPagination`, descartado). O endpoint `GET /api/pacientes/{id}/termos` retorna `IReadOnlyList` sem `pagina/tamanho`. Volume esperado por paciente é ≤ dezenas.
   - **How to apply:** se um paciente passar de 100 termos (improvável), expor `pagina/tamanho` no controller + repo Dapper e adicionar `AppPagination` no `PacienteTermosTab`. É extensão simples.

4. **Status no DB é PascalCase** (`"Pendente"`, `"Assinado"`, ...) — EF salva via `HasConversion<string>()` em `Enum.ToString()`. AssinaturaTipo também é PascalCase (`"PdfAnexado"`, `"AceiteLink"`). O service do front (`pacienteTermoService.ts`) tipa esses valores como union exato.
   - **Why:** SQL do query repository é case-sensitive (`t.status = @Status`). Inconsistente com o que o endpoint público de emissão aceita (`"pdf_anexado"`/`"aceite_link"` snake_case), mas é o que está hoje.
   - **How to apply:** ao filtrar status no front, sempre passar PascalCase. Ao emitir, sempre snake_case. O `TermoParsers` (back) faz a tradução.

5. **`resolverVariaveis` no front é só pra preview** (`utils/termoResolverVariaveis.ts`). O backend tem o canonico em `TermoResolverDeVariaveis.cs` e é quem resolve no momento da emissão (snapshot HTML imutável). Os formatadores foram alinhados 1:1 com o C#.
   - **How to apply:** se mudar regra (ex: novo placeholder, formato diferente de telefone), mudar **nos dois lugares**. Backend é a fonte da verdade — o preview pode divergir temporariamente sem quebrar emissão.

**Arquivos novos:**
- `frontend/src/services/pacienteTermoService.ts`
- `frontend/src/utils/termoResolverVariaveis.ts` + `.test.ts` (15 testes)
- `frontend/src/composables/useTermoPdf.ts`
- `frontend/src/components/termos/PacienteTermosTab.vue`
- `frontend/src/components/termos/EmitirTermoModal.vue` (wizard 3 passos)
- `frontend/src/components/termos/TermoVisualizacaoDrawer.vue`

**Arquivos alterados:**
- `frontend/src/views/pacientes/PacienteDetalheView.vue` (substitui empty state da aba `termos`)

Relaciona-se com [[project-termos-consentimento-fase1]] (backend) e [[project-termos-consentimento-fase2]] (UI de modelos).
