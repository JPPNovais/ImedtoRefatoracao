// DateStrip — strip horizontal de 14 dias com contagens
const DateStrip = ({ selectedDate, onSelect, counts, onPickerOpen }) => {
  const today = new Date();
  // build 14 days starting from 7 days before selected
  const base = new Date(selectedDate);
  base.setDate(base.getDate() - 6);
  const days = Array.from({ length: 14 }, (_, i) => {
    const d = new Date(base);
    d.setDate(base.getDate() + i);
    return d;
  });

  const sameDay = (a, b) => a.toDateString() === b.toDateString();
  const dows = ['DOM','SEG','TER','QUA','QUI','SEX','SÁB'];
  const monthName = selectedDate.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });

  const shift = (n) => {
    const nd = new Date(selectedDate);
    nd.setDate(nd.getDate() + n);
    onSelect(nd);
  };

  return (
    <div className="datestrip-wrap">
      <div className="datestrip-head">
        <div className="left">
          <div className="month">
            <i className="fa-solid fa-calendar" style={{color: 'hsl(var(--primary))'}}></i>
            <span style={{textTransform: 'capitalize'}}>{monthName}</span>
            <button className="picker-trigger" onClick={onPickerOpen}>
              <i className="fa-solid fa-calendar-day"></i>
              Ir para data
            </button>
          </div>
        </div>
        <div className="ctrls">
          <button className="today-btn" onClick={() => onSelect(new Date())}>Hoje</button>
          <button className="nav-btn" onClick={() => shift(-7)} aria-label="Semana anterior"><i className="fa-solid fa-angles-left"></i></button>
          <button className="nav-btn" onClick={() => shift(-1)} aria-label="Dia anterior"><i className="fa-solid fa-chevron-left"></i></button>
          <button className="nav-btn" onClick={() => shift(1)} aria-label="Próximo dia"><i className="fa-solid fa-chevron-right"></i></button>
          <button className="nav-btn" onClick={() => shift(7)} aria-label="Próxima semana"><i className="fa-solid fa-angles-right"></i></button>
        </div>
      </div>
      <div className="datestrip">
        {days.map((d, i) => {
          const sel = sameDay(d, selectedDate);
          const isToday = sameDay(d, today);
          const isPast = d < today && !isToday;
          const wknd = d.getDay() === 0 || d.getDay() === 6;
          // pseudo count distribution for demo (selected = real today count)
          const dayKey = d.toISOString().slice(0,10);
          let count;
          if (sel) count = counts.total;
          else {
            // deterministic pseudo based on date
            const seed = (d.getDate() * 7 + d.getMonth() * 13) % 14;
            count = wknd ? Math.max(0, seed - 8) : seed;
          }
          return (
            <div
              key={i}
              className={[
                'dchip',
                sel && 'selected',
                isToday && !sel && 'today',
                isPast && 'past',
                wknd && 'weekend',
              ].filter(Boolean).join(' ')}
              onClick={() => onSelect(d)}
            >
              <div className="dow">{dows[d.getDay()]}</div>
              <div className="dom">{d.getDate()}</div>
              <div className="meta">{count > 0 ? `${count} agend.` : '—'}</div>
              <div className="pip-row">
                {Array.from({length: Math.min(count, 5)}).map((_, k) => <div key={k} className="pip"></div>)}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

window.DateStrip = DateStrip;
