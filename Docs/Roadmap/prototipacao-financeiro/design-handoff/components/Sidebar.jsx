// Sidebar — navegação retrátil do Imedto

const Sidebar = ({ active = 'Agenda' }) => {
  // pinned: usuário fixou aberta. hovered: expandida temporariamente.
  const [pinned, setPinned] = React.useState(false);
  const [hovered, setHovered] = React.useState(false);

  React.useEffect(() => {
    document.body.classList.toggle('has-pinned-side', pinned);
  }, [pinned]);

  const items = [
    { i: 'fa-house', l: 'Início', href: '#' },
    { i: 'fa-calendar-days', l: 'Agenda', href: 'Agenda.html' },
    { i: 'fa-stethoscope', l: 'Meus atendimentos', href: 'Atendimentos.html' },
    { i: 'fa-user-injured', l: 'Pacientes', href: 'Pacientes.html' },
    { i: 'fa-file-medical', l: 'Prontuários', href: 'Prontuario.html' },
    { i: 'fa-flask', l: 'Exames', href: '#' },
    { i: 'fa-prescription-bottle-medical', l: 'Receitas', href: '#' },
    { i: 'fa-comments', l: 'Mensagens', href: '#', badge: 3 },
    { i: 'fa-boxes-stacked', l: 'Estoque', href: 'Estoque.html' },
    { i: 'fa-file-invoice-dollar', l: 'Orçamentos', href: 'Orcamentos.html' },
    { i: 'fa-sliders', l: 'Config. Orçamento', href: 'ConfigOrcamento.html' },
    { i: 'fa-coins', l: 'Financeiro', href: 'Financeiro.html' },
    { i: 'fa-chart-pie', l: 'Relatórios', href: 'Relatorios.html' },
    { i: 'fa-user-doctor', l: 'Equipe', href: 'Equipe.html' },
  ];

  const expanded = pinned || hovered;

  return (
    <aside
      className={`side ${expanded ? 'expanded' : 'collapsed'} ${pinned ? 'pinned' : ''}`}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      <div className="side-top">
        <button
          className="pin-btn"
          onClick={() => setPinned(p => !p)}
          title={pinned ? 'Desafixar menu' : 'Fixar menu'}
        >
          <i className={`fa-solid ${pinned ? 'fa-angles-left' : 'fa-angles-right'}`}></i>
        </button>
      </div>

      <nav className="nav">
        {items.map((it, idx) => (
          <a key={idx} href={it.href} className={`item ${it.l === active ? 'active' : ''}`} title={!expanded ? it.l : ''}>
            <i className={`fa-solid ${it.i}`}></i>
            <span className="lbl">{it.l}</span>
            {it.badge && (
              <span className="nav-badge">{it.badge}</span>
            )}
          </a>
        ))}
      </nav>

      <div className="foot">
        <div className="item" title={!expanded ? 'Configurações' : ''}>
          <i className="fa-solid fa-gear"></i><span className="lbl">Configurações</span>
        </div>
        <div className="item" title={!expanded ? 'Suporte' : ''}>
          <i className="fa-solid fa-circle-question"></i><span className="lbl">Suporte</span>
        </div>
      </div>
    </aside>
  );
};

window.Sidebar = Sidebar;
