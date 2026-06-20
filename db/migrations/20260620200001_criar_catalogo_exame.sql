-- Migration: criar_catalogo_exame
-- Catálogo global de exames para autocomplete em pedidos e prontuários.
-- Sem estabelecimento_id — referência global, somente leitura na aplicação.

CREATE TABLE IF NOT EXISTS exame_catalogo (
    id   bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome varchar(200) NOT NULL,
    tipo varchar(30),
    ativo boolean NOT NULL DEFAULT true
);

COMMENT ON TABLE  exame_catalogo      IS 'Catálogo global de exames (ref data). Sem tenant.';
COMMENT ON COLUMN exame_catalogo.tipo IS 'Laboratorial | Imagem | Outro';

-- Índice parcial para busca em ativos (suporta ORDER BY nome + WHERE ativo=true)
CREATE INDEX IF NOT EXISTS ix_exame_catalogo_nome ON exame_catalogo (nome) WHERE ativo = true;

-- Evita duplicatas case-insensitive no seed (e em futuras inserções via admin)
CREATE UNIQUE INDEX IF NOT EXISTS uq_exame_catalogo_nome ON exame_catalogo (lower(nome));

-- ============================================================
-- Seed — exames reais comuns (idempotente via WHERE NOT EXISTS)
-- ============================================================

-- Laboratoriais
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ácido fólico',                         'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ácido fólico');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Albumina',                              'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'albumina');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Amilase',                               'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'amilase');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ácido úrico',                           'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ácido úrico');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Beta-HCG quantitativo',                 'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'beta-hcg quantitativo');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Bilirrubinas totais e frações',         'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'bilirrubinas totais e frações');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'CA-125',                                'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ca-125');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Cálcio ionizado',                       'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'cálcio ionizado');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'CEA',                                   'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'cea');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Colesterol total',                      'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'colesterol total');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Creatinina',                            'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'creatinina');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'EAS (Urina tipo I)',                    'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'eas (urina tipo i)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ferritina',                             'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ferritina');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ferro sérico',                          'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ferro sérico');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Fosfatase alcalina',                    'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'fosfatase alcalina');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Fósforo',                               'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'fósforo');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Gama GT',                               'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'gama gt');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Glicemia de jejum',                     'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'glicemia de jejum');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'HbA1c (Hemoglobina glicada)',            'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'hba1c (hemoglobina glicada)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'HDL colesterol',                        'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'hdl colesterol');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Hemograma completo',                    'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'hemograma completo');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Hemocultura',                           'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'hemocultura');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Lactato desidrogenase (LDH)',           'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'lactato desidrogenase (ldh)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'LDL colesterol',                        'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ldl colesterol');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Lipase',                                'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'lipase');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Magnésio',                              'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'magnésio');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'PCR (Proteína C reativa)',              'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'pcr (proteína c reativa)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Potássio',                              'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'potássio');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Proteínas totais',                      'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'proteínas totais');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'PSA total',                             'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'psa total');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Sódio',                                 'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'sódio');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'T4 livre',                              'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 't4 livre');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'TGO (AST)',                             'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'tgo (ast)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'TGP (ALT)',                             'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'tgp (alt)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Triglicerídeos',                        'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'triglicerídeos');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'TSH',                                   'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'tsh');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ureia',                                 'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ureia');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Urocultura',                            'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'urocultura');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'VHS (Velocidade de hemossedimentação)', 'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'vhs (velocidade de hemossedimentação)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Vitamina B12',                          'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'vitamina b12');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Vitamina D (25-OH)',                    'Laboratorial' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'vitamina d (25-oh)');

-- Imagem
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Densitometria óssea',                          'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'densitometria óssea');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Mamografia bilateral',                          'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'mamografia bilateral');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Raio-X de coluna lombar',                       'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'raio-x de coluna lombar');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Raio-X de tórax',                               'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'raio-x de tórax');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ressonância magnética de coluna lombar',         'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ressonância magnética de coluna lombar');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Tomografia computadorizada de crânio',          'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'tomografia computadorizada de crânio');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Tomografia computadorizada de tórax',           'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'tomografia computadorizada de tórax');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ultrassonografia abdominal total',              'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ultrassonografia abdominal total');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ultrassonografia de tireoide',                  'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ultrassonografia de tireoide');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ultrassonografia pélvica',                      'Imagem' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ultrassonografia pélvica');

-- Outros
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Colonoscopia',                  'Outro' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'colonoscopia');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Ecocardiograma transtorácico',  'Outro' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'ecocardiograma transtorácico');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Eletrocardiograma (ECG)',        'Outro' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'eletrocardiograma (ecg)');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Endoscopia digestiva alta',      'Outro' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'endoscopia digestiva alta');
INSERT INTO exame_catalogo (nome, tipo) SELECT 'Espirometria',                   'Outro' WHERE NOT EXISTS (SELECT 1 FROM exame_catalogo WHERE lower(nome) = 'espirometria');
