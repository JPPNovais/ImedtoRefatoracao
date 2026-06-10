// NewAppointmentModal — fluxo completo de criação de agendamento

// Pacientes existentes mockados (para a busca)
const KNOWN_PATIENTS = [
  { id: 'p1', name: 'Ana Carolina Silva', initials: 'AC', doc: '123.456.789-00', phone: '(11) 98765-4321', age: 34, gender: 'F', last: '14/03/2025', convenio: 'Bradesco Saúde', plan: 'Top Nacional' },
  { id: 'p2', name: 'Roberto Mendes', initials: 'RM', doc: '987.654.321-00', phone: '(11) 99887-1234', age: 52, gender: 'M', last: '02/01/2025', convenio: 'Particular', plan: '—' },
  { id: 'p3', name: 'Juliana Faria Costa', initials: 'JF', doc: '456.789.123-00', phone: '(11) 97654-3210', age: 28, gender: 'F', last: '—', convenio: 'SulAmérica', plan: 'Especial 200' },
  { id: 'p4', name: 'Pedro Henrique Costa', initials: 'PH', doc: '321.654.987-00', phone: '(11) 96543-2109', age: 41, gender: 'M', last: '20/02/2025', convenio: 'Amil', plan: 'Linha Azul' },
  { id: 'p5', name: 'Marina Souza Pereira', initials: 'MS', doc: '654.321.987-00', phone: '(11) 95432-1098', age: 36, gender: 'F', last: '08/04/2025', convenio: 'Unimed', plan: 'Beta' },
  { id: 'p6', name: 'Beatriz Lima Oliveira', initials: 'BL', doc: '789.123.456-00', phone: '(11) 94321-0987', age: 29, gender: 'F', last: '—', convenio: 'Bradesco Saúde', plan: 'Top Nacional' },
];

// Slots disponíveis mockados
const HORARIOS_DISPONIVEIS = [
  '08:00', '08:30', '09:00', '09:30', '10:00', '10:30', '11:00',
  '14:00', '14:30', '15:00', '15:30', '16:00', '16:30', '17:00', '17:30',
];

const formatDocOrPhone = (s) => s; // sem máscara — só validação simples na demo

