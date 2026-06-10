// StatCards — cards de status clicáveis (filtros)
const StatCards = ({ counts, activeFilter, onFilter }) => {
  const cards = [
    { key: null,           lbl: 'Total do dia',      i: 'fa-calendar-check', color: 'hsl(var(--primary))', n: counts.total, foot: 'agendamentos' },
    { key: 'confirmed',    lbl: 'Confirmados',       i: 'fa-circle-check',   color: 'hsl(var(--success))', n: counts.confirmed, foot: `${counts.total ? Math.round(counts.confirmed*100/counts.total) : 0}% do dia` },
    { key: 'unconfirmed',  lbl: 'Aguardando',        i: 'fa-hourglass-half', color: 'hsl(var(--warning))', n: counts.unconfirmed, foot: 'reenviar lembrete' },
    { key: 'checked-in',   lbl: 'Sala de espera',    i: 'fa-couch',          color: 'hsl(var(--info))',    n: counts.checkedIn, foot: counts.checkedIn ? 'há 7 min em média' : 'nenhum aguardando' },
    { key: 'no-show',      lbl: 'Faltas',            i: 'fa-user-xmark',     color: 'hsl(var(--error))',   n: counts.noShow, foot: counts.noShow ? `${Math.round(counts.noShow*100/counts.total)}% no-show` : 'sem faltas' },
    { key: 'cancelled',    lbl: 'Cancelados',        i: 'fa-ban',            color: 'hsl(280 60% 50%)',    n: counts.cancelled, foot: counts.cancelled ? 'preencher c/ encaixe' : '—' },
  ];
  return (
    <div className="stat-grid">
      {cards.map((c, i) => (
        <div
          key={i}
          className={`stat-card ${activeFilter === c.key ? 'active' : ''}`}
          style={{ '--accent-color': c.color }}
          onClick={() => onFilter(activeFilter === c.key ? null : c.key)}
        >
          <div className="top">
            <div className="lbl">{c.lbl}</div>
            <i className={`fa-solid ${c.i}`}></i>
          </div>
          <div className="num">{c.n}<small>{c.key === null ? '' : ''}</small></div>
          <div className="foot">{c.foot}</div>
        </div>
      ))}
    </div>
  );
};

window.StatCards = StatCards;
