# Mobile — novas telas e funcionalidades do Claude Design

**ID**: 2026-06-20_001
**Status**: Aprovado por usuário em 2026-06-20 (modo autônomo — "o fluxo montado no design é exatamente o que eu preciso; sim para todas as decisões")
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (épico — 9 blocos, ~80 CAs)
**Áreas regressivas tocadas**: navegação (bottom tab bar + header), agenda (detalhe do agendamento), permissionamento (degradação por papel), financeiro (caixa/pagamento), prontuário (anexos/fotos), estoque, automação, push

> **Executor**: `imedto-developer` no app `mobile/` (Capacitor 6 + Vue 3 + TS + Pinia). O app **só consome a API** (cookie BFF + header `X-Estabelecimento-Id`). Schema **não muda** neste briefing — exceto o bloco 7 (Fotos clínicas), que tem **dependência de backend** documentada na §8. O `imedto-database`/trilha backend trata essa dependência separadamente; o mobile entrega o bloco 7 com fallback.

---

## 1. Contexto e motivação

O app mobile do médico (`mobile/`) foi construído a partir de um snapshot anterior do Claude Design e hoje cobre: Login/biometria, Seletor de estabelecimento, Agenda, Detalhe do agendamento, Novo agendamento, Pacientes, Ficha, Prontuário, Avisos, Receita/Atestado/Exame, Orçamento, Mais e os estados globais G1/G2/G3.

O **design novo** (`mobile/_design-reference/project/Imedto Mobile.html`, ~2945 linhas) reorganiza a navegação e adiciona um conjunto de telas que faltam para o médico/dono **resolver a clínica no celular sem abrir o web**: um painel de início glanceável, o caixa/recebimentos do dia, registro de pagamento na hora, check-in e conclusão de atendimento direto da agenda, estoque de bolso, fotos clínicas com região/marcador, automações e novos toggles de push.

O backend **já expõe todos os endpoints** necessários (Dashboard, Financeiro/Caixa, Cobranças, Inventário, Cadastros de estoque, Automação, Agendamento check-in/concluir, ProntuarioAnexo) — confirmado nos controllers em `backend/src/Services/Imedto.Backend.API/Controllers/`. A única lacuna é metadado de **região/marcador** nas fotos clínicas (§8).

A demanda é **fidelidade ao protótipo**: o design É a especificação aprovada. Onde houve escolha de produto, foi decidida pela opção que o design mostra (decisões registradas na §9).

## 2. Persona-alvo

- **Dono / Profissional (médico) em movimento** — entre consultas, na recepção, no centro cirúrgico. Quer ver o dia em 1 olhada (Início), receber um pagamento na hora (Pagamento/Caixa), dar check-in no paciente que chegou e concluir o atendimento oferecendo cobrança, registrar uma foto de evolução, e configurar lembretes — tudo com o polegar.
- **Recepcionista** (papel `Recepcionista`) — acesso a agenda/pacientes/financeiro_paciente/estoque conforme RBAC; **não** acessa prontuário/fotos clínicas (restrito a Profissional/Dono no backend). A UI degrada (some) o que o papel não permite.
- Frequência: **diária**, múltiplas vezes ao dia. Read-first, write-light.

## 3. Escopo

**Inclui** (9 blocos):
1. Navegação — nova tab **Início** + Avisos vira **sino no header** (badge de não-lidas).
2. **Início / Dashboard** — saudação, recebido hoje, stats do dia, próximo agendamento, "precisa de atenção".
3. **Caixa do dia** — total + split por forma, lista de recebimentos, novo recebimento, compartilhar.
4. **Pagamento (Recebimento)** — valor, forma de pagamento, parcelas (cartão), valores rápidos, observação, vínculo a agendamento/cobrança.
5. **Check-in / Atendimento na Agenda** — marcar "paciente chegou", concluir atendimento, sheet "receber agora / depois".
6. **Estoque** — lista de materiais, busca, contagem total/baixo, ajuste de quantidade (movimentação), repor.
7. **Fotos clínicas** — captura com região + marcador (Antes/Depois/Evolução), grade, visualizar, comparar antes/depois.
8. **Automação** — toggles de lembretes/confirmação + relacionamento; configuração e regras.
9. **Push — novos toggles** — preferências de push para caixa, estoque, fotos, pagamento, automação, avisos.

**Não inclui** (anti-scope-creep):
- Nenhuma mudança de schema neste briefing (exceto a dependência de backend do bloco 7, tratada fora).
- Configuração avançada de comissões, tabela de preço, taxas de cartão (existem no backend, mas o design mobile não os expõe — ficam no web).
- Editor visual de regras de automação (condições/ações JSON). O design mostra **toggles**, não um editor de regras — mapeamos toggles para a entidade certa (ver bloco 8 e §9).
- Fechamento/abertura de caixa como fluxo dedicado com conferência de valores — o design mostra apenas **visualização** do caixa do dia + novo recebimento (ver §9, decisão D3).
- Gerar **link de pagamento** real (PIX/cartão online). O design tem o botão "Gerar link", mas não há backend de link de pagamento; tratado como "em breve" (§9, decisão D4).
- Estados de convênio/guia no fluxo de pagamento mobile (existem no web).

## 4. Regras de negócio

Toda regra abaixo é **espelhada back+front**: o backend (422 `BusinessException` / 402 / 403 / RBAC) é a fonte da verdade; o front é UX. Mensagens de erro genéricas (LGPD).