// ─── Step 1: Paciente ───────────────────────────────
const PatientStep = ({ patient, setPatient, mode, setMode, newPatient, setNewPatient }) => {
  const [query, setQuery] = React.useState('');
  const filtered = React.useMemo(() => {
    if (!query.trim()) return KNOWN_PATIENTS.slice(0, 5);
    const q = query.toLowerCase();
    return KNOWN_PATIENTS.filter(p =>
      p.name.toLowerCase().includes(q) ||
      p.doc.replace(/\D/g, '').includes(q.replace(/\D/g, '')) ||
      p.phone.includes(q)
    );
  }, [query]);

  if (mode === 'new') {
    return (
      <div className="modal-step">
        <div className="step-info">
          <i className="fa-solid fa-user-plus"></i>
          <div>
            <b>Cadastro rápido de paciente</b>
            <span>Preencha os dados essenciais agora — o cadastro completo pode ser feito depois.</span>
          </div>
        </div>

        <div className="form-grid">
          <div className="field-group full">
            <label>Nome completo <em>*</em></label>
            <input
              type="text"
              placeholder="Ex: Carla Mendes Souza"
              value={newPatient.name}
              onChange={e => setNewPatient({ ...newPatient, name: e.target.value })}
              autoFocus
            />
          </div>

          <div className="field-group">
            <label>Documento (CPF) <em>*</em></label>
            <input
              type="text"
              placeholder="000.000.000-00"
              value={newPatient.doc}
              onChange={e => setNewPatient({ ...newPatient, doc: e.target.value })}
            />
          </div>

          <div className="field-group">
            <label>Telefone <em>*</em></label>
            <input
              type="text"
              placeholder="(11) 99999-9999"
              value={newPatient.phone}
              onChange={e => setNewPatient({ ...newPatient, phone: e.target.value })}
            />
          </div>

          <div className="field-group">
            <label>Data de nascimento <span className="opt">opcional</span></label>
            <input
              type="text"
              placeholder="DD/MM/AAAA"
              value={newPatient.birth || ''}
              onChange={e => setNewPatient({ ...newPatient, birth: e.target.value })}
            />
          </div>

          <div className="field-group">
            <label>Sexo <span className="opt">opcional</span></label>
            <select value={newPatient.gender || ''} onChange={e => setNewPatient({ ...newPatient, gender: e.target.value })}>
              <option value="">Selecione</option>
              <option value="F">Feminino</option>
              <option value="M">Masculino</option>
              <option value="O">Outro</option>
            </select>
          </div>
        </div>

        <div className="quick-info">
          <i className="fa-solid fa-circle-info"></i>
          O cadastro completo (endereço, convênios, alergias, histórico) poderá ser concluído pelo paciente
          ao chegar na clínica ou pela secretaria depois.
        </div>

        <button type="button" className="link-back" onClick={() => setMode('search')}>
          <i className="fa-solid fa-arrow-left"></i> Voltar para busca de paciente
        </button>
      </div>
    );
  }

  return (
    <div className="modal-step patient-step">
      <div className="search-patient">
        <i className="fa-solid fa-magnifying-glass"></i>
        <input
          type="text"
          placeholder="Buscar por nome, CPF ou telefone..."
          value={query}
          onChange={e => setQuery(e.target.value)}
          autoFocus
        />
        {query && <button className="clr" onClick={() => setQuery('')}><i className="fa-solid fa-xmark"></i></button>}
      </div>

      <div className="patient-list">
        {filtered.length === 0 ? (
          <div className="no-patient">
            <i className="fa-solid fa-user-slash"></i>
            <b>Nenhum paciente encontrado</b>
            <span>"{query}" não corresponde a nenhum paciente cadastrado.</span>
            <button className="btn-primary sm" onClick={() => { setNewPatient({ ...newPatient, name: query.match(/[a-zA-ZÀ-ÿ ]/) ? query : '' }); setMode('new'); }}>
              <i className="fa-solid fa-user-plus"></i> Cadastrar novo paciente
            </button>
          </div>
        ) : (
          filtered.map(p => (
            <button
              key={p.id}
              type="button"
              className={`patient-card ${patient?.id === p.id ? 'selected' : ''}`}
              onClick={() => setPatient(p)}
            >
              <div className="av">{p.initials}</div>
              <div className="info">
                <b>{p.name}</b>
                <span>{p.age} anos · {p.gender === 'F' ? 'Feminino' : 'Masculino'}</span>
                <span className="meta">
                  <i className="fa-solid fa-id-card"></i> {p.doc}
                  <span className="dotsep"></span>
                  <i className="fa-solid fa-phone"></i> {p.phone}
                </span>
                {p.last !== '—' ? (
                  <span className="last"><i className="fa-solid fa-clock-rotate-left"></i> Última consulta em {p.last}</span>
                ) : (
                  <span className="last new"><i className="fa-solid fa-star"></i> Primeira consulta</span>
                )}
              </div>
              {patient?.id === p.id && <i className="fa-solid fa-check-circle check"></i>}
            </button>
          ))
        )}
      </div>

      {filtered.length > 0 && (
        <div className="add-new-sticky">
          <button type="button" className="add-new-btn" onClick={() => setMode('new')}>
            <i className="fa-solid fa-user-plus"></i>
            <div>
              <b>Cadastrar novo paciente</b>
              <span>Cadastro rápido com nome, documento e telefone</span>
            </div>
            <i className="fa-solid fa-chevron-right arr"></i>
          </button>
        </div>
      )}
    </div>
  );
};

