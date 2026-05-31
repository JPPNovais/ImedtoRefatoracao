-- Seeds Wave 2 — imedto_config (8 chaves default)
-- Timestamp: 20260530131406
--
-- Todos os INSERTs são idempotentes: ON CONFLICT DO NOTHING.
-- imedto_config: ON CONFLICT (chave) DO NOTHING.
-- Sem BEGIN/COMMIT — pipeline gerencia transação.
--
-- NOTA (Wave 4 — 2026-05-30): os seeds das tabelas imedto_modelo_prontuario_global,
-- imedto_variavel_pool_global e imedto_regiao_anatomica_global foram REMOVIDOS deste
-- arquivo porque essas tabelas foram dropadas em 20260530200000_drop_catalogos_globais_wave2.sql
-- (modelo migrou para live-link via EhPadraoSistema nas tabelas legado). Mantê-los
-- quebrava o pipeline: a pipeline reconcatena todos os .sql a cada deploy, e um
-- INSERT em tabela inexistente aborta o migrate. imedto_config permanece (tabela viva).

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
