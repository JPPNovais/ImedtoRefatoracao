// TopBar — header global com busca, notificações e perfil

const TopBar = ({ onSearch }) => {
  const [open, setOpen] = React.useState(null); // 'notif' | 'profile' | null

  React.useEffect(() => {
    const close = () => setOpen(null);
    if (open) {
      document.addEventListener('click', close);
      return () => document.removeEventListener('click', close);
    }
  }, [open]);

  const notifications = [
    { id: 1, icon: 'fa-user-clock', color: 'hsl(var(--info))', title: 'Marina Souza chegou', desc: 'Sala de espera há 4 min', time: 'agora' },
    { id: 2, icon: 'fa-triangle-exclamation', color: 'hsl(var(--warning))', title: 'Beatriz Lima sem confirmação', desc: 'Lembrete enviado há 24h sem resposta', time: '12 min' },
    { id: 3, icon: 'fa-ban', color: 'hsl(var(--error))', title: 'Cancelamento — Ricardo Nunes', desc: 'Considere encaixe da lista de espera', time: '1h' },
    { id: 4, icon: 'fa-flask', color: 'hsl(280 60% 50%)', title: 'Resultado de exame disponível', desc: 'Pedro Henrique Costa · Hemograma', time: '2h' },
  ];

  return (
    <header className="topbar">
      <div className="topbar-brand">
        <img src="assets/imedto-logo-white.png" alt="Imedto" />
      </div>

      <div className="topbar-actions">
        <button className="tb-btn" title="Ajuda">
          <i className="fa-solid fa-circle-question"></i>
        </button>

        <div className="tb-pop-wrap" onClick={e => e.stopPropagation()}>
          <button
            className={`tb-btn ${open === 'notif' ? 'active' : ''}`}
            onClick={() => setOpen(open === 'notif' ? null : 'notif')}
            title="Notificações"
          >
            <i className="fa-solid fa-bell"></i>
            <span className="tb-badge">4</span>
          </button>
          {open === 'notif' && (
            <div className="tb-pop">
              <div className="tb-pop-head">
                <b>Notificações</b>
                <span className="lnk">Marcar todas como lidas</span>
              </div>
              <div className="tb-notif-list">
                {notifications.map(n => (
                  <div key={n.id} className="tb-notif">
                    <div className="ic" style={{ background: `color-mix(in srgb, ${n.color} 14%, white)`, color: n.color }}>
                      <i className={`fa-solid ${n.icon}`}></i>
                    </div>
                    <div className="tx">
                      <b>{n.title}</b>
                      <span>{n.desc}</span>
                    </div>
                    <div className="tm">{n.time}</div>
                  </div>
                ))}
              </div>
              <div className="tb-pop-foot">
                <span className="lnk">Ver todas as notificações →</span>
              </div>
            </div>
          )}
        </div>

        <div className="tb-divider"></div>

        <div className="tb-pop-wrap" onClick={e => e.stopPropagation()}>
          <button
            className={`tb-profile ${open === 'profile' ? 'active' : ''}`}
            onClick={() => setOpen(open === 'profile' ? null : 'profile')}
          >
            <div className="av">DR</div>
            <div className="who">
              <b>Dra. Renata Lopes</b>
              <span>Cardiologia · Clínica Vita</span>
            </div>
            <i className="fa-solid fa-chevron-down" style={{fontSize: 10, color: 'hsl(0 0% 100% / 0.6)'}}></i>
          </button>
          {open === 'profile' && (
            <div className="tb-pop tb-pop-profile">
              <div className="tb-profile-card">
                <div className="av-lg">DR</div>
                <div>
                  <b>Dra. Renata Lopes</b>
                  <span>CRM 12.345-SP · Cardiologia</span>
                  <span className="clinic">● Clínica Vita Centro · Sala 4</span>
                </div>
              </div>
              <div className="tb-pop-list">
                <div className="tb-pop-item"><i className="fa-solid fa-user"></i>Meu perfil</div>
                <div className="tb-pop-item"><i className="fa-solid fa-clock"></i>Minha agenda e horários</div>
                <div className="tb-pop-item"><i className="fa-solid fa-shield-halved"></i>Convênios atendidos</div>
                <div className="tb-pop-item"><i className="fa-solid fa-gear"></i>Configurações</div>
                <div className="tb-pop-item"><i className="fa-solid fa-arrows-rotate"></i>Trocar de unidade</div>
              </div>
              <div className="tb-pop-foot">
                <button className="tb-logout"><i className="fa-solid fa-arrow-right-from-bracket"></i>Sair</button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

window.TopBar = TopBar;
