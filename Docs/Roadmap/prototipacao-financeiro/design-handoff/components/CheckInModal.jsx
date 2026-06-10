// CheckInModal — check-in de recepção com seção financeira (Particular/Convênio)
// Estende o fluxo de check-in: resumo do agendamento + dados do paciente +
// seção "Atendimento" (tipo + valor/convênio) + seleção de sala.

// Tabela de preços sugerida (mock — viria da config por profissional / estabelecimento)
const PRICE_TABLE = {
  consulta: 35000, retorno: 25000, primeira: 45000,
  exame: 30000, procedimento: 60000, teleconsulta: 28000,
};

const CONVENIOS = ['Unimed', 'Bradesco Saúde', 'Amil', 'SulAmérica', 'Hapvida / NotreDame'];

const ROOMS = [
  { id: 'espera', label: 'Sala de espera', sub: 'Aguardar chamada', icon: 'fa-couch' },
  { id: 'cons-1', label: 'Consultório 1', sub: 'Dra. Renata Lopes', icon: 'fa-door-closed' },
  { id: 'cons-2', label: 'Consultório 2', sub: 'Livre', icon: 'fa-door-closed' },
  { id: 'proc', label: 'Sala de procedimentos', sub: 'Livre', icon: 'fa-syringe' },
];

