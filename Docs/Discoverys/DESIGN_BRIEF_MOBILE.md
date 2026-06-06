# Design Brief — Imedto Mobile (app do médico)

> **Propósito deste documento:** é o **brief de design**, não de implementação. Ele descreve persona, princípios, navegação, telas, estados e fluxos para você **gerar o design primeiro** (com o Claude design). A implementação (Vue/.NET/migrations) só acontece DEPOIS que o design estiver fechado e aprovado.
>
> **Escopo:** completo mobile (Tier 1 → 3). **Visual:** herda o design system do Imedto web. **Plataforma:** cross-platform neutro (iOS + Android sem favorecer nenhum).
>
> **Princípio-âncora:** mobile ≠ web espremido. O web é a *mesa de trabalho*; o mobile é o médico *fora da mesa* — entre consultas, no corredor do hospital, de plantão, em casa. Toda tela responde a: **"o médico precisa disso em movimento, de relance, agora?"**

---

## 1. Persona e jobs-to-be-done

**Persona primária: o médico (Profissional).** Secundárias: Dono (médico que também administra) e Recepção (acesso limitado por RBAC).

Os "trabalhos" que o app resolve, em ordem de frequência:

1. **"Qual minha próxima consulta?"** — abre o app 15x/dia só pra isso.
2. **"Quem é esse paciente antes de eu entrar na sala?"** — alerta clínico, histórico recente.
3. **"Recebi um aviso?"** — cancelamento de última hora, confirmação, receita assinada (push).
4. **"Preciso prescrever/atestar agora, longe do computador"** — receita, atestado, pedido de exame.
5. **"Registrar a evolução e anexar a foto da lesão"** — prontuário rápido + câmera.
6. **"Aprovar esse orçamento / mandar o link pro paciente"** — ações pontuais de relance.

---

## 2. Princípios de design (mobile-first)

1. **Glanceável > completo.** A informação certa em 1 segundo. Listas densas de relance, detalhe sob demanda.
2. **Read-first, write-light.** O mobile consulta muito e edita pouco. Edição é cirúrgica (1 campo, 1 toque), não formulário de 12 campos.
3. **Polegar manda.** Ações primárias na zona inferior (bottom tab, FAB, botões fixos no rodapé). Topo é pra contexto/título, não pra ação crítica.
4. **Capabilities nativas são o diferencial.** Push, câmera, biometria, ditado e share são o motivo do app existir — não replicar o web.
5. **LGPD é design, não disclaimer.** PII aparece só quando necessária; ver detalhe de paciente **audita o acesso**; biometria protege a tela.
6. **Degradação por permissão.** O que o RBAC nega não vira erro — **some** da UI. Recepção e médico veem apps diferentes sem perceber que falta algo.
7. **Marca consistente.** Mesma alma do web: roxo Imedto, Nunito, cantos suaves, sombras sutis.

---

## 3. Design tokens (herdados do web, adaptados ao mobile)

### Cores
| Token | Valor | Uso mobile |
|---|---|---|
| **Primária** | `#442B97` (roxo) | Tab ativa, botões primários, FAB, links |
| **Primária-Dark** | `#261854` | Pressed/hover, headers de destaque |
| **Background** | `#FCFCFD` | Fundo de telas |
| **Surface/Card** | `#FFFFFF` | Cards, sheets, list rows |
| **Muted** | `#F4F4F5` | Fundo de chips, skeletons, seções |
| **Border** | `#E4E4E7` | Divisores, contornos de input |
| **Foreground** | `#3D3D3D` | Texto principal |
| **Sucesso** | `#15B27E` | Confirmado, atendido, assinado |
| **Warning** | `#EBB105` | Pendente, atenção |
| **Erro/Alerta** | `#EF4343` | **Alerta clínico**, faltou, cancelado, destrutivo |
| **Info** | `#0DA2E7` | Avisos neutros, badges informativos |

> **Dark mode**: o design system já suporta. Pro mobile, dark mode é **desejável** (uso noturno de plantão) — gerar as duas variantes.

### Tipografia
**Nunito** — pesos 400 / 600 / 700 / 800.
- Título de tela: 20–24px / 700
- Título de card/row: 16px / 700
- Corpo: 16px / 400
- Secundário/meta: 14px / 400
- Label/badge: 12px / 600
- **Mínimo legível no mobile: 14px** (não usar 12px pra conteúdo, só labels).