- **R1 — Multi-tenant.** Toda request envia `X-Estabelecimento-Id` (já injetado pelo `lib/http.ts`). Trocar de estabelecimento recarrega contexto (agenda, financeiro, estoque, permissões). Dado de um tenant nunca aparece em outro. Mora em: backend (filtro `EstabelecimentoId` em todas as queries) + front (header automático). Validado em: back + front.
- **R2 — RBAC com degradação (G2).** O que o papel/permissão nega **some da UI** (não vira erro). Mapa de permissões por bloco (catálogo real `CatalogoPermissoes` + `PermissoesExtras`):
  - Início/Dashboard → `agenda.ver` (controller `[RequiresAcao("agenda")]`). A tab Início aparece para quem tem `agenda.ver`.
  - Caixa/KPIs → `financeiro.ver`. Card "Recebido hoje" e tela Caixa só aparecem com `financeiro.ver`.
  - Registrar pagamento → `financeiro_paciente.registrar`. Botões "Registrar pagamento" só aparecem com essa permissão.
  - Estoque → `estoque` (ver). Ajustar quantidade/criar item → papel `Dono`/`Recepcionista` (movimentação herda `estoque`; CRUD de item é `[RequiresPapel(Dono, Recepcionista)]`).
  - Automação → ler config/regras: aberto a membros; **editar** (toggles que persistem regra) → `automacao_config` (`PermissoesExtras.AutomacaoConfig`). Sem essa permissão, os toggles ficam **read-only/ocultos** conforme §9 D5.
  - Fotos clínicas → papel `Profissional`/`Dono` (anexo de prontuário é `[RequiresPapel(Profissional, Dono)]`). Recepcionista não vê o atalho de fotos.
  - Check-in / Concluir → `agenda` (controller). Disponível a quem opera a agenda.
  - `permissoesStore.pode("area.acao")` / `.podeExtra("automacao_config")` é a fonte no front (já existe em `mobile/src/stores/permissoes.ts`). Mora em: back (filtros) + front (store). Validado em: back + front.
- **R3 — Assinatura (G1).** Endpoints de mutação têm `[RequiresAssinaturaAtiva]` (402). Com assinatura inativa, as ações de escrita (pagamento, movimentação de estoque, foto, salvar automação) caem no fluxo G1 já existente (`onAssinaturaBloqueada`). Leitura de dashboard/caixa continua. Mora em: back + front. Validado em: back + front.
- **R4 — LGPD.** PII (nome do paciente, valores, fotos) nunca em log. Mensagens de erro genéricas. Fotos clínicas e ficha financeira do paciente são **dado sensível**: o acesso é auditado no backend (a ficha financeira já registra via `IPacienteAcessoLogService`; o upload/download de anexo já passa pelo handler que audita). O front nunca expõe PII em toast/erro. Mora em: back (audit) + front (mensagem genérica). Validado em: back + front.
- **R5 — Pagamento sempre vinculado a uma cobrança.** O backend registra pagamento via `POST /api/cobrancas/{cobrancaId}/pagamentos`. Logo, **registrar pagamento exige uma cobrança existente** para aquele agendamento/paciente. Fluxo: a partir do agendamento, obter a cobrança via `GET /api/cobrancas/por-agendamento/{agendamentoId}`; se não houver cobrança (ex.: agendamento sem check-in que gere cobrança), o app orienta a fazer check-in primeiro (a cobrança nasce no check-in com `ValorCobrado`). Decisão D2 da §9. Mora em: back (a cobrança é a raiz) + front (orquestra a obtenção). Validado em: back + front.
- **R6 — Valor sugerido.** `GET /api/cobrancas/valor-sugerido?profissionalUsuarioId=...` retorna `ValorSugerido` (pode ser null = não configurado). O campo de valor do Pagamento pré-preenche com esse valor quando disponível; usuário pode editar. Mora em: back + front. Validado em: front (pré-preenchimento) sobre dado do back.
- **R7 — Formas de pagamento são dinâmicas.** As formas (PIX/Dinheiro/Cartão e outras) vêm de `GET /api/financeiro/formas-pagamento?ativas=true`, cada uma com `Id` (long). O design mostra PIX/Cartão/Dinheiro fixos; o app **mapeia** os botões para os `Id`s reais retornados, e o "Parcelas" só aparece para a forma marcada como cartão. O `POST .../pagamentos` recebe `Formas: [{ FormaPagamentoId, Valor, Parcelas, Juros }]`. Decisão D6 da §9. Mora em: back (catálogo) + front (mapeamento). Validado em: back + front.
- **R8 — Caixa é leitura + novo recebimento.** `GET /api/financeiro/caixa?data=hoje` retorna `CaixaDiarioDto` (status Aberto/Fechado, `ResumoPorForma`, `TotalDia`, `TotalEstornos`). A tela Caixa exibe esses dados. "Novo recebimento" abre o fluxo de Pagamento. Abrir/fechar/reabrir caixa **não** entra no MVP mobile (D3). Mora em: back + front. Validado em: back + front.
- **R9 — Check-in gera/atualiza a cobrança e muda o status.** `POST /api/agendamentos/{id}/checkin` (body `RegistrarCheckInDto`: `SalaId?`, `TipoAtendimento="Particular"`, `ValorCobrado=0`, `ConvenioId?`) marca o paciente como chegou (`CheckInEm`) e cria a cobrança particular do atendimento. Concluir: `POST /api/agendamentos/{id}/concluir`. O status do agendamento (`AgendamentoDto.Status`) governa quais botões aparecem (R10). Mora em: back + front. Validado em: back + front.
- **R10 — Estados do agendamento governam as ações (fidelidade ao design).** Espelhar `syncApptActions` do design com o `Status` real:
  - `Agendado`/`Confirmado` → mostra **Check-in**; esconde "Iniciar atendimento" e a nota de chegada.
  - `CheckIn` (chegou, `CheckInEm != null`) → esconde Check-in; mostra "Iniciar atendimento" + nota "Chegou às HH:MM"; mostra "Registrar pagamento".
  - `Atendido`/`Concluído` → esconde Check-in/Iniciar; mostra nota "Atendimento concluído"; mostra "Registrar pagamento".
  - `Faltou` → esconde pagamento; nota "Paciente não compareceu".
  - `Cancelado`/`Expirado` → ações de fluxo ocultas. Mora em: front (deriva do Status do back). Validado em: front.
