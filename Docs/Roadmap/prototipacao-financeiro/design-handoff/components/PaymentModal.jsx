// PaymentModal — "Registrar pagamento" (cobrado ≠ pago)
// Histórico de pagamentos + novo pagamento (parcial / múltiplas formas) + desconto (com permissão).

const PAY_METHODS = [
  { id: 'pix',      label: 'PIX',               icon: 'fa-brands fa-pix' },
  { id: 'dinheiro', label: 'Dinheiro',          icon: 'fa-solid fa-money-bill-wave' },
  { id: 'credito',  label: 'Cartão de crédito', icon: 'fa-solid fa-credit-card' },
  { id: 'debito',   label: 'Cartão de débito',  icon: 'fa-regular fa-credit-card' },
];
const methodLabel = (id) => (PAY_METHODS.find(m => m.id === id) || {}).label || id;
const methodIcon = (id) => (PAY_METHODS.find(m => m.id === id) || {}).icon || 'fa-solid fa-circle';

const _BRL = (cents) => (cents / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const _parseCents = (str) => { const d = (str || '').replace(/\D/g, ''); return d ? parseInt(d, 10) : 0; };

const PaymentModal = ({ open, appt, canDiscount, creditFeeFor, originLabel, onClose, onRegister, onDiscount, onReceipt, onSaveGuia }) => {
  const feeFor = creditFeeFor || ((p) => p <= 1 ? 3.5 : p <= 6 ? 4.99 : 6.99);
  const [amountCents, setAmountCents] = React.useState(0);
  const [method, setMethod] = React.useState('pix');
  const [parcelas, setParcelas] = React.useState(1);
  const [error, setError] = React.useState('');
  const [editingDiscount, setEditingDiscount] = React.useState(false);
  const [discountCents, setDiscountCents] = React.useState(0);
  const [guia, setGuia] = React.useState({ numero: '', senha: '', data: '' });
  const [guiaSaved, setGuiaSaved] = React.useState(false);

  const billing = appt && appt.billing;
  const paidCents = billing && billing.payments ? billing.payments.reduce((s, p) => s + p.amount, 0) : 0;
  const netCents = billing ? billing.total - (billing.discount || 0) : 0;
  const saldoCents = netCents - paidCents;
  const isConvenio = billing && billing.kind === 'convenio';
  const isQuitado = !isConvenio && saldoCents <= 0;

  // Reset form quando abre / quando o saldo muda
  React.useEffect(() => {
    if (!open || !billing) return;
    if (isConvenio) {
      const g = billing.guia || {};
      setGuia({ numero: g.numero || '', senha: g.senha || '', data: g.data || '' });
      setGuiaSaved(!!billing.guia);
      return;
    }
    setAmountCents(saldoCents > 0 ? saldoCents : 0);
    setMethod('pix');
    setParcelas(1);
    setError('');
    setEditingDiscount(false);
    setDiscountCents(billing.discount || 0);
  }, [open, appt && appt.id, paidCents, billing && billing.discount]);

  if (!open || !appt) return null;

  const creditFee = method === 'credito' ? feeFor(parcelas) : 0;
  const netReceive = method === 'credito' ? Math.round(amountCents * (1 - creditFee / 100)) : amountCents;

  const handleRegister = () => {
    if (amountCents <= 0) { setError('Informe um valor maior que zero.'); return; }
    if (amountCents > saldoCents) { setError('O valor excede o saldo da cobrança.'); return; }
    setError('');
    onRegister(appt, {
      method, amount: amountCents,
      parcelas: method === 'credito' ? parcelas : 1,
      fee: creditFee, netReceive,
    });
  };

  const applyDiscount = () => {
    onDiscount(appt, discountCents);
    setEditingDiscount(false);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-payment" onClick={e => e.stopPropagation()}>
        {/* Header */}
        <header className="modal-head pay-head">
          <div className="ph-left">
            <div className="ph-title">
              <i className="fa-solid fa-hand-holding-dollar"></i>
              <h2>Registrar pagamento</h2>
            </div>
            <div className="ph-origin">
              <b>{appt.patient.name}</b>
              <span>{originLabel || 'Consulta · 10/06 · Dra. Renata Lopes'}</span>
            </div>
          </div>
          <button className="modal-close" onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>

        {isConvenio ? (
          /* ── Convênio: guia/autorização (sem pagamento de balcão) ── */
          <div className="modal-body">
            <div className="guia-banner">
              <i className="fa-solid fa-shield-halved"></i>
              <div>
                <b>Atendimento por convênio · {billing.operadora || appt.convenio}{billing.plano ? ` — ${billing.plano}` : ''}</b>
                <span>Sem pagamento de balcão — faturado à operadora. Registre a guia para enviar à conta.</span>
              </div>
            </div>

            {!guiaSaved && (
              <div className="guia-pend">
                <i className="fa-solid fa-triangle-exclamation"></i>
                Guia não preenchida — a cobrança fica pendente de faturamento até o registro.
              </div>
            )}

            <div className="charge-summary" style={{ marginTop: 14 }}>
              <div className="cs-row"><span>Valor faturado</span><b>R$ {_BRL(billing.total)}</b></div>
            </div>

            <div className="pay-section">
              <div className="es-head"><div><i className="fa-solid fa-file-medical"></i><b>Guia / autorização</b></div></div>
              <div className="form-grid">
                <div className="field-group">
                  <label>Nº da guia <em>*</em></label>
                  <input type="text" placeholder="Ex: 48291" value={guia.numero}
                    onChange={e => { setGuia({ ...guia, numero: e.target.value }); setGuiaSaved(false); }} />
                </div>
                <div className="field-group">
                  <label>Senha de autorização</label>
                  <input type="text" placeholder="Ex: AUT-99213" value={guia.senha}
                    onChange={e => { setGuia({ ...guia, senha: e.target.value }); setGuiaSaved(false); }} />
                </div>
                <div className="field-group">
                  <label>Data de autorização</label>
                  <input type="text" placeholder="DD/MM/AAAA" value={guia.data}
                    onChange={e => { setGuia({ ...guia, data: e.target.value }); setGuiaSaved(false); }} />
                </div>
              </div>
              {guiaSaved && (
                <div className="guia-ok"><i className="fa-solid fa-circle-check"></i> Guia registrada · cobrança pronta para faturamento ao convênio.</div>
              )}
            </div>
          </div>
        ) : (
          <div className="modal-body">
            {/* Resumo da cobrança */}
            <div className="charge-summary">
              <div className="cs-row">
                <span>Valor cobrado</span>
                <b>R$ {_BRL(billing.total)}</b>
              </div>
              {(billing.discount > 0 || editingDiscount || canDiscount) && (
                <div className="cs-row discount">
                  <span>
                    Desconto
                    {canDiscount && !editingDiscount && (
                      <button className="cs-edit" onClick={() => { setEditingDiscount(true); setDiscountCents(billing.discount || 0); }}>
                        <i className="fa-solid fa-pen"></i>
                      </button>
                    )}
                  </span>
                  {editingDiscount ? (
                    <div className="discount-edit">
                      <div className="value-input sm">
                        <span className="vi-prefix">R$</span>
                        <input type="text" inputMode="numeric" autoFocus
                          value={discountCents === 0 ? '' : _BRL(discountCents)}
                          placeholder="0,00"
                          onChange={e => setDiscountCents(_parseCents(e.target.value))} />
                      </div>
                      <button className="btn-mini ok" onClick={applyDiscount}><i className="fa-solid fa-check"></i></button>
                      <button className="btn-mini" onClick={() => setEditingDiscount(false)}><i className="fa-solid fa-xmark"></i></button>
                    </div>
                  ) : (
                    <b className="neg">{billing.discount > 0 ? `– R$ ${_BRL(billing.discount)}` : '—'}</b>
                  )}
                </div>
              )}
              <div className="cs-row total">
                <span>Saldo restante</span>
                <b className={isQuitado ? 'zero' : ''}>R$ {_BRL(Math.max(0, saldoCents))}</b>
              </div>
            </div>

            {/* Histórico de pagamentos */}
            {billing.payments.length > 0 && (
              <div className="pay-section">
                <div className="es-head"><div><i className="fa-solid fa-clock-rotate-left"></i><b>Pagamentos registrados</b></div></div>
                <div className="pay-history">
                  {billing.payments.map(p => (
                    <div className="ph-item" key={p.id}>
                      <div className="phi-method"><i className={methodIcon(p.method)}></i></div>
                      <div className="phi-info">
                        <b>{methodLabel(p.method)}{p.parcelas > 1 ? ` · ${p.parcelas}x` : ''}</b>
                        <span>{p.date} · {p.by}</span>
                      </div>
                      <div className="phi-amount">R$ {_BRL(p.amount)}</div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Quitado: estado de sucesso */}
            {isQuitado ? (
              <div className="pay-success">
                <div className="ps-icon"><i className="fa-solid fa-circle-check"></i></div>
                <div className="ps-text">
                  <b>Cobrança quitada</b>
                  <span>Saldo zerado · total recebido R$ {_BRL(paidCents)}</span>
                </div>
              </div>
            ) : (
              /* Form de novo pagamento */
              <div className="pay-section">
                <div className="es-head"><div><i className="fa-solid fa-plus"></i><b>Novo pagamento</b></div></div>

                <div className="field-group full" style={{ marginBottom: 14 }}>
                  <label>Forma de pagamento</label>
                  <div className="method-grid">
                    {PAY_METHODS.map(m => (
                      <button key={m.id} type="button"
                        className={`method-chip ${method === m.id ? 'active' : ''}`}
                        onClick={() => setMethod(m.id)}>
                        <i className={m.icon}></i> {m.label}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="form-grid">
                  <div className="field-group">
                    <label>Valor <em>*</em></label>
                    <div className={`value-input ${error ? 'error' : ''}`}>
                      <span className="vi-prefix">R$</span>
                      <input type="text" inputMode="numeric"
                        value={amountCents === 0 ? '' : _BRL(amountCents)}
                        placeholder="0,00"
                        onChange={e => { setAmountCents(_parseCents(e.target.value)); setError(''); }} />
                    </div>
                    {error
                      ? <span className="field-error"><i className="fa-solid fa-circle-exclamation"></i> {error}</span>
                      : <span className="field-hint"><i className="fa-solid fa-circle-info"></i> Saldo: R$ {_BRL(saldoCents)}</span>}
                  </div>

                  {method === 'credito' && (
                    <div className="field-group">
                      <label>Parcelas</label>
                      <select value={parcelas} onChange={e => setParcelas(Number(e.target.value))}>
                        {Array.from({ length: 12 }, (_, i) => i + 1).map(n => (
                          <option key={n} value={n}>{n}x de R$ {_BRL(Math.round(amountCents / n))}</option>
                        ))}
                      </select>
                    </div>
                  )}
                </div>

                {method === 'credito' && (
                  <div className="fee-note">
                    <i className="fa-solid fa-circle-info"></i>
                    Taxa {creditFee.toLocaleString('pt-BR', { minimumFractionDigits: 1 })}% — você recebe <b>R$ {_BRL(netReceive)}</b>
                    <span className="fee-tag">informativo</span>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        <footer className="modal-foot">
          <button className="btn-ghost" onClick={onClose}>Fechar</button>
          <div style={{ flex: 1 }}></div>
          {isConvenio ? (
            <button className="btn-primary" disabled={!guia.numero.trim()} onClick={() => { setGuiaSaved(true); onSaveGuia && onSaveGuia(appt, guia); }}>
              <i className="fa-solid fa-floppy-disk"></i> {guiaSaved ? 'Guia registrada' : 'Registrar guia'}
            </button>
          ) : isQuitado ? (
            <button className="btn-primary" onClick={() => onReceipt(appt)}>
              <i className="fa-solid fa-receipt"></i> Emitir recibo
            </button>
          ) : (
            <button className="btn-primary success" onClick={handleRegister}>
              <i className="fa-solid fa-circle-check"></i> Registrar pagamento
            </button>
          )}
        </footer>
      </div>
    </div>
  );
};

window.PaymentModal = PaymentModal;
