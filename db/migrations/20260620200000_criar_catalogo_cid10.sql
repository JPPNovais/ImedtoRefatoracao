-- Migration: criar_catalogo_cid10
-- Catálogo global de CID-10 para autocomplete em prontuários e pedidos de exame.
-- Sem estabelecimento_id — referência global, somente leitura na aplicação.

CREATE TABLE IF NOT EXISTS cid10 (
    codigo    varchar(10)  PRIMARY KEY,
    descricao text         NOT NULL,
    categoria varchar(10)
);

COMMENT ON TABLE  cid10           IS 'Catálogo global CID-10 (ref data). Sem tenant.';
COMMENT ON COLUMN cid10.codigo    IS 'Código CID-10 (ex: J00, E11.9).';
COMMENT ON COLUMN cid10.descricao IS 'Descrição completa da condição.';
COMMENT ON COLUMN cid10.categoria IS 'Capítulo/categoria do CID-10 (ex: J, E).';

CREATE INDEX IF NOT EXISTS ix_cid10_descricao ON cid10 (descricao);

-- ============================================================
-- Seed — ~80 CIDs reais comuns em clínica geral / consultório
-- ON CONFLICT (codigo) DO NOTHING garante idempotência
-- ============================================================

-- Capítulo I — Algumas doenças infecciosas e parasitárias (A00–B99)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('A09',   'Diarreia e gastroenterite de origem infecciosa presumível',  'A') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('B00.9', 'Infecção pelo vírus herpes simples, não especificada',       'B') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('B02.9', 'Herpes zóster sem complicações',                             'B') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('B34.9', 'Infecção viral, não especificada',                           'B') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('B37.9', 'Candidíase, não especificada',                               'B') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo II — Neoplasias (C00–D48)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('C34.9', 'Neoplasia maligna dos brônquios e dos pulmões, não especificada', 'C') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('C50.9', 'Neoplasia maligna da mama, não especificada',                    'C') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('D25.9', 'Leiomioma do útero, não especificado',                           'D') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('D50.9', 'Anemia por deficiência de ferro, não especificada',              'D') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo III — Doenças do sangue (D50–D89)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('D64.9', 'Anemia, não especificada',                                       'D') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo IV — Doenças endócrinas, nutricionais e metabólicas (E00–E90)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E03.9', 'Hipotireoidismo, não especificado',                              'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E05.9', 'Tireotoxicose, não especificada',                                'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E10.9', 'Diabetes mellitus tipo 1 sem complicações',                      'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E11',   'Diabetes mellitus tipo 2',                                       'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E11.9', 'Diabetes mellitus tipo 2 sem complicações',                      'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E14.9', 'Diabetes mellitus não especificado, sem complicações',           'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E55.9', 'Deficiência de vitamina D, não especificada',                    'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E66.9', 'Obesidade, não especificada',                                    'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E78',   'Distúrbios do metabolismo de lipoproteínas',                     'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E78.0', 'Hipercolesterolemia pura',                                       'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E78.1', 'Hipertrigliceridemia pura',                                      'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E78.5', 'Hiperlipidemia mista',                                           'E') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('E83.5', 'Distúrbios do metabolismo do cálcio',                            'E') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo V — Transtornos mentais e comportamentais (F00–F99)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F10.2', 'Transtornos mentais e comportamentais devidos ao uso de álcool — síndrome de dependência', 'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F32.0', 'Episódio depressivo leve',                                       'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F32.1', 'Episódio depressivo moderado',                                   'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F32.9', 'Episódio depressivo, não especificado',                          'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F33.0', 'Transtorno depressivo recorrente, episódio depressivo leve',     'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F40.1', 'Fobias sociais',                                                 'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F41.0', 'Transtorno de pânico',                                           'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F41.1', 'Transtorno de ansiedade generalizada',                           'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F43.1', 'Estado de estresse pós-traumático',                              'F') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('F51.0', 'Insônia não orgânica',                                           'F') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo IX — Doenças do aparelho circulatório (I00–I99)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('I10',   'Hipertensão essencial (primária)',                               'I') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('I20.9', 'Angina pectoris, não especificada',                              'I') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('I25.1', 'Doença aterosclerótica do coração',                              'I') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('I48',   'Flutter e fibrilação atrial',                                    'I') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('I50.9', 'Insuficiência cardíaca, não especificada',                       'I') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('I63.9', 'Infarto cerebral, não especificado',                             'I') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('I83.9', 'Varizes dos membros inferiores sem úlcera ou inflamação',        'I') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo X — Doenças do aparelho respiratório (J00–J99)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J00',   'Nasofaringite aguda (resfriado comum)',                          'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J01.9', 'Sinusite aguda, não especificada',                               'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J02.9', 'Faringite aguda, não especificada',                              'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J03.9', 'Amigdalite aguda, não especificada',                             'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J06.9', 'Infecção aguda das vias aéreas superiores, não especificada',    'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J18.9', 'Pneumonia não especificada',                                     'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J30.1', 'Rinite alérgica devida a pólen',                                 'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J30.4', 'Rinite alérgica, não especificada',                              'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J45.9', 'Asma, não especificada',                                         'J') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('J44.1', 'Doença pulmonar obstrutiva crônica com exacerbação aguda',       'J') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo XI — Doenças do aparelho digestivo (K00–K93)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K21.0', 'Doença de refluxo gastroesofágico com esofagite',                'K') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K21.9', 'Doença de refluxo gastroesofágico sem esofagite',                'K') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K25.9', 'Úlcera gástrica, não especificada',                              'K') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K29.5', 'Gastrite crônica, não especificada',                             'K') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K58.9', 'Síndrome do intestino irritável sem diarreia',                   'K') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K59.0', 'Constipação',                                                    'K') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K74.6', 'Cirrose hepática, não especificada',                             'K') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('K80.2', 'Cálculo da vesícula biliar sem colecistite',                     'K') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo XIII — Doenças do sistema osteomuscular (M00–M99)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M05.9', 'Artrite reumatoide soropositiva, não especificada',              'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M10.9', 'Gota, não especificada',                                         'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M17.9', 'Gonartrose, não especificada',                                   'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M19.9', 'Artrose, não especificada',                                      'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M47.9', 'Espondilose, não especificada',                                  'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M54.2', 'Cervicalgia',                                                    'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M54.4', 'Lumbago com ciática',                                            'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M54.5', 'Dor lombar baixa',                                               'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M75.1', 'Síndrome do manguito rotador',                                   'M') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('M81.9', 'Osteoporose, não especificada',                                  'M') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo XIV — Doenças do aparelho geniturinário (N00–N99)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('N18.9', 'Insuficiência renal crônica, não especificada',                  'N') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('N39.0', 'Infecção do trato urinário, local não especificado',             'N') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('N40',   'Hiperplasia da próstata',                                        'N') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('N41.1', 'Prostatite crônica',                                             'N') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('N92.0', 'Menstruação excessiva e frequente com ciclo regular',            'N') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('N95.1', 'Menopausa e climatério feminino',                                'N') ON CONFLICT (codigo) DO NOTHING;

-- Capítulo XVIII — Sintomas, sinais e achados (R00–R99)
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R05',   'Tosse',                                                          'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R06.0', 'Dispneia',                                                        'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R10.4', 'Outras dores abdominais e as não especificadas',                 'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R11',   'Náusea e vômitos',                                               'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R42',   'Tontura e instabilidade',                                        'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R51',   'Cefaleia',                                                        'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R53',   'Mal-estar e fadiga',                                             'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R63.0', 'Anorexia',                                                        'R') ON CONFLICT (codigo) DO NOTHING;
INSERT INTO cid10 (codigo, descricao, categoria) VALUES ('R73.0', 'Glicemia elevada',                                               'R') ON CONFLICT (codigo) DO NOTHING;
