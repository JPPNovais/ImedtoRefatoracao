INSERT INTO public.tipo_sala_atendimento (nome, descricao) VALUES
    ('Consultório', 'Atendimento clínico individual'),
    ('Sala de Exames', 'Exames diagnósticos e procedimentos não-invasivos'),
    ('Sala de Procedimento', 'Procedimentos ambulatoriais de pequena cirurgia'),
    ('Sala de Vacina', 'Aplicação de vacinas e imunobiológicos'),
    ('Sala de Coleta', 'Coleta de material biológico'),
    ('Sala Cirúrgica', 'Cirurgias eletivas e de emergência')
ON CONFLICT (nome) DO NOTHING;