- **R11 — Movimentação de estoque é a forma de ajustar quantidade.** O design ajusta a quantidade num stepper e "Salvar quantidade". No backend, quantidade só muda via `POST /api/inventario/movimentacoes` (`ItemInventarioId`, `Tipo` Entrada/Saída/Ajuste, `Quantidade`, `CustoUnitario`, `Observacao?`). O app calcula o delta entre quantidade atual e nova, e envia uma movimentação `Ajuste` (ou Entrada/Saída conforme sinal — D7). "Repor" abre o mesmo fluxo de movimentação tipo Entrada. Mora em: back (movimentação é a operação) + front (calcula delta). Validado em: back + front.
- **R12 — "Baixo estoque" vem do backend.** O item traz `EstoqueAbaixoMinimo` (bool) e `QuantidadeMinima`. O filtro "Em baixa" usa `GET /api/inventario/itens?apenasAbaixoMinimo=true`. O front **não recalcula** a regra — confia no flag do back. Mora em: back. Validado em: front (usa o flag).
- **R13 — Foto clínica = anexo de imagem da evolução com região + marcador.** Captura via `useCamera` (já existe) → upload via `POST /api/paciente/{pacienteId}/prontuario/anexos` (multipart). **Dependência de backend (§8):** o `AnexoDto`/`AdicionarAnexoCommand` hoje não têm campos de **região** nem **marcador** (Antes/Depois/Evolução). Até o backend expor esses campos, o mobile usa fallback: codifica `região` e `marcador` no `NomeOriginal` do arquivo (ex.: `foto-clinica__<regiao>__<marcador>__<timestamp>.jpg`) e parseia de volta na listagem. A comparação antes/depois agrupa por região e ordena por data (igual ao design). Mora em: back (storage + audit) + front (captura/parse/fallback). Validado em: back + front.
- **R14 — Bell badge reusa notificações.** O sino do header mostra `notificacoesStore.naoLidas` (já existe, alimentado por `GET /notificacoes/contador-nao-lidas`). Tocar abre a tela Avisos já existente. Mora em: front (store existente). Validado em: front.
- **R15 — Automação: toggles persistem na config existente.** O backend tem `ConfiguracaoAutomacaoDto` com flags **limitadas** (`LembretesHabilitados`, `LembretesWhatsappHabilitados`, `HorasAntecedenciaLembrete`, `ExpiracaoOrcamentosHabilitada`, `EmailRemetente`). O design mostra 6 toggles (lembrete 24h, confirmação, lembrete no dia, aniversário, recall, pós-consulta), que **não** têm contraparte 1:1 na config. Decisão D5 da §9: os toggles **com** contraparte (lembrete 24h ↔ `LembretesHabilitados`, e o canal WhatsApp ↔ `LembretesWhatsappHabilitados`) persistem via `PUT /api/automacoes/configuracao`; os toggles **sem** contraparte (confirmação, lembrete no dia, aniversário, recall, pós-consulta) aparecem como **"em breve"** (visualmente presentes, desabilitados, sem persistência) para manter a fidelidade visual sem prometer comportamento inexistente. Mora em: back (config) + front. Validado em: back + front.

## 5. Modelo de dados

**Sem migration neste briefing**, com **uma exceção que é dependência de backend** (não implementada pelo mobile):

- **Fotos clínicas (bloco 7) — dependência de backend (§8).** Para suportar fielmente região + marcador (Antes/Depois/Evolução) e a comparação, o ideal é o backend adicionar a `prontuario_anexos` (ou tabela equivalente) as colunas `regiao_anatomica` (text, opcional) e `marcador` (text/enum: Antes|Depois|Evolucao, opcional), expor no `AnexoDto`, aceitar no `AdicionarAnexoCommand` (form fields `regiao`, `marcador`), e idealmente um filtro `somenteImagens`. **Isso é trabalho da trilha backend/`imedto-database`, fora deste briefing.** Enquanto não existir, o mobile usa o fallback de R13 (metadado no nome do arquivo). Quando o backend entregar, troca-se o fallback pelos campos reais (mudança cirúrgica no service mobile).

Todas as demais telas leem/escrevem em tabelas e DTOs **já existentes** (dashboard, caixa_diario, cobrancas/pagamentos, itens_inventario/movimentacoes_estoque, configuracao_automacao). Multi-tenant (`estabelecimento_id`), audit e LGPD já estão garantidos no backend.

## 6. UX e fluxo

Princípios: **mobile ≠ web espremido**. Reusar o design system mobile (`mobile/src/components/ui/` e `layout/`): `BottomSheet`, `AppEmptyState`, `AppSearchInput`, `AppStatusPill`, `AppAvatar`, `AppToast`, `AppConfirmDialog`, `PushBanner`, `BottomTabBar`, `ActionSheet`, `EstabelecimentoSwitcher`. Tema claro/escuro via tokens (`tokens.css` + `app.css`). Alvos de toque ≥ 44px. Fidelidade ao protótipo (`Imedto Mobile.html`) em layout, ícones (Font Awesome), cores e textos.

### Bloco 1 — Navegação (regressivo)
- **Bottom tab bar**: `Início · Agenda · [FAB] · Pacientes · Mais` (ids do design: `data-tab="inicio|agenda|pacientes|mais"`). A aba **Avisos sai da barra**. Editar `BottomTabBar.vue` + router (`/` redireciona para `/inicio`; nova rota `inicio`; rota `avisos` deixa de ser tab e vira drill-in/push acessível pelo sino).
- **Header**: adicionar **sino** (`bellBtn` + `bellBadge`) à direita, sempre visível (em todas as abas), com badge = `naoLidas`. Tocar → abre Avisos. Os botões de busca/filtro da agenda (`ag-only`) continuam só na aba Agenda.
- **FAB** central permanece; a action sheet ganha a ação **"Registrar pagamento"** (já no design, `data-action="pagamento"`), com degradação por `financeiro_paciente.registrar`.

### Bloco 2 — Início (`view-inicio`)
- Hero: saudação por horário (`homeGreet` "Bom dia/Boa tarde/Boa noite,") + nome do profissional (`homeName`, vem do auth/perfil).
- Card **Recebido hoje** (`homeCash`, clicável → abre Caixa): `homeCashVal` = total recebido hoje, `homeCashCount` = "N recebimentos", `homeCashPending` = pendente. Fonte: `GET /api/financeiro/caixa` (TotalDia + ResumoPorForma) e/ou `GET /api/financeiro/kpis` (Recebido, AReceber). Só aparece com `financeiro.ver`.
- **Stats** (`homeStats`): Atendidos · Na recepção (clicável → vai p/ Agenda) · Faltas. Fonte: `GET /api/dashboard` (AgendamentosHoje, e contagem de check-in/atendidos derivada da agenda do dia) — usar `DashboardDto` + agenda do dia.
- **Próximo** (`homeNextChip` + `homeNextCard`): próximo agendamento (de `DashboardDto.ProximosAgendamentos[0]`), card clicável → detalhe do agendamento.
- **Precisa de atenção** (`homeAttn`): itens acionáveis derivados do `DashboardDto`: orçamentos pendentes (`OrcamentosPendentes`), itens abaixo do mínimo (`ItensAbaixoMinimo`/`ItensAbaixoMinimoLista`), lançamentos vencidos (`LancamentosVencidos`). Cada item navega para o destino certo (orçamentos/estoque/financeiro) respeitando RBAC (some o que o papel não permite).
- Estados: loading (skeleton dos cards), erro (estado de erro com retry), vazio (sem agendamentos/sem atenção → mensagens específicas), offline (mostra dados em cache + barra offline G3).

