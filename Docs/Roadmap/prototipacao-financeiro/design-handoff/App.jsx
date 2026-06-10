// App.jsx — Imedto Agenda

const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "density": "comfortable",
  "showEmptySlots": true,
  "showReadyChecklist": true,
  "groupByPeriod": true,
  "showWaitlist": true,
  "primaryAction": "single",
  "clinicUsesConvenio": true,
  "priceTableConfigured": true,
  "canDiscount": true
}/*EDITMODE-END*/;

const App = () => {
  const { waitlist, statuses, tipos, creditFeeFor } = window.AGENDA_DATA;
  const [appts, setAppts] = React.useState(window.AGENDA_DATA.appts);
  const [tweaks, setTweak] = useTweaks(TWEAK_DEFAULTS);
  const [checkInAppt, setCheckInAppt] = React.useState(null);
  const [paymentAppt, setPaymentAppt] = React.useState(null);
  const [selectedDate, setSelectedDate] = React.useState(new Date());
  const [filter, setFilter] = React.useState(null);
  const [expandedId, setExpandedId] = React.useState(null);
  const [search, setSearch] = React.useState('');
  const [provider, setProvider] = React.useState('Dra. Renata Lopes');
  const [tab, setTab] = React.useState('lista');
  const [toast, setToast] = React.useState(null);
  const [showNewAppt, setShowNewAppt] = React.useState(false);
  const [editAppt, setEditAppt] = React.useState(null);
  const [editFocusReschedule, setEditFocusReschedule] = React.useState(false);

  const handleCreated = (patient, details, isNew) => {
    setShowNewAppt(false);
    if (details.waitlist) {
      setToast(`${patient.name} adicionado à lista de espera${isNew ? ' (novo cadastro)' : ''}`);
    } else {
      setToast(`Agendamento criado para ${patient.name} às ${details.time}${isNew ? ' (paciente cadastrado)' : ''}`);
    }
  };

  React.useEffect(() => {
    document.body.classList.toggle('dense', tweaks.density === 'compact');
  }, [tweaks.density]);

  // counts (used by stat cards)
  const counts = React.useMemo(() => {
    const c = { total: appts.length, confirmed: 0, unconfirmed: 0, checkedIn: 0, inProgress: 0, completed: 0, noShow: 0, cancelled: 0 };
    appts.forEach(a => {
      if (a.status === 'confirmed') c.confirmed++;
      if (a.status === 'unconfirmed') c.unconfirmed++;
      if (a.status === 'checked-in') c.checkedIn++;
      if (a.status === 'in-progress') c.inProgress++;
      if (a.status === 'completed') c.completed++;
      if (a.status === 'no-show') c.noShow++;
      if (a.status === 'cancelled') c.cancelled++;
    });
    return c;
  }, [appts]);

  const filtered = React.useMemo(() => {
    let list = appts;
    if (filter) list = list.filter(a => a.status === filter);
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(a =>
        a.patient.name.toLowerCase().includes(q) ||
        a.reason.toLowerCase().includes(q) ||
        a.convenio.toLowerCase().includes(q)
      );
    }
    return list;
  }, [appts, filter, search]);

  const onAction = (appt, action) => {
    if (action === 'edit') { setEditFocusReschedule(false); setEditAppt(appt); return; }
    if (action === 'reschedule') { setEditFocusReschedule(true); setEditAppt(appt); return; }
    if (action === 'check-in') { setCheckInAppt(appt); return; }
    if (action === 'payment') { setPaymentAppt(appt); return; }
    const labels = {
      'check-in': `Check-in registrado para ${appt.patient.name}`,
      'remind': `Lembrete reenviado por WhatsApp para ${appt.patient.name}`,
      'start': `Atendimento iniciado — ${appt.patient.name}`,
      'end': `Atendimento finalizado — ${appt.patient.name}`,
      'reschedule': `Abrindo reagendamento para ${appt.patient.name}`,
      'fill': 'Encaixe da lista de espera',
      'msg': `Mensagem aberta para ${appt.patient.name}`,
      'call': `Ligando para ${appt.patient.phone}`,
      'edit': `Editando agendamento de ${appt.patient.name}`,
      'chart': `Abrindo prontuário de ${appt.patient.name}`,
      'more': 'Mais ações',
    };
    setToast(labels[action] || 'Ação executada');
    setTimeout(() => setToast(null), 2400);
  };

  const handleCheckInConfirm = (appt, data) => {
    const now = new Date().toTimeString().slice(0, 5);
    setAppts(prev => prev.map(a => a.id === appt.id ? { ...a, status: 'checked-in', arrivedAt: now } : a));
    setCheckInAppt(null);
    let msg;
    if (data.tipo === 'particular') {
      const brl = (data.valueCents / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2 });
      msg = `Check-in realizado · Cobrança de R$ ${brl} criada`;
    } else {
      msg = `Check-in realizado · Convênio ${data.convenio}${data.guia ? ` · Guia ${data.guia}` : ''}`;
    }
    setToast(msg);
    setTimeout(() => setToast(null), 3200);
  };

  const handleRegisterPayment = (appt, payment) => {
    const newPay = {
      id: 'p' + Date.now(),
      date: new Date().toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }) + ' ' + new Date().toTimeString().slice(0, 5),
      method: payment.method, amount: payment.amount,
      parcelas: payment.parcelas, by: 'Ana Souza (recepção)',
    };
    setAppts(prev => prev.map(a => {
      if (a.id !== appt.id) return a;
      const billing = { ...a.billing, payments: [...a.billing.payments, newPay] };
      return { ...a, billing };
    }));
    const paidAfter = appt.billing.payments.reduce((s, p) => s + p.amount, 0) + payment.amount;
    const net = appt.billing.total - (appt.billing.discount || 0);
    const msg = paidAfter >= net
      ? `Pagamento registrado · cobrança quitada`
      : `Pagamento de R$ ${(payment.amount / 100).toLocaleString('pt-BR', { minimumFractionDigits: 2 })} registrado`;
    setToast(msg);
    setTimeout(() => setToast(null), 2800);
  };

  const handleDiscount = (appt, discountCents) => {
    setAppts(prev => prev.map(a => a.id === appt.id ? { ...a, billing: { ...a.billing, discount: discountCents } } : a));
  };

  const handleReceipt = (appt) => {
    setToast(`Recibo emitido para ${appt.patient.name}`);
    setTimeout(() => setToast(null), 2400);
  };

  // mantém o modal de pagamento sincronizado com o appt atualizado em estado
  const paymentApptLive = React.useMemo(
    () => paymentAppt ? appts.find(a => a.id === paymentAppt.id) || paymentAppt : null,
    [paymentAppt, appts]
  );

  const onToggleExpand = (id) => setExpandedId(prev => prev === id ? null : id);

  const onJump = (id) => {
    setExpandedId(id);
    setTimeout(() => {
      const el = document.querySelector(`[data-appt="${id}"]`);
      if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }, 50);
  };

  const todayLabel = selectedDate.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  const isToday = new Date().toDateString() === selectedDate.toDateString();

  // Insert lunch break + now-line into rendered list
  const renderListWithBreaks = () => {
    const out = [];
    let prev = null;
    filtered.forEach((a) => {
      const [h, m] = a.time.split(':').map(Number);
      const minutes = h * 60 + m;
      // before this row, insert markers
      if (prev) {
        const [ph, pm] = prev.time.split(':').map(Number);
        const prevEnd = ph * 60 + pm + prev.duration;
        const gap = minutes - prevEnd;
        // lunch break gap (>= 60min between 11:30 and 14:00)
        if (gap >= 60 && prevEnd >= 11 * 60 && minutes <= 14 * 60) {
          out.push(<div className="time-gap" key={'lunch-' + a.id}>
            <i className="fa-solid fa-utensils"></i>
            Intervalo de almoço · {Math.round(gap/60*10)/10}h disponíveis
          </div>);
        } else if (tweaks.showEmptySlots && gap >= 15 && gap < 60) {
          // free slot — encaixe possível
          const fmt = (mn) => `${String(Math.floor(mn/60)).padStart(2,'0')}:${String(mn%60).padStart(2,'0')}`;
          out.push(
            <div className="empty-slot" key={'free-' + a.id} onClick={() => setShowNewAppt(true)}>
              <div className="et">{fmt(prevEnd)}</div>
              <div className="ed">Slot livre · {gap} min</div>
              <div className="add"><i className="fa-solid fa-plus"></i> Agendar</div>
            </div>
          );
        }

        // now-line: insert before first appointment whose time is >= NOW (09:42)
        const NOW = 9 * 60 + 42;
        const prevMin = ph * 60 + pm;
        if (isToday && !filter && prevMin < NOW && minutes >= NOW && !out.some(x => x.key === 'now')) {
          out.push(<div className="now-line" key="now"><i className="fa-solid fa-circle-arrow-right"></i>Agora · 09:42</div>);
        }
      }
      out.push(
        <div key={a.id} data-appt={a.id}>
          <AppointmentRow
            appt={a}
            expanded={expandedId === a.id}
            onToggle={onToggleExpand}
            onAction={onAction}
            statuses={statuses}
            tipos={tipos}
          />
        </div>
      );
      prev = a;
    });
    return out;
  };

  return (
    <>
      <TopBar onSearch={setSearch} />
      <Sidebar />
      <main>
        <div className="page">
          {/* Header */}
          <div className="page-head">
            <div>
              <h1 style={{textTransform: 'capitalize'}}>{todayLabel}</h1>
              <div className="sub">
                {isToday && <><span className="live-dot"></span><span>Atualizado agora · 09:42</span></>}
                {!isToday && <span><i className="fa-solid fa-calendar-day" style={{marginRight: 6}}></i>Visualizando agenda</span>}
                <span style={{color: 'hsl(var(--secondary) / 0.3)'}}>•</span>
                <span><i className="fa-solid fa-user-doctor" style={{marginRight: 6}}></i>{provider}</span>
                <span style={{color: 'hsl(var(--secondary) / 0.3)'}}>•</span>
                <span><i className="fa-solid fa-location-dot" style={{marginRight: 6}}></i>Clínica Vita Centro · Sala 4</span>
              </div>
            </div>
            <div style={{display: 'flex', gap: 8, alignItems: 'center'}}>
              <button className="btn-primary" onClick={() => setShowNewAppt(true)}><i className="fa-solid fa-plus"></i>Novo agendamento</button>
            </div>
          </div>

          {/* Date strip */}
          <DateStrip selectedDate={selectedDate} onSelect={setSelectedDate} counts={counts} onPickerOpen={() => setToast('Date picker abriria aqui')} />

          {/* Stat cards / filters */}
          <StatCards counts={counts} activeFilter={filter} onFilter={setFilter} />

          {/* Main grid */}
          <div className="agenda-grid">
            <div className="card agenda-card">
              {/* Toolbar */}
              <div className="agenda-toolbar">
                <div className="tabs">
                  <button className={`tab ${tab === 'lista' ? 'active' : ''}`} onClick={() => setTab('lista')}>
                    <i className="fa-solid fa-list-ul"></i>Lista
                  </button>
                  <button className={`tab ${tab === 'timeline' ? 'active' : ''}`} onClick={() => setTab('timeline')}>
                    <i className="fa-solid fa-stream"></i>Linha do tempo
                  </button>
                  <button className={`tab ${tab === 'semana' ? 'active' : ''}`} onClick={() => setTab('semana')}>
                    <i className="fa-solid fa-calendar-week"></i>Semana
                  </button>
                </div>
                <select value={provider} onChange={e => setProvider(e.target.value)}>
                  <option>Dra. Renata Lopes</option>
                  <option>Dr. Marcos Rocha</option>
                  <option>Dr. Antônio Vieira</option>
                  <option>Todos os profissionais</option>
                </select>
                <select defaultValue="all">
                  <option value="all">Todos os tipos</option>
                  <option>Consulta</option>
                  <option>Retorno</option>
                  <option>Primeira vez</option>
                  <option>Exame</option>
                  <option>Procedimento</option>
                  <option>Teleconsulta</option>
                </select>
              </div>

              {/* Active filter bar */}
              {filter && (
                <div className="filter-bar">
                  <i className="fa-solid fa-filter"></i>
                  <span>Filtrando por <b>{statuses[filter].label}</b> · <b>{filtered.length}</b> {filtered.length === 1 ? 'agendamento' : 'agendamentos'}</span>
                  <button className="clear" onClick={() => setFilter(null)}>
                    Limpar <i className="fa-solid fa-xmark" style={{fontSize: 10, marginLeft: 4}}></i>
                  </button>
                </div>
              )}

              {/* Legenda — only when no filter */}
              {!filter && (
                <div className="legend">
                  <div className="item"><span className="swatch" style={{background: 'hsl(var(--success))'}}></span>Confirmado</div>
                  <div className="item"><span className="swatch" style={{background: 'hsl(var(--warning))'}}></span>Aguardando</div>
                  <div className="item"><span className="swatch" style={{background: 'hsl(var(--info))'}}></span>Em sala</div>
                  <div className="item"><span className="swatch" style={{background: 'hsl(280 60% 50%)'}}></span>Em atendimento</div>
                  <div className="item"><span className="swatch" style={{background: 'hsl(var(--secondary) / 0.3)'}}></span>Concluído</div>
                  <div className="item"><span className="swatch" style={{background: 'hsl(var(--error))'}}></span>Faltou / Cancelado</div>
                </div>
              )}

              {/* List */}
              <div className="appts">
                {filtered.length === 0 ? (
                  <div className="empty-state">
                    <i className="fa-solid fa-calendar-xmark"></i>
                    <b>Nenhum agendamento encontrado</b>
                    <p>Tente ajustar os filtros ou criar um novo agendamento.</p>
                    <button className="btn-primary" onClick={() => setShowNewAppt(true)}><i className="fa-solid fa-plus"></i>Novo agendamento</button>
                  </div>
                ) : renderListWithBreaks()}
              </div>
            </div>

            <RightRail
              selectedDate={selectedDate}
              onSelect={setSelectedDate}
              appts={appts}
              waitlist={waitlist}
              counts={counts}
              onJump={onJump}
            />
          </div>
        </div>

        {/* Tweaks panel */}
        <TweaksPanel title="Tweaks">
          <TweakSection title="Densidade">
            <TweakRadio
              value={tweaks.density}
              onChange={v => setTweak('density', v)}
              options={[
                { value: 'comfortable', label: 'Confortável' },
                { value: 'compact', label: 'Compacto' },
              ]}
            />
          </TweakSection>
          <TweakSection title="Conteúdo">
            <TweakToggle
              label="Mostrar slots livres entre agendamentos"
              value={tweaks.showEmptySlots}
              onChange={v => setTweak('showEmptySlots', v)}
            />
            <TweakToggle
              label="Checklist de prontidão (docs, anamnese, copay)"
              value={tweaks.showReadyChecklist}
              onChange={v => setTweak('showReadyChecklist', v)}
            />
            <TweakToggle
              label="Mostrar lista de espera no painel lateral"
              value={tweaks.showWaitlist}
              onChange={v => setTweak('showWaitlist', v)}
            />
          </TweakSection>
          <TweakSection title="Check-in financeiro">
            <TweakToggle
              label="Clínica atende convênio"
              value={tweaks.clinicUsesConvenio}
              onChange={v => setTweak('clinicUsesConvenio', v)}
            />
            <TweakToggle
              label="Tabela de preços configurada (valor sugerido)"
              value={tweaks.priceTableConfigured}
              onChange={v => setTweak('priceTableConfigured', v)}
            />
            <TweakToggle
              label="Permissão para aplicar desconto"
              value={tweaks.canDiscount}
              onChange={v => setTweak('canDiscount', v)}
            />
          </TweakSection>
        </TweaksPanel>

        {toast && (
          <div className="toast">
            <i className="fa-solid fa-circle-check"></i>
            {toast}
          </div>
        )}
      </main>
      <NewAppointmentModal
        open={showNewAppt}
        onClose={() => setShowNewAppt(false)}
        onCreated={handleCreated}
      />
      <EditAppointmentModal
        open={!!editAppt}
        appt={editAppt}
        statuses={statuses}
        tipos={tipos}
        focusReschedule={editFocusReschedule}
        onClose={() => setEditAppt(null)}
        onSaved={(a, form, meta) => {
          setEditAppt(null);
          if (meta.isReschedule) {
            setToast(`${a.patient.name} reagendado para ${form.time}${meta.notifyPatient ? ' · paciente notificado' : ''}`);
          } else {
            setToast(`Alterações salvas para ${a.patient.name}${meta.notifyPatient ? ' · paciente notificado' : ''}`);
          }
          setTimeout(() => setToast(null), 2400);
        }}
      />
      <CheckInModal
        open={!!checkInAppt}
        appt={checkInAppt}
        statuses={statuses}
        tipos={tipos}
        clinicUsesConvenio={tweaks.clinicUsesConvenio}
        priceTableConfigured={tweaks.priceTableConfigured}
        onClose={() => setCheckInAppt(null)}
        onConfirm={handleCheckInConfirm}
      />
      <PaymentModal
        open={!!paymentApptLive}
        appt={paymentApptLive}
        canDiscount={tweaks.canDiscount}
        creditFeeFor={creditFeeFor}
        onClose={() => setPaymentAppt(null)}
        onRegister={handleRegisterPayment}
        onDiscount={handleDiscount}
        onReceipt={handleReceipt}
      />
    </>
  );
};

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