### Forma e profundidade
- **Border-radius:** 12px em cards e sheets (mais arredondado que o web, mais "app"), 8px em inputs/botões, `full` em pills/avatares/FAB.
- **Sombras:** sutis. `shadow` em cards, `shadow-lg` (`0 12px 32px -10px rgb(0 0 0 /.14)`) em bottom sheets e FAB. Evitar sombras pesadas — preferir borda + elevação leve.
- **Toque mínimo:** alvos de 44×44px (iOS HIG) / 48×48px (Material). Nunca menor.
- **Motion:** 150–300ms, easing `cubic-bezier(0.22, 1, 0.36, 1)`. Transições de tela: push lateral; sheets: slide-up; toasts: fade+slide do topo.

### Vocabulário de componentes (reaproveitar do design system)
O web já tem 35 componentes em `frontend/src/components/ui/`. Os equivalentes mobile a desenhar (mesma alma, ergonomia mobile):
`AppButton`, `AppBadge`, `AppStatusPill`, `AppRolePill`, `AppCard`, `AppAvatar`, `AppEmptyState`, `AppToast`, `AppConfirmDialog`, `AppSearchInput`, `AppFilterPills`, `AppInput`, `AppSelect`, `AppDatePicker`, `AppPhotoUpload`, `AppStatCard`, `AppDateStrip`.
**Novos componentes mobile-nativos a criar:** `BottomTabBar`, `BottomSheet`, `FloatingActionButton`, `SwipeableRow`, `PullToRefresh`, `EstabelecimentoSwitcher` (top bar), `PushBanner`.

---

## 4. Modelo de navegação

**Bottom tab bar fixa de 5 itens** (padrão cross-platform, polegar-friendly):

```
┌─────────────────────────────────────────────┐
│                                               │
│              [ conteúdo da tela ]             │
│                                               │
├─────────────────────────────────────────────┤
│   📅       👥       ( + )      🔔       ⋯      │
│  Agenda  Pacientes  Ação    Avisos    Mais    │
└─────────────────────────────────────────────┘
```

1. **Agenda** (default ao abrir) — meu dia / próximos atendimentos.
2. **Pacientes** — busca + lista + ficha.
3. **( + ) Ação central** — botão elevado que abre um **bottom sheet** com ações rápidas: *Nova receita · Atestado · Pedido de exame · Novo agendamento*. (As ações disponíveis respeitam RBAC — some o que não pode.)
4. **Avisos** — centro de notificações, com badge de não-lidos.
5. **Mais** — perfil, **trocar estabelecimento**, configurações leves, ajuda, sair.

**Top bar contextual** (por tela): título + à esquerda o **switcher de estabelecimento** (avatar/nome da clínica, toca pra trocar tenant), à direita ação contextual (filtro, busca).

> **Multi-estabelecimento é cidadão de primeira classe.** Um médico atende em N clínicas. O switcher no topo troca o tenant ativo e **recarrega o contexto inteiro** (agenda, pacientes, permissões daquele vínculo). Estado persistido entre sessões.

---

## 5. Inventário de telas

| # | Tela | Tier | Tab | Capability nativa |
|---|---|---|---|---|
| 0 | Login + biometria | base | — | FaceID/biometria |
| 0b | Seletor de estabelecimento | base | top bar | — |
| 1 | **Agenda do dia** (home) | 1 | Agenda | pull-to-refresh, push |
| 2 | **Detalhe do agendamento** | 1 | Agenda | swipe actions |
| 3 | **Pacientes — lista/busca** | 1 | Pacientes | — |
| 4 | **Ficha do paciente** (detalhe + alerta clínico) | 1 | Pacientes | acesso auditado, biometria |
| 5 | **Prontuário — timeline + nova evolução** | 1 | (via paciente) | câmera, voz |
| 6 | **Centro de notificações** | 1 | Avisos | push |
| 7 | **Receita rápida** (favoritos + assinatura) | 2 | Ação (+) | assinatura digital, share |
| 8 | **Atestado** | 2 | Ação (+) | assinatura, share |
| 9 | **Pedido de exame** | 2 | Ação (+) | assinatura, share |
| 10 | **Compartilhar link** (confirmação/termo) | 2 | contextual | share sheet |
| 11 | **Orçamento — ver e aprovar** | 3 | (via menu) | — |
| 12 | **Mais / Perfil** | base | Mais | — |
| G1 | Estado: assinatura expirada | global | — | — |
| G2 | Estado: sem permissão (degradação) | global | — | — |
| G3 | Estado: offline | global | — | — |

