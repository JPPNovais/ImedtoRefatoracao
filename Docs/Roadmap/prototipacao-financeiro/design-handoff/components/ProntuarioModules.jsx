// ProntuarioModules.jsx — Módulos individuais do prontuário

const { anatomyRegions, moduleCatalog: mCat } = window.IMEDTO_CARE;

// Header compartilhado de cada módulo
const ModuleCard = ({ id, icon, title, subtitle, onRemove, children, status, headerExtra }) => (
  <section className="module" id={`mod-${id}`} data-module={id}>
    <header className="module-head">
      <div className="module-handle" title="Arrastar para reordenar">
        <i className="fa-solid fa-grip-vertical"></i>
      </div>
      <div className="module-ic"><i className={`fa-solid ${icon}`}></i></div>
      <div className="module-title">
        <h3>{title}</h3>
        {subtitle && <span>{subtitle}</span>}
      </div>
      <div className="module-status">
        {status === 'filled' && <span className="ms-pill success"><i className="fa-solid fa-check"></i> Preenchido</span>}
        {status === 'partial' && <span className="ms-pill warning"><i className="fa-solid fa-circle-half-stroke"></i> Em andamento</span>}
        {status === 'empty' && <span className="ms-pill neutral">Vazio</span>}
      </div>
      {headerExtra}
      <button className="module-x" onClick={onRemove} title="Remover módulo">
        <i className="fa-solid fa-xmark"></i>
      </button>
    </header>
    <div className="module-body">{children}</div>
  </section>
);

// ─── Módulo genérico de texto ────────────────────────────────
const TextModule = ({ id, data = {}, onChange, onRemove, placeholder, rows = 4, extraFields }) => {
  const cfg = mCat[id];
  return (
    <ModuleCard id={id} icon={cfg.icon} title={cfg.name} subtitle={cfg.desc}
      status={data.text ? 'filled' : 'empty'} onRemove={onRemove}>
      <textarea rows={rows} placeholder={placeholder}
        defaultValue={data.text || ''} onBlur={(e) => onChange({ text: e.target.value })} />
      {extraFields}
    </ModuleCard>
  );
};

// ─── Sinais vitais ───────────────────────────────────────────
const VitalsModule = ({ data = {}, onChange, onRemove }) => {
  const fields = [
    { k: 'pa', l: 'PA', unit: 'mmHg', ph: '120/80' },
    { k: 'fc', l: 'FC', unit: 'bpm', ph: '72' },
    { k: 'fr', l: 'FR', unit: 'irpm', ph: '16' },
    { k: 'temp', l: 'Temp', unit: '°C', ph: '36.5' },
    { k: 'sat', l: 'SatO₂', unit: '%', ph: '98' },
    { k: 'peso', l: 'Peso', unit: 'kg', ph: '70' },
    { k: 'altura', l: 'Altura', unit: 'cm', ph: '170' },
    { k: 'glic', l: 'Glicemia', unit: 'mg/dL', ph: '95' },
  ];
  const imc = data.peso && data.altura ? (parseFloat(data.peso) / Math.pow(parseFloat(data.altura)/100, 2)).toFixed(1) : null;
  const filled = fields.filter(f => data[f.k]).length;

  return (
    <ModuleCard id="vitals" icon="fa-heart-pulse" title="Sinais vitais" status={filled > 0 ? (filled >= 4 ? 'filled' : 'partial') : 'empty'} onRemove={onRemove}>
      <div className="vitals-grid">
        {fields.map(f => (
          <div key={f.k} className="vital">
            <label>{f.l}</label>
            <div className="vital-input">
              <input type="text" placeholder={f.ph} defaultValue={data[f.k] || ''} onBlur={(e) => onChange({ [f.k]: e.target.value })} />
              <span>{f.unit}</span>
            </div>
          </div>
        ))}
        {imc && (
          <div className="vital vital-calc">
            <label>IMC</label>
            <div className="vital-input">
              <input type="text" readOnly value={imc} />
              <span>kg/m²</span>
            </div>
            <div className="vital-cls">
              {imc < 18.5 ? 'Abaixo do peso' : imc < 25 ? 'Eutrófico' : imc < 30 ? 'Sobrepeso' : 'Obesidade'}
            </div>
          </div>
        )}
      </div>
    </ModuleCard>
  );
};

