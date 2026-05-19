---
name: project-foto-profissional-propagacao
description: Foto do profissional propagada nas 7 superfícies (MinhaConta + equipe + dropdowns Agenda + cards). DELETE /api/profissional/me/foto idempotente.
metadata:
  type: project
---

Foto do profissional foi propagada em 2026-05-18 nas seguintes superfícies:

1. **Aba Equipe** (`AbaProfissionais.vue`) — `<AppAvatar>` no row
2. **Modal detalhes** (`ProfissionalDetalhesModal.vue`) — `<AppAvatar tamanho="lg">` no header
3. **Filtro Agenda + dropdowns Novo/Editar Agendamento** — `<AppAvatarSelect>` (novo no design system)
4. **Card de agendamento** (`AgendamentoRow.vue`) — `<AppAvatar tamanho="sm">` antes do nome do profissional
5. **MinhaContaView** — refatorada para usar `<AppPhotoUpload>` (padronização com Estabelecimento)

**Backend**
- `Profissional.RemoverFoto()` (aggregate, idempotente — espelha `Estabelecimento.RemoverFoto`).
- `RemoverFotoProfissionalCommand` + Handler. Ordem: delete S3 → aggregate (mesmo pattern de Estab).
- `DELETE /api/profissional/me/foto` (Auth, `[UnitOfWork]`, 204 idempotente).
- `ProfissionalPublicoDto`, `ProfissionalVinculadoDto`, `AgendamentoDto` ganharam `FotoUrl`/`ProfissionalFotoUrl`.
- Queries Dapper (`VinculoQueryRepository`, `AgendamentoQueryRepository`) atualizadas com `p.foto_url` + `LEFT JOIN profissionais ... AND deletado_em IS NULL` (defense-in-depth: profissional soft-deletado não vaza foto).

**Frontend novos componentes no design system**
- `AppAvatar` — placeholder com iniciais + cor hash determinística + img quando há foto. Tamanhos `sm`/`md`/`lg`/`xl`.
- `AppAvatarSelect` — dropdown custom (`<button>` + `<ul role=listbox>`) que substitui `<select>` nativo nos seletores de profissional. Suporta `permite-limpar` (para filtro "Todos").

**Why:** `<select>` nativo do browser não suporta imagens em `<option>` — sem custom select, não era possível mostrar avatar nos dropdowns conforme pedido pelo usuário.

**How to apply:** sempre que uma tela nova precisar de seletor de profissional, usar `AppAvatarSelect` (não `AppSelect` com options custom). Para exibição simples de avatar (sem upload), usar `AppAvatar`. Para upload, usar `AppPhotoUpload` (Estab/Profissional já consolidados).

**Decisão registrada — PDFs:**
Foto do profissional NÃO foi adicionada nos PDFs (receita, atestado, pedido-exame, prontuário). Motivos:
1. PDFs médicos seguem padrões oficiais (CFM/CRO) — foto não é prática estabelecida em rodapé de receita.
2. Caminho futuro para assinatura é ICP-Brasil, não foto.
3. Risco LGPD: PDF circula fora do sistema; mais um dado pessoal sem benefício claro.

Caso usuário valide e queira reverter, mexer apenas em `usePdfHeader.desenharRodape` (campo `opt.assinatura`) — basta receber `fotoUrl?` e desenhar `addImage` antes do nome.

**Visibilidade da foto (LGPD):** segue mesma regra de `nomeCompleto`/`especialidade` que já existia — qualquer membro ativo do mesmo estabelecimento vê. `ProfissionalPublicoDto.FotoUrl` é PII de baixo risco, justificada para UX dos seletores.

Veja também: [[project-foto-estabelecimento-pdfs]] (padrão de upload + cache invalidation com `invalidarCacheEstabelecimentoAtivo`).
