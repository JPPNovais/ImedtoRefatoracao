-- ─────────────────────────────────────────────────────────────────────────────
-- Seed dos 5 modelos padrão do sistema de Termos de Consentimento.
-- Padrões têm estabelecimento_id = NULL (visíveis para todos os tenants) e
-- versao_atual = 1. Cada um tem uma linha em termo_modelo_versao (snapshot).
--
-- Idempotente — INSERT...WHERE NOT EXISTS por título (chave natural do seed).
-- Não cria via __ef_migrations_history porque é seed manual fora do EF.
-- ─────────────────────────────────────────────────────────────────────────────

-- 1. LGPD — Tratamento de dados pessoais (Art. 7º e 11)
INSERT INTO public.termo_modelo (
    estabelecimento_id, categoria, titulo, conteudo_html,
    ativo, versao_atual, criado_em
)
SELECT NULL, 'lgpd',
       'Consentimento LGPD — Tratamento de Dados Pessoais',
$$<h2>Termo de Consentimento para Tratamento de Dados Pessoais</h2>
<p>Eu, <strong>{{paciente.nome}}</strong>, portador(a) do CPF <strong>{{paciente.cpf}}</strong>, declaro estar ciente de que os meus dados pessoais e dados pessoais sensíveis (Art. 5º, II da LGPD) serão coletados e tratados por <strong>{{estabelecimento.nome}}</strong> (CNPJ {{estabelecimento.cnpj}}) com as seguintes finalidades:</p>
<ul>
  <li>Prestação de assistência à saúde, conforme Art. 11, II, "a" da LGPD;</li>
  <li>Cumprimento de obrigação legal e regulatória (CFM, ANS, ANVISA);</li>
  <li>Comunicação direta com o titular sobre agendamentos, retornos e tratamentos;</li>
  <li>Faturamento, cobrança e operações de convênio quando aplicável.</li>
</ul>
<p>Estou ciente dos meus direitos como titular: acesso, correção, anonimização, portabilidade, eliminação e revogação do consentimento (Art. 18 da LGPD), exercíveis por solicitação formal ao estabelecimento.</p>
<p>Os dados serão retidos pelo prazo mínimo legal de 20 anos (CFM Res. 1.821/2007) para prontuário, e por 5 anos para dados financeiros.</p>
<p><strong>{{cidade_atual}}</strong>, {{data_atual}}.</p>
<p>____________________________________________<br>
{{paciente.nome}}</p>$$,
       TRUE, 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.termo_modelo
    WHERE estabelecimento_id IS NULL AND titulo = 'Consentimento LGPD — Tratamento de Dados Pessoais'
);

-- 2. Uso de Imagem
INSERT INTO public.termo_modelo (
    estabelecimento_id, categoria, titulo, conteudo_html,
    ativo, versao_atual, criado_em
)
SELECT NULL, 'imagem',
       'Autorização de Uso de Imagem',
$$<h2>Autorização de Uso de Imagem</h2>
<p>Eu, <strong>{{paciente.nome}}</strong>, CPF <strong>{{paciente.cpf}}</strong>, autorizo <strong>{{estabelecimento.nome}}</strong> a captar, armazenar e utilizar imagens (fotografias e vídeos) relacionadas ao meu tratamento clínico, exclusivamente para as finalidades abaixo selecionadas:</p>
<ul>
  <li>Acompanhamento da evolução do tratamento (uso interno);</li>
  <li>Discussão de caso entre profissionais de saúde da equipe;</li>
  <li>Inclusão em prontuário eletrônico;</li>
  <li>Publicação em material científico, didático ou divulgação, somente se solicitado e expressamente autorizado em separado.</li>
</ul>
<p>As imagens preservarão, sempre que possível, minha identificação. Tenho ciência de que posso revogar esta autorização a qualquer momento, sem que isso afete o tratamento já realizado.</p>
<p><strong>{{cidade_atual}}</strong>, {{data_atual}}.</p>
<p>____________________________________________<br>
{{paciente.nome}}</p>$$,
       TRUE, 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.termo_modelo
    WHERE estabelecimento_id IS NULL AND titulo = 'Autorização de Uso de Imagem'
);

-- 3. Procedimento (Cirúrgico/Invasivo genérico)
INSERT INTO public.termo_modelo (
    estabelecimento_id, categoria, titulo, conteudo_html,
    ativo, versao_atual, criado_em
)
SELECT NULL, 'cirurgico',
       'Termo de Consentimento para Procedimento',
$$<h2>Termo de Consentimento Livre e Esclarecido para Procedimento</h2>
<p>Eu, <strong>{{paciente.nome}}</strong>, CPF <strong>{{paciente.cpf}}</strong>, declaro que fui informado(a) por <strong>{{profissional.nome}}</strong> ({{profissional.conselho_completo}}) sobre o procedimento a ser realizado em <strong>{{estabelecimento.nome}}</strong>.</p>
<p>Recebi explicações claras sobre:</p>
<ul>
  <li>A natureza, indicação e objetivos do procedimento;</li>
  <li>Os benefícios esperados e a probabilidade de sucesso;</li>
  <li>Os riscos, complicações e efeitos colaterais possíveis;</li>
  <li>As alternativas terapêuticas disponíveis e as consequências da não realização;</li>
  <li>Os cuidados pré e pós-procedimento.</li>
