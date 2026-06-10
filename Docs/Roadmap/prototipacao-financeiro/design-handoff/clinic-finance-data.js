// clinic-finance-data.js — Financeiro da clínica (estabelecimento ativo)
// Valores monetários em CENTAVOS. Junho/2026.

window.IMEDTO_CLINIC_FINANCE = (() => {
  const estabelecimento = 'Clínica Vita — Unidade Centro';

  // ─── KPIs do mês (Junho/2026) ──────────────────────────────
  const kpis = {
    recebido:  4835000,
    aReceber:  2390000,
    despesas:  1248000,
    // saldo = recebido - despesas
    saldo:     3587000,
    descontos: 125000,
    taxas:     168045,
    estornos:   54000,
  };

  // ─── Categorias ────────────────────────────────────────────
  const CATEGORIES = {
    consulta:     { label: 'Consulta',      kind: 'in' },
    procedimento: { label: 'Procedimento',  kind: 'in' },
    cirurgia:     { label: 'Cirurgia',      kind: 'in' },
    avulso:       { label: 'Avulso',        kind: 'in' },
    aluguel:      { label: 'Aluguel',       kind: 'out' },
    salarios:     { label: 'Salários',      kind: 'out' },
    materiais:    { label: 'Materiais',     kind: 'out' },
    marketing:    { label: 'Marketing',     kind: 'out' },
    impostos:     { label: 'Impostos',      kind: 'out' },
  };
  const METHODS = {
    pix:      'PIX', dinheiro: 'Dinheiro',
    credito:  'Crédito', debito: 'Débito',
    boleto:   'Boleto', '—': '—',
  };

  // ─── Extrato de lançamentos ────────────────────────────────
  // type: 'in' (entrada) | 'out' (saída) | 'refund' (estorno)
  const entries = [
    { id: 'e1', date: '10/06 15:02', desc: 'Pagamento consulta — Camila Ferreira', patient: 'Camila Ferreira Dutra', origin: 'consulta', category: 'consulta', method: 'credito', amount: 20000, type: 'in', status: 'liquidado' },
    { id: 'e2', date: '10/06 14:55', desc: 'Pagamento consulta — Camila Ferreira', patient: 'Camila Ferreira Dutra', origin: 'consulta', category: 'consulta', method: 'pix', amount: 15000, type: 'in', status: 'liquidado' },
    { id: 'e3', date: '10/06 11:20', desc: 'Pagamento procedimento — Infiltração articular', patient: 'Pedro Henrique Costa', origin: 'procedimento', category: 'procedimento', method: 'pix', amount: 40000, type: 'in', status: 'liquidado' },
    { id: 'e4', date: '10/06 10:40', desc: 'Aluguel da unidade — Junho', patient: null, origin: 'despesa', category: 'aluguel', method: 'boleto', amount: 850000, type: 'out', status: 'liquidado' },
    { id: 'e5', date: '10/06 09:30', desc: 'Pagamento consulta — Roberto Mendes', patient: 'Roberto Mendes Pereira', origin: 'consulta', category: 'consulta', method: 'dinheiro', amount: 35000, type: 'in', status: 'liquidado' },
    { id: 'e6', date: '09/06 17:10', desc: 'Estorno — cobrança duplicada (Marina Souza)', patient: 'Marina Souza Almeida', origin: 'consulta', category: 'consulta', method: 'pix', amount: 10000, type: 'refund', status: 'liquidado' },
    { id: 'e7', date: '09/06 16:25', desc: 'Sinal cirurgia — Artroscopia de joelho', patient: 'Lúcia Mendonça Rocha', origin: 'cirurgia', category: 'cirurgia', method: 'credito', amount: 500000, type: 'in', status: 'liquidado' },
    { id: 'e8', date: '09/06 14:00', desc: 'Compra de materiais — gaze e luvas', patient: null, origin: 'despesa', category: 'materiais', method: 'boleto', amount: 128000, type: 'out', status: 'liquidado' },
    { id: 'e9', date: '08/06 18:30', desc: 'Venda avulsa — aplicação de vacina', patient: 'Gabriel Santos Vieira', origin: 'avulso', category: 'avulso', method: 'debito', amount: 9000, type: 'in', status: 'liquidado' },
    { id: 'e10', date: '08/06 13:15', desc: 'Campanha de marketing — Google Ads', patient: null, origin: 'despesa', category: 'marketing', method: 'credito', amount: 60000, type: 'out', status: 'liquidado' },
    { id: 'e11', date: '07/06 10:00', desc: 'Cobrança consulta — Eduardo Vasconcelos', patient: 'Eduardo Vasconcelos', origin: 'consulta', category: 'consulta', method: '—', amount: 30000, type: 'in', status: 'pendente' },
    { id: 'e12', date: '05/06 09:45', desc: 'Salários — equipe de recepção', patient: null, origin: 'despesa', category: 'salarios', method: 'boleto', amount: 210000, type: 'out', status: 'liquidado' },
  ];

  // ─── Caixa diário ──────────────────────────────────────────
  const cashDay = {
    date: '10/06/2026',
    status: 'aberto',            // 'aberto' | 'fechado' | 'nao-aberto'
    openedAt: '08:05',
    operator: 'Ana Souza (recepção)',
    byMethod: { dinheiro: 35000, pix: 55000, credito: 20000, debito: 0 },
    estornos: 0,
    // preenchido quando fechado
    closedAt: null, closedBy: null, obs: null,
  };

  // ─── Comissões por profissional ────────────────────────────
  const commissions = [
    {
      id: 'pr1', name: 'Dr. Ricardo Tavares', role: 'Ortopedia',
      atendimentos: 38, faturamento: 2980000, pct: 30, repasse: 894000,
      detail: [
        { date: '10/06', patient: 'Pedro Henrique Costa', proc: 'Infiltração articular', faturamento: 80000, base: '% config', comissao: 24000 },
        { date: '09/06', patient: 'Lúcia Mendonça Rocha', proc: 'Artroscopia (orçamento #1042)', faturamento: 1250000, base: 'valor do orçamento', comissao: 375000 },
        { date: '08/06', patient: 'Roberto Mendes Pereira', proc: 'Consulta', faturamento: 35000, base: '% config', comissao: 10500 },
      ],
    },
    {
      id: 'pr2', name: 'Dra. Paula Andrade', role: 'Dermatologia',
      atendimentos: 27, faturamento: 1780000, pct: 35, repasse: 623000,
      detail: [
        { date: '10/06', patient: 'Camila Ferreira Dutra', proc: 'Consulta', faturamento: 35000, base: '% config', comissao: 12250 },
        { date: '07/06', patient: 'Beatriz Lima Cardoso', proc: 'Biópsia de pele', faturamento: 60000, base: '% config', comissao: 21000 },
      ],
    },
    {
      id: 'pr3', name: 'Dra. Renata Lopes', role: 'Cardiologia',
      atendimentos: 31, faturamento: 1240000, pct: 30, repasse: 372000,
      detail: [
        { date: '10/06', patient: 'Carlos Eduardo Ramos', proc: 'MAPA 24h', faturamento: 38000, base: '% config', comissao: 11400 },
      ],
    },
  ];

  // ─── Configurações ─────────────────────────────────────────
  const config = {
    cardFees: [
      { id: 'cf1', label: 'Crédito à vista', pct: 3.5, active: true },
      { id: 'cf2', label: 'Crédito parcelado', pct: 4.2, active: true },
      { id: 'cf3', label: 'Débito', pct: 1.9, active: true },
      { id: 'cf4', label: 'PIX', pct: 0, active: true },
      { id: 'cf5', label: 'Dinheiro', pct: 0, active: true },
      { id: 'cf6', label: 'Boleto', pct: null, active: false }, // taxa não configurada
    ],
    priceTable: {
      default: 30000,
      exceptions: [
        { id: 'px1', professional: 'Dr. Ricardo Tavares', price: 35000 },
        { id: 'px2', professional: 'Dra. Paula Andrade', price: 32000 },
      ],
    },
    commissionDefault: 30,
    commissionByPro: [
      { id: 'co1', professional: 'Dr. Ricardo Tavares', pct: 30, isDefault: true },
      { id: 'co2', professional: 'Dra. Paula Andrade', pct: 35, isDefault: false },
      { id: 'co3', professional: 'Dra. Renata Lopes', pct: 30, isDefault: true },
    ],
  };

  return { estabelecimento, kpis, CATEGORIES, METHODS, entries, cashDay, commissions, config };
})();
