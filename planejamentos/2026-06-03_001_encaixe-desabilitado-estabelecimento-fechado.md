# Desabilitar "Novo encaixe" quando o estabelecimento está fechado no momento atual

**ID**: 2026-06-03_001
**Status**: Aprovado por usuário em 2026-06-03
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: agenda/atendimentos (criação de encaixe) · permissionamento (apenas leitura, sem mudança) · nenhuma no backend (regra já existe)

## 1. Contexto e motivação

Na tela "Meus atendimentos" (worklist do profissional, rota `/minhas-consultas`), o botão **"Novo encaixe"** fica **sempre habilitado**, mesmo em dias/horários em que o estabelecimento não atende (domingo, feriado, fora do expediente, intervalo de almoço).

O backend **já rejeita** corretamente esses encaixes: `CriarAgendamentoCommandHandler.Handle` chama `ValidarRegrasFuncionamento` → `Estabelecimento.ValidarPodeAgendar`, que valida passado, dia da semana de funcionamento, datas bloqueadas, faixa de expediente e horários bloqueados. `TipoServico="Encaixe"` é só um rótulo — **não há bypass**. O encaixe em dia/horário fechado retorna **422** com mensagem de negócio.

A lacuna é puramente de **UX**: hoje o profissional só descobre que não pode criar o encaixe **depois** de clicar no botão, abrir o modal, escolher o paciente e tentar salvar — quando aparece o toast de erro (`erroEncaixe`). Isso é atrito operacional: a pessoa investe cliques e atenção para receber um "não" tardio.

A demanda é tornar a indisponibilidade **visível antes da ação**: desabilitar o botão e explicar o porquê via tooltip.

## 2. Persona-alvo

**Profissional de saúde** operando o seu dia na tela "Meus atendimentos". Momento da jornada: atendimento/fila do dia. Frequência: o encaixe é uma ação pontual usada quando chega um paciente sem agendamento prévio — o profissional precisa que o sistema deixe claro, de imediato, se aquele momento permite ou não encaixar.

## 3. Escopo

**Inclui**:
- Checagem preventiva, ao abrir/atualizar a tela "Meus atendimentos", de se o estabelecimento do tenant ativo está **funcionando no momento atual** (hoje + horário corrente).
- Desabilitar o botão "Novo encaixe" quando o estabelecimento **não** está apto a receber um encaixe agora, com **tooltip explicativo**.
- Manter o botão habilitado e o fluxo de criação **inalterado** quando o estabelecimento está apto.
- Reuso do endpoint/serviço de disponibilidade já existente (`agendaService.consultarDisponibilidade`), sem novo endpoint nem nova regra de domínio.

**Não inclui**:
- Alterar o comportamento do encaixe: continua sendo criado para **AGORA** (`now + 60s`, duração 30min, `TipoServico="Encaixe"`). A data selecionada no filtro do topo **não** influencia a criação nem a checagem do botão.
- Qualquer mudança na regra de negócio do backend (`ValidarPodeAgendar` permanece intocada — ela já cobre todos os casos).
- Esconder o botão (decisão fechada: desabilitar + tooltip, nunca ocultar).
- Validar disponibilidade de **sala** ou **conflito de agenda do profissional** para o botão (fora de escopo; o save continua tratando esses casos via 422).
- Bloquear/alterar agendamentos comuns (Novo agendamento / Editar) — esses já têm tratamento próprio nos respectivos modais.

## 4. Regras de negócio

- **R1 — Fonte da verdade é o backend.** A criação do encaixe continua passando por `ValidarPodeAgendar` e retornando **422** quando o estabelecimento está fechado. A checagem do front é **apenas UX preventiva** (defesa em profundidade), nunca substitui a validação do backend. Mora em: Domain (`Estabelecimento.ValidarPodeAgendar`) + Handler (`CriarAgendamentoCommandHandler`). Validada em: back (já existe) + front (novo, espelho).