### Bloco 3 — Caixa (`push-caixa`)
- Total do dia (`caixaTotal`) + split `caixaPix`/`caixaCard`/`caixaCash` a partir de `ResumoPorForma`. Lista de movimentações (`caixaList`) a partir do extrato de recebimentos do dia (`GET /api/financeiro/extrato?dataInicio=hoje&dataFim=hoje&tipo=Receita` ou pagamentos do dia). Botão **Novo recebimento** (`caixaNew`) → Pagamento. **Compartilhar** (`caixaShare`) → `useShare` com resumo textual do caixa (sem PII desnecessária; D3). RBAC `financeiro.ver`.
- Estados: loading, erro, vazio ("Nenhum recebimento hoje" via `AppEmptyState`), offline.

### Bloco 4 — Pagamento (`push-pagamento`)
- Cabeçalho do paciente (`payPatient`/`payName`/`paySub`) — selecionável via `patSheet` (picker) quando entrada vem do FAB; pré-preenchido quando entrada vem do agendamento.
- Valor (`payValue`, máscara BRL) com valores rápidos (`payQuick`); pré-preenche com `valor-sugerido` (R6).
- Forma de pagamento (`payMethods`) mapeada às formas reais (R7); Parcelas (`payParcWrap`/`payParc`) só para cartão.
- Vínculo (`payLink`) = cobrança/agendamento de hoje (R5). Observação (`payObs`).
- **Receber** (`paySave`) → `POST /api/cobrancas/{cobrancaId}/pagamentos`. "Gerar link" (`payLink`/link de pagamento) = "em breve" (D4).
- Estados: loading do valor-sugerido/formas, erro 422 genérico, sucesso (toast + volta + atualiza caixa/agenda), offline (bloqueia envio com aviso).

### Bloco 5 — Check-in/Atendimento (no `push-agendamento`)
- Botões condicionais por status (R10): `agCheckin` ("Check-in — paciente chegou"), `agStart` ("Iniciar atendimento" → abre Prontuário/nova evolução), nota `agArrivedNote`/`agArrivedTxt`, `agPay` ("Registrar pagamento").
- Concluir atendimento → após "Atendido", abre `atendSheet`: **Registrar pagamento** (`atendReceber` → Pagamento) ou **Concluir sem cobrar** (`atendDepois`).
- Botões "Atendido"/"Faltou" usam o fluxo existente; "Atendido" chama `POST /api/agendamentos/{id}/concluir` e dispara o sheet.
- Estados: loading da ação, erro (toast genérico, reverte UI), sucesso (atualiza status + stats).

### Bloco 6 — Estoque (`push-estoque`)
- Resumo: `estCountItens` (total), `estCountLow` (em baixa), `estValor` (valor em estoque, somatório `QuantidadeAtual * CustoMedio` ou campo agregado). Busca (`estSearch`, debounce 300ms via `useDebouncedRef`). Chips Todos/Em baixa (`estPills`). Lista (`estList`) de `GET /api/inventario/itens`.
- Sheet de ajuste (`estSheet`): nome (`estSheetName`), unidade (`estUnit`), quantidade atual (`estVal`), stepper (`estMinus2`/`estStepVal`/`estPlus2`), **Salvar quantidade** (`estSave` → movimentação delta, R11), **Repor** (`estRepor` → movimentação Entrada).
- Atalho em **Mais** (`maisEstoque` + `maisEstoqueSub` "N itens · M em baixa"). RBAC: ver = `estoque`; ajustar = `Dono`/`Recepcionista`.
- Estados: loading (skeleton lista), erro, vazio ("Nenhum material" / "Nada em baixa"), offline.

### Bloco 7 — Fotos clínicas (`push-fotos`)
- Contexto do paciente (`fotoAv`/`fotoName`/`fotoMeta`). Grade (`fotoGrid`) de imagens com tag e região (overlay). **Capturar** (`fotoCapture`) → `fotoSheet` (câmera): label de região (`camRegionLabel`), input região/descrição (`camRegion`), marcador (`camTags`: Antes/Depois/Evolução), shutter (`camShutter`).
- Visualizar (`fotoViewSheet`) e **Comparar** (`fotoCompareBtn`): agrupa por região, monta Antes/Depois.
- Acesso a partir da Ficha/Prontuário do paciente (dado clínico). RBAC: `Profissional`/`Dono`.
- Upload via `useCamera` + `prontuario.service.uploadAnexo` com fallback de metadado (R13/§8). Auditoria de acesso garantida no backend.
- Estados: loading (grade skeleton), erro upload (toast genérico), vazio ("Nenhuma foto clínica"), offline (bloqueia captura/upload com aviso; pode listar cache).

### Bloco 8 — Automação (`push-automacao`)
- Intro + toggles. Os toggles **com contraparte** (lembrete 24h e canal WhatsApp) persistem via `PUT /api/automacoes/configuracao`; os **sem contraparte** ficam "em breve" (D5). RBAC de edição: `automacao_config` (sem ela, toggles read-only/ocultos). Acesso a partir de **Mais** (`maisAutomacao`).
- Estados: loading da config, erro, sucesso (toast), offline (bloqueia escrita).

