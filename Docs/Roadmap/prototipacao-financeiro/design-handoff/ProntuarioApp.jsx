// ProntuarioApp.jsx — Prontuário modular do paciente

const { focusPatient, history: careHistory, templates, moduleCatalog, me: doctor } = window.IMEDTO_CARE;

const PRONT_TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "legacyConduct": false
}/*EDITMODE-END*/;

const initialsP = (n) => n.split(' ').slice(0,2).map(x => x[0]).join('').toUpperCase();
const ageFromDob = (dob) => Math.floor((new Date() - new Date(dob)) / (365.25 * 24 * 3600 * 1000));

// Cronômetro do atendimento
const useCareTimer = (startedAt) => {
  const [now, setNow] = React.useState(Date.now());
  React.useEffect(() => {
    const t = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(t);
  }, []);
  const [h,m] = startedAt.split(':').map(Number);
  const start = new Date(); start.setHours(h, m, 0, 0);
  const diff = Math.max(0, Math.floor((now - start.getTime()) / 1000));
  return `${String(Math.floor(diff/60)).padStart(2,'0')}:${String(diff%60).padStart(2,'0')}`;
};

// ─── Cabeçalho fixo do paciente ──────────────────────────────
const PatientHeader = ({ patient, startedAt, focus, onToggleFocus, onFinish, onSave, onPrint, onReceita }) => {
  const timer = useCareTimer(startedAt);
  return (
    <div className="pront-header">
      <div className="ph-left">
        <a href="Atendimentos.html" className="ph-back" title="Voltar para fila">
          <i className="fa-solid fa-arrow-left"></i>
        </a>
        <div className="ph-avatar">{initialsP(patient.name)}</div>
        <div className="ph-info">
          <h1>{patient.name}</h1>
          <div className="ph-meta">
            <span>{ageFromDob(patient.dob)} anos</span><span>·</span>
            <span>{patient.sex === 'F' ? 'Feminino' : 'Masculino'}</span><span>·</span>
            <span>{patient.bloodType}</span><span>·</span>
            <span>CPF {patient.cpf}</span><span>·</span>
            <span>{patient.conv}</span>
          </div>
          {(patient.allergies.length > 0 || patient.chronicConditions.length > 0) && (
            <div className="ph-alerts">
              {patient.allergies.map(a => (
                <span key={a} className="ph-alert err"><i className="fa-solid fa-ban"></i> Alergia: {a}</span>
              ))}
              {patient.chronicConditions.map(c => (
                <span key={c} className="ph-alert warn"><i className="fa-solid fa-circle-info"></i> {c}</span>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="ph-right">
        <div className="ph-timer">
          <i className="fa-solid fa-stopwatch"></i>
          <div>
            <span className="ph-timer-val">{timer}</span>
            <span className="ph-timer-lbl">em atendimento</span>
          </div>
        </div>
        <button className={`ph-focus-btn ${focus ? 'on' : ''}`} onClick={onToggleFocus} title="Modo foco (F)">
          <i className={`fa-solid ${focus ? 'fa-eye-slash' : 'fa-eye'}`}></i>
          {focus ? 'Sair do foco' : 'Modo foco'}
        </button>
        <button className="btn-secondary" onClick={onSave}>
          <i className="fa-solid fa-floppy-disk"></i> Salvar rascunho
        </button>
        <button className="btn-secondary" onClick={onPrint} title="Pré-visualizar para impressão">
          <i className="fa-solid fa-print"></i> Imprimir
        </button>
        <button className="btn-secondary" onClick={onReceita} title="Emitir receita médica">
          <i className="fa-solid fa-prescription"></i> Receita
        </button>
        <button className="btn-success" onClick={onFinish}>
          <i className="fa-solid fa-circle-check"></i> Finalizar e assinar
        </button>
      </div>
    </div>
  );
};

// ─── Tabs (Prontuário | Histórico | Medicamentos) ──────────
const PageTabs = ({ tab, onChange, medsCount, historyCount }) => (
  <div className="pront-tabs">
    <button className={tab === 'pront' ? 'active' : ''} onClick={() => onChange('pront')}>
      <i className="fa-solid fa-file-medical"></i> Prontuário do atendimento
    </button>
    <button className={tab === 'history' ? 'active' : ''} onClick={() => onChange('history')}>
      <i className="fa-solid fa-clock-rotate-left"></i> Histórico clínico
      <span className="tab-badge">{historyCount}</span>
    </button>
    <button className={tab === 'meds' ? 'active' : ''} onClick={() => onChange('meds')}>
      <i className="fa-solid fa-pills"></i> Medicamentos
      <span className="tab-badge">{medsCount}</span>
    </button>
  </div>
);

// ─── Navegação por módulos (substitui sidebar antiga) ───────
const ModuleNav = ({ activeModules, moduleData, currentId, onJump }) => {
  // Agrupa por categoria
  const groups = {};
  activeModules.forEach(id => {
    const cfg = moduleCatalog[id];
    if (!cfg) return;
    const g = cfg.group || 'Outros';
    if (!groups[g]) groups[g] = [];
    groups[g].push(cfg);
  });

  const statusOf = (id) => {
    const d = moduleData[id];
    if (!d || Object.keys(d).length === 0) return 'empty';
    // qualquer campo não vazio = filled
    const hasContent = Object.values(d).some(v =>
      typeof v === 'string' ? v.trim() !== '' :
      Array.isArray(v) ? v.length > 0 :
      typeof v === 'object' && v !== null ? Object.keys(v).length > 0 :
      v
    );
    return hasContent ? 'filled' : 'empty';
  };

  return (
    <aside className="pront-nav">
      <div className="pn-head">
        <h4>Módulos deste prontuário</h4>
        <span>{activeModules.length} ativos</span>
      </div>

      {Object.entries(groups).map(([group, items]) => (
        <div key={group} className="pn-group">
          <div className="pn-group-title">{group}</div>
          {items.map(cfg => {
            const status = statusOf(cfg.id);
            const isCurrent = currentId === cfg.id;
            return (
              <button key={cfg.id} className={`pn-item ${isCurrent ? 'current' : ''} status-${status}`}
                onClick={() => onJump(cfg.id)}>
                <i className={`fa-solid ${cfg.icon} pn-ic`}></i>
                <span className="pn-lbl">{cfg.name}</span>
                <span className={`pn-dot status-${status}`} title={status === 'filled' ? 'Preenchido' : 'Vazio'}></span>
              </button>
            );
          })}
        </div>
      ))}

      {activeModules.length === 0 && (
        <div className="pn-empty">Nenhum módulo ainda. Escolha um modelo ou adicione pela biblioteca →</div>
      )}
    </aside>
  );
};

// ─── Aba: Histórico clínico (apenas timeline) ───────────────
const HistoryTab = ({ patient, onShowToast }) => {
  const past = careHistory.filter(h => h.status !== 'in-progress');
  return (
    <div className="history-tab">
      <div className="ht-head">
        <div>
          <h2>Linha do tempo de atendimentos</h2>
          <p>{past.length} atendimentos anteriores · paciente desde {new Date(past[past.length-1]?.date || '2025-08-22').toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })}</p>
        </div>
        <div className="ht-actions">
          <button className="btn-secondary" onClick={() => onShowToast('Exportando histórico completo em PDF...', 'success')}>
            <i className="fa-solid fa-file-pdf"></i> Exportar histórico completo
          </button>
        </div>
      </div>

      <div className="ht-timeline-full">
        {careHistory.map(h => (
          <div key={h.id} className={`httf-item ${h.status === 'in-progress' ? 'current' : ''}`}>
            <div className="httf-dot"></div>
            <div className="httf-card">
              <div className="httf-top">
                <div className="httf-date-block">
                  <div className="httf-day">{new Date(h.date).toLocaleDateString('pt-BR', { day: '2-digit' })}</div>
                  <div className="httf-monthyr">
                    {new Date(h.date).toLocaleDateString('pt-BR', { month: 'short' }).replace('.','')}
                    <span>{new Date(h.date).getFullYear()}</span>
                  </div>
                </div>
                <div className="httf-info">
                  <div className="httf-tpl-row">
                    <span className="httf-tpl"><i className="fa-solid fa-file-medical"></i> {h.template}</span>
                    {h.time && <span className="httf-time">{h.time}</span>}
                    {h.status === 'in-progress' && <span className="httf-now">Em andamento</span>}
                    {h.status === 'completed' && <span className="httf-done"><i className="fa-solid fa-check"></i> Assinado</span>}
                  </div>
                  <div className="httf-prof">{h.professional}</div>
                  <p className="httf-sum">{h.summary}</p>
                  {h.cid && (
                    <div className="httf-cid">
                      {h.cid.map((c, j) => <code key={j}>{c}</code>)}
                    </div>
                  )}
                  {h.attachments > 0 && (
                    <div className="httf-att"><i className="fa-solid fa-paperclip"></i> {h.attachments} anexo(s)</div>
                  )}
                </div>
                {h.status !== 'in-progress' && (
                  <div className="httf-actions">
                    <button className="btn-secondary sm" onClick={() => onShowToast('Abrindo prontuário em modo leitura...', 'info')}>
                      <i className="fa-solid fa-eye"></i> Visualizar
                    </button>
                    <button className="btn-ghost sm" onClick={() => onShowToast(`Baixando prontuário de ${new Date(h.date).toLocaleDateString('pt-BR')}...`, 'success')}>
                      <i className="fa-solid fa-download"></i> Baixar PDF
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

// ─── Aba: Medicamentos ─────────────────────────────────────
const MedicationsTab = ({ patient, onShowToast }) => {
  const [meds, setMeds] = React.useState(patient.currentMeds);
  const [editIdx, setEditIdx] = React.useState(null);
  const [newMed, setNewMed] = React.useState(null);

  const addMed = () => setNewMed({ name: '', dose: '', freq: '', since: '' });
  const saveNew = () => {
    if (!newMed.name) return;
    setMeds([...meds, newMed]);
    setNewMed(null);
    onShowToast('Medicamento adicionado', 'success');
  };
  const removeMed = (i) => {
    setMeds(meds.filter((_, idx) => idx !== i));
    onShowToast('Medicamento removido', 'success');
  };

  return (
    <div className="meds-tab">
      <div className="ht-head">
        <div>
          <h2>Medicamentos do paciente</h2>
          <p>{meds.length} em uso contínuo · última revisão hoje, 08:42</p>
        </div>
        <div className="ht-actions">
          <button className="btn-secondary" onClick={() => onShowToast('Gerando receita de continuidade...', 'success')}>
            <i className="fa-solid fa-prescription"></i> Receita de continuidade
          </button>
          <button className="btn-success" onClick={addMed}>
            <i className="fa-solid fa-plus"></i> Adicionar medicamento
          </button>
        </div>
      </div>

      <div className="meds-grid">
        {meds.map((m, i) => (
          <div key={i} className="med-card">
            <div className="med-card-ic"><i className="fa-solid fa-prescription-bottle-medical"></i></div>
            <div className="med-card-body">
              <div className="med-card-name">
                {m.name} <span className="med-card-dose">{m.dose}</span>
              </div>
              <div className="med-card-freq"><i className="fa-solid fa-clock"></i> {m.freq}</div>
              {m.since && <div className="med-card-since">Em uso desde {m.since}</div>}
            </div>
            <div className="med-card-actions">
              <button className="btn-ghost sm" title="Editar" onClick={() => setEditIdx(i)}><i className="fa-solid fa-pen"></i></button>
              <button className="btn-ghost sm" title="Suspender" onClick={() => removeMed(i)} style={{color: 'hsl(0 70% 50%)'}}>
                <i className="fa-solid fa-ban"></i>
              </button>
            </div>
          </div>
        ))}

        {newMed && (
          <div className="med-card med-card-new">
            <div className="med-card-ic"><i className="fa-solid fa-plus"></i></div>
            <div className="med-card-body">
              <div className="med-form-row">
                <input placeholder="Medicamento" value={newMed.name} onChange={e => setNewMed({...newMed, name: e.target.value})} autoFocus />
                <input placeholder="Dose (ex: 50mg)" value={newMed.dose} onChange={e => setNewMed({...newMed, dose: e.target.value})} />
              </div>
              <div className="med-form-row">
                <input placeholder="Frequência (ex: 1x/dia)" value={newMed.freq} onChange={e => setNewMed({...newMed, freq: e.target.value})} />
                <input placeholder="Em uso desde (mês/ano)" value={newMed.since} onChange={e => setNewMed({...newMed, since: e.target.value})} />
              </div>
            </div>
            <div className="med-card-actions">
              <button className="btn-success sm" onClick={saveNew}><i className="fa-solid fa-check"></i></button>
              <button className="btn-ghost sm" onClick={() => setNewMed(null)}><i className="fa-solid fa-xmark"></i></button>
            </div>
          </div>
        )}
      </div>

      {patient.allergies.length > 0 && (
        <div className="meds-allergy">
          <i className="fa-solid fa-triangle-exclamation"></i>
          <div>
            <b>Alergias registradas</b>
            <span>{patient.allergies.join(' · ')}</span>
          </div>
        </div>
      )}
    </div>
  );
};

// ─── Seletor de Template ─────────────────────────────────────
const TemplatePicker = ({ current, onPick, onCustomize }) => {
  const [open, setOpen] = React.useState(false);
  const ref = React.useRef(null);
  React.useEffect(() => {
    const close = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false); };
    if (open) document.addEventListener('click', close);
    return () => document.removeEventListener('click', close);
  }, [open]);

  const tpl = templates.find(t => t.id === current);

  return (
    <div className="tpl-picker" ref={ref}>
      <button className="tpl-current" onClick={() => setOpen(!open)}>
        <i className={`fa-solid ${tpl?.icon || 'fa-file-medical'}`}></i>
        <div>
          <span className="tpl-lbl">Tipo de prontuário</span>
          <strong>{tpl?.name || 'Em branco'}</strong>
        </div>
        <i className="fa-solid fa-chevron-down"></i>
      </button>
      {open && (
        <div className="tpl-menu">
          <div className="tpl-menu-head">Modelos da clínica</div>
          {templates.map(t => (
            <button key={t.id} className={`tpl-opt ${t.id === current ? 'active' : ''}`} onClick={() => { onPick(t.id); setOpen(false); }}>
              <i className={`fa-solid ${t.icon}`}></i>
              <div>
                <b>{t.name}</b>
                <span>{t.description}</span>
                <small>{t.modules.length} módulos · usado {t.uses}x</small>
              </div>
              {t.id === current && <i className="fa-solid fa-check"></i>}
            </button>
          ))}
          <div className="tpl-menu-foot">
            <button className="lnk" onClick={() => { onCustomize(); setOpen(false); }}>
              <i className="fa-solid fa-sliders"></i> Gerenciar modelos
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

// ─── Biblioteca de módulos (sidebar à direita) ───────────────
const ModuleLibrary = ({ activeModules, onAdd }) => {
  const available = Object.values(moduleCatalog).filter(m => !activeModules.includes(m.id));
  // Agrupa por categoria
  const groups = {};
  available.forEach(m => {
    const g = m.group || 'Outros';
    if (!groups[g]) groups[g] = [];
    groups[g].push(m);
  });

  return (
    <div className="mod-lib">
      <div className="mod-lib-head">
        <h4>Adicionar módulo</h4>
        <span>{available.length} disponíveis</span>
      </div>
      <div className="mod-lib-list">
        {Object.entries(groups).map(([group, items]) => (
          <div key={group} className="mod-lib-group">
            <div className="mod-lib-group-title">{group}</div>
            {items.map(m => (
              <button key={m.id} className="mod-lib-item" onClick={() => onAdd(m.id)}>
                <i className={`fa-solid ${m.icon}`}></i>
                <div>
                  <b>{m.name}</b>
                  <span>{m.desc}</span>
                </div>
                <i className="fa-solid fa-plus"></i>
              </button>
            ))}
          </div>
        ))}
        {available.length === 0 && (
          <div className="empty-mini">Todos os módulos já estão no prontuário</div>
        )}
      </div>
    </div>
  );
};

// ─── Modal de finalização ────────────────────────────────────
const SignModal = ({ open, onClose, onConfirm, patient }) => {
  if (!open) return null;
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal-card sign-modal" onClick={e => e.stopPropagation()}>
        <header>
          <i className="fa-solid fa-signature"></i>
          <h3>Assinar e finalizar atendimento</h3>
          <button onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>
        <div className="sign-body">
          <p>O prontuário de <strong>{patient.name}</strong> será assinado digitalmente e bloqueado para edição. Alterações posteriores ficarão registradas como adendos.</p>
          <div className="sign-info">
            <div><span>Profissional</span><b>{doctor.name}</b></div>
            <div><span>Registro</span><b>{doctor.crm}</b></div>
            <div><span>Data/hora</span><b>12/05/2026 às {new Date().toTimeString().slice(0,5)}</b></div>
            <div><span>Hash do documento</span><code>SHA-256 · A4B2…F19C</code></div>
          </div>
          <label className="sign-method"><input type="radio" name="m" defaultChecked /> <span>ICP-Brasil (e-CPF)</span></label>
          <label className="sign-method"><input type="radio" name="m" /> <span>SMS no celular cadastrado</span></label>
          <label className="sign-method"><input type="radio" name="m" /> <span>App autenticador</span></label>
        </div>
        <footer>
          <button className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className="btn-success" onClick={onConfirm}>
            <i className="fa-solid fa-signature"></i> Assinar agora
          </button>
        </footer>
      </div>
    </div>
  );
};

// ─── Pendências: helpers ─────────────────────────────────────
const PEND_KEY = 'imedto_pendencias';
const nowHHMM = () => new Date().toTimeString().slice(0, 5);
const ACTION_TOAST = {
  receita: 'Receita criada', atestado: 'Atestado emitido', exame: 'Exame solicitado',
  orcamento: 'Orçamento criado', procRealizado: 'Procedimento marcado como realizado', retorno: 'Retorno agendado',
};

// ─── Modal: Próximos passos do atendimento ───────────────────
const NextStepsModal = ({ open, items, onClose, onAction }) => {
  if (!open) return null;
  const done = items.filter(i => i.done).length;
  const total = items.length;
  const allDone = done === total;
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal-card next-steps" onClick={e => e.stopPropagation()}>
        <header>
          <i className="fa-solid fa-list-check"></i>
          <h3>Próximos passos do atendimento</h3>
          <button onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>
        <div className="ns-body">
          <p className="ns-sub">Conclua as pendências geradas nesta evolução. Você pode fazer depois — elas não se perdem.</p>
          <div className="ns-list">
            {items.map(it => (
              <div key={it.id} className={`ns-item ${it.done ? 'done' : ''}`}>
                <div className="nsi-ic"><i className={`fa-solid ${it.done ? 'fa-circle-check' : it.icon}`}></i></div>
                <div className="nsi-text">
                  <b>{it.label}</b>
                  {it.done && <span>Concluído às {it.doneAt}</span>}
                </div>
                {it.done
                  ? <span className="nsi-done"><i className="fa-solid fa-check"></i> Feito</span>
                  : <button className="nsi-go" onClick={() => onAction(it.id)}>{it.label} <i className="fa-solid fa-arrow-right"></i></button>}
              </div>
            ))}
          </div>
          <div className="ns-progress">
            <div className="nsp-bar"><div className="nsp-fill" style={{ width: (done / total * 100) + '%' }}></div></div>
            <span>{done} de {total} concluída{total > 1 ? 's' : ''}</span>
          </div>
        </div>
        <footer>
          <button className="btn-secondary" onClick={onClose}>{allDone ? 'Fechar' : 'Fazer depois'}</button>
          {allDone && <button className="btn-success" onClick={onClose}><i className="fa-solid fa-circle-check"></i> Tudo concluído</button>}
        </footer>
      </div>
    </div>
  );
};

// ─── Painel persistente de pendências ────────────────────────
const PendenciasPanel = ({ pend, onAction, onDismiss }) => {
  const done = pend.items.filter(i => i.done).length;
  const total = pend.items.length;
  const allDone = done === total;
  const [closing, setClosing] = React.useState(false);

  React.useEffect(() => {
    if (allDone) {
      const t = setTimeout(() => setClosing(true), 1600);
      const t2 = setTimeout(() => onDismiss(), 2200);
      return () => { clearTimeout(t); clearTimeout(t2); };
    }
  }, [allDone]);

  return (
    <div className={`pend-panel ${allDone ? 'complete' : ''} ${closing ? 'closing' : ''}`}>
      <div className="pp-head">
        <div className="pp-title">
          <i className={`fa-solid ${allDone ? 'fa-circle-check' : 'fa-clipboard-list'}`}></i>
          <div>
            <b>{allDone ? 'Tudo concluído' : 'Pendências do atendimento'} — {pend.date}</b>
            <span>{allDone ? 'Todas as ações deste atendimento foram realizadas.' : `${done} de ${total} concluídas · ${pend.patient}`}</span>
          </div>
        </div>
        {!allDone && <div className="pp-counter">{done}/{total}</div>}
      </div>
      {!allDone && (
        <div className="pp-items">
          {pend.items.map(it => (
            <div key={it.id} className={`pp-item ${it.done ? 'done' : ''}`}>
              <i className={`fa-solid ${it.done ? 'fa-circle-check' : it.icon}`}></i>
              <span className="ppi-label">{it.label}</span>
              {it.done
                ? <span className="ppi-status done"><i className="fa-solid fa-check"></i> Concluída às {it.doneAt}</span>
                : <><span className="ppi-status">Pendente</span><button className="ppi-go" onClick={() => onAction(it.id)}>Fazer agora <i className="fa-solid fa-arrow-right"></i></button></>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// ─── App principal ───────────────────────────────────────────
const ProntuarioApp = () => {
  const [tab, setTab] = React.useState('pront');
  const [tweaks, setTweak] = useTweaks(PRONT_TWEAK_DEFAULTS);
  const [templateId, setTemplateId] = React.useState('t1');
  const initialMods = templates.find(t => t.id === 't1').modules;
  const [activeModules, setActiveModules] = React.useState(initialMods);
  const [moduleData, setModuleData] = React.useState({
    qp: { text: 'Dor torácica em peso há cerca de 2 semanas, irradia para o braço esquerdo e melhora ao repouso.' },
    hda: { text: 'Episódios duram 5–10min, ocorrem 2–3x ao dia, principalmente aos esforços. Nega dispneia, síncope ou palpitações.' },
    vitals: { pa: '142/92', fc: '84', sat: '97', peso: '88.5', altura: '176', temp: '36.4' },
    physical: { notes: { 'chest': { status: 'altered', text: 'Bulhas rítmicas normofonéticas em 2T. Sem sopros. Ictus visível em 5º EIC linha hemiclavicular.' } } },
    cid: { list: [] },
    prescription: { items: [] },
    exams: { requested: [] },
    indicatedProc: { items: [{ procId: 'pc1', name: 'Infiltração articular', price: 80000, duration: 30, obs: 'joelho D' }] },
    conduct: { checked: { receita: true, orcamento: true, retorno: true }, obs: '' },
  });
  const [focus, setFocus] = React.useState(false);
  const [signOpen, setSignOpen] = React.useState(false);
  const [nextStepsOpen, setNextStepsOpen] = React.useState(false);
  const [pendencias, setPendencias] = React.useState(null);
  const [toast, setToast] = React.useState(null);
  const [currentMod, setCurrentMod] = React.useState(null);
  const [printOpen, setPrintOpen] = React.useState(null); // 'prontuario' | 'receita' | null

  const showToast = (m, t = 'info') => { setToast({ m, t }); setTimeout(() => setToast(null), 2400); };

  const persistPend = (p) => { try { localStorage.setItem(PEND_KEY, JSON.stringify(p)); } catch (e) {} };

  const handleSignConfirm = () => {
    setSignOpen(false);
    const checked = (moduleData.conduct && moduleData.conduct.checked) || {};
    const items = CONDUCT_ACTIONS.filter(a => checked[a.id]).map(a => ({ id: a.id, label: a.label, icon: a.icon, done: false, doneAt: null }));
    showToast('Evolução salva e assinada', 'success');
    // Conduta sem nada marcado → não gera pendência nem modal
    if (items.length === 0) return;
    const pend = { date: '10/06', patient: focusPatient.name, items };
    setPendencias(pend);
    persistPend(pend);
    setNextStepsOpen(true);
  };

  const completePend = (id) => {
    setPendencias(prev => {
      if (!prev) return prev;
      const items = prev.items.map(it => it.id === id ? { ...it, done: true, doneAt: nowHHMM() } : it);
      const np = { ...prev, items };
      persistPend(np);
      return np;
    });
    showToast(ACTION_TOAST[id] || 'Ação concluída', 'success');
  };

  const dismissPend = () => { setPendencias(null); try { localStorage.removeItem(PEND_KEY); } catch (e) {} };

  const pickTemplate = (tid) => {
    const t = templates.find(x => x.id === tid);
    setTemplateId(tid);
    setActiveModules(t.modules);
    showToast(`Modelo "${t.name}" aplicado`, 'success');
  };

  const addModule = (id) => {
    if (activeModules.includes(id)) return;
    setActiveModules([...activeModules, id]);
    showToast(`Módulo "${moduleCatalog[id].name}" adicionado`, 'success');
    setTimeout(() => jumpTo(id), 100);
  };

  const removeModule = (id) => setActiveModules(activeModules.filter(x => x !== id));
  const updateModule = (id, patch) => setModuleData(prev => ({ ...prev, [id]: { ...(prev[id] || {}), ...patch } }));

  const jumpTo = (id) => {
    setCurrentMod(id);
    const el = document.getElementById(`mod-${id}`);
    if (el) {
      // Calcula posição respeitando header sticky
      const top = el.getBoundingClientRect().top + window.scrollY - 180;
      window.scrollTo({ top, behavior: 'smooth' });
      el.classList.add('mod-pulse');
      setTimeout(() => el.classList.remove('mod-pulse'), 1200);
    }
  };

  // Detecta módulo atual no scroll
  React.useEffect(() => {
    if (tab !== 'pront') return;
    const handler = () => {
      let near = null; let bestDist = Infinity;
      activeModules.forEach(id => {
        const el = document.getElementById(`mod-${id}`);
        if (!el) return;
        const d = Math.abs(el.getBoundingClientRect().top - 200);
        if (d < bestDist) { bestDist = d; near = id; }
      });
      if (near) setCurrentMod(near);
    };
    window.addEventListener('scroll', handler, { passive: true });
    handler();
    return () => window.removeEventListener('scroll', handler);
  }, [activeModules, tab]);

  // Atalhos
  React.useEffect(() => {
    const onKey = (e) => {
      if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') return;
      if (e.key === 'f' && !e.metaKey && !e.ctrlKey) { e.preventDefault(); setFocus(f => !f); }
      if ((e.ctrlKey || e.metaKey) && e.key === 's') { e.preventDefault(); showToast('Rascunho salvo', 'success'); }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  // Render de um módulo pelo id
  const renderModule = (id) => {
    const common = {
      id,
      data: moduleData[id] || {},
      onChange: (patch) => updateModule(id, patch),
      onRemove: () => removeModule(id),
    };
    // Módulos baseados em texto livre
    const textModules = {
      qp:          'Ex: "Dor no peito há 2 semanas que piora aos esforços..."',
      hda:         'Início, duração, característica, irradiação, fatores de melhora/piora, sintomas associados...',
      hpp:         'Doenças prévias, cirurgias, internações, medicamentos de uso contínuo...',
      familyHist:  'Pai (IAM aos 58a), mãe (HAS e DM2), irmão saudável...',
      socialHist:  'Tabagista 20 maços/ano, etilista social, sedentário, dorme 6h/noite...',
      postOp:      'Paciente em pós-operatório imediato, consciente, orientado. Curativo limpo e seco. Sem sinais flogísticos. Diurese mantida.',
    };
    if (textModules[id]) return <TextModule {...common} placeholder={textModules[id]} rows={id === 'conduct' ? 5 : 4} />;

    switch (id) {
      case 'vitals':         return <VitalsModule {...common} />;
      case 'physical':       return <PhysicalModule {...common} sex={focusPatient.sex} />;
      case 'examsDone':      return <ExamsDoneModule {...common} />;
      case 'cid':            return <CIDModule {...common} />;
      case 'soap':           return <SOAPModule {...common} />;
      case 'prescription':   return <PrescriptionModule {...common} />;
      case 'exams':          return <ExamsModule {...common} />;
      case 'indicatedProc':  return <IndicatedProcModule {...common} />;
      case 'conduct':        return <ConductModule {...common} legacy={tweaks.legacyConduct} />;
      case 'certificate':    return <CertificateModule {...common} />;
      case 'inOfficeProc':   return <InOfficeProcModule {...common} />;
      case 'surgDesc':       return <SurgDescModule {...common} />;
      case 'anesthesia':     return <AnesthesiaModule {...common} />;
      case 'surgTeam':       return <SurgTeamModule {...common} />;
      case 'images':         return <ImagesModule {...common} />;
      case 'files':          return <FilesModule {...common} />;
      default: return null;
    }
  };

  return (
    <div className={`pront-shell ${focus ? 'focus-mode' : ''}`}>
      {!focus && <Sidebar active="Prontuários" />}
      {!focus && <TopBar />}
      <main className={focus ? 'focus' : ''}>
        <PatientHeader
          patient={focusPatient}
          startedAt="08:38"
          focus={focus}
          onToggleFocus={() => setFocus(!focus)}
          onFinish={() => setSignOpen(true)}
          onSave={() => showToast('Rascunho salvo', 'success')}
          onPrint={() => setPrintOpen('prontuario')}
          onReceita={() => setPrintOpen('receita')}
        />

        {!focus && <PageTabs tab={tab} onChange={setTab} medsCount={focusPatient.currentMeds.length} historyCount={careHistory.filter(h => h.status !== 'in-progress').length} />}

        {!focus && pendencias && (
          <PendenciasPanel pend={pendencias} onAction={completePend} onDismiss={dismissPend} />
        )}

        {tab === 'pront' ? (
          <div className="pront-grid">
            {!focus && (
              <ModuleNav
                activeModules={activeModules}
                moduleData={moduleData}
                currentId={currentMod}
                onJump={jumpTo}
              />
            )}

            <div className="pront-main">
              <div className="pront-toolbar">
                <TemplatePicker current={templateId} onPick={pickTemplate} onCustomize={() => showToast('Gerenciar modelos — em construção')} />
                <div className="pt-info">
                  <span>{activeModules.length} módulos · auto-save ativo</span>
                  <span className="autosave"><i className="fa-solid fa-cloud-arrow-up"></i> Salvo agora</span>
                </div>
              </div>

              <div className="modules-list">
                {activeModules.map(id => (
                  <React.Fragment key={id}>{renderModule(id)}</React.Fragment>
                ))}
                {activeModules.length === 0 && (
                  <div className="empty-modules">
                    <i className="fa-solid fa-puzzle-piece"></i>
                    <h3>Nenhum módulo neste prontuário</h3>
                    <p>Escolha um modelo acima ou adicione módulos pela biblioteca →</p>
                  </div>
                )}
              </div>
            </div>

            {!focus && <ModuleLibrary activeModules={activeModules} onAdd={addModule} />}
          </div>
        ) : tab === 'history' ? (
          <HistoryTab patient={focusPatient} onShowToast={showToast} />
        ) : (
          <MedicationsTab patient={focusPatient} onShowToast={showToast} />
        )}
      </main>

      <ProntuarioSheet
        open={printOpen === 'prontuario'}
        onClose={() => setPrintOpen(null)}
        patient={focusPatient}
        modules={activeModules}
        moduleData={moduleData}
      />

      <ReceitaSheet
        open={printOpen === 'receita'}
        onClose={() => setPrintOpen(null)}
        patient={focusPatient}
        items={moduleData.prescription?.items || []}
        continuous={moduleData.prescription?.continuous}
        controlled={moduleData.prescription?.controlled}
        onAdd={() => updateModule('prescription', {
          items: [...(moduleData.prescription?.items || []), { drug: '', dose: '', via: 'VO', freq: '', dur: '' }]
        })}
        onUpdate={(i, patch) => {
          const next = [...(moduleData.prescription?.items || [])];
          next[i] = { ...next[i], ...patch };
          updateModule('prescription', { items: next });
        }}
        onRemove={(i) => {
          const next = (moduleData.prescription?.items || []).filter((_, idx) => idx !== i);
          updateModule('prescription', { items: next });
        }}
      />

      <SignModal
        open={signOpen}
        onClose={() => setSignOpen(false)}
        onConfirm={handleSignConfirm}
        patient={focusPatient}
      />

      <NextStepsModal
        open={nextStepsOpen}
        items={pendencias ? pendencias.items : []}
        onClose={() => setNextStepsOpen(false)}
        onAction={completePend}
      />

      <TweaksPanel title="Tweaks">
        <TweakSection title="Prontuário">
          <TweakToggle
            label="Conduta de evolução antiga (texto livre, read-only)"
            value={tweaks.legacyConduct}
            onChange={v => setTweak('legacyConduct', v)}
          />
        </TweakSection>
      </TweaksPanel>

      {toast && (
        <div className={`atend-toast ${toast.t}`}>
          <i className={`fa-solid ${toast.t === 'success' ? 'fa-circle-check' : 'fa-circle-info'}`}></i>
          {toast.m}
        </div>
      )}
    </div>
  );
};

ReactDOM.createRoot(document.getElementById('root')).render(<ProntuarioApp />);