// BRL helpers — armazenamos o valor em centavos (inteiro)
const centsToBRL = (c) => (c / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const parseToCents = (str) => {
  const digits = (str || '').replace(/\D/g, '');
  return digits ? parseInt(digits, 10) : 0;
};

const CheckInModal = ({ open, appt, statuses, tipos, clinicUsesConvenio, priceTableConfigured, onClose, onConfirm }) => {
  const [tipo, setTipo] = React.useState('particular'); // 'particular' | 'convenio'
  const [valueCents, setValueCents] = React.useState(0);
  const [convenio, setConvenio] = React.useState('');
  const [guia, setGuia] = React.useState('');
  const [room, setRoom] = React.useState('espera');
  const [touched, setTouched] = React.useState(false);

  React.useEffect(() => {
    if (!appt || !open) return;
    const apptHasConvenio = appt.convenio && appt.convenio !== 'Particular';
    // Pré-seleciona com base no convênio do agendamento (se a clínica usar convênio)
    const initialTipo = clinicUsesConvenio && apptHasConvenio ? 'convenio' : 'particular';
    setTipo(initialTipo);
    setConvenio(apptHasConvenio ? appt.convenio : '');
    setGuia('');
    setRoom('espera');
    setTouched(false);
    // Valor sugerido da tabela de preços (se configurada)
    const suggested = priceTableConfigured ? (PRICE_TABLE[appt.tipo] || 35000) : 0;
    setValueCents(suggested);
  }, [appt, open, clinicUsesConvenio, priceTableConfigured]);

  if (!open || !appt) return null;

  const status = statuses[appt.status];
  const tipoInfo = tipos[appt.tipo] || {};

  // Lista de convênios — garante que o convênio do agendamento aparece
  const convenioOptions = (() => {
    const list = [...CONVENIOS];
    if (appt.convenio && appt.convenio !== 'Particular' && !list.includes(appt.convenio)) {
      list.unshift(appt.convenio);
    }
    return list;
  })();

  const isParticular = tipo === 'particular';
  const valueInvalid = isParticular && valueCents <= 0;
  const convenioInvalid = !isParticular && !convenio;
  const canConfirm = !valueInvalid && !convenioInvalid;

  const handleConfirm = () => {
    setTouched(true);
    if (!canConfirm) return;
    onConfirm(appt, {
      tipo, room,
      valueCents: isParticular ? valueCents : null,
      convenio: isParticular ? null : convenio,
      guia: isParticular ? null : (guia.trim() || null),
    });
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal-checkin" onClick={e => e.stopPropagation()}>
        {/* Header com paciente */}
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
          {/* SEÇÃO 1: Resumo do agendamento */}
          <div className="edit-section">
            <div className="es-head">
              <div><i className="fa-solid fa-calendar-check"></i><b>Resumo do agendamento</b></div>
            </div>
            <div className="checkin-summary">
              <div className="cs-time">
                <span className="hh">{appt.time}</span>
                <span className="dur">{appt.duration} min</span>
              </div>
              <div className="cs-grid">
                <div className="csg-item">
                  <span>Tipo</span>
                  <b><i className={`fa-solid ${tipoInfo.icon}`} style={{ color: `hsl(${tipoInfo.color})`, marginRight: 6 }}></i>{tipoInfo.label}</b>
                </div>
                <div className="csg-item">
                  <span>Profissional</span>
                  <b>Dra. Renata Lopes · Cardiologia</b>
                </div>
                <div className="csg-item full">
                  <span>Motivo</span>
                  <b>{appt.reason}</b>
                </div>
              </div>
            </div>
          </div>

          {/* SEÇÃO 2: Dados do paciente */}
          <div className="edit-section">
            <div className="es-head">
              <div><i className="fa-solid fa-id-card"></i><b>Dados do paciente</b></div>
              <button className="btn-text" onClick={() => onClose()}>
                <i className="fa-solid fa-arrow-up-right-from-square"></i> Abrir cadastro
              </button>
            </div>
            <div className="patient-data">
              <div className="pd-item"><span>Idade</span><b>{appt.patient.age} anos</b></div>
              <div className="pd-item"><span>Sexo</span><b>{appt.patient.gender === 'F' ? 'Feminino' : 'Masculino'}</b></div>
              <div className="pd-item"><span>Telefone</span><b>{appt.patient.phone}</b></div>
              <div className="pd-item"><span>Convênio cadastrado</span><b>{appt.convenio}{appt.plan && appt.plan !== '—' ? ` · ${appt.plan}` : ''}</b></div>
            </div>
          </div>

          {/* SEÇÃO 3: Atendimento (financeiro) — NOVO */}
          <div className="edit-section">
            <div className="es-head">
              <div><i className="fa-solid fa-hand-holding-dollar"></i><b>Atendimento</b></div>
            </div>

            {/* Tipo de atendimento */}
            <div className="field-group full" style={{ marginBottom: 16 }}>
              <label>Tipo de atendimento <em>*</em></label>
              {clinicUsesConvenio ? (
                <div className="seg-control" role="radiogroup">
                  <button
                    type="button"
                    role="radio"
                    aria-checked={isParticular}
                    className={`seg ${isParticular ? 'active' : ''}`}
                    onClick={() => setTipo('particular')}
                  >
                    <i className="fa-solid fa-wallet"></i> Particular
                  </button>
                  <button
                    type="button"
                    role="radio"
                    aria-checked={!isParticular}
                    className={`seg ${!isParticular ? 'active' : ''}`}
                    onClick={() => setTipo('convenio')}
                  >
                    <i className="fa-solid fa-id-card-clip"></i> Convênio
                  </button>
                </div>
              ) : (
                <div className="seg-single">
                  <i className="fa-solid fa-wallet"></i> Particular
                  <span className="seg-note">Clínica não atende convênio</span>
                </div>
              )}
            </div>

            {/* Branch: Particular */}
            {isParticular && (
              <div className="field-group full">
                <label>Valor da consulta <em>*</em></label>
                <div className={`value-input ${touched && valueInvalid && priceTableConfigured ? 'error' : ''}`}>
                  <span className="vi-prefix">R$</span>
                  <input
                    type="text"
                    inputMode="numeric"
                    value={valueCents === 0 ? '' : centsToBRL(valueCents)}
                    placeholder="0,00"
                    onChange={e => { setValueCents(parseToCents(e.target.value)); setTouched(true); }}
                  />
                </div>
                {/* Hint / origem / erro */}
                {valueInvalid && priceTableConfigured ? (
                  <span className="field-error"><i className="fa-solid fa-circle-exclamation"></i> Informe um valor maior que zero.</span>
                ) : !priceTableConfigured && valueCents === 0 ? (
                  <span className="field-hint warn">
                    <i className="fa-solid fa-circle-info"></i> Nenhum valor sugerido — <a href="#" onClick={e => e.preventDefault()}>configure a tabela de preços</a>
                  </span>
                ) : (
                  <span className="field-hint"><i className="fa-solid fa-tag"></i> Sugerido pela tabela de preços</span>
                )}
                <div className="charge-note">
                  <i className="fa-solid fa-circle-info"></i>
                  Valor <b>cobrado</b> nesta consulta. O pagamento é registrado depois, na aba Financeiro do paciente.
                </div>
              </div>
            )}

            {/* Branch: Convênio */}
            {!isParticular && (
              <div className="form-grid">
                <div className="field-group">
                  <label>Convênio <em>*</em></label>
                  <select
                    value={convenio}
                    onChange={e => { setConvenio(e.target.value); setTouched(true); }}
                    className={touched && convenioInvalid ? 'has-error' : ''}
                  >
                    <option value="">Selecione o convênio</option>
                    {convenioOptions.map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                  {touched && convenioInvalid && (
                    <span className="field-error"><i className="fa-solid fa-circle-exclamation"></i> Selecione o convênio.</span>
                  )}
                </div>
                <div className="field-group">
                  <label>Nº da guia / autorização <span className="opt">opcional</span></label>
                  <input
                    type="text"
                    placeholder="Ex: 2026-0098471"
                    value={guia}
                    onChange={e => setGuia(e.target.value)}
                  />
                </div>
                <div className="field-group full">
                  <div className="charge-note convenio">
                    <i className="fa-solid fa-shield-halved"></i>
                    Sem cobrança no balcão — o paciente não paga pela consulta de convênio.
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* SEÇÃO 4: Sala */}
          <div className="edit-section">
            <div className="es-head">
              <div><i className="fa-solid fa-location-dot"></i><b>Encaminhar para</b></div>
            </div>
            <div className="room-grid">
              {ROOMS.map(r => (
                <button
                  key={r.id}
                  type="button"
                  className={`room-chip ${room === r.id ? 'active' : ''}`}
                  onClick={() => setRoom(r.id)}
                >
                  <i className={`fa-solid ${r.icon}`}></i>
                  <div className="rc-text">
                    <b>{r.label}</b>
                    <span>{r.sub}</span>
                  </div>
                  {room === r.id && <i className="fa-solid fa-circle-check rc-check"></i>}
                </button>
              ))}
            </div>
          </div>
        </div>

        <footer className="modal-foot">
          <button className="btn-ghost" onClick={onClose}>Cancelar</button>
          <div style={{ flex: 1 }}></div>
          {isParticular && !valueInvalid && (
            <span className="foot-amount">
              Cobrança: <b>R$ {centsToBRL(valueCents)}</b>
            </span>
          )}
          <button className="btn-primary success" disabled={!canConfirm} onClick={handleConfirm}>
            <i className="fa-solid fa-clipboard-check"></i> Confirmar check-in
          </button>
        </footer>
      </div>
    </div>
  );
};

window.CheckInModal = CheckInModal;
