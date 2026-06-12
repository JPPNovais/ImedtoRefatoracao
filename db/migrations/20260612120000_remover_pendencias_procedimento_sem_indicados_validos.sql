-- Remoção de pendências órfãs de ação MarcarProcedimentoRealizado sem procedimentos indicados válidos.
-- Contexto: bugfix — antes do guard adicionado em PendenciaExtratorEvolucao.TemProcedimentosIndicadosValidos,
-- era possível criar pendências de MarcarProcedimentoRealizado mesmo quando a evolução não tinha
-- nenhum procedimento indicado com catalogoCirurgiaId não-nulo e valor > 0.
-- Essas pendências são impossíveis de concluir: o command handler lança 422 sem procedimentos válidos.
-- O fix de código já impede criação de novas; este script limpa as que já existiam em dev.
--
-- Critério espelha exatamente PendenciaExtratorEvolucao.TemProcedimentosIndicadosValidos:
--   - acao = 'MarcarProcedimentoRealizado'
--   - status = 'Pendente'
--   - evolucao.conteudo NÃO contém ao menos 1 item em procedimentos-indicados.procedimentos[]
--     com catalogoCirurgiaId não-nulo E soma(valor) > 0
--
-- Hard DELETE (não soft-conclude): pendências geradas por bug, sem cobrança gerada,
-- sem referência externa (SELECT de FK reversa em pg_constraint retornou 0 linhas),
-- nunca deveriam ter existido. Marcar como 'Concluida' seria enganoso — seria registrar
-- conclusão de algo que não foi realizado. DELETE é a escolha correta.
--
-- Multi-tenant: WHERE não filtra por estabelecimento_id — opera em todos os tenants pelo
-- critério de domínio. Garante limpeza completa, não apenas nos dados de teste.
--
-- Idempotente: segunda execução deleta 0 linhas e retorna sucesso (DELETE é naturalmente idempotente).
-- Não há FK reversa apontando para pendencias_atendimento, portanto não há risco de violação.
--
-- Não registra em __ef_migrations_history: é data-migration pura (sem mudança de schema EF).
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.

DELETE FROM public.pendencias_atendimento pa
USING public.prontuario_evolucoes pe
WHERE pa.evolucao_id = pe.id
  AND pa.acao   = 'MarcarProcedimentoRealizado'
  AND pa.status = 'Pendente'
  AND NOT (
      -- NÃO satisfaz: tem ao menos 1 item com catalogoCirurgiaId não-nulo E soma(valor) > 0
      EXISTS (
          SELECT 1
          FROM jsonb_array_elements(
              pe.conteudo #> '{procedimentos-indicados,procedimentos}'
          ) AS proc
          WHERE proc -> 'catalogoCirurgiaId' IS NOT NULL
            AND (proc -> 'catalogoCirurgiaId') != 'null'::jsonb
      )
      AND (
          SELECT COALESCE(SUM((proc ->> 'valor')::numeric), 0)
          FROM jsonb_array_elements(
              pe.conteudo #> '{procedimentos-indicados,procedimentos}'
          ) AS proc
          WHERE proc -> 'catalogoCirurgiaId' IS NOT NULL
            AND (proc -> 'catalogoCirurgiaId') != 'null'::jsonb
      ) > 0
  );