- **R2 — "Apto a encaixar agora" = o backend aceitaria um encaixe começando agora.** Para coerência total entre o que o botão permite e o que o backend aceita, o botão fica **desabilitado** sempre que o backend rejeitaria um encaixe começando no momento atual. Os motivos cobertos são exatamente os de `ValidarPodeAgendar`:
  1. **Dia não-funcional** — hoje não está em `DiasSemanaFuncionamento`.
  2. **Data bloqueada** — hoje consta em `DatasBloqueadas` (feriado/exceção).
  3. **Fora do expediente** — horário atual antes de `HorarioInicio` ou em/após `HorarioFim`.
  4. **Dentro de intervalo bloqueado** — horário atual cai em um `HorariosBloqueados` (ex: almoço).
  Mora em: Domain (espelhada). Validada em: front (preventiva) + back (autoritativa).

- **R3 — Mecanismo de checagem do front = reuso de `consultarDisponibilidade(profissional, hoje, hoje)`.** Esse endpoint já encapsula, server-side e multi-tenant, todas as regras de R2 (ver abaixo "abordagem técnica"). O front **não** reimplementa a lógica de horário; apenas interpreta o resultado. Mora em: Query existente (`ConsultarDisponibilidadeQueryHandlers`) + Front (interpretação). Validada em: back (cálculo) + front (interpretação).

- **R4 — Tooltip explicativo, mensagem única e clara.** Quando desabilitado, o botão exibe tooltip. Mensagem padrão suficiente: **"O estabelecimento não está em funcionamento neste momento — não é possível criar encaixe agora."** Variação opcional permitida (não obrigatória): "Estabelecimento fechado neste dia" para casos de dia não-funcional/data bloqueada vs. "Fora do horário de atendimento" para expediente/intervalo. Não complicar: uma mensagem clara basta. Mora em: Front. Sem PII.

- **R5 — Multi-tenant.** A disponibilidade consultada é **sempre** a do estabelecimento ativo do tenant (`tenant.estabelecimentoAtivoId`); o backend filtra por `estabelecimento_id` derivado do claim. Nunca usar config de outro estabelecimento. Mora em: Query (filtro server-side, já existente). Validada em: back.

- **R6 — Falha-fechada na checagem, mas não-bloqueante para o caminho feliz.** Se a consulta de disponibilidade **falhar** (erro de rede/back), o botão **permanece habilitado** (não punir o profissional por uma falha de leitura) — a rede de segurança continua sendo o 422 no save, com o tratamento de erro atual (`erroEncaixe`) preservado. Justificativa: a checagem é melhoria de UX; degradar para o comportamento atual em caso de falha é aceitável e seguro, pois o backend continua barrando. Mora em: Front.

## 5. Modelo de dados

**Sem alteração de schema.** Nenhuma tabela, coluna ou índice novo. Nenhuma migration.

A feature consome dados já existentes via o endpoint `GET /agendamentos/disponibilidade` (config de funcionamento do estabelecimento + agendamentos do dia), sem persistir nada novo.

LGPD: o fluxo não introduz novo dado pessoal. A resposta de disponibilidade pode conter `pacienteNome` em slots ocupados, mas **essa feature não usa nem exibe** esse campo — só lê `status` e a aptidão do momento atual. Sem audit novo (não é acesso a prontuário; é leitura operacional de agenda, já coberta pelo padrão existente do endpoint).

## 6. UX e fluxo

Tela: `frontend/src/views/atendimentos/MeusAtendimentosView.vue`. Botão alvo: `AppButton variant="danger"` "Novo encaixe" (linha ~308).

**Fluxo**:
1. Ao montar a tela (e ao tornar-se relevante), o front consulta `agendaService.consultarDisponibilidade(profissionalId, hoje, hoje)` uma única vez para o dia de hoje.
2. Calcula `podeEncaixarAgora: boolean` a partir do resultado (ver abordagem técnica).
3. Enquanto a checagem carrega: botão pode permanecer habilitado (estado otimista) **ou** em estado de loading curto — preferir não bloquear; a checagem é rápida. Não exibir spinner intrusivo no header.
4. `podeEncaixarAgora === false` → botão **desabilitado** (`:disabled`) + atributo `title`/tooltip com a mensagem de R4. Visual: estado desabilitado padrão do `AppButton` (opacidade reduzida, cursor `not-allowed`), sem cor de erro adicional.
5. `podeEncaixarAgora === true` → botão habilitado, abre `EncaixeModal`, fluxo atual inalterado.