### Bloco 9 — Push (novos toggles)
- Na tela Mais (ou sub-tela de notificações push), além do toggle global existente, adicionar preferências por módulo: `push-caixa`, `push-estoque`, `push-fotos`, `push-pagamento`, `push-automacao`, `push-avisos`. Decisão D8 (§9): como **não há endpoint de preferências de push por categoria** no backend, essas preferências são **locais ao device** (persistidas via `@capacitor/preferences`), influenciando apenas a exibição/roteamento de banners no app. Visualmente fiéis ao design.
- Estados: persistência local imediata; sem loading de rede.

## 7. Critérios de aceite (testáveis)

> Convenção: cada bloco tem caminho feliz + multi-tenant + RBAC + LGPD + estados + tema + toque/fidelidade. "App" = app mobile rodando local (browser dev via proxy ou device).

### Bloco 1 — Navegação
- **CA1** (caminho feliz): Dado um usuário logado com tenant ativo, Quando o app abre, Então a bottom tab bar mostra exatamente `Início · Agenda · [FAB] · Pacientes · Mais` (sem aba Avisos), e a tab inicial selecionada é **Início**.
- **CA2** (sino + badge): Dado que há N notificações não-lidas, Quando qualquer aba está aberta, Então o header mostra o sino com badge = N; Quando toco no sino, Então abre a tela Avisos existente.
- **CA3** (badge zera): Dado o badge com N>0, Quando marco todas as notificações como lidas em Avisos, Então o badge some/zera no header sem reload manual.
- **CA4** (RBAC tab Início): Dado um usuário sem `agenda.ver`, Quando o app abre, Então a tab Início não causa erro (cai num fallback acessível conforme guard do router) e o card de caixa só aparece se tiver `financeiro.ver`.
- **CA5** (regressão rotas): Dado deep-link/rota antiga `/avisos`, Quando navego, Então a tela Avisos abre normalmente (push/drill-in) sem quebrar o guard de auth/tenant.
- **CA6** (tema): Dado tema claro e escuro, Quando alterno, Então tab bar, sino e badge respeitam os tokens nos dois temas.
- **CA7** (toque/fidelidade): Dado o protótipo, Quando comparo, Então ícones/labels/ordem das tabs e o sino batem com o design; alvos de toque ≥ 44px.

### Bloco 2 — Início / Dashboard
- **CA8** (caminho feliz): Dado um dia com agendamentos e recebimentos, Quando abro Início, Então vejo saudação por horário + nome, card "Recebido hoje" com valor real, stats (atendidos/recepção/faltas), próximo agendamento e itens de atenção, todos a partir de `GET /api/dashboard` (+ `caixa`/`kpis`).
- **CA9** (navegação dos cards): Dado o card "Recebido hoje", Quando toco, Então abre Caixa; Dado o card "Próximo", Quando toco, Então abre o detalhe do agendamento; Dado um item de atenção, Quando toco, Então vou ao destino correto (orçamentos/estoque/financeiro).
- **CA10** (multi-tenant): Dado um usuário com vínculo em A e B, Quando troco de A para B, Então todos os números de Início recarregam para o tenant B e nenhum dado de A persiste.
- **CA11** (RBAC): Dado um usuário sem `financeiro.ver`, Quando abro Início, Então o card "Recebido hoje" não aparece (degrada), e o restante do painel funciona.
- **CA12** (LGPD): Dado erro no backend (422/500), Quando o dashboard falha, Então a mensagem é genérica e nenhum nome de paciente/valor aparece em log/console.
- **CA13** (estados): Dado carregamento, Então skeletons; Dado dia vazio, Então `AppEmptyState` com texto específico ("Nenhum agendamento hoje"); Dado offline, Então barra G3 + dados em cache.
- **CA14** (tema/fidelidade): Dado claro e escuro, Quando comparo com `home.png`/protótipo, Então layout, cores e ícones batem nos dois temas.

### Bloco 3 — Caixa
- **CA15** (caminho feliz): Dado recebimentos no dia, Quando abro Caixa, Então vejo `caixaTotal` correto, split PIX/Cartão/Dinheiro a partir de `ResumoPorForma`, e a lista de movimentações do dia.
- **CA16** (novo recebimento): Dado a tela Caixa, Quando toco "Novo recebimento", Então abre Pagamento; ao concluir um pagamento, o total e a lista do Caixa atualizam.
- **CA17** (compartilhar): Dado a tela Caixa, Quando toco compartilhar, Então `useShare` abre com um resumo textual do caixa do dia.
- **CA18** (RBAC): Dado um usuário sem `financeiro.ver`, Quando tento acessar Caixa, Então o atalho não aparece e a rota cai no fallback (não vaza 403 cru).
- **CA19** (multi-tenant): Dado tenant A e B, Quando troco, Então o Caixa reflete só o tenant ativo.
- **CA20** (estados/tema): loading (skeleton), erro (retry), vazio ("Nenhum recebimento hoje"), offline (aviso); fiel nos dois temas.

### Bloco 4 — Pagamento
- **CA21** (caminho feliz): Dado uma cobrança aberta de um agendamento, Quando preencho valor, escolho forma e toco "Receber", Então `POST /api/cobrancas/{cobrancaId}/pagamentos` é chamado com `Formas:[{FormaPagamentoId, Valor, Parcelas, Juros}]`, retorna 204, mostra sucesso e volta.
- **CA22** (valor sugerido): Dado um profissional com valor configurado, Quando abro Pagamento, Então o valor pré-preenche com `valor-sugerido`; Dado null, Então o campo fica vazio e editável.
- **CA23** (formas dinâmicas + parcelas): Dado as formas de `GET /api/financeiro/formas-pagamento?ativas=true`, Quando renderizo, Então os botões refletem as formas reais; Quando escolho cartão, Então o seletor de Parcelas aparece; nas demais, some.
- **CA24** (sem cobrança): Dado um agendamento sem cobrança, Quando tento registrar pagamento, Então o app orienta a fazer check-in primeiro (não quebra) — espelha R5.
- **CA25** (RBAC): Dado um usuário sem `financeiro_paciente.registrar`, Quando abro a agenda/FAB, Então os botões "Registrar pagamento" não aparecem; e se a rota for forçada, o backend retorna erro tratado genericamente.
- **CA26** (LGPD): Dado erro 422 do backend, Quando registro pagamento inválido, Então a mensagem é genérica e sem PII.
- **CA27** (multi-tenant): Dado tenant B, Quando tento pagar cobrança do tenant A, Então recebo erro genérico (não encontrado) e nada vaza.
- **CA28** (estados/tema/toque): loading (formas/valor), erro, sucesso (toast), offline (bloqueia "Receber" com aviso); fiel nos dois temas; alvos ≥ 44px.

