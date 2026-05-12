-- Seed de modelos de prontuário padrão-sistema (estabelecimento_id NULL, eh_padrao_sistema = true).
-- Sem isso, qualquer usuário novo não consegue iniciar prontuário — tabela ficava vazia em prod
-- após a migração para RDS (achado N1 do qa/REPORT-V2.md).
--
-- Idempotente: cada INSERT verifica por (nome, eh_padrao_sistema = true) antes de inserir;
-- não há unique constraint na tabela para usar ON CONFLICT. O front lista esses modelos via
-- /api/prontuario/modelos e o usuário pode escolher um ao chamar POST /api/paciente/{id}/prontuario.
--
-- Estrutura JSON: array de objetos { chave, titulo, tipo, ordem } — formato esperado pelo
-- SecaoModelo do frontend (frontend/src/services/prontuarioService.ts). O catálogo completo
-- de seções vive em frontend/src/views/configuracoes/ModelosProntuarioView.vue:17.

-- 1) Consulta clínica geral — anamnese padrão para atendimento ambulatorial.
INSERT INTO public.modelo_de_prontuario
    (estabelecimento_id, nome, descricao, estrutura, eh_padrao_sistema, ativo, criado_em)
SELECT
    NULL,
    'Consulta clínica geral',
    'Anamnese padrão para consulta ambulatorial: QP, HDA, HPP, exame físico, hipóteses e conduta.',
    '[
      {"chave":"queixa",            "titulo":"Queixa principal (QP)",         "tipo":"texto_longo", "ordem":0},
      {"chave":"hda",               "titulo":"História da doença atual (HDA)", "tipo":"texto_longo", "ordem":1},
      {"chave":"hpp",               "titulo":"História pregressa (HPP)",       "tipo":"texto_longo", "ordem":2},
      {"chave":"h-familiar",        "titulo":"História familiar",              "tipo":"texto_longo", "ordem":3},
      {"chave":"h-social",          "titulo":"História social e hábitos",      "tipo":"texto_longo", "ordem":4},
      {"chave":"exame-fisico",      "titulo":"Exame físico",                   "tipo":"texto_longo", "ordem":5},
      {"chave":"exames-realizados", "titulo":"Exames realizados",              "tipo":"texto_longo", "ordem":6},
      {"chave":"cid10",             "titulo":"CID-10",                         "tipo":"texto",       "ordem":7},
      {"chave":"conduta",           "titulo":"Conduta",                        "tipo":"texto_longo", "ordem":8}
    ]'::jsonb,
    true,
    true,
    now()
WHERE NOT EXISTS (
    SELECT 1 FROM public.modelo_de_prontuario
    WHERE eh_padrao_sistema = true AND nome = 'Consulta clínica geral'
);

-- 2) Avaliação pré-operatória — paciente em planejamento cirúrgico.
INSERT INTO public.modelo_de_prontuario
    (estabelecimento_id, nome, descricao, estrutura, eh_padrao_sistema, ativo, criado_em)
SELECT
    NULL,
    'Avaliação pré-operatória',
    'Coleta para indicação cirúrgica: anamnese, comorbidades, exames complementares e plano terapêutico.',
    '[
      {"chave":"queixa",                 "titulo":"Queixa principal (QP)",            "tipo":"texto_longo", "ordem":0},
      {"chave":"hda",                    "titulo":"História da doença atual (HDA)",    "tipo":"texto_longo", "ordem":1},
      {"chave":"hpp",                    "titulo":"História pregressa (HPP)",          "tipo":"texto_longo", "ordem":2},
      {"chave":"h-familiar",             "titulo":"História familiar",                 "tipo":"texto_longo", "ordem":3},
      {"chave":"exame-fisico",           "titulo":"Exame físico",                      "tipo":"texto_longo", "ordem":4},
      {"chave":"exames-realizados",      "titulo":"Exames realizados",                 "tipo":"texto_longo", "ordem":5},
      {"chave":"procedimentos-indicados","titulo":"Procedimentos indicados",           "tipo":"texto_longo", "ordem":6},
      {"chave":"cid10",                  "titulo":"CID-10",                            "tipo":"texto",       "ordem":7},
      {"chave":"conduta",                "titulo":"Conduta",                           "tipo":"texto_longo", "ordem":8}
    ]'::jsonb,
    true,
    true,
    now()
WHERE NOT EXISTS (
    SELECT 1 FROM public.modelo_de_prontuario
    WHERE eh_padrao_sistema = true AND nome = 'Avaliação pré-operatória'
);

-- 3) Procedimento em consultório — pequenos procedimentos / curativos / aplicações.
INSERT INTO public.modelo_de_prontuario
    (estabelecimento_id, nome, descricao, estrutura, eh_padrao_sistema, ativo, criado_em)
SELECT
    NULL,
    'Procedimento em consultório',
    'Registro de procedimentos menores realizados no consultório: técnica, evolução imediata e anexos.',
    '[
      {"chave":"queixa",                  "titulo":"Queixa principal (QP)",         "tipo":"texto_longo", "ordem":0},
      {"chave":"exame-fisico",            "titulo":"Exame físico",                  "tipo":"texto_longo", "ordem":1},
      {"chave":"procedimento-consultorio","titulo":"Procedimento em consultório",   "tipo":"texto_longo", "ordem":2},
      {"chave":"conduta",                 "titulo":"Conduta",                       "tipo":"texto_longo", "ordem":3},
      {"chave":"fotos-paciente",          "titulo":"Fotos do paciente",             "tipo":"texto_longo", "ordem":4},
      {"chave":"anexos",                  "titulo":"Anexos",                        "tipo":"texto_longo", "ordem":5}
    ]'::jsonb,
    true,
    true,
    now()
WHERE NOT EXISTS (
    SELECT 1 FROM public.modelo_de_prontuario
    WHERE eh_padrao_sistema = true AND nome = 'Procedimento em consultório'
);

-- 4) Evolução pós-operatória — acompanhamento após cirurgia já realizada.
INSERT INTO public.modelo_de_prontuario
    (estabelecimento_id, nome, descricao, estrutura, eh_padrao_sistema, ativo, criado_em)
SELECT
    NULL,
    'Evolução pós-operatória',
    'Acompanhamento clínico após cirurgia: descrição cirúrgica, ficha anestésica, evolução e conduta.',
    '[
      {"chave":"desc-cirurgica",   "titulo":"Descrição cirúrgica",       "tipo":"texto_longo", "ordem":0},
      {"chave":"equipe-cirurgica", "titulo":"Equipe cirúrgica",          "tipo":"texto_longo", "ordem":1},
      {"chave":"ficha-anestesica", "titulo":"Ficha anestésica",          "tipo":"texto_longo", "ordem":2},
      {"chave":"evolucao-pos-op",  "titulo":"Evolução pós-operatória",   "tipo":"texto_longo", "ordem":3},
      {"chave":"exame-fisico",     "titulo":"Exame físico",              "tipo":"texto_longo", "ordem":4},
      {"chave":"conduta",          "titulo":"Conduta",                   "tipo":"texto_longo", "ordem":5},
      {"chave":"fotos-paciente",   "titulo":"Fotos do paciente",         "tipo":"texto_longo", "ordem":6}
    ]'::jsonb,
    true,
    true,
    now()
WHERE NOT EXISTS (
    SELECT 1 FROM public.modelo_de_prontuario
    WHERE eh_padrao_sistema = true AND nome = 'Evolução pós-operatória'
);
