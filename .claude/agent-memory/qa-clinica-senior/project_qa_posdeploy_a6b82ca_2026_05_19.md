---
name: qa-posdeploy-a6b82ca-2026-05-19
description: QA da paridade orçamento legado — 4 pipelines (3 falhas + 1 verde), 8/10 cenários verde em produção, 2 bugs P2 visuais.
metadata:
  type: project
---

Deploy final: commit `a6b82ca` (HEAD após 4 pushes consecutivos). Paridade do orçamento com o legado entregue: Local Cirúrgico substitui Internação, consolidação de produtos via backend, cálculo de honorário por tempo, vínculo com agendamento. Pipeline final 6/6 jobs verde.

**Why:** O fullstack-clinica-senior empurrou trabalho órfão no working tree (feature "remover foto profissional" + "avatar nas telas de equipe/agenda") junto da paridade do orçamento — eu não notei antes do primeiro push e precisei de 3 hotfixes consecutivos:
1. d921c9d — paridade orçamento (FALHOU: Container.cs ref Handler que não estava no commit)
2. 04031d2 — feature remover foto completa (FALHOU: 1 teste cultura en-US no CI + AppAvatarSelect untracked)
3. 31ba849 — fix CultureInfo pt-BR no ValidarIntegridade (FALHOU: AppAvatarSelect ainda untracked, vue-tsc passou local mas build Docker quebrou)
4. a6b82ca — AppAvatarSelect.vue + integrações esquecidas na agenda (VERDE 6/6)

**Cenários QA em prod:**
- C1 ✅ "Novo orçamento" navega para `/orcamentos/novo` SEM POST (só GETs de catálogos)
- C2 ✅ Configuração de 3 Locais Cirúrgicos (IntGeral 2000/120/30/300, IntLocal 1500/60/30/200, SemInternacao fixo 500) — todos PUT 200, sem erro console
- C3 ✅ Criar orçamento ponta-a-ponta: paciente → cirurgia QA (90min, R$5000 do catálogo) → Local IntGeral (calculado R$2300 server-side: 2000 base + 1 bloco adicional de 30min × 300) → forma pgto R$7300 → POST 201 → /orcamentos/8
- C4 ✅ Erro de soma divergente: front bloqueia o botão Salvar (disabled), mostra "Faltam R$ 2.300,00 para fechar com o total (R$ 5.000,00 de R$ 7.300,00)" + alert "Soma das formas de pagamento difere do total em R$ 2300.00."
- C5 ✅ Tentar criar sem Local Cirúrgico configurado (tipo Ambulatorio): 422 BusinessException "Local cirúrgico não configurado para este estabelecimento. Configure em Orçamento → Configurações." (não 500)
- C6 ✅ Tab "Quitados" não aparece (só Todos/Pendentes/Aprovados/Perdidos)
- C7 ⏸️ Consolidação de produtos não testada em prod (catálogo de produtos vazio na conta QA); coberto por 8 unit tests do `ProdutosConsolidadorTests`
- C10 ✅ Lista mostra ORC-202605-0008 com paciente, status Rascunho, R$7.300,00, totalizadores corretos
- C13 ✅ Multi-tenant: GET /api/orcamentos/1 (outro tenant) e /api/orcamentos/9999 (inexistente) ambos retornam 422 "Orçamento não encontrado" idêntico (não vaza dado nem permite enumeração)
- C14 ✅ Zero erros no console fora dos 3 422s que eu mesmo gerei

**Bugs P2 (não-bloqueantes) encontrados na UI:**
1. **"Invalid Date" em CRIADO EM**: aparece na lista e na tela de detalhe. Provavelmente o DTO retorna `criadoEm` em formato que `new Date(...)` não parseia, ou está `null`. Conferir [[fix-criado-em-invalid-date-orcamento]].
2. **Local Cirúrgico mostrado como enum cru**: na tela de detalhe aparece "IntGeral (90 min = R$ 2.300,00)" ao invés do label amigável "Com Internação - Anestesia Geral + TOT". Falta mapeamento no detalhe.
3. **Formatação inconsistente da diferença**: a validação client-side da linha 535 (`OrcamentoFormView.vue`) usa `diferenca.value.toFixed(2)` ("R$ 2300.00") mas o resto do form usa `fmt(...)` com `Intl.NumberFormat("pt-BR")` ("R$ 2.300,00"). Trocar `toFixed(2)` por `fmt()`.
4. **`alert()` ainda no OutrasConfigsTab linha 69**: salvar config de local com erro usa alert() nativo (já no padrão pré-existente do arquivo). Trocar por AppToast.

**Não-bug — não confundir:**
- Mensagem "R$ 2300.00" do alert é do FRONT (toFixed), NÃO do backend. O backend já foi corrigido pra usar pt-BR explícito (commit 31ba849) e o teste CA-6 confirma — mas como o front bloqueia antes de chamar o back, não testei a resposta 422 do back.
