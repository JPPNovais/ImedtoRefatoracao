---
name: inspecionar-working-tree-inteiro-antes-commit
description: Antes do primeiro `git add` por path, ler `git status` inteiro — fullstack costuma deixar trabalho órfão de outras features junto.
metadata:
  type: feedback
---

Quando recebo uma entrega do `fullstack-clinica-senior` para QA, **ler `git status` completo ANTES de fazer `git add` por path específico**. O fullstack frequentemente:
- Mexe em arquivos compartilhados (`Container.cs`, `index.ts` do design system, `AppDbContext`) por causa de OUTRA feature ainda em andamento, e essa outra feature pode ter arquivos novos `untracked` ou modificações em arquivos sem relação aparente com a tarefa que me foi passada.
- Empurra registro de handler/comando no `Container.cs` enquanto o `Command*.cs` e `Handler*.cs` correspondentes ficam como untracked → se eu commitar só "minha" parte, quebro o build de produção.

**Why:** Em 2026-05-19 dei push do feat de orçamento (commit d921c9d) achando que só mexia em orçamento. O Container.cs que veio junto registrava `RemoverFotoProfissionalCommandHandler`/`RemoverFotoProfissionalCommand` (feature "remover foto"), classes que estavam UNTRACKED. Pipeline quebrou em test-backend com `CS0246 type not found`. Tive que fazer 3 hotfixes consecutivos pra completar:
- d921c9d → 04031d2 (incluiu Remover foto inteiro) → 31ba849 (fix cultura pt-BR no Domain) → a6b82ca (incluiu AppAvatarSelect untracked que index.ts referenciava)

4 deploys em vez de 1, mesma sessão.

**How to apply:**
- ANTES de `git add` por path, rodar `git status --short` e listar TUDO que aparece como `M`/`A`/`??`.
- Para CADA arquivo modificado fora do escopo declarado: rodar `git diff <arquivo>` e perguntar "isso tem dependência com o que vou commitar?". Em particular para `*/Container.cs`, `*/index.ts` (barrel), `AppDbContext.cs`, `permissions.ts`.
- Para arquivos UNTRACKED: rodar `grep -rn "<nome>" .` para ver se algum arquivo que VOU commitar referencia eles.
- Se sim → trazer pro commit ou alinhar com fullstack pra rebaixar a referência.
- Rodar `dotnet build && npm run build` DEPOIS de fazer `git add` (não antes!) — pra validar que o estado **staged** compila por si só, não o working tree inteiro.
- Bonus: rodar `dotnet test` com `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` para pegar bugs de cultura como o CA-6 antes que o CI quebre.

Caveat relacionado: vue-tsc passa local mas o Docker build no CI tem cwd diferente. Erro "Cannot find module" em barrel (index.ts) que referencia .vue não-trackeado só aparece no build do container.
