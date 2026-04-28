-- Atualiza o modelo de prontuário padrão do sistema com seções estruturadas
-- alinhadas ao legado: HPP, hist. familiar, hist. social, exames, procedimentos.
-- O frontend renderiza componentes específicos para essas chaves.

UPDATE public.modelo_de_prontuario
SET estrutura = '[
    {"chave":"queixa_principal",     "titulo":"Queixa principal",       "tipo":"texto",       "ordem":1},
    {"chave":"historia_doenca_atual","titulo":"História da doença atual","tipo":"texto_longo","ordem":2},
    {"chave":"historia_pregressa",   "titulo":"História pregressa (HPP)","tipo":"estruturado","ordem":3},
    {"chave":"historia_familiar",    "titulo":"História familiar",      "tipo":"estruturado","ordem":4},
    {"chave":"historia_social",      "titulo":"História social",        "tipo":"estruturado","ordem":5},
    {"chave":"exame_fisico",         "titulo":"Exame físico",           "tipo":"estruturado","ordem":6},
    {"chave":"exames_realizados",    "titulo":"Exames realizados",      "tipo":"estruturado","ordem":7},
    {"chave":"hipotese_diagnostica", "titulo":"Hipótese diagnóstica",   "tipo":"texto",       "ordem":8},
    {"chave":"procedimentos_indicados","titulo":"Procedimentos indicados","tipo":"estruturado","ordem":9},
    {"chave":"conduta",              "titulo":"Conduta",                "tipo":"texto_longo","ordem":10},
    {"chave":"medicacoes",           "titulo":"Medicações prescritas",  "tipo":"texto_longo","ordem":11}
]'::jsonb,
    atualizado_em = now()
WHERE eh_padrao_sistema = true
  AND nome = 'Consulta clínica geral';
