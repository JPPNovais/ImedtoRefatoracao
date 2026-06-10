// PatientDetailExtra.jsx — Tabs adicionais: Anamnese, Orçamentos, Financeiro, Convênios, Termos, Anexos

const fmtDate3 = window.fmtDate2;
const fmtMoney3 = window.fmtMoney2;

// ─── Tab Anamnese ─────────────────────────────────────
const TabAnamnese = ({ detail }) => {
  const a = detail.anamnesis;
  return (
    <div>
      <div className="prontuario-head">
        <div>
          <h2>Anamnese e histórico</h2>
          <p>Última atualização: 30 de abril de 2026 · Dra. Renata Lopes</p>
        </div>
        <button className="btn-secondary"><i className="fa-solid fa-pen"></i> Editar anamnese</button>
      </div>
      <div className="anamn-grid">
        <div className="pd-card an-card">
          <h4><i className="fa-solid fa-droplet"></i> Dados biométricos</h4>
          <div className="an-row"><span>Tipo sanguíneo</span><b>{a.bloodType}</b></div>
          <div className="an-row"><span>Peso</span><b>{a.weight}</b></div>
          <div className="an-row"><span>Altura</span><b>{a.height}</b></div>
          <div className="an-row"><span>IMC</span><b>{a.bmi}</b></div>
        </div>

        <div className="pd-card an-card">
          <h4><i className="fa-solid fa-heart"></i> Hábitos de vida</h4>
          <div className="an-row"><span>Tabagismo</span><b>{a.smoker}</b></div>
          <div className="an-row"><span>Álcool</span><b>{a.alcohol}</b></div>
          <div className="an-row"><span>Atividade física</span><b>{a.activity}</b></div>
        </div>

        <div className="pd-card an-card">
          <h4><i className="fa-solid fa-people-roof"></i> História familiar</h4>
          <p style={{ fontSize: 13, color: 'var(--c-secondary)', lineHeight: 1.6, margin: 0 }}>{a.familyHistory}</p>
        </div>

        <div className="pd-card an-card">
          <h4><i className="fa-solid fa-scissors"></i> Cirurgias prévias</h4>
          <div className="an-list">
            {a.surgeries.map((s, i) => <div key={i} className="an-list-item">{s}</div>)}
          </div>
        </div>

        <div className="pd-card an-card">
          <h4><i className="fa-solid fa-pills"></i> Medicações em uso</h4>
          <div className="an-list">
            {a.meds.map((m, i) => <div key={i} className="an-list-item">{m}</div>)}
          </div>
        </div>

        <div className="pd-card an-card">
          <h4><i className="fa-solid fa-triangle-exclamation"></i> Alergias</h4>
          <div className="an-list">
            {a.allergies.map((al, i) => <div key={i} className="an-list-item danger">{al}</div>)}
          </div>
        </div>
      </div>
    </div>
  );
};

