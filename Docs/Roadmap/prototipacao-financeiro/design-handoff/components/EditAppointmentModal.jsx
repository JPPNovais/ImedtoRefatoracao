// EditAppointmentModal — edição + reagendamento em fluxo único

// Slots disponíveis por dia (mock). Cada data: { time: 'avail'|'busy' }
// Para o mock, vou gerar os slots a partir dos appts do dia (ocupados).
const ALL_SLOTS = [
  '08:00', '08:30', '09:00', '09:30', '10:00', '10:30', '11:00', '11:30',
  '14:00', '14:30', '15:00', '15:30', '16:00', '16:30', '17:00', '17:30', '18:00',
];

// Build a map of busy slots per date — for the demo, we mark some random slots as busy
// based on the day-of-month so it's deterministic.
const getBusySlots = (dateISO, currentTime) => {
  if (!dateISO) return new Set();
  const day = Number(dateISO.split('-')[2]);
  const seed = day * 7 % ALL_SLOTS.length;
  const busy = new Set();
  // Mark roughly 50-60% of slots as busy, deterministically
  for (let i = 0; i < ALL_SLOTS.length; i++) {
    if ((i + seed) % 7 < 4) busy.add(ALL_SLOTS[i]);
  }
  // Always allow current time slot (so it shows as the existing one)
  if (currentTime) busy.delete(currentTime);
  return busy;
};

const formatDateLabel = (iso) => {
  if (!iso) return '—';
  const [y, m, d] = iso.split('-');
  const dt = new Date(Number(y), Number(m) - 1, Number(d));
  return dt.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'short' });
};

