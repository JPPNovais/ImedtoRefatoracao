# Padronização tipográfica big-bang — escala canônica no design system

**ID**: 2026-06-08_003
**Status**: Aprovado por usuário em 2026-06-08 (escopo big-bang + identidade do mockup + decisões Q1/Q2/Q3/Q4 confirmadas — sem pontos pendentes)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: nenhuma de lógica/dados — apresentação visual em TODAS as telas (regressão visual ampla, validação final do usuário em prod)

---

## 1. Contexto e motivação

Hoje a tipografia do frontend é fragmentada: não há tokens de tipografia em `main.css` (só `--font-display`/`--font-mono`), o `PageHeader` do DS (`text-2xl/font-bold` = 24px/700) é usado em ~38% das views enquanto ~62% têm heading scoped divergente (26px, 1.4rem, 1.5rem, 1.15em; pesos 700/800), e há 7+ classes concorrentes para título de seção/card e 3+ para label de campo. Resultado: cada tela tem sua própria identidade tipográfica, e mudar "o título do site" exige editar dezenas de arquivos.

O objetivo é centralizar tipografia (tokens + componentes) no design system, de modo que **mudar em 1 lugar mude o site inteiro**, alinhando todas as telas à identidade visual do mockup aprovado pelo usuário (`/tmp/design_bundle/imedto/project/`). Escopo big-bang: criar a fundação E migrar todas as telas legadas de uma vez. O usuário aceitou o risco de regressão visual e que a validação visual final será dele em prod (sandbox sem browser; QA cobre build/typecheck/testes/análise estática).

**Evidência (auditoria já levantada e confirmada em código nesta sessão):**
- 163 ocorrências de `<h1>`/`<h2>`/`<h3>` manuais (37 `<h1>`, 66 `<h2>`, 60 `<h3>`) espalhadas em views/componentes.
- 38 arquivos usam `PageHeader`; 24 arquivos têm heading scoped divergente (`card-titulo`, `painel-titulo`, `lista-titulo`, `secao-titulo`, etc.).
- 8 arquivos com `.campo-label` scoped (0.78–0.85em/600).
- 308 usos de `<AppField>` (que renderiza label internamente) + 180 `<label>` cru — todos hoje a 14px/500 via `Field.vue`/`Label.vue`, divergindo do canônico do mockup (12px/600).
- Inputs padrão (`.form-input` e equivalentes) e botões padrão (`.btn-*`) hoje a 14px (0.875rem), divergindo do canônico do mockup (13px). Mudança incluída no escopo por decisão Q3.

## 2. Persona-alvo

Indireta: todos os usuários do produto (recepção, profissional, dono, financeiro) percebem coerência visual. Direto: a equipe de desenvolvimento, que passa a ter fonte única para tipografia — "mudar 1 token muda o site".

## 3. Escopo

**Inclui:**
- Tokens de tipografia em `frontend/src/assets/main.css`: escala `--text-*`, `--font-weight-*`, `--line-height-*`, derivados do mockup.
- `PageHeader` redefinido como fonte única do título de página (30px/800/-0.015em via token), herdado por todas as telas.
- Classes utilitárias tokenizadas canônicas para **título de seção** (`.ds-section-title`, 21px/800) e **título de card** (`.ds-card-title`, 15px/700) no DS — decisão Q1. NÃO criar componente `SectionTitle`; headings existentes (`<h2>`/`<h3>`) só trocam de classe.
- Consolidação do **label de campo** no canônico do mockup (12px/600), alinhando `AppLabel`/`AppField` (hoje 14px/500) e migrando `.campo-label` scoped legado para o mesmo padrão — decisão Q2. Encolhe labels nos 308 `<AppField>` de forma intencional.
- Consolidação de **inputs e botões padrão** no canônico do mockup (13px via `--text-sm`): `.form-input`/inputs padrão e `.btn-*`/botões padrão do design system passam de 14px para 13px — decisão Q3. Mudança visual intencional em formulários e botões de TODA a plataforma.
- Migração total: 163 headings manuais → `PageHeader` (título de página) ou classe-token de seção/card; 8 arquivos `.campo-label` scoped → `AppField`/`AppLabel`/token; todos os inputs/botões que hoje usam 14px → `--text-sm` (13px).
- Alinhamento do Tailwind preset (`design-system/src/tailwind/preset.js`) à escala, se `text-2xl` etc. precisarem bater com tokens.
- `Docs/DESIGN.md`: nova seção "Escala tipográfica" (fonte de verdade).
- Discovery em `Docs/Discoverys/tipografia/01_discovery.md` registrando a fragmentação atual + escala canônica proposta.

