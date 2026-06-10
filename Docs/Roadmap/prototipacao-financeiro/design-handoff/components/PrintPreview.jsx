// PrintPreview.jsx — Pré-visualização do PDF do prontuário e da receita

const { focusPatient: pp, me: pDoc, moduleCatalog: pCat } = window.IMEDTO_CARE;

// Dados do estabelecimento (placeholder — viria do cadastro da clínica)
const clinic = {
  name: 'Clínica Imedto',
  tagline: 'Cardiologia · Medicina Diagnóstica',
  cnpj: '12.345.678/0001-90',
  address: 'Av. Paulista, 1842 · cj. 78 · Bela Vista',
  city: 'São Paulo / SP · CEP 01310-200',
  phone: '(11) 3030-4040',
  email: 'contato@clinicaimedto.com.br',
  site: 'imedto.com.br',
};

// ─── Modal genérico de pré-visualização de impressão ────────
const PrintModal = ({ open, onClose, title, type, children }) => {
  if (!open) return null;
  const handlePrint = () => {
    const sheet = document.querySelector('.print-sheet');
    if (!sheet) return;
    document.body.classList.add('print-mode');
    sheet.setAttribute('data-printing', 'true');
    setTimeout(() => {
      window.print();
      setTimeout(() => {
        document.body.classList.remove('print-mode');
        sheet.removeAttribute('data-printing');
      }, 200);
    }, 50);
  };
  return (
    <div className="print-bg" onClick={onClose}>
      <div className="print-shell" onClick={e => e.stopPropagation()}>
        <header className="print-topbar">
          <div className="pt-info">
            <i className="fa-solid fa-file-pdf"></i>
            <div>
              <strong>{title}</strong>
              <span>Pré-visualização · A4 · pronto para impressão</span>
            </div>
          </div>
          <div className="pt-actions">
            <button className="btn-secondary" onClick={onClose}>
              <i className="fa-solid fa-arrow-left"></i> Voltar editar
            </button>
            <button className="btn-secondary">
              <i className="fa-solid fa-download"></i> Baixar PDF
            </button>
            <button className="btn-success" onClick={handlePrint}>
              <i className="fa-solid fa-print"></i> Imprimir
            </button>
            <button className="pt-close" onClick={onClose}><i className="fa-solid fa-xmark"></i></button>
          </div>
        </header>
        <div className="print-scroll">
          <div className={`print-sheet sheet-${type}`}>
            {children}
          </div>
        </div>
      </div>
    </div>
  );
};

// ─── Cabeçalho institucional ─────────────────────────────────
const SheetHeader = ({ docTitle, docSubtitle }) => (
  <header className="sheet-head">
    <div className="sh-brand">
      <div className="sh-logo">
        <svg viewBox="0 0 40 40" width="44" height="44">
          <circle cx="20" cy="20" r="18" fill="hsl(218 70% 32%)" />
          <path d="M12 20 L17 20 L20 12 L24 28 L27 20 L32 20" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </div>
      <div className="sh-name">
        <h1>{clinic.name}</h1>
        <span>{clinic.tagline}</span>
      </div>
    </div>
    <div className="sh-doc">
      <h2>{docTitle}</h2>
      {docSubtitle && <span>{docSubtitle}</span>}
    </div>
    <div className="sh-contact">
      <div>{clinic.address}</div>
      <div>{clinic.city}</div>
      <div>{clinic.phone} · {clinic.email}</div>
      <div>CNPJ {clinic.cnpj}</div>
    </div>
  </header>
);

const SheetFooter = ({ pageNum = 1, pageTotal = 1 }) => (
  <footer className="sheet-foot">
    <div className="sf-sign">
      <div className="sf-line"></div>
      <strong>{pDoc.name}</strong>
      <span>{pDoc.role} · {pDoc.crm}</span>
      <small><i className="fa-solid fa-shield-halved"></i> Assinado digitalmente · ICP-Brasil · SHA-256 A4B2…F19C</small>
    </div>
    <div className="sf-meta">
      <span>Emitido em 12/05/2026 às {new Date().toTimeString().slice(0,5)}</span>
      <span>Página {pageNum} de {pageTotal}</span>
      <span>{clinic.site}</span>
    </div>
  </footer>
);

