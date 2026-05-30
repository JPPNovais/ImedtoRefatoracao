-- Seeds Wave 2 — imedto_config (8 chaves default) + catálogos globais
-- Timestamp: 20260530131406
--
-- Todos os INSERTs são idempotentes: ON CONFLICT DO NOTHING.
-- imedto_config: ON CONFLICT (chave) DO NOTHING.
-- catálogos: ON CONFLICT (id) DO NOTHING.
-- Sem BEGIN/COMMIT — pipeline gerencia transação.

-- ── Seeds imedto_config (8 chaves default) ────────────────────────────────────
-- Valor armazenado como JSONB: números sem aspas, strings com aspas, booleans sem aspas.
-- Campos novos (tipo, secao) preenchidos para todas as chaves.

INSERT INTO public.imedto_config (chave, valor, tipo, secao, descricao, atualizado_em, atualizado_por_admin_id)
VALUES
    ('trial.dias_padrao',              '14',                 'numerico', 'Trial',         'Dias do trial inicial ao criar novo estabelecimento',           NOW(), NULL),
    ('trial.limite_profissionais',     '5',                  'numerico', 'Trial',         'Máximo de profissionais permitidos durante o trial',            NOW(), NULL),
    ('assinatura.dias_aviso_expiracao','7',                  'numerico', 'Assinatura',    'Dias antes da expiração para iniciar avisos ao estabelecimento', NOW(), NULL),
    ('sistema.email_suporte',          '"suporte@imedto.com.br"', 'email', 'Sistema',    'E-mail exibido para usuários em mensagens de erro ou suporte',   NOW(), NULL),
    ('feature_flags.exemplo',          'false',              'toggle',   'Feature Flags', 'Flag de exemplo para validar pipeline de feature flags',         NOW(), NULL),
    ('comunicacao.smtp_remetente',     '"noreply@imedto.com.br"', 'email', 'Comunicação','Remetente padrão de e-mails transacionais',                      NOW(), NULL),
    ('comunicacao.from_padrao',        '"Imedto"',           'texto',    'Comunicação',   'Nome de exibição (from) em e-mails enviados pelo sistema',       NOW(), NULL),
    ('seguranca.tempo_sessao_admin_min','15',                'numerico', 'Segurança',     'Minutos de inatividade até logout automático do admin',          NOW(), NULL)
ON CONFLICT (chave) DO NOTHING;

-- ── Seed imedto_modelo_prontuario_global (1 modelo de exemplo) ────────────────

INSERT INTO public.imedto_modelo_prontuario_global
    (id, nome, descricao, conteudo_json, ativo, criado_em, atualizado_em, criado_por_admin_id, atualizado_por_admin_id)
VALUES (
    'a1b2c3d4-0001-0000-0000-000000000001',
    'Consulta Geral — Padrão Imedto',
    'Modelo base de consulta ambulatorial para uso geral. Pode ser importado e personalizado por cada clínica.',
    '{
        "versao": 1,
        "secoes": [
            {
                "id": "anamnese",
                "titulo": "Anamnese",
                "campos": [
                    { "id": "queixa_principal", "tipo": "texto_longo", "rotulo": "Queixa principal", "obrigatorio": true },
                    { "id": "historia_doenca_atual", "tipo": "texto_longo", "rotulo": "História da doença atual", "obrigatorio": false },
                    { "id": "antecedentes_pessoais", "tipo": "texto_longo", "rotulo": "Antecedentes pessoais", "obrigatorio": false },
                    { "id": "medicamentos_em_uso", "tipo": "texto_longo", "rotulo": "Medicamentos em uso", "obrigatorio": false },
                    { "id": "alergias", "tipo": "texto_curto", "rotulo": "Alergias", "obrigatorio": false }
                ]
            },
            {
                "id": "exame_fisico",
                "titulo": "Exame físico",
                "campos": [
                    { "id": "pa", "tipo": "texto_curto", "rotulo": "Pressão arterial (mmHg)", "obrigatorio": false },
                    { "id": "fc", "tipo": "numero", "rotulo": "Frequência cardíaca (bpm)", "obrigatorio": false },
                    { "id": "peso", "tipo": "numero", "rotulo": "Peso (kg)", "obrigatorio": false },
                    { "id": "altura", "tipo": "numero", "rotulo": "Altura (cm)", "obrigatorio": false },
                    { "id": "exame_geral", "tipo": "texto_longo", "rotulo": "Exame físico geral", "obrigatorio": false }
                ]
            },
            {
                "id": "conduta",
                "titulo": "Conduta",
                "campos": [
                    { "id": "hipotese_diagnostica", "tipo": "texto_longo", "rotulo": "Hipótese diagnóstica", "obrigatorio": false },
                    { "id": "plano_terapeutico", "tipo": "texto_longo", "rotulo": "Plano terapêutico", "obrigatorio": false },
                    { "id": "retorno", "tipo": "texto_curto", "rotulo": "Retorno em", "obrigatorio": false }
                ]
            }
        ]
    }',
    true,
    NOW(),
    NOW(),
    NULL,
    NULL
)
ON CONFLICT (id) DO NOTHING;