**Não inclui:**
- Qualquer mudança de lógica, comportamento, dados ou backend (zero backend).
- Cores, espaçamentos, radius, sombras, badges/status/pills (já coerentes — NÃO mexer).
- Subtítulo/descrição (`.page-sub`/`.card-sub` ~0.875em muted, já razoavelmente coerente — só padronizar via token se trivial, sem mudança de aparência).
- Família tipográfica (Nunito permanece).
- Tema escuro: tokens entram no `:root`; `.dark` só recebe override se já existir (não criar variação nova de tipografia para dark).
- Redesign de qualquer tela. Esta entrega é só substituição de tipografia por tokens/componentes mantendo layout.

## 4. Escala canônica proposta (token → valor → onde usar)

Valores **canônicos do mockup** (`colors_and_type.css` + `Configurações - submenu.html`). Onde o mockup não cobre, usa-se o valor mais comum do código atual para minimizar churn.

### 4.1 `--text-*` (font-size)

| Token | Valor | px | Origem | Onde usar |
|---|---|---|---|---|
| `--text-2xs` | 0.625rem | 10 | mockup `.group-label`/`field-label-dense` | eyebrow/label denso uppercase |
| `--text-xs` | 0.75rem | 12 | mockup `.field-label` (12px) | label de campo, badge, caption |
| `--text-sm` | 0.8125rem | 13 | mockup inputs/botões (13–13.5px) | **texto de input/botão (canônico, Q3)**, meta de lista |
| `--text-base` | 0.875rem | 14 | corpo padrão do app | corpo de texto, item de lista |
| `--text-md` | 0.9375rem | 15 | mockup `.fc-title` (15px) | título de card |
| `--text-lg` | 1.125rem | 18 | título de modal/drawer comum | título de modal/drawer |
| `--text-xl` | 1.3125rem | 21 | mockup `.panel-head h2` (21px) | título de seção/painel |
| `--text-2xl` | 1.5rem | 24 | valor atual do PageHeader (minimiza churn) | reservado / heading intermediário |
| `--text-3xl` | 1.875rem | 30 | mockup `.page-head h1` (30px) | **título de página (canônico)** |

> Observação de churn: o app hoje roda a 16px de base com corpo a 14px. A escala mantém 14px como `--text-base` (corpo de texto e itens de lista) e aplica 13px (`--text-sm`) a inputs e botões padrão, alinhados ao mockup (decisão Q3 — ESTÁ no escopo). Essa é uma mudança visual ampla em formulários/botões de toda a plataforma; ver risco R-INPUT-13PX e R-CHURN na seção 8.

### 4.2 `--font-weight-*`

| Token | Valor | Onde usar |
|---|---|---|
| `--font-weight-regular` | 400 | corpo |
| `--font-weight-medium` | 500 | ênfase leve |
| `--font-weight-semibold` | 600 | label de campo, botão, badge |
| `--font-weight-bold` | 700 | título de card, título de modal |
| `--font-weight-extrabold` | 800 | título de página, título de seção |

### 4.3 `--line-height-*`

