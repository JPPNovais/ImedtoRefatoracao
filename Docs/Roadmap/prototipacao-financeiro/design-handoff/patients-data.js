// Imedto — Patients data
// Mock data para Pacientes (lista) e Detalhe do paciente.

window.IMEDTO_PATIENTS = (() => {

  // ─── Tags reutilizáveis ────────────────────────────────
  const TAGS = {
    'vip':       { label: 'VIP', color: 'hsl(45 96% 50%)', icon: 'fa-star' },
    'gestante':  { label: 'Gestante', color: 'hsl(340 60% 55%)', icon: 'fa-baby' },
    'alergia':   { label: 'Alergia grave', color: 'hsl(var(--error))', icon: 'fa-triangle-exclamation' },
    'cronico':   { label: 'Crônico', color: 'hsl(280 50% 50%)', icon: 'fa-heart-pulse' },
    'idoso':     { label: 'Idoso', color: 'hsl(220 50% 50%)', icon: 'fa-person-cane' },
    'novo':      { label: 'Novo paciente', color: 'hsl(var(--success))', icon: 'fa-seedling' },
    'recorrente':{ label: 'Recorrente', color: 'hsl(var(--info))', icon: 'fa-rotate' },
    'inativo':   { label: 'Inativo', color: 'hsl(var(--secondary) / 0.5)', icon: 'fa-circle-pause' },
  };

  // Status de orçamento
  const BUDGET_STATUS = {
    'draft':     { label: 'Rascunho', color: 'hsl(var(--secondary) / 0.55)', pill: 'pill-muted' },
    'sent':      { label: 'Enviado', color: 'hsl(var(--info))', pill: 'pill-info' },
    'accepted':  { label: 'Aceito', color: 'hsl(var(--success))', pill: 'pill-success' },
    'partial':   { label: 'Parcialmente aceito', color: 'hsl(45 95% 45%)', pill: 'pill-warning' },
    'rejected':  { label: 'Recusado', color: 'hsl(var(--error))', pill: 'pill-error' },
    'expired':   { label: 'Vencido', color: 'hsl(var(--secondary) / 0.5)', pill: 'pill-muted' },
    'completed': { label: 'Concluído', color: 'hsl(190 70% 40%)', pill: 'pill-info' },
    'canceled':  { label: 'Cancelado', color: 'hsl(var(--secondary) / 0.4)', pill: 'pill-muted' },
  };

  // ─── Pacientes (lista resumida) ────────────────────────
  const PATIENTS = [
    {
      id: 1, name: 'Marina Souza Oliveira', initials: 'MS',
      avatarColor: 'hsl(340 60% 55%)',
      birth: '1989-03-12', age: 36, gender: 'F',
      cpf: '345.678.901-22', phone: '(11) 99887-1122',
      email: 'marina.souza@gmail.com',
      tags: ['gestante', 'recorrente'],
      alerts: ['Alergia a dipirona', 'Gestante — 24 semanas'],
      lastVisit: '2026-04-22', nextAppointment: '2026-05-08T14:30',
      balance: 0, totalVisits: 14, doctor: 'Dra. Renata Lopes',
      insurance: 'Bradesco Saúde · Top Nacional',
    },
    {
      id: 2, name: 'Pedro Henrique Costa', initials: 'PH',
      avatarColor: 'hsl(220 55% 50%)',
      birth: '1958-11-30', age: 67, gender: 'M',
      cpf: '123.456.789-00', phone: '(11) 98765-4321',
      email: 'pedro.henrique@hotmail.com',
      tags: ['idoso', 'cronico', 'vip'],
      alerts: ['Hipertenso', 'Diabético tipo 2', 'Marcapasso desde 2019'],
      lastVisit: '2026-04-30', nextAppointment: '2026-05-15T09:00',
      balance: 12900.00, totalVisits: 38, doctor: 'Dra. Renata Lopes',
      insurance: 'Particular',
    },
    {
      id: 3, name: 'Beatriz Lima Ferreira', initials: 'BL',
      avatarColor: 'hsl(280 55% 55%)',
      birth: '1995-07-18', age: 30, gender: 'F',
      cpf: '987.654.321-11', phone: '(11) 97654-3210',
      email: 'beatriz.lima@outlook.com',
      tags: ['recorrente'],
      alerts: [],
      lastVisit: '2026-04-12', nextAppointment: '2026-05-12T10:00',
      balance: 0, totalVisits: 6, doctor: 'Dra. Camila Reis',
      insurance: 'Amil · Linha 400',
    },
    {
      id: 4, name: 'Ricardo Nunes Almeida', initials: 'RN',
      avatarColor: 'hsl(170 50% 40%)',
      birth: '1972-09-05', age: 53, gender: 'M',
      cpf: '456.789.012-33', phone: '(11) 96543-2109',
      email: 'ricardo.nunes@gmail.com',
      tags: ['cronico'],
      alerts: ['Diabético tipo 2', 'Histórico de IAM em 2022'],
      lastVisit: '2026-03-15', nextAppointment: null,
      balance: 480.00, totalVisits: 22, doctor: 'Dr. Marcos Rocha',
      insurance: 'SulAmérica · Especial 200',
    },
    {
      id: 5, name: 'Ana Paula Rodrigues', initials: 'AP',
      avatarColor: 'hsl(var(--primary))',
      birth: '2001-01-22', age: 25, gender: 'F',
      cpf: '654.321.098-77', phone: '(11) 95432-1098',
      email: 'ana.paula@gmail.com',
      tags: ['novo'],
      alerts: [],
      lastVisit: null, nextAppointment: '2026-05-09T16:00',
      balance: 0, totalVisits: 0, doctor: '—',
      insurance: 'Particular',
    },
    {
      id: 6, name: 'Carlos Eduardo Mendes', initials: 'CE',
      avatarColor: 'hsl(40 80% 50%)',
      birth: '1980-05-14', age: 45, gender: 'M',
      cpf: '789.012.345-66', phone: '(11) 94321-0987',
      email: 'carlos.mendes@hotmail.com',
      tags: ['vip', 'recorrente'],
      alerts: ['Alergia a penicilina'],
      lastVisit: '2026-04-28', nextAppointment: '2026-06-02T11:00',
      balance: 0, totalVisits: 11, doctor: 'Dra. Renata Lopes',
      insurance: 'Particular',
    },
    {
      id: 7, name: 'Helena Oliveira Santos', initials: 'HO',
      avatarColor: 'hsl(190 60% 45%)',
      birth: '1945-12-08', age: 80, gender: 'F',
      cpf: '012.345.678-99', phone: '(11) 93210-9876',
      email: '—',
      tags: ['idoso', 'cronico', 'vip'],
      alerts: ['Insuficiência cardíaca', 'Anticoagulada (Marevan)', 'Risco de queda'],
      lastVisit: '2026-04-29', nextAppointment: '2026-05-13T08:30',
      balance: 0, totalVisits: 47, doctor: 'Dra. Renata Lopes',
      insurance: 'Bradesco Saúde · Top Nacional',
    },
    {
      id: 8, name: 'João Vítor Pereira', initials: 'JV',
      avatarColor: 'hsl(140 45% 45%)',
      birth: '2018-06-25', age: 7, gender: 'M',
      cpf: '321.098.765-44', phone: '(11) 92109-8765',
      email: 'natalia.pereira@gmail.com',
      tags: ['novo'],
      alerts: ['Pediatria — responsável: Natália Pereira (mãe)'],
      lastVisit: '2026-04-18', nextAppointment: null,
      balance: 0, totalVisits: 1, doctor: 'Dra. Camila Reis',
      insurance: 'Unimed · Plano Família',
    },
    {
      id: 9, name: 'Fernanda Castro Dias', initials: 'FC',
      avatarColor: 'hsl(0 60% 55%)',
      birth: '1992-08-03', age: 33, gender: 'F',
      cpf: '210.987.654-22', phone: '(11) 91098-7654',
      email: 'fernanda.castro@outlook.com',
      tags: ['alergia'],
      alerts: ['Alergia grave a frutos do mar (anafilaxia 2023)'],
      lastVisit: '2026-02-10', nextAppointment: null,
      balance: 320.00, totalVisits: 4, doctor: 'Dr. Marcos Rocha',
      insurance: 'Particular',
    },
    {
      id: 10, name: 'Tiago Almeida Cruz', initials: 'TA',
      avatarColor: 'hsl(254 50% 45%)',
      birth: '1987-02-19', age: 38, gender: 'M',
      cpf: '098.765.432-11', phone: '(11) 90987-6543',
      email: 'tiago.cruz@gmail.com',
      tags: ['inativo'],
      alerts: [],
      lastVisit: '2024-08-15', nextAppointment: null,
      balance: 0, totalVisits: 3, doctor: 'Dra. Renata Lopes',
      insurance: 'Particular',
    },
  ];

  // ─── Detalhes para o paciente principal (ID 2 — Pedro Henrique) ───
  const PATIENT_DETAIL = {
    patientId: 2,

    // Anamnese
    anamnesis: {
      bloodType: 'O+',
      weight: '78 kg',
      height: '1,72 m',
      bmi: '26.4 (sobrepeso)',
      smoker: 'Ex-tabagista (parou em 2018, 30 maços/ano)',
      alcohol: 'Social — 2x/semana',
      activity: 'Caminhada 30 min, 3x/semana',
      familyHistory: 'Pai: IAM aos 62. Mãe: AVC aos 70. Irmão: HAS.',
      surgeries: ['Colecistectomia (2010)', 'Implante de marcapasso (2019)'],
      meds: [
        'Losartana 50mg — 1x/dia (manhã)',
        'Metformina 850mg — 2x/dia (após refeições)',
        'AAS 100mg — 1x/dia',
        'Atorvastatina 20mg — 1x/dia (noite)',
      ],
      allergies: ['Dipirona — broncoespasmo (2015)'],
    },

    // Timeline de atendimentos
    encounters: [
      {
        id: 'enc-5', date: '2026-04-30', time: '15:30',
        type: 'consulta', typeLabel: 'Consulta de retorno',
        doctor: 'Dra. Renata Lopes', specialty: 'Cardiologia',
        complaint: 'Retorno trimestral. Refere boa adesão à medicação. Nega dor torácica, dispneia ou palpitações.',
        anamnesis: 'PA aferida em casa: média 130x80 mmHg. Glicemia capilar média: 118 mg/dL em jejum. Caminhadas 3x/semana mantidas.',
        exam: 'BEG, corado, hidratado. AC: BRNF 2T sem sopros, FC 68 bpm, ritmo regular. AP: MV+ bilateral sem RA. MMII sem edemas.',
        cid: 'I10 — Hipertensão essencial · E11.9 — Diabetes mellitus tipo 2',
        plan: 'Manter medicações em curso. Solicitar exames de rotina (hemograma, lipidograma, HbA1c, função renal). Retorno em 3 meses ou se intercorrência.',
        prescriptions: ['Losartana 50mg', 'Metformina 850mg', 'AAS 100mg', 'Atorvastatina 20mg'],
        examsRequested: ['Hemograma completo', 'Lipidograma', 'HbA1c', 'Ureia + creatinina', 'EAS'],
        attachments: [],
        signed: true,
      },
      {
        id: 'enc-4', date: '2026-01-28', time: '10:00',
        type: 'consulta', typeLabel: 'Consulta de retorno',
        doctor: 'Dra. Renata Lopes', specialty: 'Cardiologia',
        complaint: 'Retorno trimestral. Refere episódio único de cefaleia em fevereiro.',
        anamnesis: 'PA domiciliar média 138x86. Glicemia jejum 126 mg/dL. Aderente à dieta, peso estável.',
        exam: 'BEG. AC: BRNF 2T sem sopros. PA consultório 142x90. FC 72 bpm.',
        cid: 'I10 — Hipertensão essencial · E11.9 — Diabetes mellitus tipo 2',
        plan: 'Aumentar Losartana para 100mg/dia. Reforçar dieta hipossódica. Retorno em 90 dias com novos exames.',
        prescriptions: ['Losartana 100mg', 'Metformina 850mg', 'AAS 100mg', 'Atorvastatina 20mg'],
        examsRequested: ['Holter 24h', 'MAPA 24h'],
        attachments: ['ECG_2026_01_28.pdf'],
        signed: true,
      },
      {
        id: 'enc-3', date: '2025-10-12', time: '09:45',
        type: 'procedimento', typeLabel: 'Procedimento — Ecocardiograma',
        doctor: 'Dr. Marcos Rocha', specialty: 'Cardiologia',
        complaint: 'Avaliação de rotina pós-implante de marcapasso.',
        anamnesis: 'Paciente assintomático. Marcapasso funcionante há 6 anos.',
        exam: 'Ecocardiograma transtorácico. FE preservada (62%). Sem alterações segmentares. Marcapasso bem posicionado.',
        cid: 'Z95.0 — Presença de marcapasso cardíaco',
        plan: 'Resultado de ecocardiograma normal. Manter seguimento com cardiologista assistente.',
        prescriptions: [],
        examsRequested: [],
        attachments: ['ECO_2025_10_12.pdf', 'Laudo_ECO.pdf'],
        signed: true,
      },
      {
        id: 'enc-2', date: '2025-07-20', time: '14:00',
        type: 'consulta', typeLabel: 'Consulta de retorno',
        doctor: 'Dra. Renata Lopes', specialty: 'Cardiologia',
        complaint: 'Retorno semestral. Sem queixas.',
        anamnesis: 'PA controlada. Glicemia em jejum 115 mg/dL. Boa adesão.',
        exam: 'BEG, AC normal. PA 132x82. FC 70 bpm.',
        cid: 'I10 · E11.9',
        plan: 'Manter conduta. Solicitar ecocardiograma anual.',
        prescriptions: ['Losartana 50mg', 'Metformina 850mg', 'AAS 100mg'],
        examsRequested: ['Ecocardiograma transtorácico'],
        attachments: [],
        signed: true,
      },
      {
        id: 'enc-1', date: '2025-04-15', time: '10:30',
        type: 'consulta', typeLabel: 'Primeira consulta',
        doctor: 'Dra. Renata Lopes', specialty: 'Cardiologia',
        complaint: 'Encaminhado pelo clínico para acompanhamento cardiológico após início de Atorvastatina.',
        anamnesis: 'HAS há 12 anos, DM2 há 8 anos, dislipidemia. Marcapasso desde 2019. Aderente à medicação.',
        exam: 'BEG. PA 145x90. FC 68 bpm. AC: BRNF 2T. AP: MV+. MMII sem edemas.',
        cid: 'I10 · E11.9 · E78.5 — Hiperlipidemia',
        plan: 'Iniciar acompanhamento trimestral. Solicitar painel lipídico, função renal, HbA1c. Orientações dietéticas.',
        prescriptions: ['Losartana 50mg', 'Metformina 850mg', 'AAS 100mg', 'Atorvastatina 20mg'],
        examsRequested: ['Painel lipídico', 'HbA1c', 'Ureia + creatinina'],
        attachments: ['ECG_inicial.pdf'],
        signed: true,
      },
    ],

    // Plano de tratamento
    treatmentPlan: {
      name: 'Acompanhamento cardiológico — HAS + DM2',
      startedAt: '2025-04-15',
      progress: 0.62,
      stages: [
        { name: 'Avaliação inicial e exames basais', status: 'done', date: 'Abr/2025' },
        { name: 'Controle pressórico < 130x80 mmHg', status: 'done', date: 'Out/2025' },
        { name: 'HbA1c < 7,0', status: 'progress', date: 'Em curso' },
        { name: 'Reabilitação cardiovascular (12 sessões)', status: 'progress', date: '4 de 12 sessões' },
        { name: 'Avaliação anual com Holter + MAPA', status: 'pending', date: 'Programado: Jul/2026' },
      ],
    },

    // Orçamentos
    budgets: [
      {
        id: 'orc-2026-042', number: '#2026-042',
        title: 'Reabilitação cardiovascular — 12 sessões',
        createdAt: '2026-04-30', validUntil: '2026-05-30',
        status: 'sent',
        total: 3600.00, paid: 0,
        items: [
          { name: 'Sessão de reabilitação cardiovascular', qty: 12, unit: 250.00, total: 3000.00 },
          { name: 'Avaliação ergoespirométrica inicial', qty: 1, unit: 600.00, total: 600.00 },
        ],
        author: 'Dra. Renata Lopes',
      },
      {
        id: 'orc-2026-018', number: '#2026-018',
        title: 'Pacote de exames anuais',
        createdAt: '2026-01-28', validUntil: '2026-02-28',
        status: 'completed',
        total: 1240.00, paid: 1240.00,
        items: [
          { name: 'Holter 24h', qty: 1, unit: 480.00, total: 480.00 },
          { name: 'MAPA 24h', qty: 1, unit: 380.00, total: 380.00 },
          { name: 'Ecocardiograma', qty: 1, unit: 380.00, total: 380.00 },
        ],
        author: 'Dra. Renata Lopes',
      },
      {
        id: 'orc-2025-189', number: '#2025-189',
        title: 'Consultas trimestrais — pacote anual',
        createdAt: '2025-07-20', validUntil: '2025-08-20',
        status: 'partial',
        total: 1600.00, paid: 800.00,
        items: [
          { name: 'Consulta cardiológica', qty: 4, unit: 400.00, total: 1600.00 },
        ],
        author: 'Dra. Renata Lopes',
      },
      {
        id: 'orc-2025-024', number: '#2025-024',
        title: 'Cintilografia miocárdica (rejeitada — pelo SUS)',
        createdAt: '2025-04-15', validUntil: '2025-05-15',
        status: 'rejected',
        total: 1850.00, paid: 0,
        items: [
          { name: 'Cintilografia miocárdica de perfusão', qty: 1, unit: 1850.00, total: 1850.00 },
        ],
        author: 'Dra. Renata Lopes',
      },
    ],

    // Financeiro
    finance: {
      balanceOpen: 12900.00, // valor em aberto
      totalLifetime: 13900.00, // total cobrado
      totalPaid: 1000.00,
      // Cobranças do paciente (mais recente primeiro). Valores em CENTAVOS.
      // origin: 'consulta' | 'procedimento' | 'cirurgia'
      charges: [
        {
          id: 'c1', origin: 'consulta',
          desc: 'Consulta — Dr. Ricardo Tavares', date: '10/06',
          total: 35000, discount: 0, convenio: false,
          payments: [
            { id: 'pay1', date: '10/06 14:55', method: 'pix', amount: 15000, by: 'Ana (recepção)' },
            { id: 'pay2', date: '10/06 15:02', method: 'credito', parcelas: 2, amount: 20000, by: 'Ana (recepção)' },
          ],
          refunds: [],
        },
        {
          id: 'c2', origin: 'procedimento',
          desc: 'Infiltração articular', date: '28/05',
          total: 80000, discount: 0, convenio: false,
          payments: [
            { id: 'pay3', date: '28/05 11:20', method: 'pix', amount: 40000, by: 'Ana (recepção)' },
          ],
          refunds: [],
        },
        {
          id: 'c3', origin: 'cirurgia',
          desc: 'Artroscopia de joelho', budget: '#1042', date: '05/06',
          total: 1250000, discount: 0, convenio: false,
          payments: [],
          refunds: [],
          valueHistory: [
            { from: 1180000, to: 1250000, by: 'Dr. Ricardo Tavares', date: '05/06', reason: 'inclusão de implante' },
          ],
        },
        {
          id: 'c4', origin: 'consulta',
          desc: 'Consulta — Dra. Renata Lopes', date: '01/06',
          total: 25000, discount: 0, convenio: false,
          payments: [
            { id: 'pay4', date: '01/06 09:30', method: 'pix', amount: 25000, by: 'Ana (recepção)' },
            { id: 'pay5', date: '02/06 10:00', method: 'dinheiro', amount: 10000, by: 'Ana (recepção)', refunded: true },
          ],
          refunds: [
            { id: 'r1', date: '02/06', amount: 10000, reason: 'Cobrança duplicada', by: 'Ana (recepção)', paymentId: 'pay5' },
          ],
        },
        {
          id: 'c5', origin: 'consulta',
          desc: 'Consulta — Dr. Ricardo Tavares', date: '10/06',
          total: 35000, discount: 0, convenio: true,
          operadora: 'Unimed', plano: 'Nacional',
          guia: { numero: '48291', senha: 'AUT-99213', data: '10/06/2026' },
          payments: [], refunds: [],
        },
        {
          id: 'c6', origin: 'procedimento',
          desc: 'Infiltração articular', date: '28/05',
          total: 80000, discount: 0, convenio: true,
          operadora: 'Bradesco Saúde', plano: 'Top',
          guia: null,
          payments: [], refunds: [],
        },
      ],
    },

    // Convênios e autorizações
    insurance: {
      primary: {
        name: 'Particular',
        cardNumber: null,
        validity: null,
      },
      // Carteirinhas do paciente (1:N convênios)
      convenios: [
        {
          id: 'pc-1', operadora: 'Unimed', plano: 'Nacional', ans: '339679',
          carteirinha: '0 064 9999 8888 7777', validade: '12/2026', expired: false, principal: true,
          historico: [
            { date: '10/06/2026', desc: 'Consulta — guia 48291', status: 'faturado' },
            { date: '28/05/2026', desc: 'Procedimento — Infiltração articular', status: 'faturado' },
            { date: '12/03/2026', desc: 'Consulta de rotina', status: 'repassado' },
          ],
        },
        {
          id: 'pc-2', operadora: 'Bradesco Saúde', plano: 'Top', ans: '005711',
          carteirinha: '8 821 0042 1199 3300', validade: '03/2026', expired: true, principal: false,
          historico: [
            { date: '15/01/2026', desc: 'Consulta cardiológica', status: 'repassado' },
          ],
        },
      ],
      authorizations: [
        { id: 'auth-1', proc: 'Holter 24h (ANS 40901176)', status: 'approved', date: '2026-01-25', expiresIn: '90 dias' },
        { id: 'auth-2', proc: 'MAPA 24h (ANS 40901214)', status: 'approved', date: '2026-01-25', expiresIn: '90 dias' },
        { id: 'auth-3', proc: 'Cintilografia miocárdica', status: 'denied', date: '2025-04-22', reason: 'Glosa: ausência de justificativa clínica detalhada' },
      ],
    },

    // Termos de consentimento
    consents: [
      { id: 'c1', name: 'Termo de uso de dados (LGPD)', signedAt: '2025-04-15', signer: 'Pedro Henrique Costa', method: 'Assinatura digital' },
      { id: 'c2', name: 'Termo de consentimento — procedimento cardíaco', signedAt: '2025-04-15', signer: 'Pedro Henrique Costa', method: 'Assinatura digital' },
      { id: 'c3', name: 'Termo de compartilhamento de prontuário entre profissionais', signedAt: '2025-04-15', signer: 'Pedro Henrique Costa', method: 'Assinatura digital' },
      { id: 'c4', name: 'Autorização para reabilitação cardiovascular', signedAt: '2026-04-30', signer: 'Pedro Henrique Costa', method: 'Assinatura presencial' },
    ],

    // Anexos
    attachments: [
      { id: 'a1', name: 'ECG_inicial.pdf', size: '420 KB', uploadedAt: '2025-04-15', uploadedBy: 'Dra. Renata Lopes', type: 'pdf', folder: 'Exames' },
      { id: 'a2', name: 'ECO_2025_10_12.pdf', size: '1,2 MB', uploadedAt: '2025-10-12', uploadedBy: 'Dr. Marcos Rocha', type: 'pdf', folder: 'Exames' },
      { id: 'a3', name: 'Laudo_ECO.pdf', size: '180 KB', uploadedAt: '2025-10-12', uploadedBy: 'Dr. Marcos Rocha', type: 'pdf', folder: 'Laudos' },
      { id: 'a4', name: 'ECG_2026_01_28.pdf', size: '380 KB', uploadedAt: '2026-01-28', uploadedBy: 'Dra. Renata Lopes', type: 'pdf', folder: 'Exames' },
      { id: 'a5', name: 'Foto_marcapasso_RX.jpg', size: '2,4 MB', uploadedAt: '2025-04-15', uploadedBy: 'Dra. Renata Lopes', type: 'image', folder: 'Imagens' },
      { id: 'a6', name: 'Receita_abr2026.pdf', size: '95 KB', uploadedAt: '2026-04-30', uploadedBy: 'Dra. Renata Lopes', type: 'pdf', folder: 'Receitas' },
    ],

    // Próximas ações (para Resumo)
    nextActions: [
      { type: 'reminder', icon: 'fa-flask', title: 'Trazer exames solicitados', desc: 'Hemograma, lipidograma, HbA1c — para próxima consulta', when: 'Antes de 15/05' },
      { type: 'appointment', icon: 'fa-calendar-check', title: 'Próxima consulta agendada', desc: 'Dra. Renata Lopes · Cardiologia', when: '15 de maio · 09:00' },
      { type: 'finance', icon: 'fa-coins', title: 'Pagamento pendente', desc: 'R$ 12.900,00 · 3 cobranças em aberto', when: 'Vence em 8 dias' },
      { type: 'budget', icon: 'fa-file-invoice-dollar', title: 'Orçamento aguardando aceite', desc: '#2026-042 · Reabilitação cardiovascular', when: 'Válido até 30/05' },
    ],
  };

  return { TAGS, BUDGET_STATUS, PATIENTS, PATIENT_DETAIL };
})();
