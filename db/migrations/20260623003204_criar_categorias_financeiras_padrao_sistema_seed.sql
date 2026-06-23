-- Seed idempotente: categorias financeiras padrão da plataforma (briefing 2026-06-22_003 M1 / R1).
-- Nomes limpos (sem prefixo "Receita:/Despesa:"), escopo global, ativo=true.
-- Deve ser aplicado APÓS 20260623003204_criar_categorias_financeiras_padrao_sistema.sql.
--
-- Idempotência: ON CONFLICT (nome, tipo) DO NOTHING — reexecutar não duplica registros.
-- Multi-tenant: tabela sem estabelecimento_id; nunca retornada por queries de tenant.
-- LGPD: não é PII. Sem audit de paciente.

-- Receitas (7)
INSERT INTO public.categorias_financeiras_padrao_sistema (nome, tipo, ativo, criada_em)
VALUES
    ('Consultas',             'Receita', true, NOW()),
    ('Procedimentos',         'Receita', true, NOW()),
    ('Exames',                'Receita', true, NOW()),
    ('Cirurgias',             'Receita', true, NOW()),
    ('Repasses de convênio',  'Receita', true, NOW()),
    ('Venda de produtos',     'Receita', true, NOW()),
    ('Outras receitas',       'Receita', true, NOW())
ON CONFLICT (nome, tipo) DO NOTHING;

-- Despesas (12)
INSERT INTO public.categorias_financeiras_padrao_sistema (nome, tipo, ativo, criada_em)
VALUES
    ('Folha de pagamento',    'Despesa', true, NOW()),
    ('Pró-labore',            'Despesa', true, NOW()),
    ('Aluguel',               'Despesa', true, NOW()),
    ('Contas de consumo',     'Despesa', true, NOW()),
    ('Insumos e materiais',   'Despesa', true, NOW()),
    ('Equipamentos',          'Despesa', true, NOW()),
    ('Marketing',             'Despesa', true, NOW()),
    ('Impostos e taxas',      'Despesa', true, NOW()),
    ('Manutenção',            'Despesa', true, NOW()),
    ('Limpeza',               'Despesa', true, NOW()),
    ('Software/assinaturas',  'Despesa', true, NOW()),
    ('Outras despesas',       'Despesa', true, NOW())
ON CONFLICT (nome, tipo) DO NOTHING;
