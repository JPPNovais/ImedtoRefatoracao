-- Índices de confidencialidade — gating autor-ou-dono (briefing 2026-06-27_001)
-- ATENÇÃO: CREATE INDEX CONCURRENTLY não pode rodar dentro de transação.
-- Este arquivo é executado pelo pipeline APÓS o arquivo principal (_evolucao.sql),
-- fora de qualquer bloco BEGIN/COMMIT.
-- Idempotente: IF NOT EXISTS em cada comando.

-- 1. prontuario_evolucoes (prontuario_id, autor_usuario_id)
--    Cobre: timeline gated, listagem paginada e contagem de evoluções por autor.
--    Razão: o índice existente ix_evolucoes_prontuario_data (prontuario_id, criada_em)
--    cobre o ORDER BY mas não elimina o Filter pós-índice em autor_usuario_id.
--    Em pacientes com muitas evoluções de múltiplos profissionais, esse filter
--    percorre todas as linhas do prontuário antes de filtrar pelo autor.
--    Com (prontuario_id, autor_usuario_id) o Postgres salta direto para as linhas
--    do autor; o ORDER BY por criada_em usa ix_evolucoes_prontuario_data em paralelo
--    se o planner preferir (ou sort em memória, aceitável para volumes por autor).
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_evolucoes_prontuario_autor
    ON prontuario_evolucoes (prontuario_id, autor_usuario_id);

-- 2. prontuario_anexos: ix_anexos_evolucao (evolucao_id) já existe.
--    Sem ação para o caminho de anexo COM evolução (JOIN no gating de evolução).

-- 3. prontuario_anexos (criado_por_usuario_id) — parcial WHERE evolucao_id IS NULL
--    Cobre: gating de anexo ÓRFÃO (legado/pré-evolução) pelo uploader.
--    Razão: quando evolucao_id IS NULL, o predicado recai sobre criado_por_usuario_id.
--    Sem índice específico, a query faz seq scan / bitmap scan varrendo todos os anexos
--    do prontuário. O índice parcial é mais enxuto (cobre só os órfãos) e muito seletivo.
--    Não inclui deletado_em IS NULL na condição parcial porque o gating de download
--    de URL assinada verifica arquivamento no handler, não no WHERE de índice.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_anexos_criado_por_orfao
    ON prontuario_anexos (criado_por_usuario_id)
    WHERE evolucao_id IS NULL;

-- 4. atestados (paciente_id, profissional_usuario_id)
--    Cobre: listagem gated de atestados do paciente por autor.
--    Razão: o índice existente ix_atestados_paciente_criado (paciente_id, criado_em)
--    não cobre profissional_usuario_id — resulta em Filter pós-índice.
--    O novo índice composto elimina o filter: Postgres localiza exatamente os atestados
--    daquele profissional para aquele paciente via Index Scan direto.
--    A ordenação por criado_em ainda usa ix_atestados_paciente_criado se necessário,
--    ou sort em memória (volume por paciente+profissional é tipicamente < 100 linhas).
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_atestados_paciente_profissional
    ON atestados (paciente_id, profissional_usuario_id);

-- 5. pedidos_exame (paciente_id, profissional_usuario_id)
--    Cobre: listagem gated de pedidos de exame por autor.
--    Mesmo raciocínio do atestados (#4).
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_pedidos_exame_paciente_profissional
    ON pedidos_exame (paciente_id, profissional_usuario_id);

-- 6. receitas (paciente_id, profissional_usuario_id)
--    Cobre: listagem gated de receitas do paciente por autor.
--    Razão: o índice existente ix_receitas_paciente_emitida (paciente_id, emitida_em)
--    não cobre profissional_usuario_id — a query gated `(paciente_id = @P AND
--    (profissional_usuario_id = @X OR @ehDono))` quando ehDono=false resulta em
--    Filter pós-índice varrendo todas as receitas do paciente.
--    Nota: ix_receitas_estab_prof_emitida (estabelecimento_id, profissional_usuario_id, emitida_em)
--    cobre o relatório "minhas receitas" mas não a lista por paciente.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_receitas_paciente_profissional
    ON receitas (paciente_id, profissional_usuario_id);