---

## 6. Especificação tela a tela

> Para cada tela: **Objetivo · Layout (wireframe) · Componentes · Estados · Interações/gestos · Native**. Wireframes são direção de layout, não pixel-perfect — o Claude design refina.

### Tela 0 — Login + biometria
**Objetivo:** entrar em 1 toque nas aberturas seguintes.

```
┌─────────────────────────┐
│                         │
│        [ logo ]         │
│         Imedto          │
│                         │
│   ┌─────────────────┐   │
│   │ E-mail          │   │
│   └─────────────────┘   │
│   ┌─────────────────┐   │
│   │ Senha        👁  │   │
│   └─────────────────┘   │
│                         │
│   [   Entrar        ]   │  ← AppButton primary, largura total
│                         │
│   Esqueci minha senha   │
│  ─────────────────────  │
│      😶 Entrar com       │
│       Face ID            │  ← só aparece após 1º login
└─────────────────────────┘
```
- **Componentes:** `AppInput`, `AppButton`.
- **Estados:** idle · loading (spinner no botão) · erro (mensagem genérica, sem revelar se email existe) · biometria disponível.
- **Native:** após primeiro login, oferecer ativar biometria; aberturas seguintes pedem FaceID/digital direto.

### Tela 0b — Seletor de estabelecimento
**Objetivo:** escolher em qual clínica vou operar. Aparece se o médico tem >1 vínculo ativo; troca depois via top bar.
```
┌─────────────────────────┐
│  Onde você vai atender?  │
│                          │
│  ┌────────────────────┐  │
│  │ 🏥 Clínica Vida     │  │ ← AppCard tocável
│  │    Dono · 12 hoje   │  │   subtítulo: papel + nº consultas do dia
│  └────────────────────┘  │
│  ┌────────────────────┐  │
│  │ 🏥 Hospital Norte   │  │
│  │    Médico · 4 hoje  │  │
│  └────────────────────┘  │
└─────────────────────────┘
```
- **Componentes:** `AppCard`, `AppRolePill`, `AppAvatar`.
- **Estado vazio:** sem vínculo ativo → tela de "Você ainda não tem vínculo" + ver convites pendentes.

### Tela 1 — Agenda do dia (HOME) ⭐
**Objetivo:** a tela mais aberta do app. "Quem é o próximo, que horas, qual status."
```
┌─────────────────────────────┐
│ 🏥 Clínica Vida ▾      🔍 ⚙ │ ← top bar: switcher + busca
├─────────────────────────────┤
│  ◀  Seg 5 Jun  ▶            │ ← AppDateStrip (swipe lateral troca dia)
│  ┌───┬───┬───┬───┬───┬───┐  │
│  │ 12│ 4 │ 0 │... resumo   │ ← StatCards mini: agendados/atendidos/faltas
│                             │
│  PRÓXIMO  ⏰ em 12 min       │ ← destaque do próximo
│  ┌─────────────────────────┐│
│  │ 09:30  Maria Silva   🔴 ││ ← 🔴 = tem alerta clínico
│  │ Retorno · Sala 2        ││
│  └─────────────────────────┘│
│                             │
│  MAIS TARDE                 │
│  ┌─────────────────────────┐│
│  │ 10:00  João Souza    ✓  ││ ← ✓ confirmou presença
│  │ Consulta · Sala 1       ││
│  ├─────────────────────────┤│
│  │ 10:30  Ana Lima         ││
│  │ Avaliação · Sala 2      ││
│  └─────────────────────────┘│
├─────────────────────────────┤
│  📅    👥   (+)   🔔   ⋯     │
└─────────────────────────────┘
```
- **Componentes:** `AppDateStrip`, `AppStatCard`, `AppCard` (linha de agendamento), `AppStatusPill`, `AppAvatar`, `AppBadge` (alerta).
- **Linha de agendamento mostra:** hora · nome · tipo · sala · **status** (cor) · **marcador de alerta clínico** (🔴, sem revelar o conteúdo na lista — LGPD).
- **Estados:** loading (skeleton de 3 linhas) · vazio ("Nenhuma consulta hoje 🎉") · erro (banner + retry) · offline (badge "dados de HH:MM").
- **Interações:** *pull-to-refresh*; *swipe na linha* → ações rápidas (marcar atendido / faltou / cancelar); tocar → detalhe; trocar dia por swipe no date strip.
- **Native:** push de novo agendamento/cancelamento atualiza a lista; pull-to-refresh.