| Token | Valor | Onde usar |
|---|---|---|
| `--line-height-none` | 1 | label denso |
| `--line-height-tight` | 1.15 | títulos |
| `--line-height-snug` | 1.3 | subtítulos |
| `--line-height-normal` | 1.5 | corpo (já é o default do body) |

### 4.4 `--letter-spacing-*` (só para títulos do mockup)

| Token | Valor | Onde usar |
|---|---|---|
| `--tracking-title` | -0.015em | título de página (30px) |
| `--tracking-section` | -0.01em | título de seção (21px) |

### 4.5 Mapa semântico (token composto → componente/classe DS)

| Papel | Tamanho/peso/tracking canônico | Componente/classe DS (fonte única) |
|---|---|---|
| Título de página | 30px / 800 / -0.015em / `var(--c-primary-dark)` | `PageHeader` (`AppPageHeader`) |
| Título de seção/painel | 21px / 800 / -0.01em / `var(--c-primary-dark)` | classe `.ds-section-title` (Q1) |
| Título de card | 15px / 700 / `var(--c-primary-dark)` | classe `.ds-card-title` (Q1) |
| Label de campo | 12px / 600 / `var(--c-secondary)` | `AppLabel` + `AppField` (canônico alinhado, Q2) |
| Texto de input / botão | 13px (`--text-sm`) | `.form-input`/inputs e `.btn-*`/botões do DS (Q3) |
| Subtítulo/descrição | 14px / 400 / muted | `.ds-sub` (token, sem mudança visual) |

## 5. Regras de negócio

> Esta é uma demanda de apresentação. As "regras" são de design system / arquitetura de frontend, não de domínio. Todas moram no **Front** (DS + `main.css`); não há espelho de backend (ver CA de zero backend).

- **R1**: Título de página tem fonte única no `PageHeader`. Mora em: `design-system/src/components/page-header/PageHeader.vue`. Nenhuma view define heading de página em CSS scoped.
- **R2**: Título de seção e título de card têm fonte única no DS via classes utilitárias tokenizadas `.ds-section-title` (21px/800) e `.ds-card-title` (15px/700) em `main.css` (Q1). Nenhuma view define `card-titulo`/`painel-titulo`/`lista-titulo`/`secao-titulo` scoped.
- **R3**: Label de campo tem fonte única (`AppLabel`/`AppField`) no canônico 12px/600 (Q2). Nenhuma view define `.campo-label` scoped.
- **R3-A**: Texto de input e botão padrão têm fonte única em 13px via `--text-sm` (Q3). `.form-input`/inputs padrão e `.btn-*`/botões padrão do DS referenciam o token. Nenhuma view redefine font-size de input/botão padrão em scoped.
- **R4**: Todo valor de tipografia (size/weight/line-height/tracking) referencia token `--text-*`/`--font-weight-*`/`--line-height-*`/`--tracking-*`. Nenhum valor hardcoded de px/rem para tipografia em componente do DS ou em `main.css` fora do bloco `:root` de tokens.
- **R5**: Mudar o valor de um token reflete em todas as telas que consomem o componente/classe correspondente (efeito "1 lugar muda o site"). Verificável por CA1.
- **R6**: Nenhuma mudança altera comportamento, dados, lógica de domínio ou backend — é exclusivamente apresentação.

## 6. Modelo de dados

Nenhuma tabela, coluna, índice, migration ou audit. Demanda 100% frontend/apresentação. (CA-BACKEND valida.)

## 7. UX e fluxo

Sem novo fluxo de usuário. O resultado é coerência tipográfica:
- **Título de página**: passa a 30px/800 em todas as telas (hoje 24px no PageHeader e variado nas scoped). Mudança visual intencional e uniforme.
- **Título de seção/card**: normaliza outliers para 21px/15px via classes `.ds-section-title`/`.ds-card-title` (Q1).
- **Label**: normaliza para 12px/600 (hoje 14px/500 via `AppField`/`AppLabel` — ver R3 e Q2). Labels ficam menores e mais pesados em massa, intencional.
- **Input/botão**: normaliza para 13px (`--text-sm`) em `.form-input`/inputs e `.btn-*`/botões padrão do DS (Q3). Mudança visual ampla e intencional em formulários e botões de toda a plataforma.
- Estados (loading/erro/vazio) inalterados — só herdam a tipografia tokenizada.
- Responsivo: tokens são absolutos; o `.app-page` já trata o responsivo de layout. Não introduzir media-query de tipografia salvo se já existir.

