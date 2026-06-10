// PatientDetailApp.jsx — App de detalhe do paciente

const { TAGS: T2, PATIENTS: P2, PATIENT_DETAIL: PD } = window.IMEDTO_PATIENTS;

const PD_TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "financeAccess": true,
  "canDiscount": true
}/*EDITMODE-END*/;

const PatientDetailApp = () => {
  // Pegar id da query, default = 2 (Pedro Henrique)
  const params = new URLSearchParams(window.location.search);
  const id = parseInt(params.get('id') || '2', 10);
  const patient = P2.find(p => p.id === id) || P2.find(p => p.id === 2);
  // Detail é só do paciente 2; para outros, fazemos um fallback básico
  const detail = patient.id === 2 ? PD : { ...PD, patientId: patient.id };

  const [tab, setTab] = React.useState('resumo');
  const [tweaks, setTweak] = useTweaks(PD_TWEAK_DEFAULTS);

  const tabs = [
    { id: 'resumo', label: 'Resumo', icon: 'fa-house' },
    { id: 'prontuario', label: 'Prontuário', icon: 'fa-file-medical', badge: detail.encounters.length },
    { id: 'anamnese', label: 'Anamnese', icon: 'fa-clipboard-user' },
    { id: 'orcamentos', label: 'Orçamentos', icon: 'fa-file-invoice-dollar', badge: detail.budgets.filter(b => b.status === 'sent').length, badgeWarn: true },
    { id: 'financeiro', label: 'Financeiro', icon: 'fa-coins' },
    { id: 'convenios', label: 'Convênios', icon: 'fa-shield-halved' },
    { id: 'termos', label: 'Termos', icon: 'fa-file-signature', badge: detail.consents.length },
    { id: 'anexos', label: 'Anexos', icon: 'fa-paperclip', badge: detail.attachments.length },
  ];

  return (
    <div>
      <TopBar />
      <Sidebar active="Pacientes" />
      <main>
        <div className="page detail-page">
          <a href="Pacientes.html" className="pd-back">
            <i className="fa-solid fa-chevron-left"></i> Voltar para lista de pacientes
          </a>

          {/* Header sticky */}
          <div className="pd-header">
            <div className="pd-head-main">
              <div className="pd-avatar" style={{ background: patient.avatarColor }}>{patient.initials}</div>
              <div className="pd-info">
                <div className="pd-name-row">
                  <h1>{patient.name}</h1>
                  <span className="pd-id">#PT-{String(patient.id).padStart(5, '0')}</span>
                </div>
                <div className="pd-meta-row">
                  <span><i className="fa-solid fa-cake-candles"></i> {patient.age} anos · {fmtDate2(patient.birth)}</span>
                  <span><i className="fa-solid fa-venus-mars"></i> {patient.gender === 'F' ? 'Feminino' : 'Masculino'}</span>
                  <span><i className="fa-solid fa-id-card"></i> {patient.cpf}</span>
                  <span><i className="fa-solid fa-phone"></i> {patient.phone}</span>
                  <span><i className="fa-solid fa-envelope"></i> {patient.email}</span>
                  <span><i className="fa-solid fa-shield-halved"></i> {patient.insurance}</span>
                </div>
                <div className="pd-tags">
                  {patient.tags.map(t => {
                    const tag = T2[t];
                    return (
                      <span key={t} className="tag-pill" style={{ background: `color-mix(in srgb, ${tag.color} 15%, white)`, color: tag.color }}>
                        <i className={`fa-solid ${tag.icon}`}></i>
                        {tag.label}
                      </span>
                    );
                  })}
                </div>
              </div>
              <div className="pd-actions">
                <button className="btn-secondary"><i className="fa-solid fa-comment-dots"></i> Mensagem</button>
                <button className="btn-secondary"><i className="fa-solid fa-pen"></i> Editar</button>
                <button className="btn-primary"><i className="fa-solid fa-calendar-plus"></i> Agendar consulta</button>
              </div>
            </div>

            {/* Alertas */}
            {patient.alerts.length > 0 && (
              <div className="pd-alerts">
                <i className="fa-solid fa-triangle-exclamation"></i>
                <div className="pd-alerts-content">
                  <b>Alertas clínicos importantes</b>
                  <ul>
                    {patient.alerts.map((a, i) => <li key={i}>{a}</li>)}
                  </ul>
                </div>
              </div>
            )}

            {/* Stats rápidos */}
            <div className="pd-stats">
              <div className="pd-stat"><i className="fa-solid fa-clock-rotate-left"></i> <span>Última visita</span> <b>{patient.lastVisit ? fmtDate2(patient.lastVisit) : 'Nunca'}</b></div>
              {patient.nextAppointment && (
                <div className="pd-stat success"><i className="fa-solid fa-calendar-check"></i> <span>Próxima</span> <b>{fmtDate2(patient.nextAppointment)}</b></div>
              )}
              <div className="pd-stat"><i className="fa-solid fa-user-doctor"></i> <span>Profissional</span> <b>{patient.doctor}</b></div>
              <div className="pd-stat"><i className="fa-solid fa-stethoscope"></i> <span>Atendimentos</span> <b>{patient.totalVisits}</b></div>
              {patient.balance > 0 && (
                <div className="pd-stat danger"><i className="fa-solid fa-coins"></i> <span>Em aberto</span> <b>{fmtMoney2(patient.balance)}</b></div>
              )}
            </div>

            {/* Tabs */}
            <div className="pd-tabs">
              {tabs.map(t => (
                <button key={t.id} className={`pd-tab ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)}>
                  <i className={`fa-solid ${t.icon}`}></i>
                  {t.label}
                  {t.badge != null && t.badge > 0 && (
                    <span className={`badge ${t.badgeWarn ? 'badge-warn' : ''}`}>{t.badge}</span>
                  )}
                </button>
              ))}
            </div>
          </div>

          {/* Content */}
          <div className="pd-content">
            {tab === 'resumo' && <TabResumo patient={patient} detail={detail} />}
            {tab === 'prontuario' && <TabProntuario detail={detail} />}
            {tab === 'anamnese' && <TabAnamnese detail={detail} />}
            {tab === 'orcamentos' && <TabOrcamentos detail={detail} />}
            {tab === 'financeiro' && <TabFinanceiro detail={detail} patient={patient} hasAccess={tweaks.financeAccess} canDiscount={tweaks.canDiscount} />}
            {tab === 'convenios' && <TabConvenios detail={detail} patient={patient} />}
            {tab === 'termos' && <TabTermos detail={detail} />}
            {tab === 'anexos' && <TabAnexos detail={detail} />}
          </div>
        </div>

        <TweaksPanel title="Tweaks">
          <TweakSection title="Financeiro do paciente">
            <TweakToggle
              label="Permissão de acesso ao Financeiro"
              value={tweaks.financeAccess}
              onChange={v => setTweak('financeAccess', v)}
            />
            <TweakToggle
              label="Permissão para aplicar desconto"
              value={tweaks.canDiscount}
              onChange={v => setTweak('canDiscount', v)}
            />
          </TweakSection>
        </TweaksPanel>
      </main>
    </div>
  );
};

ReactDOM.createRoot(document.getElementById('root')).render(<PatientDetailApp />);
