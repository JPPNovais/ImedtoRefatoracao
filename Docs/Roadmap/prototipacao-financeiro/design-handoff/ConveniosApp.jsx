// ConveniosApp.jsx — Cadastro de convênios do estabelecimento (configuração)

const SEED_CONVENIOS = [
  { id: 'cv1', nome: 'Unimed', ans: '339679', ativo: true, planos: ['Nacional', 'Estadual'] },
  { id: 'cv2', nome: 'Bradesco Saúde', ans: '005711', ativo: true, planos: ['Top', 'Efetivo'] },
  { id: 'cv3', nome: 'Amil', ans: '326305', ativo: true, planos: ['S450'] },
];

const CV_TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "emptyConvenios": false
}/*EDITMODE-END*/;

// ─── Drawer de criar/editar ──────────────────────────────────
const ConvenioDrawer = ({ open, convenio, onClose, onSave }) => {
  const [form, setForm] = React.useState(null);
  const [planoInput, setPlanoInput] = React.useState('');
  React.useEffect(() => {
    if (!open) return;
    setForm(convenio ? { ...convenio, planos: [...convenio.planos] } : { id: 'cv' + Date.now(), nome: '', ans: '', ativo: true, planos: [] });
    setPlanoInput('');
  }, [open, convenio]);
  if (!open || !form) return null;

  const addPlano = () => { const v = planoInput.trim(); if (!v) return; setForm({ ...form, planos: [...form.planos, v] }); setPlanoInput(''); };
  const removePlano = (i) => setForm({ ...form, planos: form.planos.filter((_, idx) => idx !== i) });

  return (
    <div className="drawer-overlay" onClick={onClose}>
      <div className="drawer" onClick={e => e.stopPropagation()}>
        <header className="drawer-head">
          <h2>{convenio ? 'Editar convênio' : 'Novo convênio'}</h2>
          <button className="modal-close" onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
        </header>
        <div className="drawer-body">
          <div className="field-group full">
            <label>Nome do convênio <em>*</em></label>
            <input type="text" autoFocus placeholder="Ex: Unimed" value={form.nome} onChange={e => setForm({ ...form, nome: e.target.value })} />
          </div>
          <div className="field-group full">
            <label>Registro ANS <span className="opt">opcional</span></label>
            <input type="text" placeholder="Ex: 339679" value={form.ans} onChange={e => setForm({ ...form, ans: e.target.value })} />
          </div>
          <div className="field-group full">
            <label>Planos</label>
            <div className="plano-add">
              <input type="text" placeholder="Ex: Nacional" value={planoInput}
                onChange={e => setPlanoInput(e.target.value)}
                onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); addPlano(); } }} />
              <button className="btn-secondary sm" onClick={addPlano}><i className="fa-solid fa-plus"></i> Adicionar</button>
            </div>
            <div className="plano-chips">
              {form.planos.length === 0 && <span className="plano-empty">Nenhum plano adicionado ainda.</span>}
              {form.planos.map((p, i) => (
                <span key={i} className="plano-chip">{p}<button onClick={() => removePlano(i)}><i className="fa-solid fa-xmark"></i></button></span>
              ))}
            </div>
          </div>
          <label className="cv-active-toggle">
            <input type="checkbox" checked={form.ativo} onChange={e => setForm({ ...form, ativo: e.target.checked })} />
            <span className={`cfg-switch ${form.ativo ? 'on' : ''}`}><span className="knob"></span></span>
            <div><b>Convênio ativo</b><span>Convênios inativos não aparecem no check-in nem no faturamento.</span></div>
          </label>
        </div>
        <footer className="drawer-foot">
          <button className="btn-ghost" onClick={onClose}>Cancelar</button>
          <div style={{ flex: 1 }}></div>
          <button className="btn-primary" disabled={!form.nome.trim()} onClick={() => onSave(form)}>
            <i className="fa-solid fa-floppy-disk"></i> Salvar convênio
          </button>
        </footer>
      </div>
    </div>
  );
};