Componentes/estilos do DS reutilizados/estendidos: `PageHeader`, `Label`, `Field`, `.form-input`/inputs, `.btn-*`/botões, e novas classes utilitárias `.ds-section-title`/`.ds-card-title`. Nenhum componente novo (Q1 = classes utilitárias).

## 7-A. Critérios de aceite (testáveis)

- **CA1 (efeito "1 lugar muda o site" — núcleo da demanda)**: Dado o token `--text-3xl` definido em `main.css`, Quando seu valor é alterado, Então o tamanho do título de página muda em TODAS as telas que usam `PageHeader` sem editar nenhuma view. Verificável: `PageHeader.vue` não contém valor literal de font-size; referencia `var(--text-3xl)` (ou classe/token equivalente).
- **CA2 (PageHeader canônico)**: Dado o `PageHeader` redefinido, Quando renderizado, Então o `<h1>` tem font-size 30px, font-weight 800 e letter-spacing -0.015em, todos via token. Verificável por análise estática de `PageHeader.vue` (sem `text-2xl`/`font-bold` literais).
- **CA3 (tokens presentes)**: Dado `main.css`, Quando inspecionado, Então o `:root` contém a escala `--text-*` (10→30px), `--font-weight-*` (regular→extrabold), `--line-height-*` e `--tracking-*` conforme tabelas 4.1–4.4. Verificável por grep dos nomes de token.
- **CA4 (zero heading scoped legado órfão)**: Dado `frontend/src`, Quando se faz `grep -rE '(card-titulo|painel-titulo|lista-titulo|secao-titulo|page-titulo)' --include='*.vue'`, Então o resultado é vazio (todos migrados para `PageHeader` ou classes `.ds-section-title`/`.ds-card-title`).
- **CA5 (zero label scoped legado órfão)**: Dado `frontend/src`, Quando se faz `grep -rl 'campo-label' --include='*.vue'`, Então o resultado é vazio (8 arquivos migrados para `AppField`/`AppLabel`/token).
- **CA6 (label canônico 12px/600 — Q2)**: Dado `AppLabel`/`AppField`, Quando renderizam o label, Então usam 12px/600 via token (`--text-xs`/`--font-weight-semibold`) — coerente em todos os 308 `<AppField>`. Verificável por análise estática de `Label.vue`/`Field.vue` (sem 14px/500 literais ou tokenizados de outra escala).
- **CA7 (título de seção/card canônico — Q1)**: Dado um título de seção com `.ds-section-title`, Quando renderizado, Então é 21px/800/-0.01em via token; Dado um título de card com `.ds-card-title`, Então é 15px/700 via token. Verificável por análise estática das classes em `main.css` (sem componente `SectionTitle`).
- **CA8 (input/botão padrão 13px — Q3)**: Dado um input padrão (`.form-input`/equivalente) ou botão padrão (`.btn-*`) do design system, Quando renderiza, Então usa `--text-sm` (13px). Verificável por análise estática de `main.css`/estilos do DS (`.form-input` e `.btn-*` referenciam `var(--text-sm)`; sem 14px/0.875rem literais nesses seletores) e por grep de inputs/botões scoped que hardcodavam 14px migrados para o token.
- **CA9 (headings manuais migrados)**: Dado `frontend/src`, Quando se varre os 163 `<h1>/<h2>/<h3>` manuais, Então nenhum define font-size/font-weight de tipografia em CSS scoped fora do padrão de tokens (cada um usa `PageHeader`, classe-token de seção/card, ou herda o base tokenizado). Verificável por revisão das views do inventário (seção 9) + ausência de declarações scoped de `font-size` em headings nessas views.
- **CA10 (Tailwind alinhado)**: Dado o preset (`design-system/src/tailwind/preset.js`), Quando um componente DS usa classe Tailwind de tamanho (ex: `text-2xl`), Então o valor resultante bate com o token correspondente (sem divergência visual). Verificável por análise estática (ou ausência de uso de classes de size cruas nos componentes tipográficos do DS).
- **CA11 (zero backend / zero lógica)**: Dado o diff completo da entrega, Quando inspecionado, Então não há nenhuma alteração em `backend/`, `db/migrations/`, store/service, nem mudança de comportamento/dados — apenas `main.css`, componentes/estilos do DS, markup de views (troca de tag/classe) e `Docs/`. Verificável por `git diff --stat` restrito a frontend + Docs.
- **CA12 (gate automático verde — gate de entrega)**: Dado o projeto, Quando se roda build + typecheck + suíte de testes, Então tudo passa verde (nenhuma regressão de unidade — a mudança é CSS/markup). Verificável por execução dos scripts em `Docs/COMANDOS.md`. Este CA é gate obrigatório: sem build/typecheck/testes verdes, a entrega não é commitada.
- **CA13 (documentação viva)**: Dado `Docs/DESIGN.md`, Quando inspecionado, Então contém a seção "Escala tipográfica" com a tabela de tokens, o mapa semântico (incluindo inputs/botões a 13px) e o "quando usar cada componente/classe"; e `Docs/Discoverys/tipografia/01_discovery.md` existe com a auditoria + escala canônica. Verificável por leitura dos arquivos.
- **CA14 (sem regressão de aparência na maioria — validação humana)**: Dado o deploy em prod, Quando o usuário inspeciona um conjunto representativo de telas (admin, prontuário, atendimentos, relatórios, estabelecimento), Então a maioria mantém aparência equivalente e apenas os deltas intencionais (título de página 24→30px; label 14px/500→12px/600; input/botão 14px→13px) mudam de forma uniforme. Verificável por validação visual do usuário em prod (fora do escopo automatizado do QA — sandbox sem browser).

