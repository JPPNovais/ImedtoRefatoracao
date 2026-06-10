// ClinicFinanceApp.jsx — Financeiro da clínica (estabelecimento ativo)

const CF = window.IMEDTO_CLINIC_FINANCE;
const cfBRL = (cents) => 'R$ ' + (cents / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const cfPct = (n) => (n == null ? '—' : n.toLocaleString('pt-BR', { minimumFractionDigits: 1 }) + '%');
window.cfBRL = cfBRL; window.cfPct = cfPct;

const CF_TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "cashState": "aberto",
  "emptyExtrato": false
}/*EDITMODE-END*/;

// ─── KPI card ────────────────────────────────────────────────
const CfKpi = ({ icon, label, value, accent, sub }) => (
  <div className={`cf-kpi ${accent || ''}`}>
    <div className="cf-kpi-h">
      <span className="cf-kpi-ic"><i className={`fa-solid ${icon}`}></i></span>
      <span className="cf-kpi-lbl">{label}</span>
    </div>
    <div className="cf-kpi-v">{value}</div>
    {sub && <div className="cf-kpi-sub">{sub}</div>}
  </div>
);

// ─── Tab: Visão geral (extrato) ──────────────────────────────
const OverviewTab = ({ period, setPeriod, empty }) => {
  const k = CF.kpis;
  const [fType, setFType] = React.useState('all');
  const [fOrigin, setFOrigin] = React.useState('all');
  const [fMethod, setFMethod] = React.useState('all');

  const periods = [
    { id: 'today', label: 'Hoje' },
    { id: 'week', label: 'Semana' },
    { id: 'month', label: 'Mês' },
    { id: 'custom', label: 'Personalizado' },
  ];

  const entries = React.useMemo(() => {
    if (empty) return [];
    return CF.entries.filter(e => {
      if (fType === 'in' && e.type === 'out') return false;
      if (fType === 'out' && e.type !== 'out') return false;
      if (fOrigin !== 'all' && e.origin !== fOrigin) return false;
      if (fMethod !== 'all' && e.method !== fMethod) return false;
      return true;
    });
  }, [empty, fType, fOrigin, fMethod]);

  const typeClass = (t) => t === 'out' ? 'out' : t === 'refund' ? 'refund' : 'in';
  const amtPrefix = (t) => t === 'out' ? '– ' : t === 'refund' ? '– ' : '+ ';

  return (
    <>
      {/* Filtro de período */}
      <div className="cf-period">
        {periods.map(p => (
          <button key={p.id} className={`cf-pchip ${period === p.id ? 'active' : ''}`} onClick={() => setPeriod(p.id)}>
            {p.id === 'custom' && <i className="fa-regular fa-calendar"></i>} {p.label}
          </button>
        ))}
        <span className="cf-period-lbl">Junho 2026 · {CF.estabelecimento}</span>
      </div>

      {/* KPIs primários */}
      <div className="cf-kpi-grid">
        <CfKpi icon="fa-arrow-down-long" label="Recebido" value={cfBRL(k.recebido)} accent="in" />
        <CfKpi icon="fa-hourglass-half" label="A receber" value={cfBRL(k.aReceber)} accent="pending" />
        <CfKpi icon="fa-arrow-up-long" label="Despesas" value={cfBRL(k.despesas)} accent="out" />
        <CfKpi icon="fa-scale-balanced" label="Saldo" value={cfBRL(k.saldo)} accent="balance" />
      </div>

      {/* KPIs secundários */}
      <div className="cf-kpi-sec">
        <div className="cf-sec-item"><span><i className="fa-solid fa-tag"></i> Descontos concedidos</span><b>{cfBRL(k.descontos)}</b></div>
        <div className="cf-sec-item"><span><i className="fa-solid fa-credit-card"></i> Taxas de cartão</span><b>{cfBRL(k.taxas)}</b></div>
        <div className="cf-sec-item"><span><i className="fa-solid fa-rotate-left"></i> Estornos</span><b>{cfBRL(k.estornos)}</b></div>
      </div>

      {/* Extrato */}
      <div className="cf-card">
        <div className="cf-card-h">
          <div><i className="fa-solid fa-list"></i> Extrato de lançamentos</div>
          <div className="cf-filters">
            <select value={fType} onChange={e => setFType(e.target.value)}>
              <option value="all">Receitas e despesas</option>
              <option value="in">Só receitas</option>
              <option value="out">Só despesas</option>
            </select>
            <select value={fOrigin} onChange={e => setFOrigin(e.target.value)}>
              <option value="all">Toda origem</option>
              <option value="consulta">Consulta</option>
              <option value="procedimento">Procedimento</option>
              <option value="cirurgia">Cirurgia</option>
              <option value="avulso">Avulso</option>
              <option value="despesa">Despesa</option>
            </select>
            <select value={fMethod} onChange={e => setFMethod(e.target.value)}>
              <option value="all">Toda forma</option>
              <option value="pix">PIX</option>
              <option value="dinheiro">Dinheiro</option>
              <option value="credito">Crédito</option>
              <option value="debito">Débito</option>
              <option value="boleto">Boleto</option>
            </select>
          </div>
        </div>

        {entries.length === 0 ? (
          <div className="cf-empty">
            <i className="fa-solid fa-receipt"></i>
            <b>Nenhum lançamento no período</b>
            <p>Não há movimentação financeira para os filtros selecionados.</p>
          </div>
        ) : (
          <div className="cf-table">
            <div className="cf-thead">
              <div>Data</div><div>Descrição</div><div>Categoria</div><div>Forma</div><div className="ta-r">Valor</div><div>Status</div>
            </div>
            {entries.map(e => (
              <div key={e.id} className="cf-row">
                <div className="cf-date">{e.date}</div>
                <div className="cf-desc">
                  <span>{e.desc}</span>
                  {e.patient && <a href="PacienteDetalhe.html" className="cf-link"><i className="fa-solid fa-user"></i> {e.patient}</a>}
                </div>
                <div><span className={`cf-cat ${CF.CATEGORIES[e.category].kind}`}>{CF.CATEGORIES[e.category].label}</span></div>
                <div className="cf-method">{CF.METHODS[e.method]}</div>
                <div className={`cf-amt ta-r ${typeClass(e.type)}`}>{amtPrefix(e.type)}{cfBRL(e.amount).replace('R$ ', '')}</div>
                <div>
                  {e.type === 'refund'
                    ? <span className="cf-st refund">Estorno</span>
                    : e.status === 'pendente'
                      ? <span className="cf-st pending">Pendente</span>
                      : <span className="cf-st ok">Liquidado</span>}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </>
  );
};

// ─── App principal ───────────────────────────────────────────
const ClinicFinanceApp = () => {
  const [tweaks, setTweak] = useTweaks(CF_TWEAK_DEFAULTS);
  const [tab, setTab] = React.useState('overview');
  const [period, setPeriod] = React.useState('month');
  const [toast, setToast] = React.useState(null);
  const flash = (m) => { setToast(m); setTimeout(() => setToast(null), 2600); };

  const tabs = [
    { id: 'overview', label: 'Visão geral', icon: 'fa-chart-line' },
    { id: 'caixa', label: 'Caixa diário', icon: 'fa-cash-register' },
    { id: 'comissoes', label: 'Comissões', icon: 'fa-percent' },
    { id: 'config', label: 'Configurações', icon: 'fa-gear' },
  ];

  return (
    <>
      <TopBar />
      <Sidebar active="Financeiro" />
      <main>
        <div className="page cf-page">
          <div className="page-head">
            <div>
              <h1>Financeiro</h1>
              <div className="sub">
                <span><i className="fa-solid fa-building"></i> {CF.estabelecimento}</span>
                <span style={{ color: 'hsl(var(--secondary) / 0.3)' }}>•</span>
                <span><i className="fa-solid fa-lock"></i> Dados restritos a esta unidade</span>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="btn-secondary" onClick={() => flash('Exportando extrato em Excel...')}><i className="fa-solid fa-file-excel"></i> Exportar</button>
              <button className="btn-primary" onClick={() => flash('Novo lançamento avulso')}><i className="fa-solid fa-plus"></i> Lançamento</button>
            </div>
          </div>

          <div className="cf-tabs">
            {tabs.map(t => (
              <button key={t.id} className={`cf-tab ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)}>
                <i className={`fa-solid ${t.icon}`}></i> {t.label}
              </button>
            ))}
          </div>

          <div className="cf-content">
            {tab === 'overview' && <OverviewTab period={period} setPeriod={setPeriod} empty={tweaks.emptyExtrato} />}
            {tab === 'caixa' && <CaixaTab cashState={tweaks.cashState} onToast={flash} />}
            {tab === 'comissoes' && <ComissoesTab onToast={flash} />}
            {tab === 'config' && <ConfigTab onToast={flash} />}
          </div>
        </div>

        <TweaksPanel title="Tweaks">
          <TweakSection title="Estados — Caixa diário">
            <TweakRadio
              value={tweaks.cashState}
              onChange={v => setTweak('cashState', v)}
              options={[
                { value: 'aberto', label: 'Aberto' },
                { value: 'fechado', label: 'Fechado' },
                { value: 'nao-aberto', label: 'Não aberto' },
              ]}
            />
          </TweakSection>
          <TweakSection title="Estados — Extrato">
            <TweakToggle label="Período sem movimento (extrato vazio)" value={tweaks.emptyExtrato} onChange={v => setTweak('emptyExtrato', v)} />
          </TweakSection>
        </TweaksPanel>

        {toast && <div className="toast"><i className="fa-solid fa-circle-check"></i> {toast}</div>}
      </main>
    </>
  );
};

ReactDOM.createRoot(document.getElementById('root')).render(<ClinicFinanceApp />);
