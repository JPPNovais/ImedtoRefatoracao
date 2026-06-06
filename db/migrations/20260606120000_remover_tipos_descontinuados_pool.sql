-- Remoção de registros órfãos dos tipos descontinuados do pool de variáveis.
-- Briefing: 2026-06-05_001 (CA12) + addendum (Expectativa permanece; só Droga e AtividadeFisica saem).
--
-- Contexto: a coluna `tipo` é string (HasConversion<string>, varchar max 20).
-- O enum TipoVariavelPool foi atualizado pelo dev para remover Droga e AtividadeFisica.
-- Após este DELETE, nenhuma linha com esses tipos permanece no banco.
-- As 6 linhas válidas restantes (Alergia, Medicamento, Doenca, Cirurgia,
-- RelacaoFamiliar, Expectativa) NÃO são afetadas.
--
-- Hard delete: são dados de catálogo, não PII de paciente.
-- O dono confirmou remoção completa.
--
-- Idempotente: segunda execução deleta 0 linhas e retorna sucesso (DELETE é naturalmente idempotente).
--
-- NÃO toca: campos de texto livre da História Social (atividadeFisicaNivel/Obs, drogas) —
-- esses campos pertencem ao ConteudoJson da evolução, NÃO a esta tabela.
--
-- Down: não há. Dados deletados não são recuperáveis por rollback de schema.
-- Se necessário restaurar, recriar via seed manual pelo admin.
--
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.
-- Não registra em __ef_migrations_history: é data-migration pura (sem mudança de schema).

DELETE FROM public.prontuario_variaveis_pool
WHERE tipo IN ('Droga', 'AtividadeFisica');