### Tela 2 — Detalhe do agendamento
**Objetivo:** tudo sobre aquele encontro + atalhos pra agir.
```
┌─────────────────────────────┐
│ ←  Agendamento              │
├─────────────────────────────┤
│  09:30 – 10:00 · Sala 2     │
│  🟡 Agendado                 │ ← AppStatusPill
│                             │
│  ┌─────────────────────────┐│
│  │ 😀 Maria Silva          ││ ← toca → ficha do paciente
│  │ 34 anos · Retorno       ││
│  │ 🔴 Alerta clínico       ││ ← badge, conteúdo só na ficha (auditado)
│  └─────────────────────────┘│
│                             │
│  Observações                │
│  "Paciente pós-operatório"  │
│                             │
│  ── Ações ──                │
│  [ Iniciar atendimento → ]  │ ← abre prontuário (nova evolução)
│  [ ✓ Atendido ] [ ✗ Faltou ]│
│  [ 🔁 Reagendar ]            │
│  [ 📲 Enviar confirmação ]   │ ← share link público via WhatsApp
└─────────────────────────────┘
```
- **Componentes:** `AppStatusPill`, `AppCard`, `AppButton` (variantes), `AppConfirmDialog` (cancelar/faltou).
- **Estados:** confirmações destrutivas (faltou/cancelar) usam `AppConfirmDialog`.
- **Native:** "Enviar confirmação" abre o **share sheet** nativo com o link público (`AgendamentoPublico` já existe no back).

### Tela 3 — Pacientes (lista + busca)
**Objetivo:** achar um paciente rápido.
```
┌─────────────────────────────┐
│ 🏥 Clínica Vida ▾           │
│ ┌─────────────────────────┐ │
│ │ 🔍 Buscar paciente...    │ │ ← AppSearchInput (debounce)
│ └─────────────────────────┘ │
│ [Todos][Com alerta][Recentes]│ ← AppFilterPills
├─────────────────────────────┤
│ ┌─────────────────────────┐ │
│ │ 😀 Maria Silva       🔴 ││ │ ← contagem/marcador de alerta, não o texto
│ │    34 anos · últ. 02/06 ││ │
│ ├─────────────────────────┤ │
│ │ 😀 João Souza           ││ │
│ │    51 anos · últ. 28/05 ││ │
│ └─────────────────────────┘ │
└─────────────────────────────┘
```
- **Componentes:** `AppSearchInput`, `AppFilterPills`, `AppAvatar`, `AppCard`, `AppBadge`, `AppEmptyState`.
- **LGPD:** lista mostra **só o marcador** de alerta (🔴 / contagem), **nunca o texto** — conteúdo vive no detalhe, que audita o acesso.
- **Estados:** loading (skeleton) · vazio ("Nenhum paciente" / "Nada encontrado") · busca sem resultado.
- **Performance:** busca só dispara com debounce; lista paginada/infinite-scroll.

### Tela 4 — Ficha do paciente (detalhe + alerta clínico) ⭐
**Objetivo:** o briefing de 5 segundos antes de entrar na sala.
```
┌─────────────────────────────┐
│ ←  Maria Silva          ⋯   │
├─────────────────────────────┤
│      😀  Maria Silva         │
│      34 anos · F             │
│      📞 (11) 9...  ·  CPF... │ ← contato/doc mascarados
│                             │
│  ╔═════════════════════════╗│
│  ║ ⚠ ALERTA CLÍNICO        ║│ ← banner vermelho, alto contraste
│  ║ Alergia grave a         ║│   (este acesso é auditado)
│  ║ penicilina              ║│
│  ╚═════════════════════════╝│
│                             │
│ [ Histórico ][ Prontuário ] │ ← AppTabs
│ [ Documentos ][ Orçamentos ]│
│                             │
│  ── Últimas evoluções ──    │
│  • 02/06 Retorno pós-op     │
│  • 18/05 Consulta inicial   │
│                             │
│  ── Ações ──                │
│  [ + Evolução ] [ 💊 Receita]│
│  [ 📄 Atestado ] [ 🔬 Exame ]│
└─────────────────────────────┘
```
- **Componentes:** `AppAvatar`(xl), `AppTabs`, `AppCard`, banner de alerta (novo — variante de `AppCard` em `error`), `AppButton`.
- **LGPD crítico:** abrir esta tela **dispara o log de acesso** (`PacienteAcessoLog`). Opcional: re-autenticação biométrica pra revelar PII sensível (CPF, contato, alerta). PII mascarada por padrão, "toque pra revelar".
- **Estados:** sem alerta → banner não aparece; sem evoluções → empty state na aba.

