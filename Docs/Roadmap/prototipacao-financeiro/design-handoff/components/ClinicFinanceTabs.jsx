// ClinicFinanceTabs.jsx — Caixa diário, Comissões, Configurações

// ─── Tab: Caixa diário ───────────────────────────────────────
const FecharCaixaModal = ({ open, cash, onClose, onConfirm }) => {
  const _BRL = window.cfBRL;
  const [obs, setObs] = React.useState('');
  React.useEffect(() => { if (open) setObs(''); }, [open]);
  if (!open) return null;
  const total = Object.values(cash.byMethod).reduce((s, v) => s + v, 0);
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-fechar" onClick={e => e.stopPropagation()}>
        <header className="modal-head">
          <div className="fc-head">
            <div className="fc-ic"><i className="fa-solid fa-cash-register"></i></div>
            <div>
              <h2>Fechar caixa do dia</h2>
              <span>{cash.date} · aberto às {cash.openedAt} por {cash.operator}</span>
            </div>
          </div>
          <button className="modal-close" onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>
        <div className="modal-body">
          <div className="fc-summary">
            <div className="fc-row"><span><i className="fa-solid fa-money-bill-wave"></i> Dinheiro</span><b>{_BRL(cash.byMethod.dinheiro)}</b></div>
            <div className="fc-row"><span><i className="fa-brands fa-pix"></i> PIX</span><b>{_BRL(cash.byMethod.pix)}</b></div>
            <div className="fc-row"><span><i className="fa-solid fa-credit-card"></i> Crédito</span><b>{_BRL(cash.byMethod.credito)}</b></div>
            <div className="fc-row"><span><i className="fa-regular fa-credit-card"></i> Débito</span><b>{_BRL(cash.byMethod.debito)}</b></div>
            <div className="fc-row refund"><span><i className="fa-solid fa-rotate-left"></i> Estornos</span><b>– {_BRL(cash.estornos)}</b></div>
            <div className="fc-row total"><span>Total do dia</span><b>{_BRL(total - cash.estornos)}</b></div>
          </div>
          <div className="field-group full">
            <label>Observação do fechamento <span className="opt">opcional</span></label>
            <textarea rows={3} placeholder="Diferença de caixa, sangria, observações..." value={obs} onChange={e => setObs(e.target.value)}></textarea>
          </div>
        </div>
        <footer className="modal-foot">
          <button className="btn-ghost" onClick={onClose}>Cancelar</button>
          <div style={{ flex: 1 }}></div>
          <button className="btn-primary" onClick={() => onConfirm(obs)}><i className="fa-solid fa-lock"></i> Confirmar fechamento</button>
        </footer>
      </div>
    </div>
  );
};