## 8. Riscos e dependências

- **R-CHURN (alto)**: É big-bang sem QA visual no sandbox. Mitigação de design: tokens escolhidos para que a MAIORIA das telas mantenha aparência equivalente; só os deltas intencionais normalizam. Os deltas visíveis e propositais são: título de página (24px→30px), label (14px/500→12px/600) e input/botão (14px→13px). Validação visual final é do usuário em prod.
- **R-LABEL (médio)**: alterar `Field.vue`/`Label.vue` para 12px/600 impacta 308 `<AppField>` + label cru em massa. Deixa labels menores e mais pesados que hoje — aceito pelo usuário (Q2 confirmado).
- **R-INPUT-13PX (médio, CONFIRMADO NO ESCOPO — Q3)**: `--text-sm` a 13px reescreve `.form-input`/inputs e `.btn-*`/botões padrão do DS, de 14px para 13px. Afeta formulários e botões em TODA a plataforma. É mudança visual intencional, confirmada pelo usuário. A validação visual final é dele em prod (sandbox sem browser). Atenção do dev: aplicar no token/seletor canônico do DS, não tela a tela; e migrar inputs/botões com 14px hardcoded em scoped para o token.
- **R-TAILWIND (baixo)**: se `text-2xl` etc. forem usados via classe Tailwind em algum lugar e o preset não tiver `fontSize` customizado, eles seguem o default do Tailwind (24px), que coincide com `--text-2xl`. Conferir se algum componente DS usa `text-2xl` esperando outro valor (PageHeader usa — será trocado por token).
- **Dependência**: `Docs/DESIGN.md` e `Docs/Discoverys/tipografia/` atualizados nesta mesma entrega.
- **Gate automático**: build + typecheck + testes verdes (não há regressão de unidade esperada, pois é CSS/markup).

