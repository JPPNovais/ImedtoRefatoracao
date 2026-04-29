-- Item 2.15 — Fase 2: Defense-in-depth contra overlap de agendamento.
-- Bloqueia, no nivel do banco, dois agendamentos do mesmo profissional
-- com intervalos [inicio_previsto, fim_previsto) sobrepostos.
-- Complementa o check do handler (item 1.2 da Fase 1) — o banco eh a fonte
-- final de verdade caso o handler tenha bug, race condition ou bypass.
--
-- Linhas com status='Cancelado' sao excluidas do constraint (WHERE clause)
-- pois um horario cancelado nao deve impedir reagendamento no mesmo slot.
-- Status atuais (verificado em prod): Agendado, Concluido, Cancelado.

CREATE EXTENSION IF NOT EXISTS btree_gist;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'agendamentos_no_overlap'
          AND conrelid = 'public.agendamentos'::regclass
    ) THEN
        ALTER TABLE public.agendamentos
            ADD CONSTRAINT agendamentos_no_overlap
            EXCLUDE USING gist (
                profissional_usuario_id WITH =,
                tstzrange(inicio_previsto, fim_previsto, '[)') WITH &&
            )
            WHERE (status <> 'Cancelado');
    END IF;
END
$$;
