---
name: qa-posdeploy-59a5226-2026-05-16
description: QA da feature "+ Novo" inline nos selects do drawer Novo produto (Estoque-Cadastros). Aprovado em produção.
metadata:
  type: project
---

Deploy 59a5226 (push após 992d5bd) — pipeline 6/6 verdes (test-backend, test-frontend, build-push, migrate, deploy, smoke).

Feature: AppSelectComCriacao (novo no DS) + 4 modais rápidos (Categoria/Fabricante/Fornecedor/Local) que abrem por cima do AppDrawer "Novo produto" em /inventario/cadastros.

**Validado em produção (conta QA `qa-cad-rapido-1778966340@imedtoteste.local`, Dono):**
- Drawer mostra 4 botões "+ Novo" ao lado de cada select (Nova categoria / Novo fabricante / Novo fornecedor / Novo local).
- Cenário Categoria: form com Código/Nome/Unidade/QtdMin preenchidos → "+ Nova categoria" abre modal por cima → preencher "Anestésicos Teste UX" → Criar → modal fecha, categoria pré-selecionada, **TODO o form principal preservado** (não perde nada digitado).
- Cenário Fornecedor: CNPJ inválido (11.111.111/1111-11) → erro inline "CNPJ inválido" no AppField hint + msg-erro no rodapé, modal **não fecha**. CNPJ válido (11.222.333/0001-81) → POST 201, modal fecha, pré-seleção OK.
- Cenário Cancelar: abrir modal Fabricante → Cancelar → modal fecha, select volta ao "— Nenhum —" (valor anterior), nada perdido.
- Cenário criar produto end-to-end: produto "Bupivacaína Teste UX" criado, aparece na listagem com Categoria + Fornecedor recém-criados via inline.
- Network: 9 chamadas pós-deploy, todas 200/201. Zero 4xx/5xx. Console limpo (zero erros/warnings).

**Débitos abertos (não-bloqueantes):**
1. `tipoAcesso` (Recepcionista vs Profissional) não vem no BootstrapMeDto. Hoje botão "+ Novo" aparece pra qualquer usuário com acesso à tela; se um Profissional puro clicar, backend recusa 403/422 e msg aparece inline (não fecha modal) — comportamento aceitável mas pode ser melhorado.
2. Working tree segue com WIP de "Catálogos de Orçamento" + `AppConfirmDialog.vue` untracked. Stash `wip-orcamento-pre-commit-cadastros` segura o index.ts que tinha o export do AppConfirmDialog (precisará ser re-adicionado no próximo commit do orçamento). Total: 6 stashes acumulados.

Commit cirúrgico: 12 arquivos exatos (11 novos + 1 linha no index.ts). Sem AppConfirmDialog. HEREDOC, sem --no-verify.

Run: https://github.com/JPPNovais/ImedtoRefatoracao/actions/runs/25973076611
Screenshots: .qa-screenshots/cadastros-05a-drawer-com-botoes-novo.png, cadastros-05-novo-categoria-modal.png, cadastros-06-categoria-preselecionada.png, cadastros-07-produto-criado-com-cadastros-inline.png

Padrão validado pra reuso: `AppSelectComCriacao` + modais `acima-de-drawer` é a forma certa de oferecer cadastro inline sem perder estado do form principal. Aplica em qualquer drawer/form que liga a entidades mestre (futuro: drawer de Agendamento → Paciente, Profissional → Especialidade etc.).