</ul>
<p>Tive oportunidade de fazer perguntas e todas as minhas dúvidas foram esclarecidas. Estou ciente de que a medicina não é ciência exata e que o resultado depende de fatores individuais. Autorizo a equipe a tomar as condutas necessárias diante de imprevistos, conforme as boas práticas médicas.</p>
<p><strong>{{cidade_atual}}</strong>, {{data_atual}}.</p>
<p>____________________________________________<br>
{{paciente.nome}}</p>
<p>____________________________________________<br>
{{profissional.nome}} — {{profissional.conselho_completo}}</p>$$,
       TRUE, 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.termo_modelo
    WHERE estabelecimento_id IS NULL AND titulo = 'Termo de Consentimento para Procedimento'
);

-- 4. Responsabilidade Financeira
INSERT INTO public.termo_modelo (
    estabelecimento_id, categoria, titulo, conteudo_html,
    ativo, versao_atual, criado_em
)
SELECT NULL, 'financeiro',
       'Termo de Responsabilidade Financeira',
$$<h2>Termo de Responsabilidade Financeira</h2>
<p>Eu, <strong>{{paciente.nome}}</strong>, CPF <strong>{{paciente.cpf}}</strong>, declaro estar ciente e de acordo com as condições financeiras dos serviços a serem prestados por <strong>{{estabelecimento.nome}}</strong> (CNPJ {{estabelecimento.cnpj}}).</p>
<p>Comprometo-me a:</p>
<ul>
  <li>Honrar os valores acordados nos prazos estabelecidos;</li>
  <li>Comunicar com no mínimo 24 horas de antecedência qualquer impossibilidade de comparecimento, sob pena de cobrança da consulta agendada;</li>
  <li>Arcar com diferenças não cobertas pelo convênio, materiais, medicamentos e procedimentos adicionais, quando aplicáveis;</li>
  <li>Apresentar comprovante de pagamento sempre que solicitado.</li>
</ul>
<p>Estou ciente de que a inadimplência sujeita o cadastro a sanções legais (negativação em órgãos de proteção ao crédito) após notificação prévia.</p>
<p><strong>{{cidade_atual}}</strong>, {{data_atual}}.</p>
<p>____________________________________________<br>
{{paciente.nome}}</p>$$,
       TRUE, 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.termo_modelo
    WHERE estabelecimento_id IS NULL AND titulo = 'Termo de Responsabilidade Financeira'
);

-- 5. Telemedicina
INSERT INTO public.termo_modelo (
    estabelecimento_id, categoria, titulo, conteudo_html,
    ativo, versao_atual, criado_em
)
SELECT NULL, 'telemedicina',
       'Consentimento para Atendimento por Telemedicina',
$$<h2>Termo de Consentimento para Atendimento por Telemedicina</h2>
<p>Eu, <strong>{{paciente.nome}}</strong>, CPF <strong>{{paciente.cpf}}</strong>, autorizo a realização de atendimento médico a distância (telemedicina) por <strong>{{profissional.nome}}</strong> ({{profissional.conselho_completo}}), conforme regulamentado pela Resolução CFM nº 2.314/2022.</p>
<p>Declaro ciência de que:</p>
<ul>
  <li>O atendimento por telemedicina é uma modalidade legítima e equivalente em valor diagnóstico ao presencial em diversas situações, podendo, contudo, exigir consulta presencial complementar;</li>
  <li>A consulta será gravada e os dados serão tratados conforme política de privacidade do estabelecimento (LGPD);</li>
  <li>Sou responsável por garantir, no momento da consulta, ambiente reservado, boa conexão e equipamentos adequados;</li>
  <li>Receitas, atestados e pedidos de exame poderão ser emitidos digitalmente, com validade legal mediante assinatura eletrônica.</li>
</ul>
<p><strong>{{cidade_atual}}</strong>, {{data_atual}}.</p>
<p>____________________________________________<br>
{{paciente.nome}}</p>$$,
       TRUE, 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.termo_modelo
    WHERE estabelecimento_id IS NULL AND titulo = 'Consentimento para Atendimento por Telemedicina'
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Snapshot v1 para cada padrão recém-criado. Idempotente: pula se já existir.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO public.termo_modelo_versao (termo_modelo_id, versao, conteudo_html, criado_em)
SELECT m.id, 1, m.conteudo_html, m.criado_em
FROM   public.termo_modelo m
WHERE  m.estabelecimento_id IS NULL
  AND  NOT EXISTS (
      SELECT 1 FROM public.termo_modelo_versao v
      WHERE  v.termo_modelo_id = m.id AND v.versao = 1
  );
