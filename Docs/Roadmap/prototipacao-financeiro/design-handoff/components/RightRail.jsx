// RightRail — mini-calendário, próximos, lista de espera e ocupação
const MiniCal = ({ selectedDate, onSelect }) => {
  const today = new Date();
  const month = selectedDate.getMonth();
  const year = selectedDate.getFullYear();
  const first = new Date(year, month, 1);
  const startOffset = first.getDay(); // 0 = sun
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const prevMonthDays = new Date(year, month, 0).getDate();

  const cells = [];
  for (let i = startOffset - 1; i >= 0; i--) {
    cells.push({ day: prevMonthDays - i, muted: true, date: new Date(year, month - 1, prevMonthDays - i) });
  }
  for (let d = 1; d <= daysInMonth; d++) {
    cells.push({ day: d, date: new Date(year, month, d) });
  }
  while (cells.length < 42) {
    const next = cells.length - (startOffset + daysInMonth) + 1;
    cells.push({ day: next, muted: true, date: new Date(year, month + 1, next) });
  }

  const same = (a, b) => a && b && a.toDateString() === b.toDateString();
  const heads = ['D','S','T','Q','Q','S','S'];
  // pseudo: days with appointments (every weekday + some)
  const hasAppts = (d) => {
    const dow = d.getDay();
    if (dow === 0 || dow === 6) return false;
    return ((d.getDate() * 7) % 5) !== 0;
  };

  const monthName = selectedDate.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });

  return (
    <div className="card rail-card">
      <div className="rh">
        <h3>Calendário</h3>
        <div className="nav">
          <button onClick={() => { const d = new Date(selectedDate); d.setMonth(d.getMonth() - 1); onSelect(d); }}>‹</button>
          <button onClick={() => { const d = new Date(selectedDate); d.setMonth(d.getMonth() + 1); onSelect(d); }}>›</button>
        </div>
      </div>
      <div style={{fontSize: 11, fontWeight: 700, color: 'var(--c-secondary)', textAlign: 'center', marginBottom: 8, textTransform: 'capitalize'}}>{monthName}</div>
      <div className="minical">
        {heads.map((h, i) => <div key={'h'+i} className="h">{h}</div>)}
        {cells.map((c, i) => {
          const isToday = same(c.date, today);
          const isSel = same(c.date, selectedDate);
          const has = hasAppts(c.date) && !c.muted;
          return (
            <div
              key={i}
              className={[
                'd',
                c.muted && 'muted',
                isToday && 'today',
                isSel && 'selected',
                has && 'has',
              ].filter(Boolean).join(' ')}
              onClick={() => onSelect(c.date)}
            >{c.day}</div>
          );
        })}
      </div>
    </div>
  );
};

const UpNext = ({ appts, onJump }) => {
  const upcoming = appts.filter(a => ['confirmed', 'checked-in', 'unconfirmed'].includes(a.status)).slice(0, 3);
  return (
    <div className="card rail-card">
      <div className="rh">
        <h3>Próximos</h3>
        <span className="lnk">Ver todos →</span>
      </div>
      <div className="upnext">
        {upcoming.map(a => (
          <div key={a.id} className="item" onClick={() => onJump(a.id)}>
            <div className="when">{a.time}</div>
            <div className="who">
              <b>{a.patient.name}</b>
              <span>{a.reason}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

const Waitlist = ({ waitlist }) => (
  <div className="card rail-card">
    <div className="rh">
      <h3><i className="fa-solid fa-list-ul" style={{color: 'hsl(var(--warning))', marginRight: 5, fontSize: 11}}></i>Lista de espera</h3>
      <span className="lnk">Gerenciar →</span>
    </div>
    <div className="waitlist">
      {waitlist.map(w => (
        <div key={w.id} className="w">
          <div className="av">{w.initials}</div>
          <div className="info">
            <b>{w.name}</b>
            <span>{w.reason} · há {w.since}</span>
          </div>
          <button title="Encaixar"><i className="fa-solid fa-plus"></i></button>
        </div>
      ))}
    </div>
  </div>
);

const Occupancy = ({ counts }) => {
  const slots = 18; // capacidade do dia
  const used = counts.total - counts.cancelled - counts.noShow;
  const pct = Math.round(used * 100 / slots);
  return (
    <div className="card rail-card">
      <div className="rh">
        <h3><i className="fa-solid fa-gauge-high" style={{color: 'hsl(var(--success))', marginRight: 5, fontSize: 11}}></i>Ocupação do dia</h3>
      </div>
      <div className="occ-bar"><div className="fill" style={{width: pct + '%'}}></div></div>
      <div className="occ-meta">
        <span><b>{used}</b> de {slots} slots</span>
        <span><b>{pct}%</b></span>
      </div>
      <div style={{fontSize: 11, color: 'hsl(var(--secondary) / 0.6)', marginTop: 8, paddingTop: 8, borderTop: '1px dashed hsl(var(--secondary) / 0.1)'}}>
        <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: 4}}>
          <span>Faturamento previsto</span>
          <b style={{color: 'var(--c-primary-dark)', fontVariantNumeric: 'tabular-nums'}}>R$ 4.280</b>
        </div>
        <div style={{display: 'flex', justifyContent: 'space-between'}}>
          <span>Tempo médio</span>
          <b style={{color: 'var(--c-primary-dark)'}}>32 min</b>
        </div>
      </div>
    </div>
  );
};

const RightRail = ({ selectedDate, onSelect, appts, waitlist, counts, onJump }) => (
  <aside className="rail">
    <MiniCal selectedDate={selectedDate} onSelect={onSelect} />
    <UpNext appts={appts} onJump={onJump} />
    <Waitlist waitlist={waitlist} />
    <Occupancy counts={counts} />
  </aside>
);

window.RightRail = RightRail;
