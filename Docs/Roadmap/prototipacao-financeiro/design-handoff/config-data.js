// Imedto — Configurações de Orçamento + Estoque (cadastros)
// Dados base para todas as telas de configuração.

window.IMEDTO_CONFIG = (() => {

  // ─── ORÇAMENTO — Procedimentos / cirurgias ─────────────
  const procedures = [
    { id: 'p1', code: '30912025', tuss: '30912025', name: 'Colecistectomia videolaparoscópica', category: 'Cirurgia geral', duration: 90, price: 4800, active: true,
      products: [{ id: 'op2', qty: 1, included: true }, { id: 'op4', qty: 3, included: true }, { id: 'op6', qty: 1, included: true }] },
    { id: 'p2', code: '30713016', tuss: '30713016', name: 'Herniorrafia inguinal unilateral', category: 'Cirurgia geral', duration: 75, price: 3200, active: true,
      products: [{ id: 'op2', qty: 1, included: true }, { id: 'op3', qty: 1, included: true }] },
    { id: 'p3', code: '30602100', tuss: '30602100', name: 'Mastectomia radical', category: 'Cirurgia oncológica', duration: 180, price: 12500, active: true,
      products: [{ id: 'op1', qty: 2, included: false }, { id: 'op3', qty: 2, included: true }] },
    { id: 'p4', code: '30801010', tuss: '30801010', name: 'Septoplastia funcional', category: 'Otorrinolaringologia', duration: 60, price: 3600, active: true, products: [] },
    { id: 'p5', code: '30912033', tuss: '30912033', name: 'Apendicectomia videolaparoscópica', category: 'Cirurgia geral', duration: 60, price: 3800, active: true,
      products: [{ id: 'op4', qty: 3, included: true }, { id: 'op6', qty: 1, included: true }] },
    { id: 'p6', code: '40404011', tuss: '40404011', name: 'Endoscopia digestiva alta', category: 'Endoscopia', duration: 30, price: 850, active: true, products: [] },
    { id: 'p7', code: '30214061', tuss: '30214061', name: 'Cirurgia bariátrica (bypass)', category: 'Cirurgia geral', duration: 150, price: 18900, active: true,
      products: [{ id: 'op4', qty: 5, included: true }, { id: 'op5', qty: 2, included: true }, { id: 'op6', qty: 1, included: true }, { id: 'op7', qty: 1, included: false }] },
    { id: 'p8', code: '40402018', tuss: '40402018', name: 'Colonoscopia diagnóstica', category: 'Endoscopia', duration: 45, price: 1450, active: false, products: [] },
    { id: 'p9', code: '30912017', tuss: '30912017', name: 'Hernioplastia incisional', category: 'Cirurgia geral', duration: 90, price: 4200, active: true,
      products: [{ id: 'op2', qty: 2, included: true }] },
    { id: 'p10', code: '30911010', tuss: '30911010', name: 'Lipoaspiração de pequena área', category: 'Plástica', duration: 60, price: 5500, active: true, products: [] },
  ];

  // ─── ORÇAMENTO — Produtos / OPME / Materiais ───────────
  const orcProducts = [
    { id: 'op1', code: 'OPM-001', name: 'Prótese mamária 350cc anatômica', type: 'OPME', brand: 'Mentor', unit: 'un', price: 4800, supplier: 'OPM Brasil', active: true },
    { id: 'op2', code: 'OPM-002', name: 'Tela de polipropileno 15x15cm', type: 'OPME', brand: 'Ethicon', unit: 'un', price: 380, supplier: 'Johnson & Johnson', active: true },
    { id: 'op3', code: 'OPM-003', name: 'Fio Prolene 3-0 (cx 24un)', type: 'Descartável', brand: 'Ethicon', unit: 'cx', price: 240, supplier: 'Johnson & Johnson', active: true },
    { id: 'op4', code: 'OPM-004', name: 'Trocarte descartável 12mm', type: 'Descartável', brand: 'Covidien', unit: 'un', price: 320, supplier: 'Medtronic', active: true },
    { id: 'op5', code: 'OPM-005', name: 'Grampeador linear 60mm', type: 'OPME', brand: 'Ethicon', unit: 'un', price: 1800, supplier: 'Johnson & Johnson', active: true },
    { id: 'op6', code: 'OPM-006', name: 'Bisturi harmônico (lâmina)', type: 'Descartável', brand: 'Ethicon', unit: 'un', price: 1450, supplier: 'Johnson & Johnson', active: true },
    { id: 'op7', code: 'OPM-007', name: 'Kit dreno torácico', type: 'Descartável', brand: 'BD', unit: 'kit', price: 280, supplier: 'BD Medical', active: true },
    { id: 'op8', code: 'OPM-008', name: 'Curativo Tegaderm 10x12cm', type: 'Curativo', brand: '3M', unit: 'un', price: 18, supplier: '3M Brasil', active: true },
  ];

  // ─── ORÇAMENTO — Equipe (papéis padrão) ────────────────
  const teamRoles = [
    { id: 'tr1', role: 'Cirurgião principal', defaultName: 'Dra. Beatriz Almeida', honorary: 'percentage', value: 60, basedOn: 'procedimento', active: true },
    { id: 'tr2', role: 'Primeiro auxiliar', defaultName: 'Dr. Lucas Ramires', honorary: 'percentage', value: 30, basedOn: 'procedimento', active: true },
    { id: 'tr3', role: 'Segundo auxiliar', defaultName: 'Dr. Felipe Costa', honorary: 'percentage', value: 20, basedOn: 'procedimento', active: true },
    { id: 'tr4', role: 'Instrumentadora', defaultName: 'Enf. Camila Souza', honorary: 'fixed', value: 350, basedOn: 'por cirurgia', active: true },
    { id: 'tr5', role: 'Circulante', defaultName: 'Enf. Patrícia Lima', honorary: 'fixed', value: 280, basedOn: 'por cirurgia', active: true },
    { id: 'tr6', role: 'Técnico de enfermagem', defaultName: 'Téc. André Pereira', honorary: 'fixed', value: 180, basedOn: 'por cirurgia', active: true },
  ];

  // ─── ORÇAMENTO — Anestesistas ──────────────────────────
  const anesthesiologists = [
    { id: 'an1', name: 'Dr. Roberto Mendes', crm: 'CRM/SP 89.432', specialty: 'Anestesiologia geral', phone: '(11) 98765-4321', table: 'Padrão', active: true,
      pricing: [
        { type: 'Pequeno porte (até 1h)', value: 1200 },
        { type: 'Médio porte (1-2h)', value: 2200 },
        { type: 'Grande porte (2-4h)', value: 3800 },
        { type: 'Extra grande (+4h)', value: 5500 },
      ]
    },
    { id: 'an2', name: 'Dra. Helena Tavares', crm: 'CRM/SP 112.678', specialty: 'Anestesia pediátrica', phone: '(11) 99654-3210', table: 'Pediátrica', active: true,
      pricing: [
        { type: 'Pequeno porte', value: 1400 },
        { type: 'Médio porte', value: 2500 },
        { type: 'Grande porte', value: 4200 },
      ]
    },
    { id: 'an3', name: 'Dr. Eduardo Vieira', crm: 'CRM/SP 67.221', specialty: 'Cardioanestesia', phone: '(11) 91234-5678', table: 'Especial', active: true,
      pricing: [
        { type: 'Procedimento cardíaco', value: 5800 },
        { type: 'Pós-operatório UTI', value: 1200 },
      ]
    },
    { id: 'an4', name: 'Dra. Mônica Silva', crm: 'CRM/SP 98.765', specialty: 'Anestesiologia geral', phone: '(11) 95432-1098', table: 'Padrão', active: false,
      pricing: [
        { type: 'Pequeno porte (até 1h)', value: 1100 },
      ]
    },
  ];

  // ─── ORÇAMENTO — Pacotes pré-configurados ──────────────
  const packages = [
    {
      id: 'pk1', name: 'Pacote completo — Colecistectomia',
      description: 'Cirurgia, equipe, anestesia, OPMEs e curativos',
      procedures: ['p1'],
      products: [{ id: 'op2', qty: 1 }, { id: 'op4', qty: 3 }, { id: 'op6', qty: 1 }, { id: 'op8', qty: 4 }],
      team: ['tr1', 'tr2', 'tr4', 'tr5'],
      anesthesiologist: 'an1',
      totalSuggested: 9800,
      active: true,
    },
    {
      id: 'pk2', name: 'Pacote — Herniorrafia simples',
      description: 'Cirurgia ambulatorial com tela e equipe básica',
      procedures: ['p2'],
      products: [{ id: 'op2', qty: 1 }, { id: 'op3', qty: 1 }],
      team: ['tr1', 'tr2', 'tr4'],
      anesthesiologist: 'an1',
      totalSuggested: 6200,
      active: true,
    },
    {
      id: 'pk3', name: 'Pacote — Bariátrica completa',
      description: 'Bypass gástrico com toda a estrutura cirúrgica',
      procedures: ['p7'],
      products: [{ id: 'op4', qty: 5 }, { id: 'op5', qty: 2 }, { id: 'op6', qty: 1 }, { id: 'op7', qty: 1 }],
      team: ['tr1', 'tr2', 'tr3', 'tr4', 'tr5', 'tr6'],
      anesthesiologist: 'an3',
      totalSuggested: 32400,
      active: true,
    },
  ];

  // ═══════════ ESTOQUE — Configurações ═══════════════════

  // ─── Categorias de produtos do estoque ─────────────────
  const stockCategories = [
    { id: 'sc1', name: 'Medicamentos', color: 'hsl(218 70% 50%)', icon: 'fa-pills', count: 124 },
    { id: 'sc2', name: 'Materiais cirúrgicos', color: 'hsl(280 60% 50%)', icon: 'fa-scalpel', count: 78 },
    { id: 'sc3', name: 'Descartáveis', color: 'hsl(155 50% 45%)', icon: 'fa-trash-can', count: 156 },
    { id: 'sc4', name: 'Curativos', color: 'hsl(38 80% 50%)', icon: 'fa-bandage', count: 42 },
    { id: 'sc5', name: 'EPI', color: 'hsl(0 70% 50%)', icon: 'fa-user-shield', count: 18 },
    { id: 'sc6', name: 'Limpeza e higiene', color: 'hsl(190 60% 45%)', icon: 'fa-spray-can-sparkles', count: 24 },
    { id: 'sc7', name: 'Escritório', color: 'hsl(220 15% 50%)', icon: 'fa-paperclip', count: 12 },
  ];

  // ─── Produtos do estoque ───────────────────────────────
  const stockProducts = [
    { id: 'st1', code: 'MED-0142', name: 'Dipirona 500mg comprimido', category: 'sc1', brand: 'EMS', unit: 'cp', cost: 0.18, minStock: 200, currentStock: 1240, supplier: 's1', location: 'l1', active: true },
    { id: 'st2', code: 'MED-0078', name: 'Paracetamol 750mg', category: 'sc1', brand: 'Medley', unit: 'cp', cost: 0.22, minStock: 150, currentStock: 320, supplier: 's1', location: 'l1', active: true },
    { id: 'st3', code: 'MED-0203', name: 'Insulina NPH 100UI', category: 'sc1', brand: 'Novo Nordisk', unit: 'frasco', cost: 78.50, minStock: 20, currentStock: 18, supplier: 's2', location: 'l2', active: true },
    { id: 'st4', code: 'CIR-0011', name: 'Lâmina de bisturi nº 11', category: 'sc2', brand: 'Solidor', unit: 'cx', cost: 32, minStock: 10, currentStock: 24, supplier: 's3', location: 'l3', active: true },
    { id: 'st5', code: 'DES-0067', name: 'Seringa descartável 10ml', category: 'sc3', brand: 'BD', unit: 'un', cost: 0.65, minStock: 500, currentStock: 1820, supplier: 's4', location: 'l1', active: true },
    { id: 'st6', code: 'DES-0089', name: 'Agulha 25x7mm', category: 'sc3', brand: 'BD', unit: 'un', cost: 0.18, minStock: 1000, currentStock: 4200, supplier: 's4', location: 'l1', active: true },
    { id: 'st7', code: 'CUR-0023', name: 'Gaze estéril 7,5x7,5cm', category: 'sc4', brand: 'Cremer', unit: 'pct', cost: 1.80, minStock: 100, currentStock: 280, supplier: 's5', location: 'l3', active: true },
    { id: 'st8', code: 'EPI-0008', name: 'Luva nitrílica tam. M (cx 100)', category: 'sc5', brand: 'Supermax', unit: 'cx', cost: 42, minStock: 30, currentStock: 86, supplier: 's5', location: 'l1', active: true },
    { id: 'st9', code: 'CUR-0045', name: 'Esparadrapo microporoso 5cm', category: 'sc4', brand: '3M', unit: 'rolo', cost: 8.40, minStock: 50, currentStock: 142, supplier: 's6', location: 'l3', active: true },
    { id: 'st10', code: 'LIM-0002', name: 'Álcool 70% 1L', category: 'sc6', brand: 'Itajá', unit: 'frasco', cost: 12.50, minStock: 20, currentStock: 18, supplier: 's5', location: 'l3', active: true },
  ];

  // ─── Fabricantes / marcas ──────────────────────────────
  const manufacturers = [
    { id: 'mf1', name: 'EMS', country: 'Brasil', products: 42, active: true },
    { id: 'mf2', name: 'Medley', country: 'Brasil', products: 28, active: true },
    { id: 'mf3', name: 'Novo Nordisk', country: 'Dinamarca', products: 8, active: true },
    { id: 'mf4', name: 'BD (Becton Dickinson)', country: 'EUA', products: 36, active: true },
    { id: 'mf5', name: 'Johnson & Johnson', country: 'EUA', products: 24, active: true },
    { id: 'mf6', name: 'Cremer', country: 'Brasil', products: 18, active: true },
    { id: 'mf7', name: '3M', country: 'EUA', products: 22, active: true },
    { id: 'mf8', name: 'Ethicon', country: 'EUA', products: 16, active: true },
    { id: 'mf9', name: 'Supermax', country: 'Malásia', products: 6, active: true },
    { id: 'mf10', name: 'Medtronic', country: 'Irlanda', products: 14, active: false },
  ];

  // ─── Fornecedores ──────────────────────────────────────
  const suppliers = [
    { id: 's1', razaoSocial: 'Distribuidora Vital Saúde Ltda', fantasia: 'Vital Distribuição', cnpj: '12.345.678/0001-90', contactName: 'Carlos Oliveira', phone: '(11) 3050-2020', email: 'pedidos@vital.com.br', deliveryDays: 3, active: true },
    { id: 's2', razaoSocial: 'Farmaclínica Comércio S/A', fantasia: 'Farmaclínica', cnpj: '23.456.789/0001-12', contactName: 'Mariana Lima', phone: '(11) 4002-8922', email: 'mariana@farmaclinica.com.br', deliveryDays: 5, active: true },
    { id: 's3', razaoSocial: 'CirurMed Equipamentos Hospitalares', fantasia: 'CirurMed', cnpj: '34.567.890/0001-23', contactName: 'Roberto Santos', phone: '(11) 3030-4040', email: 'vendas@cirurmed.com.br', deliveryDays: 7, active: true },
    { id: 's4', razaoSocial: 'BD Brasil Comércio Ltda', fantasia: 'BD Direto', cnpj: '45.678.901/0001-34', contactName: 'Ana Beatriz', phone: '0800-7244222', email: 'atendimento@bd.com', deliveryDays: 4, active: true },
    { id: 's5', razaoSocial: 'MedSupply Distribuição', fantasia: 'MedSupply', cnpj: '56.789.012/0001-45', contactName: 'Paulo Henrique', phone: '(11) 3522-1100', email: 'paulo@medsupply.com.br', deliveryDays: 2, active: true },
    { id: 's6', razaoSocial: 'Hospital Supply Brasil', fantasia: 'HSB', cnpj: '67.890.123/0001-56', contactName: 'Júlia Mendes', phone: '(11) 2122-0033', email: 'julia.mendes@hsb.com.br', deliveryDays: 5, active: false },
  ];

  // ─── Locais de armazenamento ───────────────────────────
  const locations = [
    { id: 'l1', name: 'Almoxarifado Central', type: 'Armário', floor: 'Térreo', responsable: 'Bruno Silva', items: 184, active: true },
    { id: 'l2', name: 'Geladeira medicamentos', type: 'Refrigerado (2-8°C)', floor: 'Térreo', responsable: 'Bruno Silva', items: 24, active: true },
    { id: 'l3', name: 'Sala cirúrgica 1', type: 'Armário', floor: '1º andar', responsable: 'Enf. Camila Souza', items: 56, active: true },
    { id: 'l4', name: 'Sala cirúrgica 2', type: 'Armário', floor: '1º andar', responsable: 'Enf. Camila Souza', items: 48, active: true },
    { id: 'l5', name: 'Recepção', type: 'Gaveta', floor: 'Térreo', responsable: 'Aline Souza', items: 12, active: true },
    { id: 'l6', name: 'Cofre de controlados', type: 'Cofre', floor: 'Térreo', responsable: 'Dra. Mariana Costa', items: 14, active: true },
  ];

  return {
    // orçamento
    procedures, orcProducts, teamRoles, anesthesiologists, packages,
    // estoque
    stockCategories, stockProducts, manufacturers, suppliers, locations,
  };
})();
