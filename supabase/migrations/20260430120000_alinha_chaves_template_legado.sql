-- Alinha as chaves de seção do template padrão "Consulta clínica geral" com as
-- chaves canônicas do legado (queixa, hda, hpp, h-familiar, h-social, exame-fisico,
-- exames-realizados, hipotese-diagnostica, procedimentos-indicados, conduta, medicacoes).
--
-- Motivo: o seed anterior (20260419180000) usou snake_case (queixa_principal,
-- historia_pregressa, etc.) que NÃO bate com os v-if do SecaoProntuario.vue
-- (que segue o legado com hpp, h-familiar, etc.). Como consequência, as seções
-- estruturadas (HPP, exame físico, etc.) caíam no input genérico em vez de
-- renderizar o componente especializado.
--
-- Esta migration:
--   1. Reescreve a estrutura de TODOS os modelos existentes substituindo as
--      chaves antigas pelas chaves do legado.
--   2. Migra o `conteudo` jsonb das `prontuario_evolucoes` que ainda tenham
--      chaves antigas (renomeação plana). Trigger de imutabilidade é
--      desabilitado e reabilitado em volta da operação.
--
-- Idempotente: se a chave nova já está em uso, o UPDATE é no-op.

-- ============================================================
-- 1. Renomeia chaves no template (modelo_de_prontuario.estrutura)
-- ============================================================
WITH mapa(antiga, nova) AS (
    VALUES
        ('queixa_principal',        'queixa'),
        ('historia_doenca_atual',   'hda'),
        ('historia_pregressa',      'hpp'),
        ('historia_familiar',       'h-familiar'),
        ('historia_social',         'h-social'),
        ('exame_fisico',            'exame-fisico'),
        ('exames_realizados',       'exames-realizados'),
        ('hipotese_diagnostica',    'hipotese-diagnostica'),
        ('procedimentos_indicados', 'procedimentos-indicados')
)
UPDATE public.modelo_de_prontuario m
SET estrutura = (
    SELECT jsonb_agg(
        CASE
            WHEN s->>'chave' IN (SELECT antiga FROM mapa)
                THEN jsonb_set(s, '{chave}', to_jsonb((SELECT nova FROM mapa WHERE antiga = s->>'chave')))
            ELSE s
        END
        ORDER BY (s->>'ordem')::int
    )
    FROM jsonb_array_elements(m.estrutura) s
),
    atualizado_em = now()
WHERE jsonb_typeof(m.estrutura) = 'array'
  AND EXISTS (
      SELECT 1 FROM jsonb_array_elements(m.estrutura) s
      WHERE s->>'chave' IN (
          'queixa_principal','historia_doenca_atual','historia_pregressa',
          'historia_familiar','historia_social','exame_fisico',
          'exames_realizados','hipotese_diagnostica','procedimentos_indicados'
      )
  );

-- ============================================================
-- 2. Migra evoluções com chaves antigas para chaves do legado
-- ============================================================
-- Trigger `prontuario_evolucao_imutavel` bloqueia UPDATE em prontuario_evolucoes;
-- desabilitamos só o tempo da migração e reabilitamos no fim.

ALTER TABLE public.prontuario_evolucoes DISABLE TRIGGER USER;

UPDATE public.prontuario_evolucoes
SET conteudo = (
    conteudo
        - 'queixa_principal' - 'historia_doenca_atual' - 'historia_pregressa'
        - 'historia_familiar' - 'historia_social' - 'exame_fisico'
        - 'exames_realizados' - 'hipotese_diagnostica' - 'procedimentos_indicados'
)
    || (CASE WHEN conteudo ? 'queixa_principal'        THEN jsonb_build_object('queixa', conteudo->'queixa_principal')                              ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'historia_doenca_atual'   THEN jsonb_build_object('hda', conteudo->'historia_doenca_atual')                            ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'historia_pregressa'      THEN jsonb_build_object('hpp', conteudo->'historia_pregressa')                               ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'historia_familiar'       THEN jsonb_build_object('h-familiar', conteudo->'historia_familiar')                         ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'historia_social'         THEN jsonb_build_object('h-social', conteudo->'historia_social')                             ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'exame_fisico'            THEN jsonb_build_object('exame-fisico', conteudo->'exame_fisico')                            ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'exames_realizados'       THEN jsonb_build_object('exames-realizados', conteudo->'exames_realizados')                  ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'hipotese_diagnostica'    THEN jsonb_build_object('hipotese-diagnostica', conteudo->'hipotese_diagnostica')            ELSE '{}'::jsonb END)
    || (CASE WHEN conteudo ? 'procedimentos_indicados' THEN jsonb_build_object('procedimentos-indicados', conteudo->'procedimentos_indicados')      ELSE '{}'::jsonb END)
WHERE conteudo ?| array[
    'queixa_principal','historia_doenca_atual','historia_pregressa',
    'historia_familiar','historia_social','exame_fisico',
    'exames_realizados','hipotese_diagnostica','procedimentos_indicados'
];

ALTER TABLE public.prontuario_evolucoes ENABLE TRIGGER USER;