// ─── Exame físico (com boneco anatômico) ─────────────────────
const PhysicalModule = ({ data = {}, onChange, onRemove, sex = 'M' }) => {
  const [view, setView] = React.useState('front');
  const [selectedRegion, setSelectedRegion] = React.useState(null);
  const notes = data.notes || {};

  const toggleRegion = (id) => setSelectedRegion(selectedRegion === id ? null : id);
  const updateNote = (regionId, patch) => {
    onChange({ notes: { ...notes, [regionId]: { ...(notes[regionId] || {}), ...patch } } });
  };
  const removeNote = (regionId) => {
    const next = { ...notes }; delete next[regionId];
    onChange({ notes: next });
    if (selectedRegion === regionId) setSelectedRegion(null);
  };

  const marks = Object.fromEntries(Object.entries(notes).map(([id, n]) => [id, n.status || 'note']));
  const annotatedIds = Object.keys(notes);

  return (
    <ModuleCard id="physical" icon="fa-person" title="Exame físico" subtitle="Clique nas regiões do boneco para anotar" status={annotatedIds.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="physical-grid">
        <div className="anatomy-panel">
          <div className="anatomy-tabs">
            <button className={view === 'front' ? 'active' : ''} onClick={() => setView('front')}>Frente</button>
            <button className={view === 'back' ? 'active' : ''} onClick={() => setView('back')}>Costas</button>
          </div>
          <div className="anatomy-wrap">
            <AnatomyFigure sex={sex} view={view}
              selected={selectedRegion ? [selectedRegion] : []}
              onToggle={toggleRegion} marks={marks} />
          </div>
          <div className="anatomy-legend">
            <span><i className="dot-mark altered"></i> Alterado</span>
            <span><i className="dot-mark normal"></i> Normal</span>
            <span><i className="dot-mark note"></i> Anotado</span>
            <span><i className="dot-mark selected"></i> Selecionado</span>
          </div>
        </div>

        <div className="anatomy-side">
          {selectedRegion ? (
            <div className="region-editor">
              <div className="re-head">
                <strong>{anatomyRegions.find(r => r.id === selectedRegion)?.label || selectedRegion}</strong>
                <button className="btn-ghost sm" onClick={() => setSelectedRegion(null)}>
                  <i className="fa-solid fa-xmark"></i>
                </button>
              </div>
              <div className="re-status">
                <button className={`re-status-btn normal ${notes[selectedRegion]?.status === 'normal' ? 'active' : ''}`}
                  onClick={() => updateNote(selectedRegion, { status: 'normal' })}>
                  <i className="fa-solid fa-circle-check"></i> Normal
                </button>
                <button className={`re-status-btn altered ${notes[selectedRegion]?.status === 'altered' ? 'active' : ''}`}
                  onClick={() => updateNote(selectedRegion, { status: 'altered' })}>
                  <i className="fa-solid fa-triangle-exclamation"></i> Alterado
                </button>
                <button className={`re-status-btn note ${notes[selectedRegion]?.status === 'note' ? 'active' : ''}`}
                  onClick={() => updateNote(selectedRegion, { status: 'note' })}>
                  <i className="fa-solid fa-note-sticky"></i> Anotação
                </button>
              </div>
              <textarea placeholder="Achados clínicos: inspeção, palpação, percussão, ausculta..."
                rows="5" defaultValue={notes[selectedRegion]?.text || ''}
                onBlur={(e) => updateNote(selectedRegion, { text: e.target.value })} />
              {notes[selectedRegion]?.status && (
                <button className="btn-ghost sm" onClick={() => removeNote(selectedRegion)} style={{color: 'hsl(0 70% 50%)'}}>
                  <i className="fa-solid fa-trash"></i> Remover anotação
                </button>
              )}
            </div>
          ) : (
            <div className="region-hint">
              <i className="fa-solid fa-hand-pointer"></i>
              <p>Clique em uma região do corpo para registrar achados</p>
            </div>
          )}

          {annotatedIds.length > 0 && (
            <div className="region-list">
              <div className="rl-head">Regiões anotadas ({annotatedIds.length})</div>
              {annotatedIds.map(id => {
                const region = anatomyRegions.find(r => r.id === id);
                const note = notes[id];
                return (
                  <div key={id} className={`rl-item ${note.status || ''}`} onClick={() => setSelectedRegion(id)}>
                    <span className={`dot-mark ${note.status || 'note'}`}></span>
                    <div>
                      <b>{region?.label || id}</b>
                      <span>{note.text || '(sem descrição)'}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </ModuleCard>
  );
};

// ─── SOAP ───────────────────────────────────────────────────
const SOAPModule = ({ data = {}, onChange, onRemove }) => {
  const fields = [
    { k: 's', l: 'S — Subjetivo', desc: 'O que o paciente relata, sintomas, queixas', ph: '“Dor no peito há 3 dias, piora ao esforço.”' },
    { k: 'o', l: 'O — Objetivo', desc: 'Achados de exame, sinais vitais, exames', ph: 'PA 142/92, ausculta cardíaca rítmica, sem sopros...' },
    { k: 'a', l: 'A — Avaliação', desc: 'Hipóteses diagnósticas, raciocínio clínico', ph: 'HAS estágio 2 não controlada. Descartar angina estável.' },
    { k: 'p', l: 'P — Plano', desc: 'Conduta, prescrição, exames, retorno', ph: '1. Aumentar losartana 100mg. 2. Solicitar ECG. 3. Retorno em 30 dias.' },
  ];
  const filled = fields.filter(f => data[f.k]).length;
  return (
    <ModuleCard id="soap" icon="fa-pen-to-square" title="Evolução (SOAP)" status={filled === 4 ? 'filled' : filled > 0 ? 'partial' : 'empty'} onRemove={onRemove}>
      <div className="soap-grid">
        {fields.map(f => (
          <div key={f.k} className={`soap-field soap-${f.k}`}>
            <div className="soap-label"><b>{f.l}</b><span>{f.desc}</span></div>
            <textarea rows="3" placeholder={f.ph} defaultValue={data[f.k] || ''} onBlur={(e) => onChange({ [f.k]: e.target.value })} />
          </div>
        ))}
      </div>
    </ModuleCard>
  );
};

// ─── CID-10 ───────────────────────────────────────────
const cidSuggestions = [
  { code: 'I10', desc: 'Hipertensão essencial (primária)' },
  { code: 'I11.0', desc: 'Doença cardíaca hipertensiva com IC' },
  { code: 'I20.9', desc: 'Angina pectoris não especificada' },
  { code: 'I25.10', desc: 'Doença aterosclerótica do coração' },
  { code: 'I48.0', desc: 'Fibrilação atrial paroxística' },
  { code: 'I50.9', desc: 'Insuficiência cardíaca não especificada' },
  { code: 'E11.9', desc: 'Diabetes mellitus tipo 2 sem complicações' },
  { code: 'E78.5', desc: 'Hiperlipidemia não especificada' },
  { code: 'R07.4', desc: 'Dor torácica não especificada' },
];

const CIDModule = ({ data = {}, onChange, onRemove }) => {
  const [q, setQ] = React.useState('');
  const list = data.list || [];
  const filtered = q ? cidSuggestions.filter(c => (c.code + c.desc).toLowerCase().includes(q.toLowerCase())) : cidSuggestions.slice(0, 5);
  const add = (c) => { if (list.find(x => x.code === c.code)) return; onChange({ list: [...list, { ...c, type: 'principal' }] }); setQ(''); };
  const remove = (code) => onChange({ list: list.filter(x => x.code !== code) });
  const setType = (code, type) => onChange({ list: list.map(x => x.code === code ? { ...x, type } : x) });

  return (
    <ModuleCard id="cid" icon="fa-disease" title="CID-10" subtitle="Diagnósticos e hipóteses" status={list.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="cid-search">
        <i className="fa-solid fa-magnifying-glass"></i>
        <input type="text" placeholder="Buscar por código (I10) ou descrição (hipertensão)..." value={q} onChange={(e) => setQ(e.target.value)} />
      </div>
      {q && filtered.length > 0 && (
        <div className="cid-suggest">
          {filtered.map(c => (
            <button key={c.code} className="cid-suggest-item" onClick={() => add(c)}>
              <code>{c.code}</code> {c.desc}<i className="fa-solid fa-plus"></i>
            </button>
          ))}
        </div>
      )}
      <div className="cid-list">
        {list.length === 0 ? (
          <div className="empty-mini">Nenhum diagnóstico adicionado</div>
        ) : list.map(c => (
          <div key={c.code} className="cid-item">
            <code>{c.code}</code>
            <span className="cid-desc">{c.desc}</span>
            <select value={c.type} onChange={(e) => setType(c.code, e.target.value)}>
              <option value="principal">Principal</option>
              <option value="secundario">Secundário</option>
              <option value="hipotese">Hipótese</option>
            </select>
            <button className="btn-ghost sm" onClick={() => remove(c.code)}><i className="fa-solid fa-trash"></i></button>
          </div>
        ))}
      </div>
    </ModuleCard>
  );
};

// ─── Prescrição ─────────────────────────────────────────────
const PrescriptionModule = ({ data = {}, onChange, onRemove }) => {
  const items = data.items || [];
  const update = (i, patch) => onChange({ items: items.map((x, idx) => idx === i ? { ...x, ...patch } : x) });
  const add = () => onChange({ items: [...items, { drug: '', dose: '', via: 'VO', freq: '', dur: '' }] });
  const remove = (i) => onChange({ items: items.filter((_, idx) => idx !== i) });

  return (
    <ModuleCard id="prescription" icon="fa-prescription" title="Prescrição" subtitle={items.length ? `${items.length} medicamento(s)` : ''} status={items.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="rx-list">
        {items.map((it, i) => (
          <div key={i} className="rx-row">
            <div className="rx-num">{i+1}</div>
            <input className="rx-drug" placeholder="Medicamento" defaultValue={it.drug} onBlur={(e) => update(i, { drug: e.target.value })} />
            <input className="rx-dose" placeholder="Dose" defaultValue={it.dose} onBlur={(e) => update(i, { dose: e.target.value })} />
            <select defaultValue={it.via} onChange={(e) => update(i, { via: e.target.value })}>
              <option>VO</option><option>IV</option><option>IM</option><option>SC</option><option>Tópico</option>
            </select>
            <input className="rx-freq" placeholder="Frequência" defaultValue={it.freq} onBlur={(e) => update(i, { freq: e.target.value })} />
            <input className="rx-dur" placeholder="Duração" defaultValue={it.dur} onBlur={(e) => update(i, { dur: e.target.value })} />
            <button className="btn-ghost sm" onClick={() => remove(i)}><i className="fa-solid fa-trash"></i></button>
          </div>
        ))}
      </div>
      <button className="rx-add" onClick={add}><i className="fa-solid fa-plus"></i> Adicionar medicamento</button>
      <div className="rx-foot">
        <label className="checkbox-row">
          <input type="checkbox" defaultChecked={data.continuous} onChange={(e) => onChange({ continuous: e.target.checked })} />
          <span>Receita de uso contínuo</span>
        </label>
        <label className="checkbox-row">
          <input type="checkbox" defaultChecked={data.controlled} onChange={(e) => onChange({ controlled: e.target.checked })} />
          <span>Receituário controlado</span>
        </label>
      </div>
    </ModuleCard>
  );
};

// ─── Exames (solicitação) ───────────────────────────────────
const ExamsModule = ({ data = {}, onChange, onRemove }) => {
  const requested = data.requested || [];
  const examOptions = ['ECG', 'Teste ergométrico', 'Holter 24h', 'Ecocardiograma', 'Hemograma completo', 'Perfil lipídico', 'Glicemia de jejum', 'TSH', 'TC de tórax', 'Raio-X de tórax'];
  const toggle = (e) => onChange({ requested: requested.includes(e) ? requested.filter(x => x !== e) : [...requested, e] });
  return (
    <ModuleCard id="exams" icon="fa-flask" title="Solicitação de exames" subtitle={requested.length ? `${requested.length} solicitado(s)` : ''} status={requested.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="exams-chips">
        {examOptions.map(e => (
          <button key={e} className={`exam-chip ${requested.includes(e) ? 'on' : ''}`} onClick={() => toggle(e)}>
            {requested.includes(e) && <i className="fa-solid fa-check"></i>}{e}
          </button>
        ))}
      </div>
      <Field label="Outros / detalhamento clínico" full>
        <textarea rows="2" placeholder="Especificar achados que justificam os exames..." defaultValue={data.justification || ''} onBlur={(e) => onChange({ justification: e.target.value })} />
      </Field>
    </ModuleCard>
  );
};

// ─── Exames realizados ──────────────────────────────────────
const ExamsDoneModule = ({ data = {}, onChange, onRemove }) => {
  const list = data.list || [];
  const add = () => onChange({ list: [...list, { name: '', date: '', result: '' }] });
  const update = (i, p) => onChange({ list: list.map((x, idx) => idx === i ? { ...x, ...p } : x) });
  const remove = (i) => onChange({ list: list.filter((_, idx) => idx !== i) });
  return (
    <ModuleCard id="examsDone" icon="fa-vials" title="Exames realizados" subtitle="Resultados de exames já feitos" status={list.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="done-list">
        {list.map((e, i) => (
          <div key={i} className="done-row">
            <input placeholder="Exame (ex: ECG, hemograma)" defaultValue={e.name} onBlur={(ev) => update(i, { name: ev.target.value })} />
            <input type="date" defaultValue={e.date} onBlur={(ev) => update(i, { date: ev.target.value })} />
            <input placeholder="Resultado / laudo resumido" defaultValue={e.result} onBlur={(ev) => update(i, { result: ev.target.value })} />
            <button className="btn-ghost sm" onClick={() => remove(i)}><i className="fa-solid fa-trash"></i></button>
          </div>
        ))}
        {list.length === 0 && <div className="empty-mini">Nenhum exame adicionado</div>}
      </div>
      <button className="rx-add" onClick={add}><i className="fa-solid fa-plus"></i> Adicionar exame realizado</button>
    </ModuleCard>
  );
};

// ─── Procedimentos indicados (seletor do catálogo do estabelecimento) ──
const procBRL = (cents) => 'R$ ' + (cents / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const procParseCents = (str) => { const d = (str || '').replace(/\D/g, ''); return d ? parseInt(d, 10) : 0; };

const IndicatedProcModule = ({ data = {}, onChange, onRemove }) => {
  const items = data.items || [];
  // Catálogo do estabelecimento (cresce com "Criar procedimento")
  const [catalog, setCatalog] = React.useState(() => (window.IMEDTO_CARE.procedureCatalog || []).slice());
  const [q, setQ] = React.useState('');
  const [open, setOpen] = React.useState(false);
  const [creating, setCreating] = React.useState(null); // {name, price, duration}
  const ref = React.useRef(null);

  React.useEffect(() => {
    const close = (e) => { if (ref.current && !ref.current.contains(e.target)) { setOpen(false); setCreating(null); } };
    if (open) document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, [open]);

  const addedIds = items.map(i => i.procId);
  const filtered = catalog.filter(c =>
    !addedIds.includes(c.id) && c.name.toLowerCase().includes(q.trim().toLowerCase())
  );
  const noResults = q.trim() && filtered.length === 0;

  const addProc = (proc) => {
    onChange({ items: [...items, { procId: proc.id, name: proc.name, price: proc.price, duration: proc.duration, obs: '' }] });
    setQ(''); setOpen(false);
  };
  const startCreate = () => setCreating({ name: q.trim(), price: 0, duration: '' });
  const confirmCreate = () => {
    if (!creating.name.trim() || creating.price <= 0) return;
    const np = { id: 'pc' + Date.now(), name: creating.name.trim(), price: creating.price, duration: Number(creating.duration) || null };
    setCatalog([np, ...catalog]);
    onChange({ items: [...items, { procId: np.id, name: np.name, price: np.price, duration: np.duration, obs: '', created: true }] });
    setCreating(null); setQ(''); setOpen(false);
  };
  const updateObs = (i, obs) => onChange({ items: items.map((x, idx) => idx === i ? { ...x, obs } : x) });
  const remove = (i) => onChange({ items: items.filter((_, idx) => idx !== i) });

  return (
    <ModuleCard id="indicatedProc" icon="fa-list-check" title="Procedimentos indicados"
      subtitle="Selecione do catálogo do estabelecimento" status={items.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>

      {/* Itens selecionados */}
      {items.length > 0 && (
        <div className="ip-list">
          {items.map((it, i) => (
            <div key={i} className="ip-item">
              <div className="ip-main">
                <div className="ip-name">
                  <i className="fa-solid fa-syringe"></i>
                  <b>{it.name}</b>
                  {it.created && <span className="ip-new">novo</span>}
                </div>
                <span className="ip-price">{procBRL(it.price)}{it.duration ? ` · ${it.duration} min` : ''}</span>
              </div>
              <input className="ip-obs" placeholder="Observação (opcional) — ex: joelho D"
                defaultValue={it.obs} onBlur={(e) => updateObs(i, e.target.value)} />
              <button className="btn-ghost sm" onClick={() => remove(i)} title="Remover"><i className="fa-solid fa-trash"></i></button>
            </div>
          ))}
        </div>
      )}

      {/* Seletor com busca */}
      <div className="ip-selector" ref={ref}>
        <div className="ip-search" onClick={() => setOpen(true)}>
          <i className="fa-solid fa-magnifying-glass"></i>
          <input type="text" placeholder="Buscar procedimento do catálogo..."
            value={q} onFocus={() => setOpen(true)}
            onChange={(e) => { setQ(e.target.value); setOpen(true); setCreating(null); }} />
        </div>

        {open && (
          <div className="ip-dropdown">
            {creating ? (
              <div className="ip-create-form">
                <div className="ipc-title"><i className="fa-solid fa-circle-plus"></i> Criar procedimento no estabelecimento</div>
                <div className="ipc-grid">
                  <div className="field-group full">
                    <label>Nome do procedimento <em>*</em></label>
                    <input type="text" autoFocus value={creating.name} onChange={(e) => setCreating({ ...creating, name: e.target.value })} placeholder="Ex: Infiltração articular" />
                  </div>
                  <div className="field-group">
                    <label>Valor <em>*</em></label>
                    <div className="value-input sm">
                      <span className="vi-prefix">R$</span>
                      <input type="text" inputMode="numeric" value={creating.price === 0 ? '' : (creating.price / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2 })}
                        placeholder="0,00" onChange={(e) => setCreating({ ...creating, price: procParseCents(e.target.value) })} />
                    </div>
                  </div>
                  <div className="field-group">
                    <label>Duração (min)</label>
                    <input type="number" value={creating.duration} onChange={(e) => setCreating({ ...creating, duration: e.target.value })} placeholder="30" />
                  </div>
                </div>
                <div className="ipc-actions">
                  <button className="btn-ghost sm" onClick={() => setCreating(null)}>Cancelar</button>
                  <button className="btn-primary sm" disabled={!creating.name.trim() || creating.price <= 0} onClick={confirmCreate}>
                    <i className="fa-solid fa-check"></i> Criar e adicionar
                  </button>
                </div>
              </div>
            ) : (
              <>
                {noResults ? (
                  <div className="ip-noresult">
                    <i className="fa-solid fa-magnifying-glass-minus"></i>
                    <span>Nenhum procedimento encontrado para "<b>{q.trim()}</b>"</span>
                  </div>
                ) : (
                  <div className="ip-options">
                    {filtered.map(c => (
                      <button key={c.id} className="ip-option" onClick={() => addProc(c)}>
                        <i className="fa-solid fa-syringe"></i>
                        <span className="ipo-name">{c.name}</span>
                        <span className="ipo-price">{procBRL(c.price)}</span>
                        <i className="fa-solid fa-plus ipo-add"></i>
                      </button>
                    ))}
                    {filtered.length === 0 && !q.trim() && <div className="empty-mini" style={{ padding: '8px 12px' }}>Todos os procedimentos já foram adicionados.</div>}
                  </div>
                )}
                <button className={`ip-create-btn ${noResults ? 'highlight' : ''}`} onClick={startCreate}>
                  <i className="fa-solid fa-plus"></i> Criar procedimento{q.trim() ? ` "${q.trim()}"` : ''}
                </button>
              </>
            )}
          </div>
        )}
      </div>
    </ModuleCard>
  );
};

// ─── Conduta (checklist fixo de 6 ações + observação) ──────
const CONDUCT_ACTIONS = [
  { id: 'receita',      label: 'Criar receita',                 icon: 'fa-prescription' },
  { id: 'atestado',     label: 'Criar atestado',                icon: 'fa-file-signature' },
  { id: 'exame',        label: 'Pedir exame',                   icon: 'fa-flask' },
  { id: 'orcamento',    label: 'Criar orçamento',               icon: 'fa-file-invoice-dollar' },
  { id: 'procRealizado',label: 'Marcar procedimento realizado', icon: 'fa-circle-check' },
  { id: 'retorno',      label: 'Agendar retorno',               icon: 'fa-calendar-plus' },
];

const ConductModule = ({ data = {}, onChange, onRemove, legacy }) => {
  const checked = data.checked || {};
  const count = Object.values(checked).filter(Boolean).length;

  // Retrocompatibilidade: evolução antiga com conduta em texto livre → read-only
  if (legacy || data.legacyText) {
    return (
      <ModuleCard id="conduct" icon="fa-route" title="Conduta" subtitle="Evolução anterior · somente leitura" status="filled" onRemove={onRemove}>
        <div className="conduct-legacy">
          <div className="cl-tag"><i className="fa-solid fa-lock"></i> Registro imutável de evolução assinada</div>
          <p>{data.legacyText || '1. Iniciada losartana 50mg 1x/dia.\n2. Solicitados exames complementares.\n3. Orientada dieta hipossódica e atividade física regular.\n4. Retorno em 30 dias com resultados.'}</p>
        </div>
      </ModuleCard>
    );
  }

  const toggle = (id) => onChange({ checked: { ...checked, [id]: !checked[id] } });

  return (
    <ModuleCard id="conduct" icon="fa-route" title="Conduta" subtitle="Marque as ações deste atendimento"
      status={count > 0 ? 'filled' : 'empty'} onRemove={onRemove}
      headerExtra={count > 0 ? <span className="conduct-count">{count} {count > 1 ? 'ações' : 'ação'}</span> : null}>
      <div className="conduct-checklist">
        {CONDUCT_ACTIONS.map(a => (
          <label key={a.id} className={`conduct-item ${checked[a.id] ? 'on' : ''}`}>
            <input type="checkbox" checked={!!checked[a.id]} onChange={() => toggle(a.id)} />
            <span className="ci-box"><i className="fa-solid fa-check"></i></span>
            <i className={`fa-solid ${a.icon} ci-ic`}></i>
            <span className="ci-label">{a.label}</span>
          </label>
        ))}
      </div>
      <div className="conduct-obs">
        <label>Observação clínica</label>
        <textarea rows={3} placeholder="Orientações, raciocínio clínico, plano terapêutico..."
          defaultValue={data.obs || ''} onBlur={(e) => onChange({ obs: e.target.value })} />
      </div>
    </ModuleCard>
  );
};

// ─── Equipe cirúrgica ──────────────────────────────────────
const SurgTeamModule = ({ data = {}, onChange, onRemove }) => {
  const fields = [
    { k: 'surgeon', l: 'Cirurgião(ã) principal' },
    { k: 'aux1', l: '1º Auxiliar' },
    { k: 'aux2', l: '2º Auxiliar' },
    { k: 'anesth', l: 'Anestesiologista' },
    { k: 'instr', l: 'Instrumentadora' },
    { k: 'circ', l: 'Circulante' },
  ];
  const filled = fields.filter(f => data[f.k]).length;
  return (
    <ModuleCard id="surgTeam" icon="fa-user-doctor" title="Equipe cirúrgica" status={filled > 0 ? (filled >= 3 ? 'filled' : 'partial') : 'empty'} onRemove={onRemove}>
      <div className="field-grid">
        {fields.map(f => (
          <Field key={f.k} label={f.l}>
            <input type="text" placeholder="Nome · CRM" defaultValue={data[f.k] || ''} onBlur={(e) => onChange({ [f.k]: e.target.value })} />
          </Field>
        ))}
      </div>
    </ModuleCard>
  );
};

// ─── Ficha anestésica ──────────────────────────────────────
const AnesthesiaModule = ({ data = {}, onChange, onRemove }) => {
  const types = ['Geral', 'Raquianestesia', 'Peridural', 'Sedação', 'Local', 'Bloqueio'];
  return (
    <ModuleCard id="anesthesia" icon="fa-bed-pulse" title="Ficha anestésica" status={data.type ? 'filled' : 'empty'} onRemove={onRemove}>
      <Field label="Tipo de anestesia" full>
        <div className="cert-types">
          {types.map(t => (
            <button key={t} className={`cert-type ${data.type === t ? 'active' : ''}`} onClick={() => onChange({ type: t })}>
              <i className="fa-solid fa-syringe"></i> {t}
            </button>
          ))}
        </div>
      </Field>
      <div className="field-grid">
        <Field label="Drogas utilizadas"><input type="text" placeholder="Propofol, fentanil..." defaultValue={data.drugs || ''} onBlur={(e) => onChange({ drugs: e.target.value })} /></Field>
        <Field label="Início → Término"><input type="text" placeholder="08:15 → 10:42" defaultValue={data.time || ''} onBlur={(e) => onChange({ time: e.target.value })} /></Field>
        <Field label="Monitorização"><input type="text" placeholder="ECG, SpO2, capnografia..." defaultValue={data.monitor || ''} onBlur={(e) => onChange({ monitor: e.target.value })} /></Field>
        <Field label="ASA"><select defaultValue={data.asa || 'I'} onChange={(e) => onChange({ asa: e.target.value })}>
          <option>I</option><option>II</option><option>III</option><option>IV</option><option>V</option><option>VI</option>
        </select></Field>
      </div>
      <Field label="Intercorrências e observações" full>
        <textarea rows="3" placeholder="Sem intercorrências relevantes durante o ato anestésico..." defaultValue={data.notes || ''} onBlur={(e) => onChange({ notes: e.target.value })} />
      </Field>
    </ModuleCard>
  );
};

// ─── Descrição cirúrgica ───────────────────────────────────
const SurgDescModule = ({ data = {}, onChange, onRemove }) => (
  <ModuleCard id="surgDesc" icon="fa-scissors" title="Descrição cirúrgica" status={data.technique ? 'filled' : 'empty'} onRemove={onRemove}>
    <div className="field-grid">
      <Field label="Procedimento realizado"><input type="text" defaultValue={data.procedure || ''} onBlur={(e) => onChange({ procedure: e.target.value })} /></Field>
      <Field label="Duração"><input type="text" placeholder="Ex: 1h 45min" defaultValue={data.duration || ''} onBlur={(e) => onChange({ duration: e.target.value })} /></Field>
    </div>
    <Field label="Descrição da técnica" full>
      <textarea rows="5" placeholder="Tempo 1: antissepsia... Tempo 2: incisão... Tempo 3: ..." defaultValue={data.technique || ''} onBlur={(e) => onChange({ technique: e.target.value })} />
    </Field>
    <Field label="Intercorrências" full>
      <textarea rows="2" placeholder="Sem intercorrências." defaultValue={data.complications || ''} onBlur={(e) => onChange({ complications: e.target.value })} />
    </Field>
  </ModuleCard>
);

// ─── Procedimento em consultório ───────────────────────────
const InOfficeProcModule = ({ data = {}, onChange, onRemove }) => (
  <ModuleCard id="inOfficeProc" icon="fa-syringe" title="Procedimento em consultório" status={data.procedure ? 'filled' : 'empty'} onRemove={onRemove}>
    <div className="field-grid">
      <Field label="Procedimento"><input type="text" placeholder="Ex: Infiltração, sutura, drenagem..." defaultValue={data.procedure || ''} onBlur={(e) => onChange({ procedure: e.target.value })} /></Field>
      <Field label="Local / região"><input type="text" placeholder="Ex: Joelho direito" defaultValue={data.site || ''} onBlur={(e) => onChange({ site: e.target.value })} /></Field>
      <Field label="Material utilizado"><input type="text" placeholder="Ex: Triancinolona 40mg, agulha 25x7" defaultValue={data.materials || ''} onBlur={(e) => onChange({ materials: e.target.value })} /></Field>
      <Field label="Anestesia local"><input type="text" placeholder="Ex: Lidocaína 2% s/ vaso" defaultValue={data.anesth || ''} onBlur={(e) => onChange({ anesth: e.target.value })} /></Field>
    </div>
    <Field label="Descrição e evolução imediata" full>
      <textarea rows="3" placeholder="Técnica utilizada, tolerância do paciente, orientações pós..." defaultValue={data.notes || ''} onBlur={(e) => onChange({ notes: e.target.value })} />
    </Field>
  </ModuleCard>
);

// ─── Atestados ─────────────────────────────────
const CertificateModule = ({ data = {}, onChange, onRemove }) => {
  const types = [
    { id: 'atestado', l: 'Atestado médico' },
    { id: 'declaracao', l: 'Declaração de comparecimento' },
    { id: 'relatorio', l: 'Relatório clínico' },
    { id: 'apto', l: 'Apto físico / esporte' },
  ];
  return (
    <ModuleCard id="certificate" icon="fa-file-signature" title="Atestados e documentos" status={data.type ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="cert-types">
        {types.map(t => (
          <button key={t.id} className={`cert-type ${data.type === t.id ? 'active' : ''}`} onClick={() => onChange({ type: t.id })}>
            <i className="fa-solid fa-file-lines"></i> {t.l}
          </button>
        ))}
      </div>
      {data.type && (
        <>
          <div className="field-grid">
            <Field label="Dias de afastamento"><input type="number" placeholder="Ex: 3" defaultValue={data.days || ''} onBlur={(e) => onChange({ days: e.target.value })} /></Field>
            <Field label="CID (opcional)"><input type="text" placeholder="Ex: I10" defaultValue={data.cid || ''} onBlur={(e) => onChange({ cid: e.target.value })} /></Field>
          </div>
          <Field label="Texto do documento" full>
            <textarea rows="4" placeholder="Atesto para os devidos fins que..." defaultValue={data.text || ''} onBlur={(e) => onChange({ text: e.target.value })} />
          </Field>
        </>
      )}
    </ModuleCard>
  );
};

// ─── Fotos do paciente ─────────────────────────────────────
const ImagesModule = ({ data = {}, onChange, onRemove }) => {
  const photos = data.photos || [];
  return (
    <ModuleCard id="images" icon="fa-camera" title="Fotos do paciente" subtitle="Fotos clínicas, comparativo antes/depois" status={photos.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="img-grid">
        {photos.map((p, i) => (
          <div key={i} className="img-cell">
            <div className="img-ph"><i className="fa-solid fa-image"></i></div>
            <span>{p.label}</span>
          </div>
        ))}
        <div className="img-cell img-add"><i className="fa-solid fa-camera"></i><span>Foto agora</span></div>
        <div className="img-cell img-add"><i className="fa-solid fa-upload"></i><span>Anexar</span></div>
      </div>
      <div className="img-compare">
        <span><i className="fa-solid fa-layer-group"></i> Modo comparação antes/depois</span>
        <button className="btn-secondary sm">Ativar</button>
      </div>
    </ModuleCard>
  );
};

// ─── Anexos (PDFs / arquivos) ──────────────────────────────
const FilesModule = ({ data = {}, onChange, onRemove }) => {
  const items = data.items || [
    { name: 'Laudo Holter 24h.pdf', size: '1.2 MB', date: '08/02/2026' },
  ];
  return (
    <ModuleCard id="files" icon="fa-paperclip" title="Anexos" subtitle={items.length ? `${items.length} arquivo(s)` : 'PDFs, laudos externos'} status={items.length > 0 ? 'filled' : 'empty'} onRemove={onRemove}>
      <div className="files-list">
        {items.map((f, i) => (
          <div key={i} className="file-row">
            <i className="fa-solid fa-file-pdf"></i>
            <div className="file-info">
              <b>{f.name}</b>
              <span>{f.size} · enviado em {f.date}</span>
            </div>
            <button className="btn-ghost sm"><i className="fa-solid fa-download"></i></button>
            <button className="btn-ghost sm"><i className="fa-solid fa-trash"></i></button>
          </div>
        ))}
        <button className="file-add"><i className="fa-solid fa-cloud-arrow-up"></i> Arrastar arquivos ou clicar para anexar</button>
      </div>
    </ModuleCard>
  );
};

// ─── Helpers ────────────────────────────────────────────────
const Field = ({ label, children, full }) => (
  <div className={`field ${full ? 'full' : ''}`}>
    <label>{label}</label>
    {children}
  </div>
);

// Export
Object.assign(window, {
  ModuleCard, Field, TextModule,
  VitalsModule, PhysicalModule, SOAPModule,
  CIDModule, PrescriptionModule, ExamsModule, ExamsDoneModule,
  IndicatedProcModule, SurgTeamModule, AnesthesiaModule, SurgDescModule,
  InOfficeProcModule, CertificateModule, ImagesModule, FilesModule,
  ConductModule, CONDUCT_ACTIONS,
});