// ─── Step 2: Detalhes ────────────────────────────────
const DetailsStep = ({ details, setDetails, patient }) => {
  const updateField = (k, v) => setDetails({ ...details, [k]: v });
  const wait = !!details.waitlist;

  return (
    <div className="modal-step">
      <div className="patient-pinned">
        <div className="av">{patient.initials || (patient.name?.split(' ').map(s=>s[0]).slice(0,2).join('').toUpperCase())}</div>
        <div className="info">
          <b>{patient.name}</b>
          <span>{patient.doc} · {patient.phone}</span>
        </div>
      </div>

      {/* Toggle: Lista de espera */}
      <button
        type="button"
        className={`waitlist-toggle ${wait ? 'on' : ''}`}
        onClick={() => updateField('waitlist', !wait)}
      >
        <div className="wt-icon">
          <i className={`fa-solid ${wait ? 'fa-hourglass-half' : 'fa-calendar-check'}`}></i>
        </div>
        <div className="wt-info">
          <b>{wait ? 'Adicionar à lista de espera' : 'Agendar para data/horário específico'}</b>
          <span>
            {wait
              ? 'O paciente aguardará um encaixe — data, horário e duração ficam opcionais.'
              : 'Sem horário disponível? Marque para colocar na lista de espera.'}
          </span>
        </div>
        <div className={`wt-switch ${wait ? 'on' : ''}`}>
          <span className="knob"></span>
        </div>
      </button>

      <div className="form-grid">
        <div className="field-group">
          <label>Profissional <em>*</em></label>
          <select value={details.provider} onChange={e => updateField('provider', e.target.value)}>
            <option>Dra. Renata Lopes — Cardiologia</option>
            <option>Dr. Marcos Rocha — Cardiologia</option>
            <option>Dr. Antônio Vieira — Cardiologia</option>
            <option>Dra. Camila Reis — Clínica geral</option>
          </select>
        </div>

        <div className="field-group">
          <label>Tipo de atendimento <em>*</em></label>
          <select value={details.tipo} onChange={e => updateField('tipo', e.target.value)}>
            <option value="consulta">Consulta</option>
            <option value="retorno">Retorno</option>
            <option value="primeira">Primeira vez</option>
            <option value="exame">Exame</option>
            <option value="procedimento">Procedimento</option>
            <option value="teleconsulta">Teleconsulta</option>
          </select>
        </div>

        <div className="field-group">
          <label>Data {wait ? <span className="opt">opcional</span> : <em>*</em>}</label>
          <input type="date" value={details.date} onChange={e => updateField('date', e.target.value)} />
        </div>

        <div className="field-group">
          <label>Duração {wait ? <span className="opt">opcional</span> : <em>*</em>}</label>
          <select value={details.duration} onChange={e => updateField('duration', Number(e.target.value))}>
            <option value={15}>15 minutos</option>
            <option value={20}>20 minutos</option>
            <option value={30}>30 minutos</option>
            <option value={45}>45 minutos</option>
            <option value={60}>60 minutos</option>
            <option value={90}>90 minutos</option>
          </select>
        </div>

        <div className="field-group full">
          <label>Horário disponível {wait ? <span className="opt">opcional</span> : <em>*</em>}</label>
          <div className="time-slots">
            {HORARIOS_DISPONIVEIS.map(t => (
              <button
                key={t}
                type="button"
                className={`slot ${details.time === t ? 'active' : ''}`}
                onClick={() => updateField('time', t)}
              >
                {t}
              </button>
            ))}
          </div>
        </div>

        {wait && (
          <div className="field-group full">
            <label>Preferência de período <span className="opt">opcional</span></label>
            <div className="period-prefs">
              {[
                { v: 'manha', l: 'Manhã', i: 'fa-sun' },
                { v: 'tarde', l: 'Tarde', i: 'fa-cloud-sun' },
                { v: 'qualquer', l: 'Qualquer horário', i: 'fa-clock' },
              ].map(p => (
                <button
                  key={p.v}
                  type="button"
                  className={`p-pref ${details.periodPref === p.v ? 'active' : ''}`}
                  onClick={() => updateField('periodPref', p.v)}
                >
                  <i className={`fa-solid ${p.i}`}></i> {p.l}
                </button>
              ))}
            </div>
          </div>
        )}

        {wait && (
          <div className="field-group full">
            <label>Urgência <span className="opt">opcional</span></label>
            <div className="urgency-row">
              {[
                { v: 'rotina', l: 'Rotina', c: 'success' },
                { v: 'priori', l: 'Prioritário', c: 'warning' },
                { v: 'urgente', l: 'Urgente', c: 'error' },
              ].map(u => (
                <button
                  key={u.v}
                  type="button"
                  className={`urg ${u.c} ${details.urgency === u.v ? 'active' : ''}`}
                  onClick={() => updateField('urgency', u.v)}
                >
                  <span className="d"></span> {u.l}
                </button>
              ))}
            </div>
          </div>
        )}

        <div className="field-group">
          <label>Convênio <em>*</em></label>
          <select value={details.convenio} onChange={e => updateField('convenio', e.target.value)}>
            <option>Particular</option>
            <option>Bradesco Saúde</option>
            <option>SulAmérica</option>
            <option>Amil</option>
            <option>Unimed</option>
            <option>Hapvida / NotreDame</option>
            <option>Porto Seguro Saúde</option>
          </select>
        </div>

        <div className="field-group">
          <label>Plano <span className="opt">opcional</span></label>
          <input type="text" placeholder="Ex: Top Nacional" value={details.plan} onChange={e => updateField('plan', e.target.value)} />
        </div>

        <div className="field-group full">
          <label>Motivo da consulta <em>*</em></label>
          <input
            type="text"
            placeholder="Ex: Dor no peito ao esforço"
            value={details.reason}
            onChange={e => updateField('reason', e.target.value)}
          />
        </div>

        <div className="field-group full">
          <label>Observações <span className="opt">opcional</span></label>
          <textarea
            rows={3}
            placeholder="Notas internas sobre o atendimento..."
            value={details.notes}
            onChange={e => updateField('notes', e.target.value)}
          ></textarea>
        </div>

        <div className="field-group full reminder-row">
          <label>Lembrete automático</label>
          <div className="reminder-toggles">
            <label className={`tg ${details.reminderWA ? 'on' : ''}`}>
              <input type="checkbox" checked={details.reminderWA} onChange={e => updateField('reminderWA', e.target.checked)} />
              <i className="fa-brands fa-whatsapp"></i> WhatsApp
            </label>
            <label className={`tg ${details.reminderSMS ? 'on' : ''}`}>
              <input type="checkbox" checked={details.reminderSMS} onChange={e => updateField('reminderSMS', e.target.checked)} />
              <i className="fa-solid fa-comment-sms"></i> SMS
            </label>
            <label className={`tg ${details.reminderEmail ? 'on' : ''}`}>
              <input type="checkbox" checked={details.reminderEmail} onChange={e => updateField('reminderEmail', e.target.checked)} />
              <i className="fa-solid fa-envelope"></i> E-mail
            </label>
          </div>
          <span className="hint">Enviado automaticamente 24h antes do atendimento.</span>
        </div>
      </div>
    </div>
  );
};