## 9. Observações para execução

**Não-negociável:**
- Tokens em `main.css` no bloco `:root` (e `.dark` só se já houver override de tipografia — não criar novo).
- `PageHeader` consome tokens (não valores literais).
- Zero backend, zero lógica, zero dados.
- Migração total: ao final, `grep` por `campo-label`, `card-titulo`, `painel-titulo`, `lista-titulo`, `secao-titulo` em `frontend/src` retorna vazio (as classes-token canônicas `.ds-section-title`/`.ds-card-title` vivem só em `main.css`).
- Q1 travado: usar classes utilitárias `.ds-section-title`/`.ds-card-title` em `main.css`. NÃO criar componente `SectionTitle`. Headings existentes só trocam de classe.
- Q2 travado: `AppLabel`/`AppField` renderizam label a 12px/600.
- Q3 travado: `.form-input`/inputs e `.btn-*`/botões padrão do DS a 13px (`--text-sm`).
- Reuso > duplicação: estender `PageHeader`/`Label`/`Field`/`.form-input`/`.btn-*` existentes; nenhum componente novo.
- Zero backend/lógica é não-negociável (CA11). Build+typecheck+testes verdes é gate de entrega (CA12). Validação visual humana em prod é a aceitação final (CA14).

**Liberdade técnica do dev:**
- Nomes finais dos tokens (semânticos vs. t-shirt) desde que coerentes com a tabela 4.x e documentados.
- Forma exata de aplicar 13px a inputs/botões (token no seletor base do DS) desde que centralizada (não tela a tela) e que satisfaça CA8.

**Inventário de migração (mapa por área — o dev precisa do conjunto):**

*Label scoped (`.campo-label`) → `AppField`/`AppLabel`/token (8 arquivos):*
- `components/atendimentos/EncaixeModal.vue`
- `components/prontuario/CancelarReceitaModal.vue`
- `components/prontuario/secoes/SecaoExamesRealizados.vue`
- `components/prontuario/secoes/SecaoHistoriaFamiliar.vue`
- `components/prontuario/secoes/SecaoHistoriaPregressa.vue`
- `components/prontuario/secoes/SecaoProcedimentosIndicados.vue`
- `views/estabelecimento/EstabelecimentoView.vue`
- `views/minhaConta/MinhaContaView.vue`

*Heading de seção/card scoped → `SectionTitle`/classe-token (24 arquivos):*
- `components/agenda/CheckInModal.vue`
- `components/estabelecimento/FuncionamentoTab.vue`, `ListasVariaveisTab.vue`, `ReparticoesTab.vue`, `UnidadesTab.vue`
- `components/estoque/EstoqueItemDrawer.vue`
- `components/pacientes/PacienteFormModal.vue`
- `components/prontuario/EvolucaoDetalheDrawer.vue`, `ReceitasPacienteTab.vue`, `secoes/SecaoHistoriaFamiliar.vue`
- `components/relatorios/RelatorioAgendaTab.vue`, `RelatorioFinanceiroTab.vue`, `RelatorioOrcamentosTab.vue`, `RelatorioPessoasTab.vue`, `RelatorioVisaoGeral.vue`
- `components/termos/TermoFormEmbutido.vue`, `TermosPainelEmbutido.vue`
- `views/configuracoes/ModelosProntuarioView.vue`, `TermoFormView.vue`, `TermosListaView.vue`
- `views/estabelecimento/EstabelecimentoView.vue`
- `views/financeiro/FinanceiroView.vue`
- `views/minhaConta/MinhaContaView.vue`
- `views/profissionais/MeusConvitesView.vue`