const CaixaTab = ({ cashState, onToast }) => {
  const CF = window.IMEDTO_CLINIC_FINANCE;
  const _BRL = window.cfBRL;
  const cash = CF.cashDay;
  const [modalOpen, setModalOpen] = React.useState(false);
  const [localClosed, setLocalClosed] = React.useState(null); // {at, by, obs}

  const total = Object.values(cash.byMethod).reduce((s, v) => s + v, 0) - cash.estornos;
  const effectiveState = localClosed ? 'fechado' : cashState;

  const ResumoGrid = () => (
    <div className="cx-methods">
      <div className="cx-method"><span><i className="fa-solid fa-money-bill-wave"></i> Dinheiro</span><b>{_BRL(cash.byMethod.dinheiro)}</b></div>
      <div className="cx-method"><span><i className="fa-brands fa-pix"></i> PIX</span><b>{_BRL(cash.byMethod.pix)}</b></div>
      <div className="cx-method"><span><i className="fa-solid fa-credit-card"></i> Crédito</span><b>{_BRL(cash.byMethod.credito)}</b></div>
      <div className="cx-method"><span><i className="fa-regular fa-credit-card"></i> Débito</span><b>{_BRL(cash.byMethod.debito)}</b></div>
      <div className="cx-method refund"><span><i className="fa-solid fa-rotate-left"></i> Estornos</span><b>– {_BRL(cash.estornos)}</b></div>
      <div className="cx-method total"><span>Total do dia</span><b>{_BRL(total)}</b></div>
    </div>
  );

  if (effectiveState === 'nao-aberto') {
    return (
      <div className="cx-state-card empty">
        <div className="cx-state-ic"><i className="fa-solid fa-cash-register"></i></div>
        <b>Caixa ainda não aberto hoje</b>
        <p>Abra o caixa para começar a registrar os recebimentos do dia {cash.date}.</p>
        <button className="btn-primary" onClick={() => onToast('Caixa aberto às ' + new Date().toTimeString().slice(0, 5))}><i className="fa-solid fa-unlock"></i> Abrir caixa</button>
      </div>
    );
  }

  if (effectiveState === 'fechado') {
    const info = localClosed || { at: '18:42', by: 'Ana Souza (recepção)', obs: 'Sem diferença de caixa.' };
    return (
      <>
        <div className="cx-status closed">
          <div className="cx-status-l">
            <span className="cx-badge closed"><i className="fa-solid fa-lock"></i> Caixa fechado</span>
            <div className="cx-status-info">
              <b>{cash.date}</b>
              <span>Fechado por {info.by} · às {info.at}</span>
            </div>
          </div>
          <span className="cx-readonly"><i className="fa-solid fa-eye"></i> Somente leitura</span>
        </div>
        <div className="cf-card">
          <div className="cf-card-h"><div><i className="fa-solid fa-receipt"></i> Resumo do fechamento</div></div>
          <div style={{ padding: '16px 18px' }}><ResumoGrid /></div>
          {info.obs && <div className="cx-obs"><i className="fa-solid fa-comment"></i> {info.obs}</div>}
        </div>
      </>
    );
  }

  // aberto
  return (
    <>
      <div className="cx-status open">
        <div className="cx-status-l">
          <span className="cx-badge open"><span className="cx-dot"></span> Caixa aberto</span>
          <div className="cx-status-info">
            <b>{cash.date}</b>
            <span>Aberto às {cash.openedAt} por {cash.operator}</span>
          </div>
        </div>
        <button className="btn-primary" onClick={() => setModalOpen(true)}><i className="fa-solid fa-lock"></i> Fechar caixa</button>
      </div>
      <div className="cf-card">
        <div className="cf-card-h"><div><i className="fa-solid fa-coins"></i> Resumo do dia por forma de pagamento</div></div>
        <div style={{ padding: '16px 18px' }}><ResumoGrid /></div>
      </div>
      <FecharCaixaModal
        open={modalOpen}
        cash={cash}
        onClose={() => setModalOpen(false)}
        onConfirm={(obs) => {
          setModalOpen(false);
          setLocalClosed({ at: new Date().toTimeString().slice(0, 5), by: cash.operator, obs: obs.trim() || 'Sem observações.' });
          onToast('Caixa fechado com sucesso');
        }}
      />
    </>
  );
};