// ─── Step 3: Resumo ──────────────────────────────────
const ConfirmStep = ({ patient, details, isNewPatient }) => {
  const dateLabel = (() => {
    if (!details.date) return '—';
    const [y, m, d] = details.date.split('-');
    const dt = new Date(Number(y), Number(m) - 1, Number(d));
    return dt.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  })();
  const tipoLabels = { consulta: 'Consulta', retorno: 'Retorno', primeira: 'Primeira vez', exame: 'Exame', procedimento: 'Procedimento', teleconsulta: 'Teleconsulta' };
  const periodLabels = { manha: 'Manhã', tarde: 'Tarde', qualquer: 'Qualquer horário' };
  const urgencyLabels = { rotina: 'Rotina', priori: 'Prioritário', urgente: 'Urgente' };
  const isWait = details.waitlist;

  return (
    <div className="modal-step">
      <div className={`confirm-card ${isWait ? 'wait' : ''}`}>
        <div className="confirm-head">
          {isWait ? (
            <>
              <div className="big-time wait">
                <i className="fa-solid fa-hourglass-half"></i>
              </div>
              <div className="when">
                <b>Lista de espera</b>
                <span>Aguardando encaixe · {details.provider}</span>
              </div>
            </>
          ) : (
            <>
              <div className="big-time">
                <span className="hh">{details.time || '—'}</span>
                <span className="dur">{details.duration} min</span>
              </div>
              <div className="when">
                <b style={{ textTransform: 'capitalize' }}>{dateLabel}</b>
                <span>{details.provider}</span>
              </div>
            </>
          )}
        </div>

        <div className="confirm-body">
          <div className="kv">
            <span>Paciente</span>
            <b>
              {patient.name}
              {isNewPatient && <em className="new-tag">novo cadastro</em>}
            </b>
          </div>
          <div className="kv">
            <span>Documento</span>
            <b>{patient.doc}</b>
          </div>
          <div className="kv">
            <span>Telefone</span>
            <b>{patient.phone}</b>
          </div>
          <div className="kv">
            <span>Tipo</span>
            <b>{tipoLabels[details.tipo]}</b>
          </div>
          {isWait && (
            <>
              <div className="kv">
                <span>Preferência</span>
                <b>{periodLabels[details.periodPref] || '—'}{details.date ? ` · a partir de ${dateLabel}` : ''}</b>
              </div>
              <div className="kv">
                <span>Urgência</span>
                <b><span className={`urg-pill ${details.urgency}`}>{urgencyLabels[details.urgency] || 'Rotina'}</span></b>
              </div>
            </>
          )}
          <div className="kv">
            <span>Convênio</span>
            <b>{details.convenio}{details.plan ? ` · ${details.plan}` : ''}</b>
          </div>
          <div className="kv">
            <span>Motivo</span>
            <b>{details.reason || '—'}</b>
          </div>
          {details.notes && (
            <div className="kv">
              <span>Observações</span>
              <b className="notes-b">{details.notes}</b>
            </div>
          )}
          <div className="kv">
            <span>Lembrete</span>
            <b>
              {[
                details.reminderWA && 'WhatsApp',
                details.reminderSMS && 'SMS',
                details.reminderEmail && 'E-mail',
              ].filter(Boolean).join(' + ') || 'Não enviar'}
            </b>
          </div>
        </div>
      </div>

      {isWait ? (
        <div className="confirm-info wait">
          <i className="fa-solid fa-hourglass-half"></i>
          <div>
            <b>Será adicionado à lista de espera.</b>
            Quando surgir um encaixe compatível com a preferência do paciente, a secretaria
            será notificada para entrar em contato.
          </div>
        </div>
      ) : (
        <div className="confirm-info">
          <i className="fa-solid fa-circle-check"></i>
          Tudo pronto. Ao confirmar, o agendamento será adicionado à agenda{' '}
          {details.reminderWA || details.reminderSMS || details.reminderEmail ? (
            <>e o lembrete será disparado 24h antes.</>
          ) : (
            <>sem envio de lembrete automático.</>
          )}
        </div>
      )}
    </div>
  );
};