const ConveniosApp = () => {
  const [tweaks, setTweak] = useTweaks(CV_TWEAK_DEFAULTS);
  const [convenios, setConvenios] = React.useState(SEED_CONVENIOS);
  const [drawer, setDrawer] = React.useState({ open: false, convenio: null });
  const [toast, setToast] = React.useState(null);
  const flash = (m) => { setToast(m); setTimeout(() => setToast(null), 2400); };

  const list = tweaks.emptyConvenios ? [] : convenios;

  const save = (form) => {
    setConvenios(prev => {
      const exists = prev.find(c => c.id === form.id);
      return exists ? prev.map(c => c.id === form.id ? form : c) : [...prev, form];
    });
    setDrawer({ open: false, convenio: null });
    if (tweaks.emptyConvenios) setTweak('emptyConvenios', false);
    flash(`Convênio "${form.nome}" salvo`);
  };
  const toggleActive = (id) => setConvenios(prev => prev.map(c => c.id === id ? { ...c, ativo: !c.ativo } : c));
  const remove = (id) => { setConvenios(prev => prev.filter(c => c.id !== id)); flash('Convênio removido'); };

  return (
    <>
      <TopBar />
      <Sidebar active="Financeiro" />
      <main>
        <div className="page cf-page">
          <a href="Configurações - submenu.html" className="pd-back"><i className="fa-solid fa-chevron-left"></i> Configurações</a>
          <div className="page-head">
            <div>
              <h1>Convênios</h1>
              <div className="sub">
                <span><i className="fa-solid fa-building"></i> Clínica Vita — Unidade Centro</span>
                <span style={{ color: 'hsl(var(--secondary) / 0.3)' }}>•</span>
                <span>{list.length} {list.length === 1 ? 'convênio' : 'convênios'}</span>
              </div>
            </div>
            <button className="btn-primary" onClick={() => setDrawer({ open: true, convenio: null })}>
              <i className="fa-solid fa-plus"></i> Novo convênio
            </button>
          </div>

          {list.length === 0 ? (
            <div className="cf-empty" style={{ marginTop: 20 }}>
              <i className="fa-solid fa-id-card-clip"></i>
              <b>Nenhum convênio cadastrado</b>
              <p>Cadastre os convênios que a clínica atende para habilitar o faturamento por plano de saúde.</p>
              <button className="btn-primary" onClick={() => setDrawer({ open: true, convenio: null })}><i className="fa-solid fa-plus"></i> Cadastrar primeiro convênio</button>
            </div>
          ) : (
            <div className="cv-list">
              {list.map(c => (
                <div key={c.id} className={`cv-item ${!c.ativo ? 'inactive' : ''}`}>
                  <div className="cv-item-ic"><i className="fa-solid fa-shield-halved"></i></div>
                  <div className="cv-item-main">
                    <div className="cv-item-name">
                      <b>{c.nome}</b>
                      {c.ans && <span className="cv-ans">ANS {c.ans}</span>}
                      {!c.ativo && <span className="cv-inactive-tag">Inativo</span>}
                    </div>
                    <div className="cv-item-planos">
                      {c.planos.length === 0 ? <span className="cv-noplano">Sem planos cadastrados</span> :
                        c.planos.map((p, i) => <span key={i} className="cv-plano-tag">{p}</span>)}
                    </div>
                  </div>
                  <div className="cv-item-actions">
                    <button className={`cfg-switch ${c.ativo ? 'on' : ''}`} title={c.ativo ? 'Desativar' : 'Ativar'} onClick={() => toggleActive(c.id)}><span className="knob"></span></button>
                    <button className="lr-btn" title="Editar" onClick={() => setDrawer({ open: true, convenio: c })}><i className="fa-solid fa-pen"></i></button>
                    <button className="lr-btn danger" title="Remover" onClick={() => remove(c.id)}><i className="fa-solid fa-trash"></i></button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <TweaksPanel title="Tweaks">
          <TweakSection title="Estados">
            <TweakToggle label="Nenhum convênio cadastrado (empty state)" value={tweaks.emptyConvenios} onChange={v => setTweak('emptyConvenios', v)} />
          </TweakSection>
        </TweaksPanel>

        {toast && <div className="toast"><i className="fa-solid fa-circle-check"></i> {toast}</div>}
      </main>

      <ConvenioDrawer open={drawer.open} convenio={drawer.convenio} onClose={() => setDrawer({ open: false, convenio: null })} onSave={save} />
    </>
  );
};

ReactDOM.createRoot(document.getElementById('root')).render(<ConveniosApp />);