### Tela 5 — Prontuário: timeline + nova evolução ⭐ (câmera + voz)
**Objetivo:** registrar a evolução do encontro de forma rápida, anexando foto/áudio.
```
TIMELINE                         NOVA EVOLUÇÃO (sheet)
┌─────────────────────────┐     ┌─────────────────────────┐
│ ←  Prontuário · Maria   │     │  Nova evolução       ✕  │
├─────────────────────────┤     ├─────────────────────────┤
│ ┌─────────────────────┐ │     │ Modelo: [Retorno ▾]     │ ← template/especialidade
│ │ 02/06 · Dr. Você    │ │     │                         │
│ │ Retorno pós-op      │ │     │ ┌─────────────────────┐ │
│ │ "Cicatrização ok..."│ │     │ │ Evolução...      🎙 │ │ ← ditado por voz
│ │ 📎 2 anexos          │ │     │ │                     │ │
│ ├─────────────────────┤ │     │ └─────────────────────┘ │
│ │ 18/05 · Dr. Você    │ │     │                         │
│ │ Consulta inicial    │ │     │ Anexos:                 │
│ └─────────────────────┘ │     │ [ 📷 Foto ] [ 🖼 Galeria]│ ← câmera nativa
│                         │     │ [img][img]              │
│      [ + Evolução ]     │     │                         │
│                         │     │ [   Salvar evolução   ] │ ← rodapé fixo
└─────────────────────────┘     └─────────────────────────┘
```
- **Componentes:** `AppCard` (item da timeline), `AppTextarea`, `AppSelect` (modelo), `AppPhotoUpload`, `AppButton`, `BottomSheet` (novo).
- **Native:** **câmera** (fotografar lesão/exame/documento e anexar), **ditado por voz** no textarea. Foto → upload S3 (`ProntuarioAnexo` já existe).
- **Estados:** salvando (botão loading) · upload de anexo (progress) · falha de upload (retry por anexo) · offline → "salvar como rascunho local" (ver §7 offline).
- **Escopo mobile:** **nova evolução + anexo**, não o builder completo de templates (isso fica no web).

### Tela 6 — Centro de notificações ⭐ (push)
**Objetivo:** o canal que alcança o médico fora da clínica.
```
┌─────────────────────────────┐
│ Avisos                 ✓ ler│
├─────────────────────────────┤
│ ● Novo agendamento          │ ← ● = não lido (ponto roxo)
│   Maria Silva · 10:30 hoje  │
│   há 5 min                  │
├─────────────────────────────┤
│ ● Consulta cancelada        │ ← ícone/cor por categoria
│   João Souza · 14:00        │
│   há 1 h                    │
├─────────────────────────────┤
│   Receita assinada ✓        │
│   Pronta para envio         │
│   há 2 h                    │
└─────────────────────────────┘
```
- **Componentes:** lista de `AppCard`/rows, `AppBadge` (categoria), ponto de não-lido, `AppEmptyState`.
- **Categorias** (já no domínio `CategoriaNotificacao`): novo agendamento · lembrança/cancelamento · receita pronta · confirmação de presença · convite de vínculo.
- **Native:** **push (APNs/FCM)** é o grande destravamento — o back já dispara via `Notificacoes`/SignalR; mobile só precisa do canal nativo. Tocar na push → deep-link pra tela certa.
- **Estados:** vazio · todos lidos · agrupamento por dia (Hoje / Ontem / Anteriores).