const PatientBlock = ({ patient }) => {
  const age = Math.floor((new Date() - new Date(patient.dob)) / (365.25 * 24 * 3600 * 1000));
  return (
    <section className="sheet-patient">
      <div className="sp-row">
        <div className="sp-cell sp-name">
          <label>Paciente</label>
          <strong>{patient.name}</strong>
        </div>
        <div className="sp-cell"><label>Idade</label><span>{age} anos</span></div>
        <div className="sp-cell"><label>Sexo</label><span>{patient.sex === 'F' ? 'Feminino' : 'Masculino'}</span></div>
        <div className="sp-cell"><label>Tipo sang.</label><span>{patient.bloodType}</span></div>
      </div>
      <div className="sp-row">
        <div className="sp-cell"><label>CPF</label><span>{patient.cpf}</span></div>
        <div className="sp-cell"><label>Nascimento</label><span>{new Date(patient.dob).toLocaleDateString('pt-BR')}</span></div>
        <div className="sp-cell"><label>Convênio</label><span>{patient.conv}</span></div>
        <div className="sp-cell"><label>Telefone</label><span>{patient.phone}</span></div>
      </div>
    </section>
  );
};

// ─── Renderiza um módulo no PDF ────────────────────────────
const ModulePrint = ({ id, data, label }) => {
  const cfg = pCat[id] || {};
  const title = label || cfg.name || id;

  // Render conforme tipo
  const renderContent = () => {
    if (data.text) return <p className="mp-text">{data.text}</p>;

    if (id === 'vitals') {
      const items = [
        ['PA', data.pa, 'mmHg'], ['FC', data.fc, 'bpm'], ['FR', data.fr, 'irpm'],
        ['Temp', data.temp, '°C'], ['SatO₂', data.sat, '%'],
        ['Peso', data.peso, 'kg'], ['Altura', data.altura, 'cm'], ['Glicemia', data.glic, 'mg/dL'],
      ].filter(([_, v]) => v);
      if (data.peso && data.altura) {
        const imc = (parseFloat(data.peso) / Math.pow(parseFloat(data.altura)/100, 2)).toFixed(1);
        items.push(['IMC', imc, 'kg/m²']);
      }
      return (
        <div className="mp-vitals">
          {items.map(([l, v, u], i) => <div key={i}><label>{l}</label><strong>{v} <small>{u}</small></strong></div>)}
        </div>
      );
    }

    if (id === 'physical' && data.notes) {
      const entries = Object.entries(data.notes);
      if (entries.length === 0) return null;
      return (
        <ul className="mp-list">
          {entries.map(([region, n]) => (
            <li key={region}><strong>{region}:</strong> {n.status && <em>({n.status})</em>} {n.text}</li>
          ))}
        </ul>
      );
    }

    if (id === 'cid' && data.list?.length) {
      return (
        <ul className="mp-list mp-cid">
          {data.list.map((c, i) => (
            <li key={i}><code>{c.code}</code> {c.desc} <em>· {c.type}</em></li>
          ))}
        </ul>
      );
    }

    if (id === 'soap') {
      const fields = [['S — Subjetivo', data.s], ['O — Objetivo', data.o], ['A — Avaliação', data.a], ['P — Plano', data.p]];
      return (
        <dl className="mp-soap">
          {fields.filter(([_, v]) => v).map(([l, v], i) => (
            <React.Fragment key={i}>
              <dt>{l}</dt><dd>{v}</dd>
            </React.Fragment>
          ))}
        </dl>
      );
    }

    if (id === 'exams' && data.requested?.length) {
      return (
        <div>
          <p><strong>Exames solicitados:</strong> {data.requested.join(' · ')}</p>
          {data.justification && <p className="mp-text">{data.justification}</p>}
        </div>
      );
    }

    if (id === 'examsDone' && data.list?.length) {
      return (
        <ul className="mp-list">
          {data.list.map((e, i) => <li key={i}><strong>{e.name}</strong> {e.date && `· ${e.date}`} — {e.result}</li>)}
        </ul>
      );
    }

    if (id === 'indicatedProc' && data.items?.length) {
      return (
        <ul className="mp-list">
          {data.items.map((p, i) => <li key={i}><strong>{p.name}</strong> <em>({p.urgency})</em> — {p.justification}</li>)}
        </ul>
      );
    }

    if (id === 'surgTeam') {
      const map = [['Cirurgião principal', data.surgeon], ['1º Auxiliar', data.aux1], ['2º Auxiliar', data.aux2], ['Anestesiologista', data.anesth], ['Instrumentadora', data.instr], ['Circulante', data.circ]].filter(([_,v]) => v);
      return <dl className="mp-kv">{map.map(([k,v]) => <React.Fragment key={k}><dt>{k}</dt><dd>{v}</dd></React.Fragment>)}</dl>;
    }

    if (id === 'anesthesia') {
      return (
        <div>
          {data.type && <p><strong>Tipo:</strong> {data.type} {data.asa && ` · ASA ${data.asa}`}</p>}
          {data.drugs && <p><strong>Drogas:</strong> {data.drugs}</p>}
          {data.time && <p><strong>Tempo:</strong> {data.time}</p>}
          {data.monitor && <p><strong>Monitorização:</strong> {data.monitor}</p>}
          {data.notes && <p className="mp-text">{data.notes}</p>}
        </div>
      );
    }

    if (id === 'surgDesc') {
      return (
        <div>
          {data.procedure && <p><strong>Procedimento:</strong> {data.procedure} {data.duration && ` · ${data.duration}`}</p>}
          {data.technique && <p className="mp-text">{data.technique}</p>}
          {data.complications && <p><strong>Intercorrências:</strong> {data.complications}</p>}
        </div>
      );
    }

    if (id === 'inOfficeProc') {
      return (
        <div>
          {data.procedure && <p><strong>{data.procedure}</strong> {data.site && ` · ${data.site}`}</p>}
          {data.materials && <p><strong>Material:</strong> {data.materials}</p>}
          {data.anesth && <p><strong>Anestesia:</strong> {data.anesth}</p>}
          {data.notes && <p className="mp-text">{data.notes}</p>}
        </div>
      );
    }

    return null;
  };

  const content = renderContent();
  if (!content) return null;

  return (
    <section className="mp-section">
      <h3>{title}</h3>
      {content}
    </section>
  );
};

