// PatientDetail.jsx — Componente principal da tela de detalhes do paciente

const { TAGS, BUDGET_STATUS, PATIENTS, PATIENT_DETAIL } = window.IMEDTO_PATIENTS;

const fmtDate2 = (d) => {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
};
const fmtDay = (d) => new Date(d).toLocaleDateString('pt-BR', { day: '2-digit' });
const fmtMon = (d) => new Date(d).toLocaleDateString('pt-BR', { month: 'short' }).replace('.', '');
const fmtYr = (d) => new Date(d).getFullYear();
const fmtMoney2 = (v) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

// ─── Tab Resumo ───────────────────────────────────────
const TabResumo = ({ patient, detail }) => (
  <div>
    <div className="summary-grid">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        {/* Próximas ações */}
        <div className="pd-card">
          <div className="pd-card-head">
            <h3><i className="fa-solid fa-bolt"></i> Próximas ações</h3>
          </div>
          <div className="next-actions">
            {detail.nextActions.map((a, i) => (
              <div key={i} className="na-item">
                <div className={`na-icon ${a.type}`}><i className={`fa-solid ${a.icon}`}></i></div>
                <div className="na-info">
                  <b>{a.title}</b>
                  <span>{a.desc}</span>
                </div>
                <div className="na-when">{a.when}</div>
              </div>
            ))}
          </div>
        </div>

        {/* Plano de tratamento */}
        <div className="pd-card">
          <div className="pd-card-head">
            <h3><i className="fa-solid fa-route"></i> {detail.treatmentPlan.name}</h3>
            <span className="lnk">Editar plano</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
            <span style={{ fontSize: 12, color: 'hsl(var(--secondary) / 0.65)' }}>Progresso</span>
            <b style={{ fontSize: 13, color: 'var(--c-primary-dark)' }}>{Math.round(detail.treatmentPlan.progress * 100)}%</b>
            <span style={{ marginLeft: 'auto', fontSize: 11, color: 'hsl(var(--secondary) / 0.5)' }}>Iniciado em {fmtDate2(detail.treatmentPlan.startedAt)}</span>
          </div>
          <div className="tp-progress"><div className="tp-fill" style={{ width: `${detail.treatmentPlan.progress * 100}%` }}></div></div>
          <div className="tp-stages">
            {detail.treatmentPlan.stages.map((s, i) => (
              <div key={i} className="tp-stage">
                <div className={`tp-bullet ${s.status}`}>
                  <i className={`fa-solid ${s.status === 'done' ? 'fa-check' : s.status === 'progress' ? 'fa-rotate' : 'fa-clock'}`}></i>
                </div>
                <div className="tp-info">
                  <b>{s.name}</b>
                  <span>{s.date}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Coluna lateral — stats */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        <div className="pd-card side-card">
          <h4>Histórico clínico</h4>
          <div className="stat-line"><span>Total de atendimentos</span><b>{patient.totalVisits}</b></div>
          <div className="stat-line"><span>Última visita</span><b>{fmtDate2(patient.lastVisit)}</b></div>
          <div className="stat-line"><span>Profissional principal</span><b>{patient.doctor}</b></div>
          <div className="stat-line"><span>Convênio</span><b style={{ fontSize: 12 }}>{patient.insurance}</b></div>
        </div>

        <div className="pd-card side-card">
          <h4>Financeiro</h4>
          <div className="stat-line"><span>Total faturado</span><b>{fmtMoney2(detail.finance.totalLifetime)}</b></div>
          <div className="stat-line"><span>Total pago</span><b className="success">{fmtMoney2(detail.finance.totalPaid)}</b></div>
          <div className="stat-line"><span>Em aberto</span><b className="danger">{fmtMoney2(detail.finance.balanceOpen)}</b></div>
        </div>

        <div className="pd-card side-card">
          <h4>Documentos</h4>
          <div className="stat-line"><span>Termos assinados</span><b>{detail.consents.length}</b></div>
          <div className="stat-line"><span>Anexos no prontuário</span><b>{detail.attachments.length}</b></div>
          <div className="stat-line"><span>Orçamentos ativos</span><b>{detail.budgets.filter(b => b.status === 'sent' || b.status === 'partial').length}</b></div>
        </div>
      </div>
    </div>
  </div>
);

// ─── Tab Prontuário (timeline) ────────────────────────
const TabProntuario = ({ detail }) => {
  const [expanded, setExpanded] = React.useState(new Set(['enc-5']));
  const toggle = (id) => {
    const n = new Set(expanded);
    if (n.has(id)) n.delete(id); else n.add(id);
    setExpanded(n);
  };

  return (
    <div>
      <div className="prontuario-head">
        <div>
          <h2>Prontuário eletrônico</h2>
          <p>{detail.encounters.length} atendimentos registrados · todos assinados digitalmente</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn-secondary"><i className="fa-solid fa-filter"></i> Filtrar</button>
          <button className="btn-secondary"><i className="fa-solid fa-download"></i> Exportar PDF</button>
          <button className="btn-primary"><i className="fa-solid fa-plus"></i> Novo atendimento</button>
        </div>
      </div>

      <div className="timeline">
        {detail.encounters.map(e => {
          const isOpen = expanded.has(e.id);
          return (
            <div key={e.id} className={`tl-entry ${e.type === 'procedimento' ? 'procedure' : ''} ${isOpen ? 'expanded' : ''}`}>
              <div className="tl-head" onClick={() => toggle(e.id)}>
                <div className="tl-date">
                  <span className="day">{fmtDay(e.date)}</span>
                  <span className="month">{fmtMon(e.date)}</span>
                  <span className="year">{fmtYr(e.date)}</span>
                </div>
                <div className="tl-meta">
                  <div className="tl-type">
                    <span className={`tl-type-pill ${e.type === 'procedimento' ? 'procedure' : ''}`}>
                      <i className={`fa-solid ${e.type === 'procedimento' ? 'fa-stethoscope' : 'fa-notes-medical'}`}></i>
                      {e.typeLabel}
                    </span>
                    <span className="tl-time">{e.time}</span>
                    {e.signed && <span className="tl-signed"><i className="fa-solid fa-signature"></i> Assinado</span>}
                  </div>
                  <div className="tl-doctor">{e.doctor} <span>· {e.specialty}</span></div>
                </div>
                <i className="fa-solid fa-chevron-right tl-toggle"></i>
              </div>
              <div className="tl-body">
                <div className="tl-section">
                  <h5><i className="fa-solid fa-comment-medical"></i> Queixa principal</h5>
                  <p>{e.complaint}</p>
                </div>
                <div className="tl-section">
                  <h5><i className="fa-solid fa-clipboard-list"></i> Anamnese</h5>
                  <p>{e.anamnesis}</p>
                </div>
                <div className="tl-section">
                  <h5><i className="fa-solid fa-stethoscope"></i> Exame físico</h5>
                  <p>{e.exam}</p>
                </div>
                <div className="tl-section">
                  <h5><i className="fa-solid fa-tag"></i> Diagnóstico (CID-10)</h5>
                  <p>{e.cid}</p>
                </div>
                <div className="tl-section">
                  <h5><i className="fa-solid fa-list-check"></i> Conduta</h5>
                  <p>{e.plan}</p>
                </div>
                <div className="tl-grid">
                  {e.prescriptions.length > 0 && (
                    <div className="tl-section">
                      <h5><i className="fa-solid fa-prescription-bottle-medical"></i> Prescrições</h5>
                      <ul className="tl-list">{e.prescriptions.map((p, i) => <li key={i}>{p}</li>)}</ul>
                    </div>
                  )}
                  {e.examsRequested.length > 0 && (
                    <div className="tl-section">
                      <h5><i className="fa-solid fa-flask"></i> Exames solicitados</h5>
                      <ul className="tl-list">{e.examsRequested.map((x, i) => <li key={i}>{x}</li>)}</ul>
                    </div>
                  )}
                </div>
                {e.attachments.length > 0 && (
                  <div className="tl-section">
                    <h5><i className="fa-solid fa-paperclip"></i> Anexos</h5>
                    <div className="tl-attachments">
                      {e.attachments.map((a, i) => (
                        <span key={i} className="tl-att"><i className="fa-solid fa-file-pdf"></i> {a}</span>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

window.TabResumo = TabResumo;
window.TabProntuario = TabProntuario;
window.fmtDate2 = fmtDate2;
window.fmtMoney2 = fmtMoney2;
