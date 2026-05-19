---
name: qa-posdeploy-708d5c0-2026-05-18
description: QA pós-deploy commit 708d5c0 — Prontuário consolida exame físico em seção, adiciona abas Atestado e Pedidos de exame.
metadata:
  type: project
---

**Commit 708d5c0** (2026-05-18) — `feat(prontuario): atestado, pedido de exame e exame físico consolidado`.

**Pipeline GitHub Actions** run 26053147532: 6/6 jobs verde (test-backend, test-frontend, build-push, migrate, deploy, smoke).

**Validação em prod (https://app.imedto.com) — 10/10 critérios verde:**

1. Aba "Exame físico" removida — abas atuais: Consulta atual, Consultas anteriores, Receitas, Atestado, Pedidos de exame.
2. SecaoExameFisico renderiza sinais vitais + antropometria (com IMC calculado em tempo real: peso 68 + altura 170 → 23.5 + "Eutrófico") + ectoscopia + mapa corporal interativo (FRENTE/COSTAS).
3. Emissão atestado de Afastamento via modelo pré-criado + CID J06.9: 201, lista atualizada, toast "Atestado emitido.", PDF aberto em nova aba via `blob:` URL — sem noopener (padrão confirmado).
4. Pedido de exame Laboratorial com 3 exames (Hemograma+TSH+Glicemia) + indicação + CID Z00.0: 201, PDF gerado com tabela de exames.
5. Modelo de atestado por estabelecimento confirmado (filtro `estabelecimento_id` em ListarModelos).
6. Validações negativas back: dias=0 → 422, sem indicação → 422, CID inválido → 422, sem exames → 422. Todas com mensagem genérica.
7. Multi-tenant: estab alheio (id=1) → 403 `SemAcesso`; estab inexistente → 404; paciente cross-tenant → 422 "Paciente não encontrado." (mensagem genérica).
8. Anti-tampering: payload contendo `estabelecimentoId: 1` extra → ignorado, atestado criado com `estabelecimento_id=17` (do tenant accessor).
9. Audit LGPD: cada emissão e listagem gera entrada em `prontuario_acesso_log` com tipo correto (Escrita/Leitura).
10. Persistência após reload da página: aba "Consultas anteriores 1" mantida, lazy loading correto (não chama `/atestados` nem `/pedidos-exame` no bootstrap — só quando clica na aba).

**Performance**: page reload dispara apenas 8 requests (bootstrap, paciente, prontuario, modelos, regioes-anatomicas, etc.) — abas Atestado/Pedido só carregam quando ativadas. Critério "buscar apenas quando precisar" do CLAUDE.md atendido.

**Console limpo**: zero errors/warns em todo o fluxo.

**Débito P2 (não-bloqueante, registrado para backlog):**
- `SecaoExameFisico` usa `<input class="input-field">` / `<select class="input-field">` cru em vez de AppInput/AppSelect. Padrão herdado da ExameFisicoTab original deletada — não introduzido nesta PR. Quando migrar, beneficia também a ectoscopia (9 selects).

**Risco MVP confirmado em prod:**
- Lista de atestados/pedidos é `IReadOnlyList<>` não paginada. Para >50 itens por paciente vai impactar — backlog.

**Conta de QA criada para este ciclo:**
- usuário `qa-prontuario-708d5c0@imedto.test` (id `982d3578-6c37-4a7e-9075-f80b744f6213`)
- estabelecimento id=17 ("QA Prontuario Estab"), plano Trial
- paciente id=213 ("Paciente QA Prontuario")
- pode ser reutilizada em ciclos futuros do prontuário.