-- ── Seed imedto_variavel_pool_global (3 variáveis de exemplo) ─────────────────

INSERT INTO public.imedto_variavel_pool_global
    (id, nome, tipo, valores_json, descricao, ativo, criado_em, atualizado_em, criado_por_admin_id, atualizado_por_admin_id)
VALUES
    (
        'b2c3d4e5-0001-0000-0000-000000000001',
        'Pressão Arterial',
        'lista',
        '["Normal", "Pré-hipertensão", "Hipertensão Estágio 1", "Hipertensão Estágio 2", "Crise hipertensiva"]',
        'Classificação da pressão arterial conforme diretrizes da SBC',
        true, NOW(), NOW(), NULL, NULL
    ),
    (
        'b2c3d4e5-0002-0000-0000-000000000002',
        'Peso (kg)',
        'numerico',
        '[]',
        'Peso corporal em quilogramas, medido em balança',
        true, NOW(), NOW(), NULL, NULL
    ),
    (
        'b2c3d4e5-0003-0000-0000-000000000003',
        'Observações',
        'texto',
        '[]',
        'Campo livre para observações clínicas gerais',
        true, NOW(), NOW(), NULL, NULL
    )
ON CONFLICT (id) DO NOTHING;

-- ── Seed imedto_regiao_anatomica_global (~15 regiões básicas) ─────────────────

INSERT INTO public.imedto_regiao_anatomica_global
    (id, nome, sinonimos, sistema_corporal, ativo, criado_em, atualizado_em)
VALUES
    ('c3d4e5f6-0001-0000-0000-000000000001', 'Cabeça',             ARRAY['crânio','cefálico'],                'neurologico',        true, NOW(), NOW()),
    ('c3d4e5f6-0002-0000-0000-000000000002', 'Pescoço',            ARRAY['cervical','região cervical'],       'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0003-0000-0000-000000000003', 'Tórax anterior',     ARRAY['tórax','peito'],                   'cardiovascular',     true, NOW(), NOW()),
    ('c3d4e5f6-0004-0000-0000-000000000004', 'Tórax posterior',    ARRAY['dorso','costas superiores'],       'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0005-0000-0000-000000000005', 'Abdômen',            ARRAY['abdome','barriga'],                 'geral',              true, NOW(), NOW()),
    ('c3d4e5f6-0006-0000-0000-000000000006', 'Membro superior direito', ARRAY['MS direito','braço direito'], 'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0007-0000-0000-000000000007', 'Membro superior esquerdo', ARRAY['MS esquerdo','braço esquerdo'], 'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0008-0000-0000-000000000008', 'Membro inferior direito', ARRAY['MI direito','perna direita'], 'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0009-0000-0000-000000000009', 'Membro inferior esquerdo', ARRAY['MI esquerdo','perna esquerda'], 'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0010-0000-0000-000000000010', 'Coluna cervical',    ARRAY['cervical','pescoço posterior'],    'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0011-0000-0000-000000000011', 'Coluna torácica',    ARRAY['coluna dorsal','dorso médio'],     'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0012-0000-0000-000000000012', 'Coluna lombar',      ARRAY['lombar','região lombar','L1-L5'],  'musculoesqueletico', true, NOW(), NOW()),
    ('c3d4e5f6-0013-0000-0000-000000000013', 'Pele e tegumento',   ARRAY['tegumentar','pele','derme'],       'tegumentar',         true, NOW(), NOW()),
    ('c3d4e5f6-0014-0000-0000-000000000014', 'Região cardiovascular', ARRAY['coração','cardíaco','cardiovascular'], 'cardiovascular', true, NOW(), NOW()),
    ('c3d4e5f6-0015-0000-0000-000000000015', 'Sistema neurológico',ARRAY['neurológico','SNC','sistema nervoso'], 'neurologico',    true, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;