### Bloco 5 — Check-in / Atendimento
- **CA29** (check-in): Dado um agendamento `Agendado`/`Confirmado`, Quando toco "Check-in — paciente chegou", Então `POST /api/agendamentos/{id}/checkin` é chamado, o status vira `CheckIn`, e a UI mostra a nota "Chegou às HH:MM" + "Iniciar atendimento".
- **CA30** (botões por status): Dado cada status (R10), Quando abro o detalhe, Então os botões aparecem/somem exatamente como em `syncApptActions` (Agendado→check-in; CheckIn→iniciar+nota+pagar; Atendido→nota+pagar; Faltou→sem pagar).
- **CA31** (concluir → cobrar): Dado um atendimento, Quando marco "Atendido" (`POST .../concluir`), Então abre o `atendSheet` com "Registrar pagamento" e "Concluir sem cobrar"; Quando escolho "Registrar pagamento", Então abre Pagamento já vinculado àquele paciente/cobrança.
- **CA32** (RBAC): Dado um usuário sem permissão de agenda, Quando tento check-in/concluir, Então o backend bloqueia e a UI trata genericamente; o botão de pagamento respeita `financeiro_paciente.registrar`.
- **CA33** (multi-tenant): Dado tenant B, Quando tento check-in num agendamento do A, Então erro genérico, sem vazamento.
- **CA34** (estados/LGPD/tema): loading na ação, erro genérico (reverte UI, sem PII), sucesso (atualiza stats); fiel nos dois temas.

### Bloco 6 — Estoque
- **CA35** (caminho feliz): Dado itens cadastrados, Quando abro Estoque, Então vejo resumo (itens/em baixa/valor) e a lista de `GET /api/inventario/itens`.
- **CA36** (busca + baixa): Dado a lista, Quando digito na busca, Então filtra com debounce ~300ms; Quando escolho "Em baixa", Então a lista usa `apenasAbaixoMinimo=true` e mostra só itens com `EstoqueAbaixoMinimo`.
- **CA37** (ajuste de quantidade): Dado um item, Quando ajusto no stepper e toco "Salvar quantidade", Então `POST /api/inventario/movimentacoes` é chamado com o delta correto (tipo Ajuste/Entrada/Saída conforme D7) e a quantidade do item atualiza.
- **CA38** (repor): Dado um item, Quando toco "Repor", Então abre o fluxo de movimentação tipo Entrada e a quantidade aumenta.
- **CA39** (RBAC ver vs ajustar): Dado um usuário com `estoque` mas papel Profissional (não Dono/Recepcionista), Quando abro Estoque, Então vejo a lista mas os botões de salvar/repor (movimentação CRUD restrita) degradam conforme a regra do backend; Dado sem `estoque`, Então o atalho em Mais não aparece.
- **CA40** (multi-tenant): Dado tenant B, Quando abro Estoque, Então vejo só itens do B.
- **CA41** (estados/tema): loading (skeleton), erro, vazio ("Nenhum material"/"Nada em baixa"), offline; fiel nos dois temas; `maisEstoqueSub` mostra "N itens · M em baixa".

### Bloco 7 — Fotos clínicas
- **CA42** (caminho feliz captura): Dado a ficha/prontuário de um paciente, Quando capturo uma foto, informo região e marcador (Depois) e confirmo, Então a imagem sobe via `POST /api/paciente/{pacienteId}/prontuario/anexos` (multipart) e aparece na grade com região + tag.
- **CA43** (comparar): Dado 2+ fotos da mesma região, Quando abro o visualizador/Comparar, Então vejo o par Antes/Depois agrupado por região (ordenado por data), como no design.
- **CA44** (fallback metadado): Dado que o backend ainda não tem campos de região/marcador, Quando subo a foto, Então região+marcador são preservados no `NomeOriginal` e reidratados na listagem (R13); a UI permanece correta.
- **CA45** (RBAC): Dado um usuário Recepcionista (sem papel Profissional/Dono), Quando abro a ficha, Então o atalho de Fotos clínicas não aparece; se forçar a chamada, o backend retorna 403 tratado genericamente.
- **CA46** (LGPD/audit): Dado o upload/visualização de foto, Quando ocorre, Então o acesso é auditado no backend e nenhuma PII/URL assinada vaza em log; mensagem de erro genérica.
- **CA47** (multi-tenant): Dado tenant B, Quando tento ver fotos de paciente do A, Então erro genérico (não encontrado).
- **CA48** (estados/tema/offline): loading (grade skeleton), erro upload (toast genérico), vazio ("Nenhuma foto clínica"), offline (bloqueia captura/upload com aviso); fiel nos dois temas.

### Bloco 8 — Automação
- **CA49** (caminho feliz): Dado a config de automação, Quando abro Automação a partir de Mais, Então vejo os toggles com o estado real da `ConfiguracaoAutomacaoDto` (lembrete 24h refletindo `LembretesHabilitados`, canal WhatsApp refletindo `LembretesWhatsappHabilitados`).
- **CA50** (persistência): Dado `automacao_config`, Quando ligo/desligo o toggle de lembrete 24h, Então `PUT /api/automacoes/configuracao` persiste e um toast confirma; ao reabrir, o estado persiste.
- **CA51** ("em breve"): Dado os toggles sem contraparte no backend (confirmação, lembrete no dia, aniversário, recall, pós-consulta), Quando os vejo, Então estão marcados como "em breve" (desabilitados, sem persistir) — fiéis ao design mas honestos.
- **CA52** (RBAC): Dado um usuário sem `automacao_config`, Quando abro Automação, Então os toggles ficam read-only/ocultos (degrada) e nenhuma escrita é permitida.
- **CA53** (multi-tenant/estados/tema): config reflete só o tenant ativo; loading/erro/sucesso/offline tratados; fiel nos dois temas.