// ─── Folha do prontuário completo ──────────────────────────
const ProntuarioSheet = ({ patient, modules, moduleData, open, onClose }) => {
  // Filtra módulos que serão impressos (excluindo prescrição e atestados)
  const printable = modules.filter(id => !['prescription', 'certificate', 'images', 'files'].includes(id));
  const filled = printable.filter(id => {
    const d = moduleData[id];
    if (!d) return false;
    return Object.values(d).some(v =>
      typeof v === 'string' ? v.trim() : Array.isArray(v) ? v.length : (v && typeof v === 'object') ? Object.keys(v).length : v
    );
  });

  return (
    <PrintModal open={open} onClose={onClose} title="Prontuário do atendimento" type="prontuario">
      <SheetHeader docTitle="PRONTUÁRIO MÉDICO" docSubtitle={`Atendimento de 12 de maio de 2026 · às 08:38`} />
      <PatientBlock patient={patient} />
      <div className="sheet-body">
        {filled.map(id => <ModulePrint key={id} id={id} data={moduleData[id] || {}} />)}
        {filled.length === 0 && (
          <p style={{textAlign:'center', color:'#999', padding:'40px'}}>Nenhum módulo preenchido para imprimir.</p>
        )}
      </div>
      <SheetFooter />
    </PrintModal>
  );
};

