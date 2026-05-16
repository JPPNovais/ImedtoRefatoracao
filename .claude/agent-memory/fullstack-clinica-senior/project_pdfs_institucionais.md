---
name: project_pdfs_institucionais
description: Padrão visual unificado dos PDFs do produto (Receita backend QuestPDF, Prontuário e Relatório frontend jsPDF).
metadata:
  type: project
---

Os 3 geradores de PDF (Receita backend `QuestPdfReceitaService`, Prontuário `useProntuarioPdf`, Relatório `useRelatorioPdf`) aplicam o **mesmo design institucional** extraído do mock `PrintPreview` (azul-marinho `#1A2440`, Nunito, marca d'água "IMEDTO" sutil, header com logo+CNPJ+telefone, rodapé com paginação).

**Why:** evitar 3 layouts divergentes do mesmo produto. Receita controlada usa variante vermelha (`#dc2626`) + caixa amarela "1ª via Farmácia / 2ª via Paciente"; relatório omite o bloco de assinatura e troca por aviso "Documento de gestão". Implementado em 2026-05-16 com base no plano `PLANO_REDESIGN_PDF.md`.

**How to apply:**
- **Frontend**: tudo passa pelo helper [[composables_usePdfHeader]] — chame `desenharCabecalho` + `desenharBlocoPaciente` + `finalizarPaginas`. Cores em `PDF_THEME`, fonte registrada via `registrarFontesNunito(doc)`. Dados do estabelecimento via `carregarEstabelecimentoAtivo(tenantId)` (cache em sessão).
- **Backend**: `QuestPdfReceitaService.cs` (Infrastructure) com fontes Nunito como `EmbeddedResource` no `.csproj` do projeto Infrastructure (registradas via `FontManager.RegisterFont` no static init). Dados do `Estabelecimento` são lidos no SQL de Dapper do gerador (JOIN com `estabelecimentos`).
- Para tocar um dos 3 PDFs, **mantenha o padrão**: nunca duplique header/footer/watermark em outro composable — estenda o helper.
