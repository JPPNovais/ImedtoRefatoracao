-- Índices CONCURRENTLY — Assinatura Digital de Receitas
-- Briefing: 2026-06-01_001_assinatura-digital-receitas
--
-- CREATE INDEX CONCURRENTLY não pode rodar dentro de transação.
-- Este arquivo é executado SEPARADAMENTE (fora do bloco transacional do migrate.sh)
-- pela pipeline: psql -c "$(cat 20260601120001_indices_assinatura_digital_concurrently.sql)"
--
-- Todos os índices abaixo são idempotentes: usam IF NOT EXISTS.

-- Índice parcial na tabela receitas: acelera o job ExpirarAssinaturasPendentesJob
-- que busca registros em AssinaturaPendente para marcar como AssinaturaExpirada.
-- Parcial: apenas as linhas no estado que o job precisa varrer — tamanho mínimo.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_receitas_status_assinatura_pendente
    ON public.receitas (assinatura_digital_status)
    WHERE assinatura_digital_status = 'AssinaturaPendente';

-- Índice em assinatura_solicitada_em para o job filtrar por janela de tempo
-- (pendentes há mais de 30 minutos). Composto com o índice parcial acima via
-- consulta: WHERE assinatura_digital_status = 'AssinaturaPendente'
--           AND assinatura_solicitada_em < now() - interval '30 minutes'
-- Este índice simples complementa o parcial para o filtro de tempo.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_receitas_assinatura_solicitada_em
    ON public.receitas (assinatura_solicitada_em)
    WHERE assinatura_digital_status = 'AssinaturaPendente';