**Recheck temporal (importante)**: o momento atual avança. Um profissional pode abrir a tela às 11:59 (apto) e tentar encaixar às 12:01 (almoço). Para evitar "botão habilitado mas save falha":
- A checagem deve ser **reavaliada** quando o usuário clicar "Hoje" / navegar e voltar para hoje, e **revalidada ao reabrir/refrescar a tela**.
- **Não** é obrigatório um timer de polling contínuo (evitar consulta desnecessária — ver Performance). O 422 no save é a rede de segurança para o caso de borda exato de virada de minuto/intervalo (R6, edge cases).
- Liberdade técnica do dev: pode-se reavaliar a aptidão de forma leve no clique do botão (sem nova request, recomputando contra os dados já carregados + relógio atual) antes de abrir o modal. Decisão de implementação fica com o dev, desde que os CAs passem.

**Estados**:
- **Loading** (checagem em andamento): botão não-bloqueado preferencialmente; nenhum flicker visual agressivo.
- **Apto**: botão habilitado, sem tooltip de bloqueio.
- **Não apto**: botão desabilitado + tooltip.
- **Erro na checagem**: botão habilitado (R6) — degrada para o comportamento atual.

**Mobile-ready**: tooltip via `title` funciona em desktop; em mobile (sem hover) o estado desabilitado já comunica a indisponibilidade. Aceitável manter `title` como reforço.

**Design system**: reusar `AppButton` (já em uso) e seu suporte a `:disabled` + `title`. Não criar componente novo. Se `AppButton` ainda não aceitar `title`/tooltip nativo, o dev pode envolver em um `<span :title>` ou passar `title` — escolha o menor caminho dentro do padrão existente; isso **não** justifica novo componente de design system.

## 7. Critérios de aceite (testáveis)

- **CA1** (caminho feliz — dia e horário funcionais): Dado um estabelecimento que funciona hoje e o horário atual está dentro do expediente e fora de intervalos bloqueados, Quando abro "Meus atendimentos", Então o botão "Novo encaixe" está **habilitado** e, ao clicar, o `EncaixeModal` abre e a criação de encaixe ocorre normalmente como hoje.

- **CA2** (dia não-funcional): Dado que hoje é um dia da semana **não** presente em `DiasSemanaFuncionamento` (ex: domingo num estabelecimento seg–sex), Quando abro "Meus atendimentos", Então o botão "Novo encaixe" está **desabilitado** com tooltip explicativo (R4) e o clique não abre o modal.

- **CA3** (data bloqueada / feriado): Dado que hoje consta em `DatasBloqueadas`, Quando abro "Meus atendimentos", Então o botão "Novo encaixe" está **desabilitado** com tooltip.