### Bloco 9 — Push (novos toggles)
- **CA54** (caminho feliz): Dado a tela Mais, Quando vejo as preferências de push, Então existem os toggles caixa/estoque/fotos/pagamento/automação/avisos, fiéis ao design.
- **CA55** (persistência local): Dado um toggle, Quando alterno, Então a preferência persiste localmente (`@capacitor/preferences`) e sobrevive a reiniciar o app (D8).
- **CA56** (efeito): Dado um toggle de categoria desligado, Quando chega um push/banner daquela categoria em foreground, Então o app respeita a preferência (não exibe o banner daquela categoria).
- **CA57** (tema/toque): fiel nos dois temas; alvos ≥ 44px.

### Transversais (todos os blocos)
- **CA58** (sem regressão): Dado o app já existente (Agenda, Pacientes, Prontuário, Receita/Atestado/Exame, Orçamento, Login/biometria, Seletor), Quando rodo a suíte e navego, Então nenhuma tela existente quebra após a mudança de navegação/header.
- **CA59** (build/typecheck): Dado o código novo, Quando rodo `npm run typecheck` e `npm run build` no `mobile/`, Então passam sem erros.
- **CA60** (reuso DS): Dado componentes novos, Quando reviso, Então sheets usam `BottomSheet`, vazios usam `AppEmptyState`, busca usa `AppSearchInput`/`useDebouncedRef`, status usa `AppStatusPill`, avatar usa `AppAvatar` — sem reinventar.

## 8. Riscos e dependências

- **DEPENDÊNCIA DE BACKEND — Fotos clínicas (bloco 7).** O `ProntuarioAnexo` (controller + `AnexoDto` + `AdicionarAnexoCommand`) **não tem** campos de **região anatômica** nem **marcador (Antes/Depois/Evolução)**, e não há filtro "somente imagens". O design depende desses metadados para a grade e a comparação antes/depois. **Ação:** a trilha backend/`imedto-database` deve adicionar `regiao_anatomica` + `marcador` ao anexo (colunas + DTO + command form fields) e, idealmente, um filtro de imagens. **Mitigação no mobile (R13):** fallback codificando os metadados no `NomeOriginal` até o backend entregar, com troca cirúrgica depois. **Não bloqueia** os blocos 1–6, 8, 9.
- **REGRESSIVO — Navegação (bloco 1).** Tirar Avisos da bottom bar e mover para o sino mexe em `BottomTabBar.vue`, `TabsLayout.vue` e no `router/index.ts` (redirect de `/` e a rota `avisos`). Risco de quebrar deep-links/push que apontam para Avisos e o guard de tab. Vigiar: push banner que abre Avisos, guard `requiresTenant`/`perm`, e o estado "tab ativa".
- **Pagamento exige cobrança (R5).** O backend não cria pagamento solto; depende de uma cobrança (nasce no check-in). Fluxo a partir do FAB ("Registrar pagamento" sem agendamento) precisa resolver a cobrança — pode não existir. Mitigação: orientar check-in primeiro; o FAB de pagamento é melhor a partir do agendamento/atendimento concluído. Vigiar UX de "paciente avulso".
- **Automação parcial (R15/D5).** Vários toggles do design não têm contraparte no backend. Risco de o usuário achar que ligou algo que não existe. Mitigação: marcar "em breve" honestamente. Decisão registrada em D5.
- **Push por categoria (D8).** Não há backend de preferências de push por categoria; preferências são locais ao device. Risco: expectativa de que o servidor respeite a preferência (ex.: não enviar push de estoque). Mitigação: documentado como local-only no MVP.
- **Caixa abrir/fechar fora do MVP (D3).** O design só visualiza o caixa; o backend tem abrir/fechar/reabrir. Se o caixa estiver "Fechado", a tela ainda exibe o resumo (read-only). Vigiar mensagem quando fechado.
- **Performance.** Listas (estoque, movimentações, extrato) usam paginação do backend; busca com debounce. Dashboard é 1 request agregado. Sem N+1 no front.
- **DI/runtime.** Como sempre, validar local com o app rodando (não só suíte): Dapper/SQL, lifetime de DI, CSS de runtime e tema só aparecem com o app de pé (regra do projeto).

## 9. Observações para execução

**Suposições tomadas (modo autônomo — sem perguntas; o design é a spec):**
- **D1 — Tab inicial = Início.** O design abre em `view-inicio`. `/` redireciona para `/inicio`.
- **D2 — Pagamento ancorado na cobrança do agendamento.** Origem primária do Pagamento é o agendamento (check-in/atendimento). Via FAB, exige seleção de paciente e resolução da cobrança; sem cobrança, orienta check-in.
- **D3 — Caixa = visualização + novo recebimento.** Abrir/fechar/reabrir caixa fica fora do MVP mobile (existe no web). Se "Fechado", exibe read-only. Compartilhar = resumo textual via `useShare`, sem PII desnecessária.
- **D4 — "Gerar link de pagamento" = "em breve".** Não há backend de link/checkout online. O botão fica presente (fidelidade), desabilitado/explicado.
- **D5 — Automação: persistir só o que o backend suporta.** Lembrete 24h ↔ `LembretesHabilitados`; canal WhatsApp ↔ `LembretesWhatsappHabilitados`, via `PUT /api/automacoes/configuracao`. Demais toggles = "em breve". Edição exige `automacao_config`.
- **D6 — Mapeamento PIX/Cartão/Dinheiro → formas reais.** Os 3 botões do design mapeiam para as `FormaPagamentoDto` ativas por nome; "Cartão" habilita Parcelas. Se houver mais formas ativas, exibir as adicionais seguindo o mesmo padrão visual.
- **D7 — Ajuste de estoque via movimentação delta.** "Salvar quantidade" calcula `nova - atual`: delta > 0 → Entrada; delta < 0 → Saída; usar `Ajuste` quando a intenção for correção de contagem. "Repor" = Entrada. `CustoUnitario` da movimentação usa o `CustoMedio`/`CustoUnitario` atual do item.
- **D8 — Push por categoria = preferências locais.** Persistidas via `@capacitor/preferences`, filtram banners/roteamento no app. Sem endpoint de servidor no MVP.
- **D9 — Bell badge reusa `notificacoesStore`.** Nada novo no backend; só o sino no header consumindo `naoLidas`.