// ─── Folha da receita médica ───────────────────────────────
const ReceitaSheet = ({ patient, items, continuous, controlled, open, onClose, onAdd, onUpdate, onRemove }) => {
  const filledItems = items.filter(it => it.drug);
  return (
    <PrintModal open={open} onClose={onClose}
      title={controlled ? 'Receituário de controle especial' : continuous ? 'Receita de uso contínuo' : 'Receita médica'}
      type={`receita ${controlled ? 'receita-controlled' : ''}`}>
      <SheetHeader
        docTitle={controlled ? 'RECEITUÁRIO DE CONTROLE ESPECIAL' : continuous ? 'RECEITA DE USO CONTÍNUO' : 'RECEITA MÉDICA'}
        docSubtitle={`Emitida em 12 de maio de 2026 · válida por ${controlled ? '30' : '180'} dias`}
      />
      <PatientBlock patient={patient} />

      <div className="receita-body">
        <ol className="receita-list">
          {filledItems.map((it, i) => (
            <li key={i} className="rx-print-item">
              <div className="rxp-num">{i+1}</div>
              <div className="rxp-info">
                <div className="rxp-line1">
                  <strong>{it.drug}</strong>
                  {it.dose && <span className="rxp-dose">{it.dose}</span>}
                  {it.via && <span className="rxp-via">{it.via}</span>}
                </div>
                <div className="rxp-line2">
                  {it.freq && <span><i className="fa-solid fa-clock"></i> {it.freq}</span>}
                  {it.dur && <span><i className="fa-solid fa-calendar-days"></i> Por {it.dur}</span>}
                </div>
                <div className="rxp-line3">
                  <em>Tomar conforme orientação médica. Não interromper o tratamento sem consultar o médico.</em>
                </div>
              </div>
            </li>
          ))}
          {filledItems.length === 0 && (
            <li className="rxp-empty">
              <i className="fa-solid fa-prescription"></i>
              <p>Nenhum medicamento prescrito ainda. Adicione abaixo.</p>
            </li>
          )}
        </ol>

        {/* Editor inline — só visível na pré-visualização, não no impresso */}
        <div className="receita-editor no-print">
          <div className="re-head">
            <i className="fa-solid fa-pen-to-square"></i>
            <h4>Editar medicamentos da receita</h4>
            <span>Modificações refletem na pré-visualização à esquerda imediatamente</span>
          </div>
          {items.map((it, i) => (
            <div key={i} className="re-row">
              <div className="re-num">{i+1}</div>
              <input placeholder="Medicamento" value={it.drug} onChange={e => onUpdate(i, { drug: e.target.value })} />
              <input placeholder="Dose" value={it.dose} onChange={e => onUpdate(i, { dose: e.target.value })} />
              <select value={it.via} onChange={e => onUpdate(i, { via: e.target.value })}>
                <option>VO</option><option>IV</option><option>IM</option><option>SC</option><option>Tópico</option>
              </select>
              <input placeholder="Frequência" value={it.freq} onChange={e => onUpdate(i, { freq: e.target.value })} />
              <input placeholder="Duração" value={it.dur} onChange={e => onUpdate(i, { dur: e.target.value })} />
              <button className="btn-ghost sm" onClick={() => onRemove(i)}><i className="fa-solid fa-trash"></i></button>
            </div>
          ))}
          <button className="re-add" onClick={onAdd}>
            <i className="fa-solid fa-plus"></i> Adicionar medicamento
          </button>
        </div>
      </div>

      {controlled && (
        <div className="receita-2via">
          <span><i className="fa-solid fa-copy"></i> 1ª via — Farmácia · 2ª via — Paciente</span>
        </div>
      )}

      <SheetFooter />
    </PrintModal>
  );
};

Object.assign(window, { ProntuarioSheet, ReceitaSheet });