### Tela 7 — Receita rápida ⭐ (favoritos + assinatura + share)
**Objetivo:** prescrever de plantão/em casa, sem computador.
```
┌─────────────────────────────┐
│ ←  Nova receita             │
├─────────────────────────────┤
│ Paciente: Maria Silva       │ ← pré-preenchido se veio da ficha
│ Tipo: [ Simples ▾ ]         │ ← TipoReceita (simples/controlada)
│                             │
│ ⭐ Favoritos                 │ ← MedicamentoFavorito brilha no mobile
│ [ Amoxicilina 500mg ]       │   chips de 1 toque
│ [ Dipirona 1g ] [ + busca ] │
│                             │
│ ── Itens ──                 │
│ ┌─────────────────────────┐ │
│ │ Amoxicilina 500mg       ││ │
│ │ 1 cp 8/8h · 7 dias    ✏ ││ │ ← via administração + posologia
│ └─────────────────────────┘ │
│ [ + Adicionar medicamento ] │
├─────────────────────────────┤
│ [   Assinar e gerar     ]   │ ← assinatura digital ANVISA, rodapé fixo
└─────────────────────────────┘
   ↓ após assinar
┌─────────────────────────────┐
│      ✓ Receita assinada      │
│  [ 📲 Enviar ao paciente ]   │ ← share sheet (PDF/link)
│  [ 👁 Ver PDF ] [ ✓ Concluir]│
└─────────────────────────────┘
```
- **Componentes:** `AppSelect`, chips de favoritos (`AppFilterPills`-like), `AppCard` (item), `AppButton`, sheet de edição de posologia.
- **Native:** assinatura digital ANVISA (`ReceitaAssinatura`); **share** do PDF/link pro paciente.
- **Estados:** assinando (loading + feedback de processo) · falha de assinatura · sucesso.

### Telas 8 e 9 — Atestado / Pedido de exame
Mesmo padrão da receita, mais simples: paciente → conteúdo (CID/dias no atestado; exames no pedido) → **assinar** → **share**. Reaproveitam o mesmo esqueleto de tela (formulário curto + rodapé fixo "Assinar e gerar" + tela de sucesso com share). `Atestados` e `PedidosExame` já existem no domínio.

### Tela 10 — Compartilhar link (confirmação / termo)
Não é tela cheia — é uma **ação** que abre o **share sheet nativo** com o link público (`AgendamentoPublico` / `TermoPublico`, que já usam token). Disparada do detalhe do agendamento ou da ficha. Copy curta: "Enviar para o paciente confirmar presença".

### Tela 11 — Orçamento: ver e aprovar (Tier 3)
**Objetivo:** o Dono aprova um orçamento do celular. **Montar** orçamento (12 itens, equipe, anestesia) **fica no web** — aqui é só leitura + decisão.
```
┌─────────────────────────────┐
│ ←  Orçamento #1042          │
│ 🟡 Aguardando aprovação      │
├─────────────────────────────┤
│ Paciente: João Souza        │
│ Procedimento: ...           │
│ ── Itens ──                 │
│ • Item A .......... R$ 1.200│
│ • Anestesia ....... R$   800│
│ Total ............. R$ 2.000│
├─────────────────────────────┤
│ [ ✓ Aprovar ] [ ✗ Recusar ] │ ← respeita permissão orcamento.aprovar
└─────────────────────────────┘
```
- **Degradação:** se o usuário não tem `orcamento.aprovar`, os botões somem (vira só leitura).

### Tela 12 — Mais / Perfil
Lista simples: dados pessoais · **trocar estabelecimento** · notificações (preferências de push) · biometria on/off · tema (claro/escuro/automático) · ajuda · **sair**. Configurações pesadas (equipe, permissões, automações, IA, planos) **não estão aqui** — só atalho "Abrir no navegador".

---

## 7. Padrões mobile-nativos transversais

Estes não são telas — são comportamentos que o design precisa prever em todo lugar:

| Capability | Onde aparece | Comportamento de design |
|---|---|---|
| **Push** | Notificações, agenda, receita | Banner no topo (foreground) + deep-link ao tocar. `PushBanner` component. |
| **Câmera** | Prontuário, ficha (anexos) | Sheet "Foto / Galeria"; preview com remover; upload com progress. |
| **Biometria** | Login, revelar PII | FaceID/digital; fallback senha; opcional ao abrir ficha sensível. |
| **Share sheet** | Receita, atestado, exame, confirmação, termo | Botão "Enviar ao paciente" → share OS nativo (WhatsApp/e-mail). |
| **Ditado por voz** | Evolução de prontuário, observações | Ícone 🎙 no textarea; usa STT do OS. |
| **Pull-to-refresh** | Agenda, pacientes, notificações | Gesto padrão; spinner roxo. |
| **Swipe actions** | Linha de agendamento | Swipe revela ações (atendido/faltou). |
| **Offline leve** | Agenda do dia, ficha aberta | Cache da última carga; badge "dados de HH:MM"; rascunho local de evolução com sync ao voltar. |