*Headings manuais `<h1>/<h2>/<h3>` (163 ocorrências em ~64 arquivos):* o dev deve varrer todos. Classificar cada um:
- Heading que é título da página → trocar por `PageHeader`.
- Heading que é título de seção/painel → classe `.ds-section-title`.
- Heading que é título de card → classe `.ds-card-title`.
- Heading puramente estrutural sem estilo próprio (raro) → herda token via base CSS.

*Inputs e botões a 13px (Q3 — `--text-sm`):*
- Seletores base do DS: `.form-input` (e equivalentes de input/select/textarea padrão) e `.btn-*` (botões padrão) em `main.css`/estilos do DS → referenciar `var(--text-sm)`. Esta é a aplicação centralizada que cobre a plataforma inteira.
- Inputs/botões com `font-size` 14px (`0.875rem`) hardcoded em CSS scoped que estiver sendo tocado na migração → migrar para o token. O dev varre `grep -rE 'font-size:\s*(14px|0\.875rem)' frontend/src --include='*.vue'` em inputs/botões e migra os pontos do design system; campos de texto fora do padrão de input/botão (ex.: corpo de lista a 14px) permanecem em `--text-base`.

**Estratégia de varredura (sugerida):** rodar grep dos padrões acima, migrar área por área (admin/config → prontuário → atendimentos → relatórios → estabelecimento → minha conta → termos → estoque/agenda), e ao fim validar que os greps de classes legadas retornam vazio.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar seção "Escala tipográfica" como fonte de verdade: a tabela de tokens (4.1–4.4), o mapa semântico (4.5) **incluindo inputs/botões a 13px (`--text-sm`, Q3)**, e "quando usar `PageHeader` vs `.ds-section-title` vs `.ds-card-title` vs `AppLabel`/`AppField` vs `.form-input`/`.btn-*`". Atualizar a descrição do `PageHeader` (passa a 30px/800), do label (12px/600) e registrar inputs/botões a 13px. Manter coerência com o cabeçalho "Quando ler/atualizar". (Atualizado pelo dev na entrega, via CA13.)
- **`Docs/Discoverys/tipografia/01_discovery.md`** (nova subpasta) — registrar a auditoria da fragmentação atual (números desta seção 1) + a escala canônica proposta, como memória da decisão (custo de não documentar = futura re-auditoria).
- Demais docs (`ARQUITETURA.md`, `INFRA.md`, `COMANDOS.md`, `LGPD.md`) — nenhum, demanda não toca arquitetura de backend/infra/dados/PII.

---

## 11. Decisões confirmadas pelo usuário (2026-06-08)

> Todas as decisões abaixo foram travadas pelo usuário. Não há pendências; o briefing está aprovado e pronto para o dev.

- **Q1 — CONFIRMADO: classes utilitárias tokenizadas.** `.ds-section-title` (21px/800) e `.ds-card-title` (15px/700) em `main.css`. NÃO criar componente `SectionTitle`. Headings existentes (`<h2>`/`<h3>`) só trocam de classe. (Bateu com o default do BA.)
- **Q2 — CONFIRMADO: label canônico 12px/600 (fiel ao mockup).** `AppLabel`/`AppField` passam de 14px/500 para 12px/600. Encolhe labels nos 308 `<AppField>` — intencional. `.campo-label` scoped legado migra para o mesmo padrão. (Bateu com o default do BA.)
- **Q3 — CONFIRMADO: aplicar 13px do mockup AGORA (diverge do default anterior do BA).** `--text-sm` (13px) reescreve `.form-input`/inputs padrão e `.btn-*`/botões padrão do DS, de 14px para 13px, fiel ao mockup. Afeta formulários e botões em TODA a plataforma — registrado como mudança visual intencional. Está no escopo (seção 3), no inventário de migração (seção 9) e tem CA próprio (CA8). Validação visual final é do usuário em prod (sandbox sem browser).
- **Q4 — CONFIRMADO: título de página 30px/800/-0.015em para todas as telas.** `PageHeader` vai a 30px e isso aparece em telas que hoje têm 24px. Mudança intencional e uniforme.