// ─── Tab: Comissões ──────────────────────────────────────────
const ComissoesTab = ({ onToast }) => {
  const CF = window.IMEDTO_CLINIC_FINANCE;
  const _BRL = window.cfBRL;
  const [expanded, setExpanded] = React.useState(null);
  const totalRepasse = CF.commissions.reduce((s, c) => s + c.repasse, 0);

  return (
    <div className="cf-card">
      <div className="cf-card-h">
        <div><i className="fa-solid fa-percent"></i> Comissões por profissional · Junho 2026</div>
        <div className="cf-total-pill">Total a repassar: <b>{_BRL(totalRepasse)}</b></div>
      </div>
      <div className="cm-table">
        <div className="cm-thead">
          <div>Profissional</div><div className="ta-c">Atendimentos</div><div className="ta-r">Faturamento</div><div className="ta-c">% comissão</div><div className="ta-r">A repassar</div><div></div>
        </div>
        {CF.commissions.map(c => {
          const open = expanded === c.id;
          return (
            <div key={c.id} className={`cm-block ${open ? 'open' : ''}`}>
              <div className="cm-row" onClick={() => setExpanded(open ? null : c.id)}>
                <div className="cm-pro">
                  <div className="cm-av">{c.name.split(' ').slice(0, 2).map(x => x[0]).join('').replace('.', '')}</div>
                  <div><b>{c.name}</b><span>{c.role}</span></div>
                </div>
                <div className="ta-c">{c.atendimentos}</div>
                <div className="ta-r cm-fat">{_BRL(c.faturamento)}</div>
                <div className="ta-c"><span className="cm-pct">{c.pct}%</span></div>
                <div className="ta-r cm-rep">{_BRL(c.repasse)}</div>
                <div className="ta-c"><i className={`fa-solid fa-chevron-${open ? 'up' : 'down'} cm-chev`}></i></div>
              </div>
              {open && (
                <div className="cm-detail">
                  <div className="cmd-head"><div>Data</div><div>Atendimento</div><div>Base</div><div className="ta-r">Faturamento</div><div className="ta-r">Comissão</div></div>
                  {c.detail.map((d, i) => (
                    <div key={i} className="cmd-row">
                      <div className="cmd-date">{d.date}</div>
                      <div><b>{d.proc}</b><span>{d.patient}</span></div>
                      <div><span className={`cm-base ${d.base.includes('orçamento') ? 'budget' : ''}`}>{d.base}</span></div>
                      <div className="ta-r">{_BRL(d.faturamento)}</div>
                      <div className="ta-r cm-rep">{_BRL(d.comissao)}</div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};

// ─── Tab: Configurações ──────────────────────────────────────
const ConfigTab = ({ onToast }) => {
  const CF = window.IMEDTO_CLINIC_FINANCE;
  const _BRL = window.cfBRL;
  const cfg = CF.config;
  const [fees, setFees] = React.useState(cfg.cardFees);
  const [priceDefault] = React.useState(cfg.priceTable.default);

  const toggleFee = (id) => setFees(fees.map(f => f.id === id ? { ...f, active: !f.active } : f));

  return (
    <div className="cfg-grid">
      {/* Taxas de cartão */}
      <div className="cf-card">
        <div className="cf-card-h"><div><i className="fa-solid fa-credit-card"></i> Taxa de cartão por forma de pagamento</div></div>
        <div className="cfg-list">
          {fees.map(f => (
            <div key={f.id} className={`cfg-fee ${!f.active ? 'inactive' : ''}`}>
              <span className="cfg-fee-name">{f.label}</span>
              {f.pct == null ? (
                <span className="cfg-fee-hint"><i className="fa-solid fa-triangle-exclamation"></i> taxa não configurada</span>
              ) : (
                <span className="cfg-fee-pct">{f.pct.toLocaleString('pt-BR', { minimumFractionDigits: 1 })}%</span>
              )}
              <button className={`cfg-switch ${f.active ? 'on' : ''}`} onClick={() => toggleFee(f.id)}><span className="knob"></span></button>
            </div>
          ))}
        </div>
      </div>

      {/* Tabela de preços */}
      <div className="cf-card">
        <div className="cf-card-h"><div><i className="fa-solid fa-tag"></i> Tabela de preços de consulta</div></div>
        <div className="cfg-price">
          <div className="cfg-price-default">
            <div><span>Valor padrão do estabelecimento</span><b>{_BRL(priceDefault)}</b></div>
            <button className="btn-secondary sm" onClick={() => onToast('Editar valor padrão')}><i className="fa-solid fa-pen"></i> Editar</button>
          </div>
          <div className="cfg-sub-title">Exceções por profissional</div>
          {cfg.priceTable.exceptions.map(ex => (
            <div key={ex.id} className="cfg-exc">
              <span><i className="fa-solid fa-user-doctor"></i> {ex.professional}</span>
              <b>{_BRL(ex.price)}</b>
              <button className="cfg-x" onClick={() => onToast('Remover exceção')}><i className="fa-solid fa-xmark"></i></button>
            </div>
          ))}
          <button className="cfg-add" onClick={() => onToast('Adicionar exceção por profissional')}><i className="fa-solid fa-plus"></i> Adicionar exceção</button>
        </div>
      </div>

      {/* Comissão por profissional */}
      <div className="cf-card cfg-span">
        <div className="cf-card-h"><div><i className="fa-solid fa-percent"></i> Comissão por profissional</div><span className="cfg-default-tag">Padrão do sistema: {cfg.commissionDefault}%</span></div>
        <div className="cfg-comm">
          {cfg.commissionByPro.map(p => (
            <div key={p.id} className="cfg-comm-row">
              <span className="cfg-comm-name"><i className="fa-solid fa-user-doctor"></i> {p.professional}</span>
              <div className="cfg-comm-val">
                <span className="cfg-comm-pct">{p.pct}%</span>
                {p.isDefault && <span className="cfg-padrao">padrão</span>}
              </div>
              <button className="btn-secondary sm" onClick={() => onToast(`Editar comissão de ${p.professional}`)}><i className="fa-solid fa-pen"></i></button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

Object.assign(window, { CaixaTab, ComissoesTab, ConfigTab });