---

## 8. Estados globais (desenhar uma vez, valem pra tudo)

- **G1 — Assinatura expirada:** o back retorna 402 (`RequiresAssinaturaAtiva`). Mobile mostra tela-bloqueio amigável: "Sua assinatura expirou" + CTA "Renovar" (abre web/checkout) + "Falar com suporte". Login e perfil continuam acessíveis.
- **G2 — Sem permissão (RBAC):** o que o vínculo não permite **não aparece** — tab/ação/botão somem. Nunca mostrar erro "acesso negado" pra algo que o usuário não deveria nem ver. Recepção ≠ médico ≠ dono, sem fricção.
- **G3 — Offline:** banner discreto no topo "Sem conexão — mostrando dados salvos". Ações de escrita ficam desabilitadas ou viram rascunho local. Reconectou → sync silencioso + toast.
- **Loading global:** skeletons (não spinners de tela cheia) nas listas; spinner só em ações pontuais (botões).
- **Erro de rede em ação:** `AppToast` de erro + retry, mensagem genérica (sem vazar detalhe técnico nem PII).

---

## 9. Acessibilidade e LGPD na camada visual

- **Contraste:** alerta clínico e status usam cor **+ ícone + texto** (nunca só cor — daltonismo).
- **Toque:** alvos ≥ 44px; espaçamento generoso entre ações destrutivas e comuns.
- **PII mínima na superfície:** listas mostram marcador, não conteúdo sensível. CPF/contato mascarados; "toque pra revelar" (com biometria opcional).
- **Acesso auditado é visível:** sutil "Este acesso foi registrado" no rodapé da ficha — transparência LGPD vira confiança.
- **Mensagens genéricas:** "não encontrado" em vez de revelar existência de paciente/tenant alheio.

---

## 10. O que NÃO vai pro mobile (anti-escopo)

Para o design não inchar — estas ficam no web e, no app, no máximo viram atalho "abrir no navegador":

- Equipe / Permissões / matriz RBAC (configuração densa de Dono).
- Financeiro completo (lançamentos, categorias, formas de pagamento) — no máximo *ver* total do dia.
- Configurações: modelos de prontuário (builder), termos, automações, IA settings, dados do estabelecimento.
- Inventário/estoque, relatórios completos, gestão de assinatura/planos.
- **Montar** orçamento (só ver/aprovar entra).

---

## 11. Como usar este brief com o Claude design

Sugestão de ordem pra gerar o design (do mais valioso ao complementar):

1. **Comece pelos tokens e pela navegação** (§3 e §4) — gere a `BottomTabBar`, top bar com switcher e a base de tema (claro + escuro).
2. **Tier 1 primeiro, na ordem do uso:** Agenda do dia (1) → Detalhe do agendamento (2) → Pacientes (3) → Ficha do paciente (4) → Prontuário/nova evolução (5) → Notificações (6).
3. **Tier 2:** Receita (7) como template, depois Atestado/Exame (8/9) reusando o esqueleto.
4. **Tier 3 e globais:** Orçamento (11), Mais (12), e os estados G1–G3.
5. **Para cada tela, peça as variações de estado** descritas: loading (skeleton), vazio, erro, sucesso, sem-permissão.
6. **Peça as duas variantes de tema** (claro/escuro) — uso noturno de plantão importa.

**Prompt-semente sugerido por tela:** *"Gere a tela [N] do Imedto Mobile seguindo o design brief: herda os tokens (roxo #442B97, Nunito, radius 12px, sombras sutis), cross-platform neutro, bottom tab nav. Layout conforme o wireframe. Inclua os estados loading/vazio/erro. Respeite a regra de LGPD: [regra da tela]."*

---

> **Próximo passo após o design:** com as telas aprovadas, aí sim aciona-se a pipeline (`imedto-business-analyst` → briefing com CAs → `imedto-developer`/`imedto-database` → `imedto-qa`). Este documento alimenta o design; o briefing técnico vem depois, derivado das telas fechadas.