**Não-negociáveis:**
- Multi-tenant (header `X-Estabelecimento-Id`), RBAC com degradação (G2), LGPD (PII fora de log, mensagens genéricas, audit no back), G1 (assinatura), G3 (offline) — em **todos** os blocos.
- Reuso do design system mobile e dos services/stores existentes (`prontuario.service.uploadAnexo`, `useCamera`, `useShare`, `notificacoesStore`, `permissoesStore`, `lib/http.ts`). Antes de criar service/store/componente novo, `grep` por equivalente.
- Fidelidade ao protótipo `Imedto Mobile.html` (layout, ícones, textos, ordem). Tema claro/escuro via tokens — **sem** `font-size`/`font-weight` literais (CLAUDE.md §5).
- Validação local com o app rodando antes de qualquer push (regra do projeto): testar cada CA com o app de pé, inclusive fluxos de escrita conferindo o efeito real no backend.

**Liberdade técnica:**
- Organização interna de novos services (`dashboard.service.ts`, `financeiro.service.ts`, `cobranca.service.ts`, `inventario.service.ts`, `automacao.service.ts`) e stores espelhando o padrão existente.
- Como derivar os stats do Início (combinação de `GET /api/dashboard` + agenda do dia) fica a critério do dev, desde que os números batam com o design e respeitem RBAC.

### Mapa tela → endpoint(s) real(is) (método + rota)

| Tela / ação | Endpoint(s) | RBAC |
|---|---|---|
| Início (painel) | `GET /api/dashboard` (DashboardDto) | `agenda.ver` (controller) |
| Início — card recebido hoje | `GET /api/financeiro/caixa?data=hoje`; `GET /api/financeiro/kpis?dataInicio&dataFim` | `financeiro.ver` |
| Caixa | `GET /api/financeiro/caixa?data=hoje`; lista via `GET /api/financeiro/extrato?dataInicio=hoje&dataFim=hoje` | `financeiro.ver` |
| Caixa — compartilhar | `useShare` (sem rede) | `financeiro.ver` |
| Pagamento — valor sugerido | `GET /api/cobrancas/valor-sugerido?profissionalUsuarioId=` | autenticado/tenant |
| Pagamento — formas | `GET /api/financeiro/formas-pagamento?ativas=true` | `financeiro.ver` (controller financeiro) |
| Pagamento — cobrança do agendamento | `GET /api/cobrancas/por-agendamento/{agendamentoId}` | `financeiro_paciente.ver` |
| Pagamento — registrar | `POST /api/cobrancas/{cobrancaId}/pagamentos` (RegistrarPagamentosDto) | `financeiro_paciente.registrar` |
| Check-in | `POST /api/agendamentos/{id}/checkin` (RegistrarCheckInDto) | `agenda` |
| Concluir atendimento | `POST /api/agendamentos/{id}/concluir` | `agenda` |
| Detalhe agendamento (status/cobrança) | `GET /api/agendamentos/{id}` (AgendamentoDto) | `agenda` |
| Estoque — lista | `GET /api/inventario/itens?apenasAbaixoMinimo=&pagina=&tamanho=` | `estoque` |
| Estoque — movimentação (ajuste/repor) | `POST /api/inventario/movimentacoes` (RegistrarMovimentacaoDto) | `estoque` + escrita (Dono/Recepcionista no CRUD de item) |
| Estoque — cadastros (opções) | `GET /api/inventario/cadastros/{categorias\|fabricantes\|fornecedores\|locais}/opcoes` | `estoque` |
| Automação — config | `GET /api/automacoes/configuracao`; `PUT /api/automacoes/configuracao` | leitura: membro; escrita: `automacao_config` |
| Automação — regras/eventos (se usados) | `GET /api/automacoes/regras`; `GET /api/automacoes/eventos` | leitura: membro; escrita regras: `automacao_config` |
| Fotos — listar | `GET /api/paciente/{pacienteId}/prontuario/anexos[?evolucaoId=]` | papel `Profissional`/`Dono` |
| Fotos — upload | `POST /api/paciente/{pacienteId}/prontuario/anexos` (multipart) | papel `Profissional`/`Dono` |
| Fotos — URL assinada | `GET /api/paciente/{pacienteId}/prontuario/anexos/{anexoId}/url` | papel `Profissional`/`Dono` |
| Sino (avisos) badge | `GET /api/notificacoes/contador-nao-lidas` (já consumido pelo store) | autenticado/tenant |

## 10. Atualização de documentação

- `mobile/README.md` — atualizar a seção "Telas implementadas" e a lista de `views/`/`services/`/`stores/` para incluir os 9 blocos novos (Início/Dashboard, Caixa, Pagamento, Check-in/Atendimento, Estoque, Fotos clínicas, Automação, novos toggles de push) e a **nova navegação** (tab Início, Avisos no sino). Atualizar a estrutura para refletir novos services (`dashboard`, `financeiro`, `cobranca`, `inventario`, `automacao`).
- `Docs/DESIGN.md` — se forem criados componentes mobile reutilizáveis novos para o design system mobile (ex.: card de stat, card de caixa, tile de foto, stepper de quantidade), registrá-los na seção de componentes; caso reaproveite os existentes, registrar a nota "navegação mobile: Início + Avisos-no-sino" como padrão de chrome do app.
- `Docs/LGPD.md` — adicionar nota sobre **Fotos clínicas no mobile** como dado sensível (acesso auditado, URL assinada temporária, sem PII em log, restrito a Profissional/Dono) e sobre a aba financeira/pagamento do paciente acessada pelo mobile (audit já existente).
- `Docs/ARQUITETURA.md` — quando a dependência de backend do bloco 7 (região/marcador no anexo) for implementada pela trilha backend, registrar a extensão do `AnexoDto`/`AdicionarAnexoCommand`. **Neste briefing mobile não há mudança de arquitetura backend** — registrar apenas se/quando a dependência for entregue.

> Observação para a pipeline: este é um épico grande. O `imedto-developer` pode entregar por blocos (1→9), mas o **bloco 1 (navegação) é pré-requisito** dos demais (header/tab). O **bloco 7 (fotos)** entra com fallback e depende da trilha backend para a versão final dos metadados.
