// AppointmentRow — uma linha de agendamento com ações contextuais por status

const ReadyPills = ({ ready }) => {
  const items = [
    { k: 'docs',  ok: ready.docs,  l: 'Documentos' },
    { k: 'forms', ok: ready.forms, l: 'Anamnese' },
    { k: 'copay', ok: ready.copay, l: 'Coparticipação' },
  ];
  return (
    <div className="ready-row">
      {items.map((it, i) => (
        <span key={i} className={`ready-pill ${it.ok ? 'ok' : 'miss'}`}>
          <i className={`fa-solid ${it.ok ? 'fa-check' : 'fa-circle-exclamation'}`}></i>
          {it.l}
        </span>
      ))}
    </div>
  );
};

// Estado financeiro da cobrança (cobrado ≠ pago)
const BRL = (cents) => (cents / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const billingState = (billing) => {
  if (!billing) return null;
  if (billing.kind === 'convenio') return { kind: 'convenio' };
  const net = billing.total - (billing.discount || 0);
  const paid = (billing.payments || []).reduce((s, p) => s + p.amount, 0);
  const saldo = net - paid;
  let state = 'aberta';
  if (saldo <= 0) state = 'paga';
  else if (paid > 0) state = 'parcial';
  return { kind: 'particular', state, net, paid, saldo, total: billing.total };
};

const PaymentBadge = ({ billing, onClick }) => {
  const b = billingState(billing);
  if (!b) return null;
  if (b.kind === 'convenio') {
    return (
      <button className="pay-badge convenio" onClick={onClick} title="Cobrança de convênio">
        <i className="fa-solid fa-id-card-clip"></i> Convênio
      </button>
    );
  }
  if (b.state === 'paga') {
    return (
      <button className="pay-badge paga" onClick={onClick} title="Pagamento concluído">
        <i className="fa-solid fa-circle-check"></i> Pago
      </button>
    );
  }
  if (b.state === 'parcial') {
    const pct = Math.round(b.paid / b.net * 100);
    return (
      <button className="pay-badge parcial" onClick={onClick} title="Parcialmente paga">
        <span className="pb-bar"><span className="pb-fill" style={{ width: pct + '%' }}></span></span>
        R$ {BRL(b.paid)} de R$ {BRL(b.net)}
      </button>
    );
  }
  return (
    <button className="pay-badge aberta" onClick={onClick} title="Cobrança em aberto">
      <i className="fa-solid fa-circle-dollar-to-slot"></i> A receber R$ {BRL(b.saldo)}
    </button>
  );
};

const minutesAgo = (hhmm) => {
  if (!hhmm) return null;
  const now = new Date();
  const [h, m] = hhmm.split(':').map(Number);
  // for the demo we treat "now" as 09:42 — so checked-in at 09:38 = 4 min ago
  const NOW_H = 9, NOW_M = 42;
  return (NOW_H * 60 + NOW_M) - (h * 60 + m);
};

const AppointmentRow = ({ appt, expanded, onToggle, onAction, statuses, tipos }) => {
  const status = statuses[appt.status];
  const tipo = tipos[appt.tipo];
  const accent = `hsl(${status.color})`;
  const tipoColor = `hsl(${tipo.color})`;
  const cls = ['appt', expanded && 'expanded', appt.status].filter(Boolean).join(' ');

  // Compute waiting time for checked-in
  const waitMin = appt.status === 'checked-in' ? minutesAgo(appt.arrivedAt) : null;
  const waitOver = waitMin !== null && waitMin > 15;

  // build primary actions per status
  const actions = (() => {
    switch (appt.status) {
      case 'confirmed':
        return [
          { i: 'fa-clipboard-check', l: 'Check-in', cta: true, solid: true, on: 'check-in' },
          { i: 'fa-rotate-right', l: '', tip: 'Reagendar', on: 'reschedule' },
          { i: 'fa-pen-to-square', l: '', tip: 'Editar', on: 'edit' },
        ];
      case 'unconfirmed':
        return [
          { i: 'fa-paper-plane', l: 'Reenviar lembrete', cta: true, solid: false, on: 'remind' },
          { i: 'fa-rotate-right', l: '', tip: 'Reagendar', on: 'reschedule' },
          { i: 'fa-pen-to-square', l: '', tip: 'Editar', on: 'edit' },
        ];
      case 'checked-in':
        return [
          { i: 'fa-pen-to-square', l: '', tip: 'Editar', on: 'edit' },
        ];
      case 'in-progress':
        return [
          { i: 'fa-flag-checkered', l: 'Finalizar', cta: true, solid: true, on: 'end' },
        ];
      case 'completed':
        return [
          { i: 'fa-pen-to-square', l: '', tip: 'Editar', on: 'edit' },
        ];
      case 'no-show':
        return [
          { i: 'fa-rotate-right', l: 'Reagendar', cta: true, solid: false, on: 'reschedule' },
          { i: 'fa-phone', l: '', tip: 'Contatar', on: 'call' },
        ];
      case 'cancelled':
        return [
          { i: 'fa-rotate-right', l: 'Reagendar', cta: true, solid: false, on: 'reschedule' },
          { i: 'fa-user-plus', l: '', tip: 'Encaixe', on: 'fill' },
        ];
      default:
        return [];
    }
  })();

  // status pill text + extra info
  let pillText = status.label;
  let extraTag = null;
  if (appt.status === 'completed' && appt.endedAt) {
    extraTag = <span className="pill p-muted"><i className="fa-solid fa-clock" style={{fontSize: 9, marginRight: 4}}></i>{appt.startedAt}–{appt.endedAt}</span>;
  } else if (appt.status === 'in-progress' && appt.startedAt) {
    const elapsed = minutesAgo(appt.startedAt);
    extraTag = <span className="pill p-purple"><i className="fa-solid fa-circle-play" style={{fontSize: 9, marginRight: 4}}></i>iniciado há {elapsed} min</span>;
  } else if (appt.status === 'checked-in' && waitMin !== null) {
    extraTag = <span className={`waiting-tag ${waitOver ? 'over' : ''}`}>
      <i className={`fa-solid ${waitOver ? 'fa-triangle-exclamation' : 'fa-clock'}`}></i>
      Aguardando há {waitMin} min
    </span>;
  } else if (appt.status === 'unconfirmed') {
    extraTag = <span className="pill p-warning"><i className="fa-brands fa-whatsapp" style={{fontSize: 10, marginRight: 4}}></i>lembrete enviado · sem resposta</span>;
  }

  return (
    <div className={cls} onClick={() => onToggle(appt.id)}>
      <div className="timecol">
        <div className="time">{appt.time}</div>
        <div className="duration"><i className="fa-regular fa-clock"></i>{appt.duration} min</div>
      </div>
      <div className="stripe" style={{ background: accent }}></div>
      <div className="body">
        <div className="head-row">
          <div className="av">{appt.patient.initials}</div>
          <span className="pat-name">{appt.patient.name}</span>
          <span className="age">{appt.patient.age} anos · {appt.patient.gender}</span>
          <span className={`pill ${status.pill}`}><span className="dot"></span>{pillText}</span>
          {extraTag}
          {appt.billing && (
            <PaymentBadge
              billing={appt.billing}
              onClick={(e) => { e.stopPropagation(); onAction(appt, 'payment'); }}
            />
          )}
        </div>
        <div className="meta-row">
          <span><i className={`fa-solid ${tipo.icon}`} style={{color: tipoColor}}></i>{tipo.label}</span>
          <span className="sep"></span>
          <span><i className="fa-solid fa-shield-halved"></i>{appt.convenio}{appt.plan !== '—' ? ` · ${appt.plan}` : ''}</span>
          <span className="sep"></span>
          <span><i className="fa-solid fa-notes-medical"></i>{appt.reason}</span>
        </div>
        {(appt.status === 'confirmed' || appt.status === 'checked-in' || appt.status === 'unconfirmed') && (
          <ReadyPills ready={appt.ready} />
        )}
        {expanded && (
          <div className="appt-detail">
            <div className="field"><b>Telefone</b><span>{appt.patient.phone}</span></div>
            <div className="field"><b>Convênio</b><span>{appt.convenio} {appt.plan !== '—' && `(${appt.plan})`}</span></div>
            <div className="field"><b>Última consulta</b><span>14/03/2025</span></div>
            <div className="field"><b>Lembrete</b><span>{
              appt.reminder === 'sent-confirmed' ? 'Confirmado pelo paciente' :
              appt.reminder === 'sent-pending' ? 'Enviado · aguardando resposta' :
              appt.reminder === 'sent-no-reply' ? 'Sem resposta após 24h' :
              appt.reminder === 'cancelled-by-patient' ? 'Cancelado pelo paciente' : '—'
            }</span></div>
            <div className="field"><b>Origem</b><span>Portal do paciente</span></div>
            <div className="field"><b>Tempo de atendimento</b><span>{appt.duration} min previstos</span></div>
            {appt.notes && <div className="notes"><b><i className="fa-solid fa-circle-info"></i> Observação:</b> {appt.notes}</div>}
          </div>
        )}
      </div>
      <div className="actions" onClick={(e) => e.stopPropagation()}>
        {actions.map((a, i) => a.cta ? (
          <button key={i} className={`btn-cta ${a.solid ? 'solid' : ''} ${a.success ? 'success' : ''}`} onClick={() => onAction(appt, a.on)}>
            <i className={`fa-solid ${a.i}`}></i>{a.l}
          </button>
        ) : (
          <button key={i} className="btn-icon-sm" title={a.tip} onClick={() => onAction(appt, a.on)}>
            <i className={`fa-solid ${a.i}`}></i>
          </button>
        ))}
        <button className="btn-icon-sm" title="Mais ações" onClick={(e) => { e.stopPropagation(); onAction(appt, 'more'); }}>
          <i className="fa-solid fa-ellipsis-vertical"></i>
        </button>
      </div>
    </div>
  );
};

window.AppointmentRow = AppointmentRow;