// ─── Modal raiz ──────────────────────────────────────
const NewAppointmentModal = ({ open, onClose, onCreated }) => {
  const [step, setStep] = React.useState(1);
  const [mode, setMode] = React.useState('search'); // 'search' | 'new'
  const [patient, setPatient] = React.useState(null);
  const [newPatient, setNewPatient] = React.useState({ name: '', doc: '', phone: '', birth: '', gender: '' });

  const todayISO = new Date().toISOString().slice(0, 10);
  const [details, setDetails] = React.useState({
    date: todayISO,
    time: '',
    duration: 30,
    tipo: 'consulta',
    provider: 'Dra. Renata Lopes — Cardiologia',
    convenio: 'Particular',
    plan: '',
    reason: '',
    notes: '',
    reminderWA: true, reminderSMS: false, reminderEmail: false,
    waitlist: false,
    periodPref: 'qualquer',
    urgency: 'rotina',
  });

  // reset ao abrir
  React.useEffect(() => {
    if (open) {
      setStep(1); setMode('search'); setPatient(null);
      setNewPatient({ name: '', doc: '', phone: '', birth: '', gender: '' });
    }
  }, [open]);

  if (!open) return null;

  const isNewPatient = mode === 'new';
  const effectivePatient = isNewPatient ? {
    ...newPatient,
    initials: (newPatient.name || '').split(' ').filter(Boolean).map(s => s[0]).slice(0, 2).join('').toUpperCase() || '?',
  } : patient;

  const canStep1 = isNewPatient
    ? newPatient.name.trim().length > 2 && newPatient.doc.trim() && newPatient.phone.trim()
    : !!patient;

  const canStep2 = details.waitlist
    ? details.reason.trim().length > 0
    : details.time && details.reason.trim().length > 0;

  const next = () => setStep(s => Math.min(3, s + 1));
  const back = () => setStep(s => Math.max(1, s - 1));

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <header className="modal-head">
          <div>
            <h2>Novo agendamento</h2>
            <span>Crie um agendamento em poucos passos</span>
          </div>
          <button className="modal-close" onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>

        {/* Stepper */}
        <div className="stepper">
          {[
            { n: 1, l: 'Paciente', i: 'fa-user' },
            { n: 2, l: 'Detalhes', i: 'fa-calendar-day' },
            { n: 3, l: 'Confirmar', i: 'fa-check' },
          ].map((s, idx, arr) => (
            <React.Fragment key={s.n}>
              <div className={`step-pill ${step === s.n ? 'active' : ''} ${step > s.n ? 'done' : ''}`}>
                <span className="num">{step > s.n ? <i className="fa-solid fa-check"></i> : s.n}</span>
                <span className="lbl">{s.l}</span>
              </div>
              {idx < arr.length - 1 && <div className={`step-bar ${step > s.n ? 'done' : ''}`}></div>}
            </React.Fragment>
          ))}
        </div>

        <div className="modal-body">
          {step === 1 && (
            <PatientStep
              patient={patient}
              setPatient={setPatient}
              mode={mode}
              setMode={setMode}
              newPatient={newPatient}
              setNewPatient={setNewPatient}
            />
          )}
          {step === 2 && <DetailsStep details={details} setDetails={setDetails} patient={effectivePatient} />}
          {step === 3 && <ConfirmStep patient={effectivePatient} details={details} isNewPatient={isNewPatient} />}
        </div>

        <footer className="modal-foot">
          <button className="btn-ghost" onClick={onClose}>Cancelar</button>
          <div style={{ flex: 1 }}></div>
          {step > 1 && (
            <button className="btn-secondary" onClick={back}>
              <i className="fa-solid fa-arrow-left"></i> Voltar
            </button>
          )}
          {step < 3 && (
            <button
              className="btn-primary"
              disabled={(step === 1 && !canStep1) || (step === 2 && !canStep2)}
              onClick={next}
            >
              Avançar <i className="fa-solid fa-arrow-right"></i>
            </button>
          )}
          {step === 3 && (
            <button className="btn-primary success" onClick={() => onCreated(effectivePatient, details, isNewPatient)}>
              <i className={`fa-solid ${details.waitlist ? 'fa-hourglass-half' : 'fa-circle-check'}`}></i>
              {details.waitlist ? 'Adicionar à lista de espera' : 'Confirmar agendamento'}
            </button>
          )}
        </footer>
      </div>
    </div>
  );
};

window.NewAppointmentModal = NewAppointmentModal;
