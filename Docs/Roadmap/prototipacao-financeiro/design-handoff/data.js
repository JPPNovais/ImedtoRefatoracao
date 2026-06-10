// Imedto — Agenda data
// Realistic Brazilian clinic data, with statuses reflecting market best practices.

window.AGENDA_DATA = (() => {
  const today = new Date();
  const todayStr = today.toLocaleDateString('pt-BR');
  // status taxonomy following industry best practices:
  // confirmed | unconfirmed | checked-in (sala de espera) | in-progress | completed | no-show | cancelled
  // also: rescheduled is treated as a separate UI state when relevant.
  const statuses = {
    confirmed:    { label: 'Confirmado',      pill: 'p-success', dot: 'var(--c-success)',         color: '160 79% 39%' },
    unconfirmed:  { label: 'Aguardando confirmação', pill: 'p-warning', dot: 'var(--c-warning)',  color: '45 96% 47%' },
    'checked-in': { label: 'Em sala de espera', pill: 'p-info',   dot: 'var(--c-info)',           color: '199 89% 48%' },
    'in-progress':{ label: 'Em atendimento',  pill: 'p-purple',  dot: 'hsl(280 60% 50%)',         color: '280 60% 50%' },
    completed:    { label: 'Concluído',       pill: 'p-muted',   dot: 'hsl(var(--secondary) / 0.4)', color: '0 0% 60%' },
    'no-show':    { label: 'Faltou',          pill: 'p-error',   dot: 'var(--c-error)',           color: '0 84% 60%' },
    cancelled:    { label: 'Cancelado',       pill: 'p-error',   dot: 'var(--c-error)',           color: '0 84% 60%' },
  };

  const tipos = {
    consulta:   { label: 'Consulta',     icon: 'fa-stethoscope',   color: '254 56% 38%' },
    retorno:    { label: 'Retorno',      icon: 'fa-rotate-right',  color: '199 89% 48%' },
    primeira:   { label: 'Primeira vez', icon: 'fa-user-plus',     color: '160 79% 39%' },
    exame:      { label: 'Exame',        icon: 'fa-vial',          color: '280 60% 50%' },
    procedimento:{ label: 'Procedimento',icon: 'fa-syringe',       color: '45 96% 47%' },
    teleconsulta:{label: 'Teleconsulta', icon: 'fa-video',         color: '199 89% 48%' },
  };

  // appointments for today
  const appts = [
    {
      id: 'a1', time: '07:30', duration: 30,
      patient: { name: 'Ana Carolina Silva', age: 34, gender: 'F', initials: 'AC', phone: '(11) 98765-4321' },
      status: 'completed', tipo: 'retorno',
      convenio: 'Bradesco Saúde', plan: 'Top Nacional',
      reason: 'Retorno — pós-exame de sangue',
      reminder: 'sent-confirmed',
      arrivedAt: '07:25', startedAt: '07:32', endedAt: '07:55',
      ready: { docs: true, copay: true, forms: true },
      notes: '',
    },
    {
      id: 'a2', time: '08:00', duration: 30,
      patient: { name: 'Roberto Mendes Pereira', age: 58, gender: 'M', initials: 'RM', phone: '(11) 99876-1234' },
      status: 'completed', tipo: 'consulta',
      convenio: 'Particular', plan: '—',
      reason: 'Hipertensão — acompanhamento',
      reminder: 'sent-confirmed',
      arrivedAt: '07:55', startedAt: '08:01', endedAt: '08:28',
      ready: { docs: true, copay: true, forms: true },
      notes: '',
    },
    {
      id: 'a3', time: '08:30', duration: 30,
      patient: { name: 'Juliana Faria Oliveira', age: 28, gender: 'F', initials: 'JF', phone: '(11) 91234-5678' },
      status: 'no-show', tipo: 'consulta',
      convenio: 'Amil', plan: 'S550',
      reason: 'Cefaleia recorrente',
      reminder: 'sent-no-reply',
      ready: { docs: false, copay: false, forms: false },
      notes: 'Paciente não compareceu. SMS enviado às 08:45 — sem resposta.',
    },
    {
      id: 'a4', time: '09:00', duration: 45,
      patient: { name: 'Pedro Henrique Costa', age: 45, gender: 'M', initials: 'PH', phone: '(11) 98123-4567' },
      status: 'in-progress', tipo: 'primeira',
      convenio: 'SulAmérica', plan: 'Especial',
      reason: 'Primeira consulta — Dor torácica intermitente',
      reminder: 'sent-confirmed',
      arrivedAt: '08:42', startedAt: '09:03',
      ready: { docs: true, copay: true, forms: true },
      notes: 'Trazer ECG anterior (paciente já enviou via portal).',
    },
    {
      id: 'a5', time: '09:45', duration: 30,
      patient: { name: 'Marina Souza Almeida', age: 41, gender: 'F', initials: 'MS', phone: '(11) 99234-5678' },
      status: 'checked-in', tipo: 'retorno',
      convenio: 'Unimed', plan: 'Nacional',
      reason: 'Retorno — diabetes tipo 2',
      reminder: 'sent-confirmed',
      arrivedAt: '09:38',
      ready: { docs: true, copay: true, forms: false },
      notes: 'Aguardando preencher anamnese digital.',
    },
    {
      id: 'a6', time: '10:15', duration: 30,
      patient: { name: 'Carlos Eduardo Ramos', age: 62, gender: 'M', initials: 'CE', phone: '(11) 98345-6789' },
      status: 'confirmed', tipo: 'exame',
      convenio: 'Bradesco Saúde', plan: 'Nacional Plus',
      reason: 'MAPA 24h — entrega de aparelho',
      reminder: 'sent-confirmed',
      ready: { docs: true, copay: true, forms: true },
      notes: '',
    },
    {
      id: 'a7', time: '10:45', duration: 30,
      patient: { name: 'Beatriz Lima Cardoso', age: 29, gender: 'F', initials: 'BL', phone: '(11) 97456-7890' },
      status: 'unconfirmed', tipo: 'consulta',
      convenio: 'Amil', plan: 'Fácil',
      reason: 'Acompanhamento clínico geral',
      reminder: 'sent-pending',
      ready: { docs: false, copay: true, forms: false },
      notes: 'Reenviar lembrete por WhatsApp — sem resposta há 24h.',
    },
    // 11:15 lunch start
    {
      id: 'a8', time: '13:30', duration: 30,
      patient: { name: 'Fernando Toledo', age: 51, gender: 'M', initials: 'FT', phone: '(11) 98567-8901' },
      status: 'confirmed', tipo: 'teleconsulta',
      convenio: 'Particular', plan: '—',
      reason: 'Telemedicina — discussão de exames',
      reminder: 'sent-confirmed',
      ready: { docs: true, copay: true, forms: true },
      notes: 'Link de vídeo enviado por e-mail.',
    },
    {
      id: 'a9', time: '14:00', duration: 60,
      patient: { name: 'Lúcia Mendonça Rocha', age: 67, gender: 'F', initials: 'LM', phone: '(11) 99678-9012' },
      status: 'confirmed', tipo: 'procedimento',
      convenio: 'SulAmérica', plan: 'Premium',
      reason: 'Pequena cirurgia — remoção de lesão',
      reminder: 'sent-confirmed',
      ready: { docs: true, copay: true, forms: true },
      notes: 'Jejum de 6h confirmado. Acompanhante presente.',
    },
    {
      id: 'a10', time: '15:00', duration: 30,
      patient: { name: 'Gabriel Santos Vieira', age: 19, gender: 'M', initials: 'GS', phone: '(11) 98789-0123' },
      status: 'confirmed', tipo: 'primeira',
      convenio: 'Unimed', plan: 'Estudante',
      reason: 'Primeira consulta — Avaliação geral',
      reminder: 'sent-confirmed',
      ready: { docs: true, copay: false, forms: false },
      notes: 'Cobrar coparticipação na chegada.',
    },
    {
      id: 'a11', time: '15:30', duration: 30,
      patient: { name: 'Renata Albuquerque', age: 38, gender: 'F', initials: 'RA', phone: '(11) 97890-1234' },
      status: 'unconfirmed', tipo: 'retorno',
      convenio: 'Bradesco Saúde', plan: 'Top',
      reason: 'Retorno — resultados laboratoriais',
      reminder: 'sent-pending',
      ready: { docs: true, copay: true, forms: true },
      notes: '',
    },
    {
      id: 'a12', time: '16:00', duration: 30,
      patient: { name: 'Ricardo Nunes Bastos', age: 55, gender: 'M', initials: 'RN', phone: '(11) 98901-2345' },
      status: 'cancelled', tipo: 'consulta',
      convenio: 'Amil', plan: 'S380',
      reason: 'Consulta — segunda opinião',
      reminder: 'cancelled-by-patient',
      ready: { docs: false, copay: false, forms: false },
      notes: 'Cancelado pelo paciente em 09/10 às 19:42 — viagem de trabalho. Reagendar.',
    },
    {
      id: 'a13', time: '16:30', duration: 45,
      patient: { name: 'Camila Ferreira Dutra', age: 33, gender: 'F', initials: 'CF', phone: '(11) 99012-3456' },
      status: 'confirmed', tipo: 'consulta',
      convenio: 'Particular', plan: '—',
      reason: 'Consulta clínica geral',
      reminder: 'sent-confirmed',
      ready: { docs: true, copay: true, forms: true },
      notes: '',
    },
    {
      id: 'a14', time: '17:30', duration: 30,
      patient: { name: 'Eduardo Vasconcelos', age: 71, gender: 'M', initials: 'EV', phone: '(11) 98123-9876' },
      status: 'confirmed', tipo: 'retorno',
      convenio: 'Unimed', plan: 'Sênior',
      reason: 'Retorno — ajuste de medicação',
      reminder: 'sent-confirmed',
      ready: { docs: true, copay: true, forms: true },
      notes: 'Acompanhado pela filha.',
    },
  ];

  // Simulated waitlist (for filling cancellations / encaixe)
  const waitlist = [
    { id: 'w1', name: 'Mariana Cunha', initials: 'MC', reason: 'Encaixe urgente', since: '2 dias' },
    { id: 'w2', name: 'Tiago Bezerra',  initials: 'TB', reason: 'Reagendar consulta', since: '5 dias' },
    { id: 'w3', name: 'Larissa Veiga',  initials: 'LV', reason: 'Encaixe — tarde', since: '1 dia' },
  ];

  // ─── Cobranças / pagamentos (cobrado ≠ pago) ───────────────
  // Tabela de preços por tipo (centavos) — espelha a config por profissional
  const PRICE_TABLE = {
    consulta: 35000, retorno: 25000, primeira: 45000,
    exame: 30000, procedimento: 60000, teleconsulta: 28000,
  };
  // Taxas de cartão (informativas, não editáveis no balcão)
  const CARD_FEES = { credito: 3.5, debito: 1.5, pix: 0, dinheiro: 0 };
  const creditFeeFor = (parcelas) => parcelas <= 1 ? 3.5 : parcelas <= 6 ? 4.99 : 6.99;

  // Anexa cobranças a agendamentos faturáveis (consulta/retorno/primeira/procedimento/tele).
  // Exame e a "pequena cirurgia" (procedimento a9) ficam SEM cobrança pelo agendamento.
  const billingMap = {
    // Particular — PAGA (atendimento concluído)
    a2: { kind: 'particular', total: 35000, discount: 0, payments: [
      { id: 'p1', date: '10/06 08:05', method: 'pix', amount: 35000, by: 'Ana (recepção)' },
    ] },
    // Convênio — sem pagamento de balcão
    a1: { kind: 'convenio' },
    a4: { kind: 'convenio' },
    a5: { kind: 'convenio' },
    // Particular — ABERTA (nenhum pagamento)
    a8: { kind: 'particular', total: 28000, discount: 0, payments: [] },
    // Convênio
    a10: { kind: 'convenio' },
    a14: { kind: 'convenio' },
    // Particular — PARCIALMENTE PAGA (1 pagamento + saldo) — exemplo do brief
    a13: { kind: 'particular', total: 35000, discount: 0, payments: [
      { id: 'p1', date: '10/06 14:55', method: 'pix', amount: 15000, by: 'Ana (recepção)' },
    ] },
    // Convênio (retorno)
    a11: { kind: 'convenio' },
  };
  appts.forEach(a => { if (billingMap[a.id]) a.billing = billingMap[a.id]; });

  return { appts, waitlist, statuses, tipos, todayStr, PRICE_TABLE, CARD_FEES, creditFeeFor };
})();