- **CA4** (fora do expediente em dia útil): Dado um dia funcional, mas o horário atual está **antes** de `HorarioInicio` ou em/**após** `HorarioFim`, Quando abro "Meus atendimentos", Então o botão "Novo encaixe" está **desabilitado** com tooltip.

- **CA5** (dentro de intervalo bloqueado): Dado um dia/horário dentro do expediente, mas o momento atual cai em um `HorariosBloqueados` (ex: almoço 12:00–13:00), Quando abro "Meus atendimentos", Então o botão "Novo encaixe" está **desabilitado** com tooltip.

- **CA6** (defesa em profundidade — backend continua autoritativo): Dado um estabelecimento fechado agora, Quando uma chamada `POST /agendamentos` com `TipoServico="Encaixe"` para o momento atual é feita **burlando** o front (ex: via API direta ou botão forçado a habilitado), Então o backend retorna **422** com mensagem de negócio e **nenhum** encaixe é criado.

- **CA7** (multi-tenant): Dado um usuário com vínculo nos estabelecimentos A e B, com A ativo no tenant, Quando a checagem de disponibilidade roda, Então ela consulta **somente** a disponibilidade de A (filtro por `estabelecimento_id` do claim) e a aptidão do botão reflete A, nunca B; tentativa de consultar disponibilidade de B retorna vazio/404 genérico sem vazar dados de B.

- **CA8** (RBAC — sem regressão): Dado um usuário **sem** a permissão `agenda.ver`, Quando tenta acessar `/minhas-consultas`, Então o acesso à rota é negado pelo guard existente (a tela e o botão não são alcançáveis). A feature **não** adiciona nem remove permissões; apenas herda o gate atual da rota `MinhasConsultas`.

- **CA9** (estado de erro na checagem — falha-fechada não-bloqueante): Dado que a consulta de disponibilidade **falha** (erro 500/timeout), Quando abro "Meus atendimentos", Então o botão "Novo encaixe" **permanece habilitado** (R6) e, se o estabelecimento estiver de fato fechado, o erro 422 no save continua sendo exibido com a mensagem atual.

- **CA10** (LGPD — sem PII): Dado qualquer estado de bloqueio do botão, Quando o tooltip é exibido e/ou um erro ocorre, Então a mensagem é **genérica** (sobre funcionamento do estabelecimento) e **não** contém nome de paciente, CPF, nem qualquer PII; nenhum log inclui PII.

- **CA11** (performance — sem consulta desnecessária): Dado o carregamento da tela, Quando ela monta, Então a checagem de disponibilidade dispara **no máximo uma** request para `hoje..hoje` (não por dia navegado, não em loop). Navegar para outros dias no filtro **não** dispara nova checagem de aptidão do botão (o encaixe é sempre "agora"). Re-checar só ocorre ao voltar/atualizar para hoje, conforme R6/UX.

- **CA12** (coerência temporal de borda): Dado que o botão estava habilitado e o momento atual cruzou para fora do expediente/para um intervalo entre o carregamento e o clique, Quando o save é tentado, Então o backend retorna 422 e o tratamento de erro atual (`erroEncaixe` no modal) exibe a mensagem — o usuário não fica num estado quebrado. (Rede de segurança; não exige polling.)

## 8. Riscos e dependências

- **Risco — alinhamento de slot vs. "agora".** O endpoint de disponibilidade trabalha com **slots fixos** (gerados a partir de `HorarioInicio`/duração), enquanto o encaixe começa em um instante arbitrário (`now+60s`). Mitigação na abordagem técnica (§9): para a decisão dia/expediente/intervalo, o que importa é o **status do dia** e se o **horário atual** cai dentro do expediente e fora de bloqueios — informação derivável de `status` + faixa de funcionamento, não do alinhamento exato a um slot. Não usar "existe slot livre exatamente agora" como critério (slot pode estar `agendado`/`passado` sem que o estabelecimento esteja fechado).
- **Risco — relógio do cliente vs. BrasiliaTime do servidor.** O backend usa `BrasiliaTime.Now`; o front usa relógio local. O `+60s` já existente no `criarEncaixe` é a mitigação para o caso de borda no save. Para a checagem do botão, pequenas divergências de relógio são absorvidas por R6/CA12 (backend é autoritativo).
- **Edge case — estabelecimento sem configuração de funcionamento.** O backend aplica defaults seguros (08:00–18:00, seg–sex, duração 30, no `ConsultarDisponibilidadeQueryHandlers`). A checagem do front deve refletir esse mesmo resultado (ele já vem calculado do back) — não recriar defaults no front.
- **Dependência**: endpoint `GET /agendamentos/disponibilidade` e `agendaService.consultarDisponibilidade` (já existentes; sem alteração). `tenant.estabelecimentoAtivoId` (já disponível na tela).
- **Sem risco de regressão de schema/migration** (nenhuma mudança de banco).

## 9. Observações para execução

**Abordagem técnica escolhida (mecanismo de checagem) — não-negociável no objetivo, com liberdade no detalhe:**

Reusar `agendaService.consultarDisponibilidade(auth.usuario.id, hoje, hoje)` e derivar `podeEncaixarAgora` do `DisponibilidadeDia` de hoje. O endpoint **já cobre todas as 4 regras de R2** server-side e multi-tenant — é a opção mais simples e reutilizável, sem novo endpoint nem nova exposição de dados:

- `status === "fechado"` → dia não-funcional **ou** data bloqueada → **não apto** (cobre CA2 e CA3).
- Para expediente/intervalo (CA4, CA5): o `DisponibilidadeDia` traz `slots[]` com `disponivel` e `motivo`. A leitura recomendada para "agora":
  - Se `status === "fechado"` → não apto.
  - Caso contrário, determinar se o **horário atual** está dentro do expediente e fora de bloqueio. Como os `slots` já marcam `motivo: "bloqueado"` (intervalo) e `motivo: "passado"` (horário decorrido), e o `status` global do dia é `"indisponivel"` quando **nenhum** slot está disponível, o dev tem duas vias igualmente aceitáveis — **escolha a mais simples que faça os CAs passarem**:
    - **(a) Derivar do próprio resultado de disponibilidade** o slot que contém o horário atual e checar seu `motivo`/`disponivel` (cuidando do alinhamento: o horário atual entre slots; usar o slot vigente). Atenção: um slot `agendado` **não** significa estabelecimento fechado — para o botão de encaixe, `agendado` **não** deve desabilitar (encaixe sobrepõe agenda por design); apenas `fechado`/fora-de-expediente/`bloqueado` desabilitam.
    - **(b) Buscar a config de funcionamento do estabelecimento** (`estabelecimentoService` já expõe `horarioInicio`, `horarioFim`, `diasSemanaFuncionamento`, `horariosBloqueados`, `datasBloqueadas` em `listarMeus`/bootstrap) e comparar o relógio atual contra ela no front. Isso evita ambiguidade de slot, mas **duplica** parte da lógica de horário no front — só aceitável se o resultado for mais simples e claro, e **sempre** mantendo o backend como autoritativo (R1).
  - **Recomendação do BA**: começar por **(a)** restrito ao nível que os CAs exigem; se o alinhamento de slot ao "agora" ficar confuso, cair para (b) usando os campos de funcionamento já disponíveis. **Não** introduzir terceira fonte de verdade nem novo endpoint. **`motivo: "agendado"` jamais desabilita o botão de encaixe.**
- **Multi-tenant**: a request usa o tenant ativo implicitamente (claim); não passar `estabelecimentoId` de fonte não confiável.
- **Performance**: uma request `hoje..hoje` no mount; sem polling; sem disparo ao navegar para outros dias (o botão é sempre sobre "agora"). Reusar dados já carregados se possível.
- **Preservar** integralmente o tratamento de erro atual de `criarEncaixe` (`erroEncaixe` + toast) como rede de segurança (CA9, CA12).
- **Reuso > duplicação**: antes de criar helper de "horário dentro do expediente", `grep` por lógica equivalente (o cálculo já vive em `ConsultarDisponibilidadeQueryHandlers` e em `Estabelecimento.ValidarPodeAgendar`). Não criar serviço/composable novo se um `computed` na própria view resolve.

**Não-negociável**: backend permanece a fonte da verdade (R1); o botão desabilitado é UX; multi-tenant e ausência de PII nas mensagens.

**Liberdade técnica**: escolha entre via (a)/(b) acima; forma de exibir o tooltip dentro do padrão `AppButton`; se recomputa aptidão no clique sem nova request.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar **nota curta** (não reescrever seção) na regra de funcionamento/agenda registrando que: a aptidão de criar encaixe "agora" na tela "Meus atendimentos" é uma **checagem preventiva de UX** que reusa `consultarDisponibilidade`, e que **`Estabelecimento.ValidarPodeAgendar` permanece a fonte da verdade** (front é espelho, backend retorna 422). Objetivo: futuros agentes não duplicarem a regra nem assumirem que o front decide. Mudança incremental, cirúrgica.
- **Demais docs**: nenhum. `INFRA.md` (sem mudança de infra), `COMANDOS.md` (sem novo comando/migration), `LGPD.md` (sem novo dado pessoal/audit/endpoint), `DESIGN.md` (sem componente novo — reuso de `AppButton`).
