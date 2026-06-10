// AnatomyFigure.jsx — Boneco anatômico SVG com regiões clicáveis (M/F, frente/costas)

const AnatomyFigure = ({ sex = 'M', view = 'front', selected = [], onToggle, marks = {} }) => {
  // marks: { regionId: 'normal' | 'altered' | 'note' } — colore a região por status
  const isSelected = (id) => selected.includes(id);
  const mark = (id) => marks[id];

  const fillFor = (id) => {
    if (isSelected(id)) return 'hsl(265 70% 60% / 0.55)'; // primary
    const m = mark(id);
    if (m === 'altered') return 'hsl(0 75% 60% / 0.45)'; // error
    if (m === 'normal') return 'hsl(155 60% 45% / 0.30)'; // success
    if (m === 'note') return 'hsl(38 92% 50% / 0.40)'; // warning
    return 'transparent';
  };

  const stroke = (id) => isSelected(id) ? 'hsl(265 70% 45%)' : 'hsl(220 14% 50% / 0.55)';
  const strokeW = (id) => isSelected(id) ? 2.5 : 1.2;

  const R = (props) => (
    <path
      {...props}
      onClick={(e) => { e.stopPropagation(); onToggle && onToggle(props['data-id']); }}
      fill={fillFor(props['data-id'])}
      stroke={stroke(props['data-id'])}
      strokeWidth={strokeW(props['data-id'])}
      style={{ cursor: 'pointer', transition: 'fill 160ms' }}
    />
  );

  // Mostra dimorfismo simples via largura de ombros/quadris.
  const isF = sex === 'F';
  const shoulderW = isF ? 56 : 64;
  const hipW = isF ? 52 : 46;

  if (view === 'front') {
    return (
      <svg viewBox="0 0 160 360" xmlns="http://www.w3.org/2000/svg" className="anatomy-svg" preserveAspectRatio="xMidYMid meet">
        {/* SILHUETA BASE — sutil */}
        <g fill="hsl(220 14% 95%)" stroke="hsl(220 14% 80%)" strokeWidth="1">
          {/* Cabeça */}
          <ellipse cx="80" cy="28" rx="18" ry="22" />
          {/* Pescoço */}
          <rect x="73" y="48" width="14" height="10" />
          {/* Tronco */}
          <path d={`M ${80 - shoulderW/2} 58 Q ${80 - shoulderW/2 - 4} 100 ${80 - hipW/2} 170 L ${80 + hipW/2} 170 Q ${80 + shoulderW/2 + 4} 100 ${80 + shoulderW/2} 58 Z`} />
          {/* Braços */}
          <path d={`M ${80 - shoulderW/2} 60 Q ${30} 110 ${36} 200 L ${48} 200 Q ${50} 110 ${80 - shoulderW/2 + 8} 70 Z`} />
          <path d={`M ${80 + shoulderW/2} 60 Q ${130} 110 ${124} 200 L ${112} 200 Q ${110} 110 ${80 + shoulderW/2 - 8} 70 Z`} />
          {/* Mãos */}
          <ellipse cx="42" cy="208" rx="9" ry="11" />
          <ellipse cx="118" cy="208" rx="9" ry="11" />
          {/* Pelve/Glúteos frente — aproximação */}
          <path d={`M ${80 - hipW/2} 170 L ${80 - hipW/2 + 4} 195 L ${80 + hipW/2 - 4} 195 L ${80 + hipW/2} 170 Z`} />
          {/* Coxas */}
          <path d={`M ${80 - hipW/2 + 2} 195 L ${68} 280 L ${78} 280 L ${80 - 2} 195 Z`} />
          <path d={`M ${80 + hipW/2 - 2} 195 L ${92} 280 L ${82} 280 L ${80 + 2} 195 Z`} />
          {/* Joelhos */}
          <ellipse cx="73" cy="285" rx="7" ry="6" />
          <ellipse cx="87" cy="285" rx="7" ry="6" />
          {/* Pernas */}
          <path d={`M 67 290 L 70 340 L 78 340 L 76 290 Z`} />
          <path d={`M 93 290 L 90 340 L 82 340 L 84 290 Z`} />
          {/* Pés */}
          <ellipse cx="73" cy="346" rx="8" ry="6" />
          <ellipse cx="87" cy="346" rx="8" ry="6" />
        </g>

        {/* REGIÕES CLICÁVEIS — sobrepostas */}
        <R data-id="head"       d="M 62 6 Q 62 -2 80 -2 Q 98 -2 98 6 L 98 48 L 62 48 Z" />
        <R data-id="face"       d="M 67 18 L 93 18 L 93 44 L 67 44 Z" opacity="0.001" />
        <R data-id="neck"       d="M 72 48 L 88 48 L 88 60 L 72 60 Z" />
        <R data-id="l-shoulder" d={`M ${80 + shoulderW/2 - 14} 56 Q ${80 + shoulderW/2 + 6} 60 ${130} 84 L ${118} 90 Q ${80 + shoulderW/2 - 4} 72 ${80 + shoulderW/2 - 14} 68 Z`} />
        <R data-id="r-shoulder" d={`M ${80 - shoulderW/2 + 14} 56 Q ${80 - shoulderW/2 - 6} 60 ${30} 84 L ${42} 90 Q ${80 - shoulderW/2 + 4} 72 ${80 - shoulderW/2 + 14} 68 Z`} />
        <R data-id="chest"      d={`M ${80 - shoulderW/2 + 6} 68 L ${80 + shoulderW/2 - 6} 68 L ${80 + shoulderW/2 - 10} 120 L ${80 - shoulderW/2 + 10} 120 Z`} />
        <R data-id="l-arm"      d={`M ${118} 90 L ${130} 84 Q ${136} 110 ${124} 200 L ${112} 200 Q ${118} 130 ${108} 110 Z`} />
        <R data-id="r-arm"      d={`M ${42} 90 L ${30} 84 Q ${24} 110 ${36} 200 L ${48} 200 Q ${42} 130 ${52} 110 Z`} />
        <R data-id="abdomen"    d={`M ${80 - shoulderW/2 + 10} 120 L ${80 + shoulderW/2 - 10} 120 L ${80 + hipW/2} 170 L ${80 - hipW/2} 170 Z`} />
        <R data-id="l-hand"     d={`M 109 200 L 127 200 L 127 222 L 109 222 Z`} />
        <R data-id="r-hand"     d={`M 33 200 L 51 200 L 51 222 L 33 222 Z`} />
        <R data-id="pelvis"     d={`M ${80 - hipW/2} 170 L ${80 + hipW/2} 170 L ${80 + hipW/2 - 4} 195 L ${80 - hipW/2 + 4} 195 Z`} />
        <R data-id="l-thigh"    d={`M 80 195 L ${80 + hipW/2 - 2} 195 L 92 280 L 82 280 Z`} />
        <R data-id="r-thigh"    d={`M 80 195 L ${80 - hipW/2 + 2} 195 L 68 280 L 78 280 Z`} />
        <R data-id="l-knee"     d={`M 80 282 L 94 282 L 94 290 L 80 290 Z`} />
        <R data-id="r-knee"     d={`M 80 282 L 66 282 L 66 290 L 80 290 Z`} />
        <R data-id="l-leg"      d={`M 80 290 L 95 290 L 92 340 L 82 340 Z`} />
        <R data-id="r-leg"      d={`M 80 290 L 65 290 L 68 340 L 78 340 Z`} />
        <R data-id="l-foot"     d={`M 80 340 L 96 340 L 96 354 L 80 354 Z`} />
        <R data-id="r-foot"     d={`M 80 340 L 64 340 L 64 354 L 80 354 Z`} />

        {/* Detalhes finos da silhueta para dar carácter */}
        {!isF && (
          <g fill="none" stroke="hsl(220 14% 70%)" strokeWidth="0.8" opacity="0.6">
            <line x1="80" y1="68" x2="80" y2="120" />
            <path d="M 68 90 Q 80 95 92 90" />
            <path d="M 68 102 Q 80 107 92 102" />
          </g>
        )}
        {isF && (
          <g fill="none" stroke="hsl(220 14% 70%)" strokeWidth="0.8" opacity="0.6">
            <ellipse cx="74" cy="92" rx="6" ry="7" />
            <ellipse cx="86" cy="92" rx="6" ry="7" />
            <line x1="80" y1="110" x2="80" y2="160" strokeDasharray="2 2" />
          </g>
        )}
        {/* Umbigo */}
        <circle cx="80" cy="145" r="1.5" fill="hsl(220 14% 60%)" />
      </svg>
    );
  }

  // BACK VIEW
  return (
    <svg viewBox="0 0 160 360" xmlns="http://www.w3.org/2000/svg" className="anatomy-svg" preserveAspectRatio="xMidYMid meet">
      <g fill="hsl(220 14% 95%)" stroke="hsl(220 14% 80%)" strokeWidth="1">
        <ellipse cx="80" cy="28" rx="18" ry="22" />
        <rect x="73" y="48" width="14" height="10" />
        <path d={`M ${80 - shoulderW/2} 58 Q ${80 - shoulderW/2 - 4} 100 ${80 - hipW/2} 170 L ${80 + hipW/2} 170 Q ${80 + shoulderW/2 + 4} 100 ${80 + shoulderW/2} 58 Z`} />
        <path d={`M ${80 - shoulderW/2} 60 Q ${30} 110 ${36} 200 L ${48} 200 Q ${50} 110 ${80 - shoulderW/2 + 8} 70 Z`} />
        <path d={`M ${80 + shoulderW/2} 60 Q ${130} 110 ${124} 200 L ${112} 200 Q ${110} 110 ${80 + shoulderW/2 - 8} 70 Z`} />
        <ellipse cx="42" cy="208" rx="9" ry="11" />
        <ellipse cx="118" cy="208" rx="9" ry="11" />
        <path d={`M ${80 - hipW/2} 170 L ${80 - hipW/2 + 4} 195 L ${80 + hipW/2 - 4} 195 L ${80 + hipW/2} 170 Z`} />
        <path d={`M ${80 - hipW/2 + 2} 195 L ${68} 280 L ${78} 280 L ${80 - 2} 195 Z`} />
        <path d={`M ${80 + hipW/2 - 2} 195 L ${92} 280 L ${82} 280 L ${80 + 2} 195 Z`} />
        <ellipse cx="73" cy="285" rx="7" ry="6" />
        <ellipse cx="87" cy="285" rx="7" ry="6" />
        <path d={`M 67 290 L 70 340 L 78 340 L 76 290 Z`} />
        <path d={`M 93 290 L 90 340 L 82 340 L 84 290 Z`} />
        <ellipse cx="73" cy="346" rx="8" ry="6" />
        <ellipse cx="87" cy="346" rx="8" ry="6" />
      </g>

      {/* Regiões — costas */}
      <R data-id="b-head"  d="M 62 6 L 98 6 L 98 48 L 62 48 Z" />
      <R data-id="neck"    d="M 72 48 L 88 48 L 88 60 L 72 60 Z" />
      <R data-id="r-shoulder" d={`M ${80 + shoulderW/2 - 14} 56 Q ${80 + shoulderW/2 + 6} 60 ${130} 84 L ${118} 90 Q ${80 + shoulderW/2 - 4} 72 ${80 + shoulderW/2 - 14} 68 Z`} />
      <R data-id="l-shoulder" d={`M ${80 - shoulderW/2 + 14} 56 Q ${80 - shoulderW/2 - 6} 60 ${30} 84 L ${42} 90 Q ${80 - shoulderW/2 + 4} 72 ${80 - shoulderW/2 + 14} 68 Z`} />
      <R data-id="b-upper" d={`M ${80 - shoulderW/2 + 6} 68 L ${80 + shoulderW/2 - 6} 68 L ${80 + shoulderW/2 - 10} 130 L ${80 - shoulderW/2 + 10} 130 Z`} />
      <R data-id="r-arm"   d={`M ${118} 90 L ${130} 84 Q ${136} 110 ${124} 200 L ${112} 200 Q ${118} 130 ${108} 110 Z`} />
      <R data-id="l-arm"   d={`M ${42} 90 L ${30} 84 Q ${24} 110 ${36} 200 L ${48} 200 Q ${42} 130 ${52} 110 Z`} />
      <R data-id="b-lower" d={`M ${80 - shoulderW/2 + 10} 130 L ${80 + shoulderW/2 - 10} 130 L ${80 + hipW/2} 170 L ${80 - hipW/2} 170 Z`} />
      <R data-id="r-hand"  d={`M 109 200 L 127 200 L 127 222 L 109 222 Z`} />
      <R data-id="l-hand"  d={`M 33 200 L 51 200 L 51 222 L 33 222 Z`} />
      <R data-id="b-glutes" d={`M ${80 - hipW/2} 170 L ${80 + hipW/2} 170 L ${80 + hipW/2 - 4} 195 L ${80 - hipW/2 + 4} 195 Z`} />
      <R data-id="r-thigh" d={`M 80 195 L ${80 + hipW/2 - 2} 195 L 92 280 L 82 280 Z`} />
      <R data-id="l-thigh" d={`M 80 195 L ${80 - hipW/2 + 2} 195 L 68 280 L 78 280 Z`} />
      <R data-id="r-knee"  d={`M 80 282 L 94 282 L 94 290 L 80 290 Z`} />
      <R data-id="l-knee"  d={`M 80 282 L 66 282 L 66 290 L 80 290 Z`} />
      <R data-id="r-leg"   d={`M 80 290 L 95 290 L 92 340 L 82 340 Z`} />
      <R data-id="l-leg"   d={`M 80 290 L 65 290 L 68 340 L 78 340 Z`} />
      <R data-id="r-foot"  d={`M 80 340 L 96 340 L 96 354 L 80 354 Z`} />
      <R data-id="l-foot"  d={`M 80 340 L 64 340 L 64 354 L 80 354 Z`} />

      {/* Coluna vertebral guia */}
      <line x1="80" y1="62" x2="80" y2="195" stroke="hsl(220 14% 70%)" strokeWidth="0.8" strokeDasharray="3 2" opacity="0.6" />
    </svg>
  );
};

window.AnatomyFigure = AnatomyFigure;