// ─── Tab Orçamentos ───────────────────────────────────
const TabOrcamentos = ({ detail }) => {
  const { BUDGET_STATUS } = window.IMEDTO_PATIENTS;
  return (
    <div>
      <div className="prontuario-head">
        <div>
          <h2>Orçamentos</h2>
          <p>{detail.budgets.length} orçamentos · {detail.budgets.filter(b => b.status === 'sent').length} aguardando aceite</p>
        </div>
        <button className="btn-primary"><i className="fa-solid fa-plus"></i> Novo orçamento</button>
      </div>
      <div className="budgets-list">
        {detail.budgets.map(b => {
          const st = BUDGET_STATUS[b.status];
          return (
            <div key={b.id} className="budget-card">
              <div className="bc-head">
                <div className="bc-title">
                  <span className="bc-num">{b.number}</span>
                  <span className="bc-name">{b.title}</span>
                  <span className="bc-meta">Criado em {fmtDate3(b.createdAt)} · válido até {fmtDate3(b.validUntil)} · {b.author}</span>
                </div>
                <div className="bc-status">
                  <span className={`pill ${st.pill}`} style={{ background: `color-mix(in srgb, ${st.color} 15%, white)`, color: st.color }}>{st.label}</span>
                  <span className="bc-money">{fmtMoney3(b.total)}</span>
                  {b.paid > 0 && b.paid < b.total && <span className="bc-paid partial">{fmtMoney3(b.paid)} pago</span>}
                  {b.paid === b.total && b.total > 0 && <span className="bc-paid"><i className="fa-solid fa-check"></i> Quitado</span>}
                </div>
              </div>
              <div className="bc-items">
                {b.items.map((it, i) => (
                  <div key={i} className="bc-item">
                    <span>{it.qty}× {it.name}</span>
                    <b>{fmtMoney3(it.total)}</b>
                  </div>
                ))}
              </div>
              <div className="bc-actions">
                <button className="btn-secondary"><i className="fa-solid fa-eye"></i> Ver</button>
                <button className="btn-secondary"><i className="fa-solid fa-paper-plane"></i> Reenviar</button>
                <button className="btn-secondary"><i className="fa-solid fa-pen"></i> Editar</button>
                <button className="btn-secondary"><i className="fa-solid fa-print"></i> Imprimir</button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

// ─── Tab Financeiro ───────────────────────────────────
const ORIGIN_META = {
  consulta:     { label: 'Consulta',     icon: 'fa-stethoscope', color: '254 56% 38%' },
  procedimento: { label: 'Procedimento', icon: 'fa-syringe',      color: '38 92% 42%' },
  cirurgia:     { label: 'Cirurgia',     icon: 'fa-hospital',     color: '0 84% 55%' },
};
const STATUS_META = {
  aberta:    { label: 'Aberta',             cls: 'warn' },
  parcial:   { label: 'Parcialmente paga',  cls: 'info' },
  paga:      { label: 'Paga',               cls: 'ok' },
  cancelada: { label: 'Cancelada',          cls: 'muted' },
};
const fBRL = (c) => 'R$ ' + (c / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const finMethodLabel = (id) => ({ pix: 'PIX', dinheiro: 'Dinheiro', credito: 'Cartão de crédito', debito: 'Cartão de débito' }[id] || id);
const finMethodIcon = (id) => ({ pix: 'fa-brands fa-pix', dinheiro: 'fa-solid fa-money-bill-wave', credito: 'fa-solid fa-credit-card', debito: 'fa-regular fa-credit-card' }[id] || 'fa-solid fa-circle');

const chargeState = (c) => {
  const net = c.total - (c.discount || 0);
  const paid = (c.payments || []).reduce((s, p) => s + p.amount, 0) - (c.refunds || []).reduce((s, r) => s + r.amount, 0);
  const saldo = net - paid;
  let status = 'aberta';
  if (c.cancelled) status = 'cancelada';
  else if (c.convenio) status = 'convenio';
  else if (saldo <= 0 && paid > 0) status = 'paga';
  else if (paid > 0) status = 'parcial';
  return { net, paid, saldo, status };
};

// ── Modal de confirmação de estorno ──
const EstornoModal = ({ open, payment, charge, onClose, onConfirm }) => {
  const [reason, setReason] = React.useState('');
  React.useEffect(() => { if (open) setReason(''); }, [open, payment && payment.id]);
  if (!open || !payment) return null;
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-estorno" onClick={e => e.stopPropagation()}>
        <header className="modal-head">
          <div className="est-head">
            <div className="est-icon"><i className="fa-solid fa-rotate-left"></i></div>
            <div>
              <h2>Estornar pagamento</h2>
              <span>Esta ação fica registrada no histórico — o pagamento original não é removido.</span>
            </div>
          </div>
          <button className="modal-close" onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>
        <div className="modal-body">
          <div className="est-summary">
            <div className="es-item"><span>Pagamento</span><b>{finMethodLabel(payment.method)}{payment.parcelas > 1 ? ` · ${payment.parcelas}x` : ''}</b></div>
            <div className="es-item"><span>Valor</span><b className="neg">{fBRL(payment.amount)}</b></div>
            <div className="es-item"><span>Registrado</span><b>{payment.date} · {payment.by}</b></div>
            <div className="es-item"><span>Cobrança</span><b>{charge.desc}</b></div>
          </div>
          <div className="field-group full">
            <label>Motivo do estorno <em>*</em></label>
            <textarea rows={3} autoFocus placeholder="Ex: cobrança duplicada, valor incorreto, desistência do paciente..."
              value={reason} onChange={e => setReason(e.target.value)}></textarea>
            <span className="field-hint"><i className="fa-solid fa-circle-info"></i> Obrigatório — fica auditado com seu usuário e data.</span>
          </div>
        </div>
        <footer className="modal-foot">
          <button className="btn-ghost" onClick={onClose}>Cancelar</button>
          <div style={{ flex: 1 }}></div>
          <button className="btn-primary danger" disabled={!reason.trim()} onClick={() => onConfirm(charge, payment, reason.trim())}>
            <i className="fa-solid fa-rotate-left"></i> Confirmar estorno
          </button>
        </footer>
      </div>
    </div>
  );
};

const ChargeCard = ({ charge, patient, expanded, onToggle, onPay, onEstorno, onReceipt }) => {
  const om = ORIGIN_META[charge.origin] || ORIGIN_META.consulta;
  const st = chargeState(charge);
  const sm = STATUS_META[st.status] || STATUS_META.aberta;
  const hasSaldo = !charge.convenio && st.saldo > 0 && !charge.cancelled;
  return (
    <div className={`charge-card ${expanded ? 'open' : ''}`}>
      <div className="cc-main" onClick={() => onToggle(charge.id)}>
        <div className="cc-origin" style={{ '--ot': `hsl(${om.color})` }}>
          <i className={`fa-solid ${om.icon}`}></i>
          <span>{om.label}</span>
        </div>
        <div className="cc-desc">
          <b>{charge.desc}{charge.budget ? ` · Orçamento ${charge.budget}` : ''}</b>
          <span>{charge.date}{charge.discount > 0 ? ` · desconto ${fBRL(charge.discount)}` : ''}</span>
        </div>
        <div className="cc-status">
          {charge.convenio
            ? <span className="cc-pill convenio"><i className="fa-solid fa-id-card-clip"></i> Convênio</span>
            : <span className={`cc-pill ${sm.cls}`}>{sm.label}</span>}
          {charge.convenio && !charge.guia && <span className="cc-guia-pend"><i className="fa-solid fa-triangle-exclamation"></i> sem guia</span>}
        </div>
        <div className="cc-amount">
          <b>{fBRL(charge.total)}</b>
          {!charge.convenio && st.saldo > 0 && st.paid > 0 && <span className="cc-saldo">saldo {fBRL(st.saldo)}</span>}
          {!charge.convenio && st.status === 'paga' && <span className="cc-ok"><i className="fa-solid fa-check"></i> quitada</span>}
        </div>
        <div className="cc-actions" onClick={e => e.stopPropagation()}>
          {hasSaldo && (
            <button className="btn-primary sm" onClick={() => onPay(charge)}>
              <i className="fa-solid fa-circle-dollar-to-slot"></i> Registrar pagamento
            </button>
          )}
          {charge.convenio && (
            <button className={`btn-${charge.guia ? 'secondary' : 'primary'} sm`} onClick={() => onPay(charge)}>
              <i className="fa-solid fa-file-medical"></i> {charge.guia ? 'Ver guia' : 'Registrar guia'}
            </button>
          )}
          <button className="cc-chev" onClick={() => onToggle(charge.id)}>
            <i className={`fa-solid fa-chevron-${expanded ? 'up' : 'down'}`}></i>
          </button>
        </div>
      </div>

      {expanded && (
        <div className="cc-detail">
          {/* Histórico de valor (cirurgia / orçamento) */}
          {charge.valueHistory && charge.valueHistory.length > 0 && (
            <div className="cc-block">
              <div className="ccb-title"><i className="fa-solid fa-clock-rotate-left"></i> Histórico de valor do orçamento</div>
              {charge.valueHistory.map((v, i) => (
                <div className="value-change" key={i}>
                  <span className="vc-from">{fBRL(v.from)}</span>
                  <i className="fa-solid fa-arrow-right"></i>
                  <span className="vc-to">{fBRL(v.to)}</span>
                  <span className="vc-meta">{v.by} · {v.date} · "{v.reason}"</span>
                </div>
              ))}
            </div>
          )}

          {/* Convênio: explica que não há pagamento de balcão */}
          {charge.convenio ? (
            <div className="cc-block">
              <div className="ccb-title"><i className="fa-solid fa-file-medical"></i> Guia / autorização</div>
              {charge.guia ? (
                <div className="guia-card">
                  <div className="guia-field"><span>Operadora</span><b>{charge.operadora}{charge.plano ? ` — ${charge.plano}` : ''}</b></div>
                  <div className="guia-field"><span>Nº da guia</span><b>{charge.guia.numero}</b></div>
                  <div className="guia-field"><span>Senha</span><b>{charge.guia.senha || '—'}</b></div>
                  <div className="guia-field"><span>Autorizado em</span><b>{charge.guia.data || '—'}</b></div>
                </div>
              ) : (
                <div className="guia-pend-inline">
                  <i className="fa-solid fa-triangle-exclamation"></i>
                  <div>
                    <b>Guia não preenchida</b>
                    <span>Registre o nº da guia para faturar esta cobrança ao convênio {charge.operadora}.</span>
                  </div>
                </div>
              )}
              <div className="cc-convenio-note">
                <i className="fa-solid fa-shield-halved"></i>
                Faturada ao convênio — sem pagamento de balcão. O repasse da operadora chega semanas depois.
              </div>
            </div>
          ) : (
            <div className="cc-block">
              <div className="ccb-title"><i className="fa-solid fa-receipt"></i> Pagamentos e estornos</div>
              {charge.payments.length === 0 && charge.refunds.length === 0 && (
                <div className="cc-empty-pay">Nenhum pagamento registrado ainda.</div>
              )}
              <div className="pay-ledger">
                {charge.payments.map(p => {
                  const refund = (charge.refunds || []).find(r => r.paymentId === p.id);
                  return (
                    <React.Fragment key={p.id}>
                      <div className={`ledger-row pay ${refund ? 'voided' : ''}`}>
                        <div className="lr-icon"><i className={finMethodIcon(p.method)}></i></div>
                        <div className="lr-info">
                          <b>{finMethodLabel(p.method)}{p.parcelas > 1 ? ` · ${p.parcelas}x` : ''}</b>
                          <span>{p.date} · {p.by}</span>
                        </div>
                        <div className="lr-amount">{fBRL(p.amount)}</div>
                        <div className="lr-acts">
                          {!refund ? (
                            <>
                              <button className="lr-btn" onClick={() => onReceipt(charge, p)} title="Emitir recibo"><i className="fa-solid fa-receipt"></i></button>
                              <button className="lr-btn danger" onClick={() => onEstorno(charge, p)} title="Estornar"><i className="fa-solid fa-rotate-left"></i></button>
                            </>
                          ) : (
                            <span className="lr-voided-tag">estornado</span>
                          )}
                        </div>
                      </div>
                      {refund && (
                        <div className="ledger-row refund">
                          <div className="lr-icon"><i className="fa-solid fa-rotate-left"></i></div>
                          <div className="lr-info">
                            <b>Estorno</b>
                            <span>{refund.date} · {refund.by} · "{refund.reason}"</span>
                          </div>
                          <div className="lr-amount">– {fBRL(refund.amount)}</div>
                          <div className="lr-acts"></div>
                        </div>
                      )}
                    </React.Fragment>
                  );
                })}
              </div>
              <div className="cc-saldo-line">
                <span>Saldo da cobrança</span>
                <b className={st.saldo <= 0 ? 'zero' : ''}>{fBRL(Math.max(0, st.saldo))}</b>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

const TabFinanceiro = ({ detail, patient, hasAccess = true, canDiscount = true }) => {
  const [charges, setCharges] = React.useState(detail.finance.charges);
  const [expandedId, setExpandedId] = React.useState('c2');
  const [payCharge, setPayCharge] = React.useState(null);
  const [estorno, setEstorno] = React.useState(null);
  const [toast, setToast] = React.useState(null);

  const flash = (msg) => { setToast(msg); setTimeout(() => setToast(null), 2600); };

  // Totais (em centavos) computados das cobranças
  const totals = React.useMemo(() => {
    let cobrado = 0, pago = 0, saldo = 0;
    charges.forEach(c => {
      if (c.cancelled || c.convenio) { if (c.cancelled) return; }
      const s = chargeState(c);
      if (!c.convenio) { cobrado += s.net; pago += s.paid; saldo += Math.max(0, s.saldo); }
    });
    return { cobrado, pago, saldo };
  }, [charges]);

  if (!hasAccess) {
    return (
      <div className="fin-restricted">
        <div className="fr-icon"><i className="fa-solid fa-lock"></i></div>
        <b>Acesso restrito</b>
        <p>O módulo financeiro do paciente contém dados sensíveis e exige permissão específica.<br />Solicite acesso ao administrador da clínica.</p>
        <span className="fr-audit"><i className="fa-solid fa-shield-halved"></i> Acessos a esta aba são auditados (LGPD)</span>
      </div>
    );
  }

  // Constrói o "appt" esperado pelo PaymentModal a partir da cobrança ativa
  const payAppt = payCharge ? {
    id: payCharge.id,
    patient: { name: patient ? patient.name : 'Paciente' },
    convenio: payCharge.convenio ? (payCharge.operadora || 'Convênio') : 'Particular',
    billing: {
      kind: payCharge.convenio ? 'convenio' : 'particular',
      total: payCharge.total, discount: payCharge.discount || 0, payments: payCharge.payments,
      operadora: payCharge.operadora, plano: payCharge.plano, guia: payCharge.guia,
    },
  } : null;
  const payOrigin = payCharge ? `${ORIGIN_META[payCharge.origin].label} · ${payCharge.desc} · ${payCharge.date}` : '';

  const handleRegister = (_appt, payment) => {
    const np = {
      id: 'p' + Date.now(), date: new Date().toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }) + ' ' + new Date().toTimeString().slice(0, 5),
      method: payment.method, amount: payment.amount, parcelas: payment.parcelas, by: 'Você (recepção)',
    };
    setCharges(prev => prev.map(c => c.id === payCharge.id ? { ...c, payments: [...c.payments, np] } : c));
    setPayCharge(prev => prev ? { ...prev, payments: [...prev.payments, np] } : prev);
    const after = payCharge.payments.reduce((s, p) => s + p.amount, 0) + payment.amount;
    flash(after >= (payCharge.total - (payCharge.discount || 0)) ? 'Pagamento registrado · cobrança quitada' : `Pagamento de ${fBRL(payment.amount)} registrado`);
  };
  const handleDiscount = (_appt, discountCents) => {
    setCharges(prev => prev.map(c => c.id === payCharge.id ? { ...c, discount: discountCents } : c));
    setPayCharge(prev => prev ? { ...prev, discount: discountCents } : prev);
  };
  const handleConfirmEstorno = (charge, payment, reason) => {
    const refund = { id: 'r' + Date.now(), date: new Date().toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }), amount: payment.amount, reason, by: 'Você (recepção)', paymentId: payment.id };
    setCharges(prev => prev.map(c => c.id === charge.id
      ? { ...c, refunds: [...(c.refunds || []), refund], payments: c.payments.map(p => p.id === payment.id ? { ...p, refunded: true } : p) }
      : c));
    setEstorno(null);
    flash(`Estorno de ${fBRL(payment.amount)} registrado`);
  };
  const handleSaveGuia = (_appt, guia) => {
    setCharges(prev => prev.map(c => c.id === payCharge.id ? { ...c, guia } : c));
    setPayCharge(prev => prev ? { ...prev, guia } : prev);
    flash('Guia registrada');
  };

  return (
    <div>
      <div className="prontuario-head">
        <div>
          <h2>Financeiro</h2>
          <p>Cobranças, pagamentos e recibos do paciente · <i className="fa-solid fa-shield-halved" style={{ fontSize: 11 }}></i> acesso auditado</p>
        </div>
      </div>

      <div className="fin-summary">
        <div className="pd-card fin-card">
          <span>Total cobrado</span>
          <b>{fBRL(totals.cobrado)}</b>
        </div>
        <div className="pd-card fin-card success">
          <span>Total pago</span>
          <b>{fBRL(totals.pago)}</b>
        </div>
        <div className="pd-card fin-card danger">
          <span>Saldo em aberto</span>
          <b>{fBRL(totals.saldo)}</b>
        </div>
      </div>

      {charges.length === 0 ? (
        <div className="fin-empty">
          <i className="fa-solid fa-receipt"></i>
          <b>Nenhuma movimentação financeira</b>
          <p>Cobranças aparecem aqui quando o paciente faz check-in particular ou aprova um orçamento.</p>
        </div>
      ) : (
        <div className="charge-list">
          {charges.map(c => (
            <ChargeCard key={c.id} charge={c} patient={patient}
              expanded={expandedId === c.id}
              onToggle={(id) => setExpandedId(prev => prev === id ? null : id)}
              onPay={(ch) => setPayCharge(ch)}
              onEstorno={(ch, p) => setEstorno({ charge: ch, payment: p })}
              onReceipt={(ch, p) => flash(`Recibo de ${fBRL(p.amount)} emitido (PDF)`)}
            />
          ))}
        </div>
      )}

      <PaymentModal
        open={!!payCharge}
        appt={payAppt}
        canDiscount={canDiscount}
        originLabel={payOrigin}
        onClose={() => setPayCharge(null)}
        onRegister={handleRegister}
        onDiscount={handleDiscount}
        onReceipt={() => { flash('Recibo emitido (PDF)'); setPayCharge(null); }}
        onSaveGuia={handleSaveGuia}
      />
      <EstornoModal
        open={!!estorno}
        payment={estorno && estorno.payment}
        charge={estorno && estorno.charge}
        onClose={() => setEstorno(null)}
        onConfirm={handleConfirmEstorno}
      />
      {toast && <div className="toast"><i className="fa-solid fa-circle-check"></i> {toast}</div>}
    </div>
  );
};

// ─── Tab Convênios ────────────────────────────────────
const TabConvenios = ({ detail, patient }) => {
  const convenios = (detail.insurance && detail.insurance.convenios) || [];
  return (
    <div>
      <div className="prontuario-head">
        <div>
          <h2>Convênios do paciente</h2>
          <p>Carteirinhas, autorizações e histórico de uso · um paciente pode ter mais de um convênio</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <a href="Convenios.html" className="btn-secondary"><i className="fa-solid fa-gear"></i> Gerenciar convênios</a>
          <button className="btn-primary"><i className="fa-solid fa-plus"></i> Adicionar carteirinha</button>
        </div>
      </div>

      {convenios.length === 0 ? (
        <div className="fin-empty">
          <i className="fa-solid fa-id-card-clip"></i>
          <b>Nenhum convênio cadastrado</b>
          <p>Adicione a carteirinha do paciente para faturar atendimentos por convênio.</p>
          <button className="btn-primary"><i className="fa-solid fa-plus"></i> Adicionar carteirinha</button>
        </div>
      ) : (
        <div className="cv-cards">
          {convenios.map(c => (
            <div key={c.id} className={`cv-card ${c.expired ? 'expired' : ''}`}>
              <div className="cv-card-top">
                <div className="cv-op">
                  <div className="cv-op-ic"><i className="fa-solid fa-shield-halved"></i></div>
                  <div>
                    <b>{c.operadora} <span className="cv-plano">{c.plano}</span></b>
                    <span>ANS {c.ans}{c.principal ? ' · principal' : ''}</span>
                  </div>
                </div>
                {c.expired
                  ? <span className="cv-validity expired"><i className="fa-solid fa-triangle-exclamation"></i> Vencida {c.validade}</span>
                  : <span className="cv-validity ok">Válida até {c.validade}</span>}
              </div>
              <div className="cv-number">
                <span>Carteirinha</span>
                <b>{c.carteirinha}</b>
              </div>
              {c.expired && (
                <div className="cv-alert">
                  <i className="fa-solid fa-circle-exclamation"></i>
                  Carteirinha vencida — atualize a validade antes de faturar novos atendimentos.
                </div>
              )}
              <div className="cv-hist">
                <div className="cv-hist-title">Histórico de uso</div>
                {c.historico.map((h, i) => (
                  <div key={i} className="cv-hist-row">
                    <span className="cv-hist-date">{h.date}</span>
                    <span className="cv-hist-desc">{h.desc}</span>
                    <span className={`cv-hist-st ${h.status}`}>{h.status === 'repassado' ? 'Repassado' : 'Faturado'}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Autorizações */}
      <div className="pd-card" style={{ marginTop: 16 }}>
        <div className="pd-card-head">
          <h3><i className="fa-solid fa-clipboard-check"></i> Histórico de autorizações</h3>
        </div>
        <div className="auth-list">
          {detail.insurance.authorizations.map(a => (
            <div key={a.id} className="auth-item">
              <div className={`auth-icon ${a.status}`}>
                <i className={`fa-solid ${a.status === 'approved' ? 'fa-check' : 'fa-xmark'}`}></i>
              </div>
              <div className="auth-info">
                <b>{a.proc}</b>
                <span>
                  {a.status === 'approved'
                    ? `Aprovado em ${fmtDate3(a.date)} · expira em ${a.expiresIn}`
                    : <>Negado em {fmtDate3(a.date)} · <span className="reason">{a.reason}</span></>}
                </span>
              </div>
              <button className="btn-secondary"><i className="fa-solid fa-file-lines"></i> Detalhes</button>
            </div>
          ))}
        </div>
      </div>

      {/* Em breve — partes avançadas do ciclo de convênio */}
      <div className="soon-section">
        <div className="soon-head">Ciclo de convênio — próximas entregas</div>
        <div className="soon-grid">
          <div className="soon-card">
            <span className="soon-badge"><i className="fa-solid fa-clock"></i> Em breve</span>
            <div className="soon-ic"><i className="fa-solid fa-hand-holding-dollar"></i></div>
            <b>Coparticipação do paciente</b>
            <p>Cobrança do valor parcial pago no balcão quando o plano exige coparticipação.</p>
          </div>
          <div className="soon-card">
            <span className="soon-badge"><i className="fa-solid fa-clock"></i> Em breve</span>
            <div className="soon-ic"><i className="fa-solid fa-scale-balanced"></i></div>
            <b>Conciliação do repasse</b>
            <p>Comparação entre o que a operadora pagou e o que foi faturado em cada lote.</p>
          </div>
          <div className="soon-card">
            <span className="soon-badge"><i className="fa-solid fa-clock"></i> Em breve</span>
            <div className="soon-ic"><i className="fa-solid fa-file-circle-xmark"></i></div>
            <b>Glosas</b>
            <p>Itens recusados pela operadora, com motivo, para análise e recurso.</p>
          </div>
        </div>
      </div>
    </div>
  );
};

// ─── Tab Termos ───────────────────────────────────────
const TabTermos = ({ detail }) => (
  <div>
    <div className="prontuario-head">
      <div>
        <h2>Termos e consentimentos</h2>
        <p>{detail.consents.length} termos assinados · todos válidos pela LGPD</p>
      </div>
      <button className="btn-primary"><i className="fa-solid fa-file-signature"></i> Solicitar novo termo</button>
    </div>
    <div className="consents-list">
      {detail.consents.map(c => (
        <div key={c.id} className="consent-item">
          <i className="fa-solid fa-circle-check"></i>
          <div className="consent-info">
            <b>{c.name}</b>
            <span>Assinado por {c.signer} em {fmtDate3(c.signedAt)} · {c.method}</span>
          </div>
          <button className="btn-secondary"><i className="fa-solid fa-eye"></i> Ver</button>
          <button className="btn-secondary"><i className="fa-solid fa-download"></i></button>
        </div>
      ))}
    </div>
  </div>
);

// ─── Tab Anexos ───────────────────────────────────────
const TabAnexos = ({ detail }) => {
  const folders = [...new Set(detail.attachments.map(a => a.folder))];
  const [folder, setFolder] = React.useState('all');
  const filtered = folder === 'all' ? detail.attachments : detail.attachments.filter(a => a.folder === folder);

  return (
    <div>
      <div className="prontuario-head">
        <div>
          <h2>Anexos e documentos</h2>
          <p>{detail.attachments.length} arquivos · organizados em {folders.length} pastas</p>
        </div>
        <button className="btn-primary"><i className="fa-solid fa-upload"></i> Enviar arquivo</button>
      </div>

      <div className="filter-pills" style={{ marginBottom: 16 }}>
        <button className={`fp ${folder === 'all' ? 'active' : ''}`} onClick={() => setFolder('all')}>Todos ({detail.attachments.length})</button>
        {folders.map(f => (
          <button key={f} className={`fp ${folder === f ? 'active' : ''}`} onClick={() => setFolder(f)}>
            <i className="fa-solid fa-folder"></i> {f} ({detail.attachments.filter(a => a.folder === f).length})
          </button>
        ))}
      </div>

      <div className="att-grid">
        {filtered.map(a => (
          <div key={a.id} className="att-card">
            <div className={`att-icon ${a.type}`}>
              <i className={`fa-solid ${a.type === 'pdf' ? 'fa-file-pdf' : 'fa-image'}`}></i>
            </div>
            <div className="att-info">
              <b>{a.name}</b>
              <span>{a.size} · {fmtDate3(a.uploadedAt)} · {a.uploadedBy}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

window.TabAnamnese = TabAnamnese;
window.TabOrcamentos = TabOrcamentos;
window.TabFinanceiro = TabFinanceiro;
window.TabConvenios = TabConvenios;
window.TabTermos = TabTermos;
window.TabAnexos = TabAnexos;