const EditAppointmentModal = ({ open, appt, statuses, tipos, onClose, onSaved, focusReschedule }) => {
  // Estado do form — inicializado a partir do appt
  const [form, setForm] = React.useState(null);
  const [showCalendar, setShowCalendar] = React.useState(false);
  const [reschedReason, setReschedReason] = React.useState('');

  React.useEffect(() => {
    if (!appt || !open) return;
    // Compute appointment ISO date — for demo, use today
    const today = new Date();
    const iso = today.toISOString().slice(0, 10);
    setForm({
      // editable fields
      tipo: appt.tipo,
      provider: 'Dra. Renata Lopes — Cardiologia',
      duration: appt.duration,
      convenio: appt.convenio,
      plan: appt.plan === '—' ? '' : appt.plan,
      reason: appt.reason,
      notes: appt.notes || '',
      reminderWA: true, reminderSMS: false, reminderEmail: false,
      // schedule
      date: iso,
      origDate: iso,
      time: appt.time,
      origTime: appt.time,
      // notify on save
      notifyPatient: true,
    });
    setShowCalendar(!!focusReschedule);
    setReschedReason('');
  }, [appt, open, focusReschedule]);

  // ⚠️ Hooks devem vir SEMPRE antes de qualquer early return
  const busySlots = React.useMemo(
    () => getBusySlots(form?.date, form?.date === form?.origDate ? form?.origTime : null),
    [form?.date, form?.origDate, form?.origTime]
  );

  const nextDays = React.useMemo(() => {
    const out = [];
    const base = new Date();
    for (let i = 0; i < 14; i++) {
      const d = new Date(base);
      d.setDate(base.getDate() + i);
      out.push({
        iso: d.toISOString().slice(0, 10),
        label: d.toLocaleDateString('pt-BR', { weekday: 'short' }).slice(0, 3).toUpperCase(),
        day: d.getDate(),
        month: d.toLocaleDateString('pt-BR', { month: 'short' }),
        isToday: i === 0,
      });
    }
    return out;
  }, []);

  if (!open || !appt || !form) return null;

  const status = statuses[appt.status];
  const update = (k, v) => setForm({ ...form, [k]: v });

  const dateChanged = form.date !== form.origDate;
  const timeChanged = form.time !== form.origTime;
  const isReschedule = dateChanged || timeChanged;

  const canSave = form.reason.trim().length > 0 && (!isReschedule || reschedReason.length > 0);

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-edit" onClick={e => e.stopPropagation()}>
        <header className="modal-head edit-head">
          <div className="eh-patient">
            <div className="av">{appt.patient.initials}</div>
            <div>
              <h2>{appt.patient.name}</h2>
              <span className="eh-sub">
                <span className={`pill ${status.pill}`}><span className="dot"></span>{status.label}</span>
                <span className="eh-time"><i className="fa-solid fa-clock"></i> {appt.time} · {appt.duration} min</span>
              </span>
            </div>
          </div>
          <button className="modal-close" onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>

        <div className="modal-body">
          {/* SEÇÃO 1: Reagendamento */}
          <div className="edit-section">
            <div className="es-head">
              <div>
                <i className="fa-solid fa-calendar-day"></i>
                <b>Data e horário</b>
              </div>
              {!showCalendar && (
                <button className="btn-text" onClick={() => setShowCalendar(true)}>
                  <i className="fa-solid fa-rotate-right"></i> Reagendar
                </button>
              )}
            </div>

            {!showCalendar ? (
              <div className="current-slot">
                <div className="cs-time">
                  <span className="hh">{form.origTime}</span>
                  <span className="dur">{form.duration} min</span>
                </div>
                <div className="cs-info">
                  <b style={{ textTransform: 'capitalize' }}>{formatDateLabel(form.origDate)}</b>
                  <span>Mantém data e horário atual</span>
                </div>
              </div>
            ) : (
              <div className="resched-block">
                {isReschedule && (
                  <div className="from-to">
                    <div className="ft-side from">
                      <span className="lbl">De</span>
                      <b>{form.origTime}</b>
                      <span className="dt">{formatDateLabel(form.origDate)}</span>
                    </div>
                    <i className="fa-solid fa-arrow-right ft-arr"></i>
                    <div className="ft-side to">
                      <span className="lbl">Para</span>
                      <b>{form.time}</b>
                      <span className="dt">{formatDateLabel(form.date)}</span>
                    </div>
                    <button className="ft-undo" onClick={() => { update('date', form.origDate); update('time', form.origTime); }} title="Desfazer">
                      <i className="fa-solid fa-rotate-left"></i>
                    </button>
                  </div>
                )}

                <div className="day-strip">
                  {nextDays.map(d => (
                    <button
                      key={d.iso}
                      type="button"
                      className={`day-btn ${form.date === d.iso ? 'active' : ''}`}
                      onClick={() => update('date', d.iso)}
                    >
                      <span className="dow">{d.label}</span>
                      <span className="dn">{d.day}</span>
                      <span className="mo">{d.month}</span>
                    </button>
                  ))}
                </div>

                <div className="slots-info">
                  <span><i className="fa-solid fa-circle" style={{color: 'hsl(var(--success))', fontSize: 8}}></i> Vago</span>
                  <span><i className="fa-solid fa-circle" style={{color: 'hsl(var(--secondary) / 0.3)', fontSize: 8}}></i> Ocupado</span>
                  <span className="orig-mark"><i className="fa-solid fa-location-dot"></i> Horário atual</span>
                </div>

                <div className="time-slots">
                  {ALL_SLOTS.map(t => {
                    const isBusy = busySlots.has(t);
                    const isOriginal = t === form.origTime && form.date === form.origDate;
                    const isActive = form.time === t && form.date === form.date;
                    return (
                      <button
                        key={t}
                        type="button"
                        className={`slot ${isActive ? 'active' : ''} ${isBusy ? 'busy' : 'free'} ${isOriginal ? 'original' : ''}`}
                        disabled={isBusy}
                        onClick={() => !isBusy && update('time', t)}
                        title={isBusy ? 'Ocupado' : isOriginal ? 'Horário atual' : 'Disponível'}
                      >
                        {t}
                        {isOriginal && <i className="fa-solid fa-location-dot mark"></i>}
                        {isBusy && <i className="fa-solid fa-lock mark"></i>}
                      </button>
                    );
                  })}
                </div>

                {isReschedule && (
                  <div className="field-group full" style={{marginTop: 4}}>
                    <label>Motivo do reagendamento <em>*</em></label>
                    <select value={reschedReason} onChange={e => setReschedReason(e.target.value)}>
                      <option value="">Selecione um motivo</option>
                      <option value="paciente">Solicitação do paciente</option>
                      <option value="profissional">Profissional indisponível</option>
                      <option value="urgencia">Urgência clínica / encaixe</option>
                      <option value="convenio">Pendência com convênio</option>
                      <option value="erro">Erro de agendamento</option>
                      <option value="outro">Outro</option>
                    </select>
                  </div>
                )}
              </div>
            )}
          </div>

          {/* SEÇÃO 2: Detalhes do atendimento */}
          <div className="edit-section">
            <div className="es-head">
              <div>
                <i className="fa-solid fa-stethoscope"></i>
                <b>Detalhes do atendimento</b>
              </div>
            </div>

            <div className="form-grid">
              <div className="field-group">
                <label>Profissional</label>
                <select value={form.provider} onChange={e => update('provider', e.target.value)}>
                  <option>Dra. Renata Lopes — Cardiologia</option>
                  <option>Dr. Marcos Rocha — Cardiologia</option>
                  <option>Dr. Antônio Vieira — Cardiologia</option>
                  <option>Dra. Camila Reis — Clínica geral</option>
                </select>
              </div>

              <div className="field-group">
                <label>Tipo de atendimento</label>
                <select value={form.tipo} onChange={e => update('tipo', e.target.value)}>
                  <option value="consulta">Consulta</option>
                  <option value="retorno">Retorno</option>
                  <option value="primeira">Primeira vez</option>
                  <option value="exame">Exame</option>
                  <option value="procedimento">Procedimento</option>
                  <option value="teleconsulta">Teleconsulta</option>
                </select>
              </div>

              <div className="field-group">
                <label>Duração</label>
                <select value={form.duration} onChange={e => update('duration', Number(e.target.value))}>
                  <option value={15}>15 minutos</option>
                  <option value={20}>20 minutos</option>
                  <option value={30}>30 minutos</option>
                  <option value={45}>45 minutos</option>
                  <option value={60}>60 minutos</option>
                  <option value={90}>90 minutos</option>
                </select>
              </div>

              <div className="field-group">
                <label>Convênio</label>
                <select value={form.convenio} onChange={e => update('convenio', e.target.value)}>
                  <option>Particular</option>
                  <option>Bradesco Saúde</option>
                  <option>SulAmérica</option>
                  <option>Amil</option>
                  <option>Unimed</option>
                  <option>Hapvida / NotreDame</option>
                  <option>Porto Seguro Saúde</option>
                </select>
              </div>

              <div className="field-group full">
                <label>Plano <span className="opt">opcional</span></label>
                <input type="text" placeholder="Ex: Top Nacional" value={form.plan} onChange={e => update('plan', e.target.value)} />
              </div>

              <div className="field-group full">
                <label>Motivo da consulta <em>*</em></label>
                <input
                  type="text"
                  placeholder="Ex: Dor no peito ao esforço"
                  value={form.reason}
                  onChange={e => update('reason', e.target.value)}
                />
              </div>

              <div className="field-group full">
                <label>Observações internas <span className="opt">opcional</span></label>
                <textarea
                  rows={3}
                  placeholder="Notas internas sobre o atendimento..."
                  value={form.notes}
                  onChange={e => update('notes', e.target.value)}
                ></textarea>
              </div>

              <div className="field-group full reminder-row">
                <label>Lembrete automático</label>
                <div className="reminder-toggles">
                  <label className={`tg ${form.reminderWA ? 'on' : ''}`}>
                    <input type="checkbox" checked={form.reminderWA} onChange={e => update('reminderWA', e.target.checked)} />
                    <i className="fa-brands fa-whatsapp"></i> WhatsApp
                  </label>
                  <label className={`tg ${form.reminderSMS ? 'on' : ''}`}>
                    <input type="checkbox" checked={form.reminderSMS} onChange={e => update('reminderSMS', e.target.checked)} />
                    <i className="fa-solid fa-comment-sms"></i> SMS
                  </label>
                  <label className={`tg ${form.reminderEmail ? 'on' : ''}`}>
                    <input type="checkbox" checked={form.reminderEmail} onChange={e => update('reminderEmail', e.target.checked)} />
                    <i className="fa-solid fa-envelope"></i> E-mail
                  </label>
                </div>
              </div>
            </div>
          </div>

          {/* Notificação ao paciente — sempre no final */}
          <div className="notify-section">
            <label className={`notify-toggle ${form.notifyPatient ? 'on' : ''}`}>
              <input type="checkbox" checked={form.notifyPatient} onChange={e => update('notifyPatient', e.target.checked)} />
              <div className="nt-box"><i className="fa-solid fa-check"></i></div>
              <div className="nt-info">
                <b>Notificar o paciente sobre as alterações</b>
                <span>
                  {isReschedule
                    ? `Mensagem automática via WhatsApp informando o novo horário (${form.time} em ${formatDateLabel(form.date)}).`
                    : 'Mensagem automática via WhatsApp informando as alterações no agendamento.'}
                </span>
              </div>
            </label>
          </div>
        </div>

        <footer className="modal-foot">
          <button className="btn-ghost" onClick={onClose}>Cancelar</button>
          <div style={{ flex: 1 }}></div>
          <button
            className={`btn-primary ${isReschedule ? 'success' : ''}`}
            disabled={!canSave}
            onClick={() => onSaved(appt, form, { isReschedule, reschedReason, notifyPatient: form.notifyPatient })}
          >
            <i className={`fa-solid ${isReschedule ? 'fa-calendar-check' : 'fa-floppy-disk'}`}></i>
            {isReschedule ? 'Confirmar reagendamento' : 'Salvar alterações'}
          </button>
        </footer>
      </div>
    </div>
  );
};

window.EditAppointmentModal = EditAppointmentModal;
