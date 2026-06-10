// Imedto — Atendimentos & Prontuário data
// Fila do dia do profissional logado + histórico clínico modular.

window.IMEDTO_CARE = (() => {

  const today = '2026-05-12';
  const me = {
    id: 'me',
    name: 'Dra. Mariana Costa',
    role: 'Cardiologista',
    crm: 'CRM/SP 123.456',
    avatar: null,
  };

  // ─── Fila do dia ─────────────────────────────────────────────
  const queue = [
    {
      id: 'a1', time: '08:00', duration: 30, status: 'completed',
      finishedAt: '08:34',
      patient: {
        id: 'p1', name: 'Ana Clara Mendes', age: 42, sex: 'F',
        photo: null, conv: 'Bradesco Saúde', tags: ['VIP'],
        phone: '(11) 98765-4321',
      },
      reason: 'Retorno — Hipertensão',
      procedure: 'Consulta cardiológica',
      vitals: { pa: '128/82', fc: 76, peso: 68.2, temp: 36.5 },
      summary: 'Quadro estável. Manter losartana 50mg. Retorno em 90d.',
    },
    {
      id: 'a2', time: '08:30', duration: 40, status: 'in-progress',
      startedAt: '08:38',
      patient: {
        id: 'p2', name: 'Roberto Lima Silva', age: 58, sex: 'M',
        photo: null, conv: 'Particular', tags: ['Diabético', 'Hipertenso'],
        phone: '(11) 99654-3210',
      },
      reason: '1ª consulta — Dor torácica',
      procedure: 'Consulta cardiológica + ECG',
      template: 'Primeira consulta',
      alerts: ['Alergia a dipirona', 'Diabetes tipo 2'],
    },
    {
      id: 'a3', time: '09:30', duration: 30, status: 'waiting',
      arrivedAt: '09:18',
      patient: {
        id: 'p3', name: 'Júlia Pereira Costa', age: 29, sex: 'F',
        photo: null, conv: 'Amil', tags: ['Gestante 28sem'],
        phone: '(11) 91234-5678',
      },
      reason: 'Retorno — pré-natal cardiológico',
      procedure: 'Consulta cardiológica',
      alerts: ['Gestante — 28 semanas'],
    },
    {
      id: 'a4', time: '10:00', duration: 30, status: 'confirmed',
      patient: {
        id: 'p4', name: 'Eduardo Almeida', age: 51, sex: 'M',
        photo: null, conv: 'SulAmérica', tags: [],
        phone: '(11) 98899-7766',
      },
      reason: 'Avaliação pré-operatória',
      procedure: 'Avaliação cardiológica',
    },
    {
      id: 'a5', time: '10:30', duration: 30, status: 'confirmed',
      patient: {
        id: 'p5', name: 'Sofia Andrade Rocha', age: 36, sex: 'F',
        photo: null, conv: 'Particular', tags: [],
        phone: '(11) 97766-5544',
      },
      reason: 'Retorno — exames',
      procedure: 'Consulta cardiológica',
    },
    {
      id: 'a6', time: '11:00', duration: 30, status: 'unconfirmed',
      patient: {
        id: 'p6', name: 'Carlos Henrique Souza', age: 64, sex: 'M',
        photo: null, conv: 'Unimed', tags: ['Idoso'],
        phone: '(11) 96655-4433',
      },
      reason: '1ª consulta',
      procedure: 'Consulta cardiológica',
    },
    {
      id: 'a7', time: '14:00', duration: 60, status: 'confirmed',
      patient: {
        id: 'p7', name: 'Luana Martins Fernandes', age: 45, sex: 'F',
        photo: null, conv: 'Bradesco Saúde', tags: [],
        phone: '(11) 95544-3322',
      },
      reason: 'Holter 24h — colocação',
      procedure: 'Holter de 24 horas',
    },
    {
      id: 'a8', time: '15:00', duration: 30, status: 'confirmed',
      patient: {
        id: 'p8', name: 'Marcos Antônio Ribeiro', age: 39, sex: 'M',
        photo: null, conv: 'Particular', tags: [],
        phone: '(11) 94433-2211',
      },
      reason: 'Retorno',
      procedure: 'Consulta cardiológica',
    },
    {
      id: 'a9', time: '15:30', duration: 30, status: 'confirmed',
      patient: {
        id: 'p9', name: 'Beatriz Carvalho Lopes', age: 28, sex: 'F',
        photo: null, conv: 'Amil', tags: [],
        phone: '(11) 93322-1100',
      },
      reason: '1ª consulta',
      procedure: 'Consulta cardiológica',
    },
    {
      id: 'a10', time: '16:00', duration: 30, status: 'confirmed',
      patient: {
        id: 'p10', name: 'Felipe Oliveira', age: 47, sex: 'M',
        photo: null, conv: 'Particular', tags: ['Atleta'],
        phone: '(11) 92211-0099',
      },
      reason: 'Avaliação esportiva',
      procedure: 'Avaliação cardiológica',
    },
  ];

  // ─── Tipos de prontuário (templates de módulos) ─────────────
  const templates = [
    {
      id: 't1', name: 'Primeira consulta', icon: 'fa-stethoscope',
      description: 'Avaliação inicial completa com anamnese e exame físico',
      modules: ['qp', 'hda', 'hpp', 'familyHist', 'socialHist', 'vitals', 'physical', 'examsDone', 'cid', 'indicatedProc', 'conduct', 'prescription', 'exams'],
      uses: 124,
    },
    {
      id: 't2', name: 'Retorno', icon: 'fa-rotate',
      description: 'Acompanhamento curto, foco em evolução e ajuste de conduta',
      modules: ['vitals', 'soap', 'conduct', 'prescription'],
      uses: 287,
    },
    {
      id: 't3', name: 'Pré-operatório', icon: 'fa-notes-medical',
      description: 'Avaliação cardiológica pré-cirúrgica',
      modules: ['qp', 'hda', 'hpp', 'vitals', 'physical', 'exams', 'indicatedProc', 'cid', 'certificate'],
      uses: 42,
    },
    {
      id: 't4', name: 'Pós-operatório', icon: 'fa-bandage',
      description: 'Avaliação após procedimento, evolução e curativos',
      modules: ['vitals', 'physical', 'postOp', 'images', 'prescription', 'files'],
      uses: 18,
    },
    {
      id: 't5', name: 'Procedimento cirúrgico', icon: 'fa-user-doctor',
      description: 'Descrição cirúrgica, anestesia e equipe',
      modules: ['indicatedProc', 'surgTeam', 'anesthesia', 'surgDesc', 'postOp', 'prescription'],
      uses: 12,
    },
    {
      id: 't6', name: 'Procedimento em consultório', icon: 'fa-syringe',
      description: 'Procedimentos ambulatoriais (curativos, infiltrações, suturas)',
      modules: ['qp', 'vitals', 'inOfficeProc', 'images', 'conduct', 'prescription'],
      uses: 21,
    },
    {
      id: 't7', name: 'Procedimento estético', icon: 'fa-camera',
      description: 'Com foco em registro fotográfico antes/depois',
      modules: ['qp', 'hpp', 'inOfficeProc', 'images', 'conduct', 'certificate'],
      uses: 7,
    },
    {
      id: 't8', name: 'Teleconsulta', icon: 'fa-video',
      description: 'Atendimento remoto sem exame físico',
      modules: ['qp', 'hda', 'cid', 'conduct', 'prescription', 'certificate'],
      uses: 33,
    },
  ];

  // ─── Catálogo de módulos disponíveis ────────────────────────
  const moduleCatalog = {
    // História clínica (anamnese desmembrada)
    qp:          { id: 'qp', name: 'Queixa principal (QP)', icon: 'fa-comment-medical', desc: 'Motivo da consulta nas palavras do paciente', group: 'Anamnese' },
    hda:         { id: 'hda', name: 'História da doença atual (HDA)', icon: 'fa-clipboard-list', desc: 'Início, evolução e características da queixa', group: 'Anamnese' },
    hpp:         { id: 'hpp', name: 'História pregressa (HPP)', icon: 'fa-clock-rotate-left', desc: 'Doenças prévias, cirurgias, internações', group: 'Anamnese' },
    familyHist:  { id: 'familyHist', name: 'História familiar', icon: 'fa-people-roof', desc: 'Antecedentes em parentes de 1º grau', group: 'Anamnese' },
    socialHist:  { id: 'socialHist', name: 'História social e hábitos de vida', icon: 'fa-mug-hot', desc: 'Tabagismo, etilismo, atividade física, sono', group: 'Anamnese' },

    // Avaliação
    vitals:      { id: 'vitals', name: 'Sinais vitais', icon: 'fa-heart-pulse', desc: 'PA, FC, peso, temperatura, IMC', group: 'Avaliação' },
    physical:    { id: 'physical', name: 'Exame físico', icon: 'fa-person', desc: 'Boneco anatômico com regiões', group: 'Avaliação' },
    examsDone:   { id: 'examsDone', name: 'Exames realizados', icon: 'fa-vials', desc: 'Resultados de exames já feitos pelo paciente', group: 'Avaliação' },
    cid:         { id: 'cid', name: 'CID-10', icon: 'fa-disease', desc: 'Códigos de diagnóstico e hipóteses', group: 'Avaliação' },
    soap:        { id: 'soap', name: 'Evolução SOAP', icon: 'fa-pen-to-square', desc: 'Subjetivo · Objetivo · Avaliação · Plano', group: 'Avaliação' },

    // Conduta
    conduct:     { id: 'conduct', name: 'Conduta', icon: 'fa-route', desc: 'Plano terapêutico e orientações', group: 'Conduta' },
    prescription:{ id: 'prescription', name: 'Prescrição', icon: 'fa-prescription', desc: 'Medicamentos com dose e duração', group: 'Conduta' },
    exams:       { id: 'exams', name: 'Solicitação de exames', icon: 'fa-flask', desc: 'Exames a solicitar para o paciente', group: 'Conduta' },
    indicatedProc: { id: 'indicatedProc', name: 'Procedimentos indicados', icon: 'fa-list-check', desc: 'Procedimentos cirúrgicos ou ambulatoriais indicados', group: 'Conduta' },
    certificate: { id: 'certificate', name: 'Atestados', icon: 'fa-file-signature', desc: 'Atestado, declaração, relatório', group: 'Conduta' },

    // Cirúrgico
    inOfficeProc:{ id: 'inOfficeProc', name: 'Procedimento em consultório', icon: 'fa-syringe', desc: 'Procedimentos realizados durante a consulta', group: 'Cirúrgico' },
    surgDesc:    { id: 'surgDesc', name: 'Descrição cirúrgica', icon: 'fa-scissors', desc: 'Tempo cirúrgico, intercorrências, técnica', group: 'Cirúrgico' },
    anesthesia:  { id: 'anesthesia', name: 'Ficha anestésica', icon: 'fa-bed-pulse', desc: 'Tipo de anestesia, drogas, monitorização', group: 'Cirúrgico' },
    surgTeam:    { id: 'surgTeam', name: 'Equipe cirúrgica', icon: 'fa-user-doctor', desc: 'Cirurgião, auxiliares, anestesista, instrumentadora', group: 'Cirúrgico' },
    postOp:      { id: 'postOp', name: 'Evolução pós-operatória', icon: 'fa-bandage', desc: 'Evolução clínica do pós-operatório', group: 'Cirúrgico' },

    // Anexos
    images:      { id: 'images', name: 'Fotos do paciente', icon: 'fa-camera', desc: 'Fotos clínicas, comparativo antes/depois', group: 'Anexos' },
    files:       { id: 'files', name: 'Anexos', icon: 'fa-paperclip', desc: 'PDFs, laudos externos, documentos', group: 'Anexos' },
  };

  // ─── Paciente em foco para o prontuário ─────────────────────
  const focusPatient = {
    id: 'p2',
    name: 'Roberto Lima Silva',
    age: 58, sex: 'M', dob: '1967-09-14',
    cpf: '124.567.890-12',
    conv: 'Particular',
    phone: '(11) 99654-3210',
    email: 'roberto.lima@email.com',
    address: 'Rua das Flores, 245 — Vila Mariana, São Paulo',
    bloodType: 'O+',
    photo: null,
    allergies: ['Dipirona', 'AAS'],
    chronicConditions: ['Diabetes tipo 2', 'Hipertensão arterial'],
    currentMeds: [
      { name: 'Losartana', dose: '50mg', freq: '1x/dia (manhã)', since: 'ago/2025' },
      { name: 'Metformina', dose: '850mg', freq: '2x/dia (café e jantar)', since: 'fev/2024' },
      { name: 'Sinvastatina', dose: '20mg', freq: '1x/dia (noite)', since: 'nov/2025' },
    ],
    insurance: { name: 'Particular', plan: '—' },
  };

  // ─── Histórico (timeline) do paciente ───────────────────────
  const history = [
    {
      id: 'h0', date: '2026-05-12', time: '08:38', status: 'in-progress',
      template: 'Primeira consulta', professional: 'Dra. Mariana Costa',
      summary: 'Em atendimento agora',
    },
    {
      id: 'h1', date: '2026-02-18', time: '10:00', status: 'completed',
      template: 'Retorno', professional: 'Dra. Mariana Costa',
      summary: 'Controle pressórico bom. Ajustada metformina para 850mg 2x/dia.',
      cid: ['I10 — Hipertensão', 'E11 — Diabetes tipo 2'],
      attachments: 2,
    },
    {
      id: 'h2', date: '2025-11-08', time: '14:30', status: 'completed',
      template: 'Retorno', professional: 'Dra. Mariana Costa',
      summary: 'Solicitado holter 24h e perfil lipídico. Aumentada sinvastatina.',
      cid: ['I10'],
      attachments: 1,
    },
    {
      id: 'h3', date: '2025-08-22', time: '09:00', status: 'completed',
      template: 'Primeira consulta', professional: 'Dra. Mariana Costa',
      summary: 'Primeira consulta. Diagnóstico HAS estágio 2. Iniciado tratamento.',
      cid: ['I10'],
      attachments: 3,
    },
  ];

  // ─── Regiões anatômicas pré-mapeadas (para exame físico) ────
  const anatomyRegions = [
    { id: 'head',     label: 'Cabeça' },
    { id: 'face',     label: 'Face' },
    { id: 'neck',     label: 'Pescoço' },
    { id: 'r-shoulder', label: 'Ombro D' },
    { id: 'l-shoulder', label: 'Ombro E' },
    { id: 'chest',    label: 'Tórax' },
    { id: 'r-arm',    label: 'Braço D' },
    { id: 'l-arm',    label: 'Braço E' },
    { id: 'abdomen',  label: 'Abdome' },
    { id: 'r-hand',   label: 'Mão D' },
    { id: 'l-hand',   label: 'Mão E' },
    { id: 'pelvis',   label: 'Pelve' },
    { id: 'r-thigh',  label: 'Coxa D' },
    { id: 'l-thigh',  label: 'Coxa E' },
    { id: 'r-knee',   label: 'Joelho D' },
    { id: 'l-knee',   label: 'Joelho E' },
    { id: 'r-leg',    label: 'Perna D' },
    { id: 'l-leg',    label: 'Perna E' },
    { id: 'r-foot',   label: 'Pé D' },
    { id: 'l-foot',   label: 'Pé E' },
    // costas
    { id: 'b-head',   label: 'Nuca' },
    { id: 'b-upper',  label: 'Dorso superior' },
    { id: 'b-lower',  label: 'Lombar' },
    { id: 'b-glutes', label: 'Glúteos' },
  ];

  // ─── Catálogo de procedimentos do estabelecimento ativo ─────
  // Valores em centavos. "Criar procedimento" no prontuário adiciona aqui.
  const procedureCatalog = [
    { id: 'pc1', name: 'Infiltração articular', price: 80000, duration: 30 },
    { id: 'pc2', name: 'Artroscopia de joelho', price: 950000, duration: 90 },
    { id: 'pc3', name: 'Drenagem de abscesso', price: 45000, duration: 20 },
    { id: 'pc4', name: 'Sutura simples', price: 25000, duration: 15 },
    { id: 'pc5', name: 'Biópsia de pele', price: 60000, duration: 25 },
    { id: 'pc6', name: 'Cauterização de lesão', price: 35000, duration: 15 },
    { id: 'pc7', name: 'Mapa ambulatorial de PA (MAPA 24h)', price: 38000, duration: 30 },
  ];

  return { today, me, queue, templates, moduleCatalog, focusPatient, history, anatomyRegions, procedureCatalog };
})();
