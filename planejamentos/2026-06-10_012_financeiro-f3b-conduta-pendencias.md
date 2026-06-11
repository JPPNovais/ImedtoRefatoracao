# Financeiro F3B — Conduta como checklist → pendências do atendimento

**ID**: 2026-06-10_012
**Status**: Aprovado por usuário em 2026-06-10 (execução autônoma autorizada — decisões fechadas no plano mestre, sem nova rodada de perguntas)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (seção Conduta + save de evolução), página do paciente, permissionamento (leitura), audit/LGPD

> **Refere-se ao plano mestre**: [`Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md`](../Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md) §3.5, §2.1 (`PendenciaAtendimento`), §F3B, §5 q6 [RESOLVIDA].
> **Continuação de**: F1 (`2026-06-10_009`), F2 (`2026-06-10_010`), F3 (`2026-06-10_011`). Numeração de CAs continua em **CA59**.

---

## 1. Contexto e motivação

A seção **Conduta** da evolução do prontuário é hoje texto livre — `key="conduta"`, `tipo: "texto_longo"` em [`modeloProntuarioBuilder.ts`](../frontend/src/components/ui/modeloProntuarioBuilder.ts), renderizada como textarea genérica por [`SecaoProntuario.vue`](../frontend/src/components/prontuario/SecaoProntuario.vue) dentro de `tabs/ConsultaAtualTab.vue`. O profissional escreve "encaminho para receita, peço retorno em 30 dias" e nada acontece: ninguém é lembrado, nada é rastreado, e o financeiro não tem ponto de partida operacional.

A F3B transforma a Conduta em **checklist de ações fixas do sistema** que, ao salvar a evolução, viram **pendências do atendimento** daquele paciente. É a **ponte operacional para o financeiro**: marcar "procedimento realizado" será o gatilho da cobrança de procedimento (F4) e da baixa de estoque; marcar "criar orçamento" será a entrada da cirurgia (F5). Sem a F3B, F4/F5 não têm de onde partir no prontuário.

A dor concreta: recepção/profissional **esquece** de criar a receita, agendar o retorno, pedir o exame. O painel persistente na página do paciente elimina o esquecimento; a conclusão automática elimina o trabalho manual de "dar baixa" no checklist.

**Evidência de viabilidade (código verificado)**: o handler de salvar evolução ([`RegistrarEvolucaoCommandHandler.cs`](../backend/src/Services/Imedto.Backend.Application/Prontuarios/Commands/RegistrarEvolucaoCommandHandler.cs)) já tem um hook pós-save idêntico ao que precisamos (`PoolExtratorEvolucao.ExtrairECriar` cria itens derivados do `ConteudoJson` de forma falha-suave) e já publica eventos de domínio. Os cinco eventos necessários para a conclusão automática **já existem e carregam `PacienteId` + `EstabelecimentoId`**: `ReceitaEmitidaEvent`, `AtestadoEmitidoEvent`, `PedidoExameEmitidoEvent`, `OrcamentoCriadoEvent`, `AgendamentoCriadoEvent`. O padrão `IEventHandler<TEvent>` com fan-out já é usado em produção. **Isso elimina a necessidade de novos eventos** — apenas novos handlers que ouvem eventos existentes.

## 2. Persona-alvo

- **Profissional** (médico/dentista) — preenche a evolução, marca as ações de conduta, é empurrado pelo modal pós-salvar, executa as ações (cria receita, agenda retorno).
- **Recepção** — vê o painel persistente na página do paciente para garantir que nada do atendimento ficou pendente (criar orçamento, agendar retorno).
- **Frequência**: a cada atendimento que gera evolução. Alta — é o coração do fluxo clínico.

## 3. Escopo

**Inclui**:
- Componente de seção dedicado `SecaoCondutaChecklist.vue` substituindo o render textarea da seção `conduta`: **6 checkboxes fixos** (criar receita · criar atestado · pedir exame · criar orçamento · marcar procedimento realizado · agendar retorno) + **campo de observação livre**.
- Persistência das ações marcadas + observação **no `ConteudoJson` da evolução** (append-only intacto — a Conduta marcada fica registrada na evolução como sempre esteve, agora estruturada).
- Entidade de domínio `PendenciaAtendimento` (backend) + tabela `pendencias_atendimento`.
- **Criação idempotente** das pendências ao salvar a evolução com itens marcados (salvar a mesma evolução 2x não duplica).
- **Modal pós-salvar** ("Próximos passos") com links diretos só dos itens marcados, contador ("0 de N concluídas") e botão "Fazer depois".
- **Painel persistente** de pendências em [`PacienteDetalheView.vue`](../frontend/src/views/pacientes/PacienteDetalheView.vue), visível enquanto houver pendência aberta, somindo quando tudo concluir.
- **Conclusão automática** por gatilho de cada ação, via novos `IEventHandler` que ouvem os eventos existentes (mecanismo detalhado em R7–R12 e §9).
- **Conclusão manual** de uma pendência pelo painel (UX de escape) — botão "Marcar como concluída".
- **Endpoint leve** de leitura de pendências abertas por paciente (alimenta o painel).
- **Retrocompat**: evoluções antigas com `conduta` em texto livre renderizam **read-only** (sem checklist), sem quebrar o prontuário.

**Não inclui (anti-escopo, ver plano mestre §F3B OUT)**:
- A **cobrança** de procedimento em si (F4) e o **orçamento de cirurgia** em si (F5). A F3B apenas cria a pendência e a conclui via gatilho; quem gera cobrança/baixa de estoque/orçamento é F4/F5.
- **Gatilho real de "procedimento realizado"**: NÃO existe ainda na base (a ação "marcar procedimento como realizado" só nasce de verdade na F4). Nesta fase esse item conclui **apenas manualmente** pelo painel, e o gancho de evento fica **preparado** (ver decisão D-6).
- **Lista configurável** de ações — é fixa do sistema nesta versão.
- Notificação interna (sino) por pendência — fora desta versão (o painel persistente já cobre o "não esquecer").

## 4. Regras de negócio

- **R1** — A seção `conduta` passa a renderizar `SecaoCondutaChecklist.vue` (6 ações fixas + observação) quando o template da evolução marcar a seção como checklist; evoluções/templates legados sem essa marcação renderizam o textarea/texto read-only. Mora em: Front (`SecaoProntuario.vue` decide o componente) + catálogo `modeloProntuarioBuilder.ts`. Validada em: front (render) + back (o `ConteudoJson` é persistido como string opaca — sem regra no back sobre o formato).

- **R2** — Ao salvar a evolução, para cada ação marcada cria-se uma `PendenciaAtendimento` (tenant + paciente + evolução + ação + status=Pendente). Mora em: Handler (`RegistrarEvolucaoCommandHandler`, no mesmo ponto do `PoolExtrator`, **falha-suave** — erro ao criar pendência nunca derruba o salvamento da evolução). Validada em: back.

- **R3** (idempotência da criação) — A criação é idempotente pela chave `(evolucao_id, acao)`: salvar/reprocessar a mesma evolução não cria pendência duplicada (UNIQUE no banco + checagem no domínio). Mora em: Domain + constraint de schema. Validada em: back.

- **R4** (minimização LGPD) — `PendenciaAtendimento` **NÃO carrega conteúdo clínico**: guarda apenas o **tipo da ação** (enum), os vínculos (tenant/paciente/evolução/agendamento), status e `referencia_id` do documento que a concluiu. A observação livre e o conteúdo clínico ficam **só no `ConteudoJson` da evolução** — nunca copiados para a pendência. Mora em: Domain (entidade enxuta) + Query (DTO do painel só com tipo+status). Validada em: back + revisão de DTO.

- **R5** (multi-tenant) — Toda query/comando de pendência filtra `estabelecimento_id`. Repositório falha-fechada (sem tenant claim → vazio). Pendência de paciente de outro tenant nunca é lida nem concluída. Mora em: Query + Handler + Repo. Validada em: back.

- **R6** (RBAC) — Quem **vê** o painel e pode **concluir manualmente** uma pendência é quem tem `prontuario.ver` (ver painel) e `prontuario.editar` (concluir manualmente) — as mesmas permissões que já gateiam a evolução. A conclusão automática por gatilho **não exige permissão extra** (o ato de criar a receita/orçamento/etc. já passou pela sua própria RBAC). Mora em: Front (gate do botão) + Back (repo/handler valida tenant; o endpoint de conclusão manual exige contexto autenticado com acesso ao prontuário). Validada em: back + front.

- **R7** (conclusão automática — receita) — Ao publicar `ReceitaEmitidaEvent`, um `IEventHandler` conclui a pendência aberta `(estabelecimento, paciente, acao=CriarReceita)` mais recente, gravando `referencia_id = ReceitaId`, `concluida_em`, `status=Concluida`. Mora em: novo `ConcluirPendenciaAoEmitirReceitaHandler`. Validada em: back.

- **R8** (conclusão automática — atestado) — Idem R7 ouvindo `AtestadoEmitidoEvent` → `acao=CriarAtestado`, `referencia_id=AtestadoId`.

- **R9** (conclusão automática — exame) — Idem ouvindo `PedidoExameEmitidoEvent` → `acao=PedirExame`, `referencia_id=PedidoExameId`.

- **R10** (conclusão automática — orçamento) — Idem ouvindo `OrcamentoCriadoEvent` → `acao=CriarOrcamento`, `referencia_id=OrcamentoId`.

- **R11** (conclusão automática — retorno) — Idem ouvindo `AgendamentoCriadoEvent` com `InicioPrevisto` no **futuro** → `acao=AgendarRetorno`, `referencia_id=AgendamentoId`. (Filtro de futuro evita que o próprio check-in/agendamento corrente conclua o item por engano.)

- **R12** (conclusão automática — idempotência) — Se não há pendência aberta da ação para aquele paciente/tenant, o handler é **no-op** (não cria, não erra). Concluir duas vezes a mesma pendência é no-op (já concluída permanece concluída, com a `referencia_id` da primeira conclusão). Mora em: cada `IEventHandler` (busca "pendência **aberta**" — se já concluída, ignora). Validada em: back.

- **R13** (procedimento realizado — sem gatilho real nesta fase) — `acao=MarcarProcedimentoRealizado` só pode ser concluída **manualmente** pelo painel na F3B. O `IEventHandler` correspondente **não é implementado agora** — fica documentado como gancho a plugar na F4. Marcar a ação na conduta cria a pendência normalmente. Mora em: decisão de escopo (sem handler de gatilho). Validada em: ausência do handler + CA de conclusão manual.

- **R14** (conclusão manual) — O painel oferece "Marcar como concluída" para qualquer pendência aberta; grava `status=Concluida`, `concluida_em`, `referencia_id=null` (concluída sem documento associado). Mora em: Handler `ConcluirPendenciaManualCommand` + Front. Validada em: back + front.

- **R15** (append-only) — A evolução permanece imutável: a pendência é uma **projeção operacional separada**, mutável só no `status`/`concluida_em`/`referencia_id`. Nada na F3B edita uma evolução já salva. Mora em: Domain (`ProntuarioEvolucao` intacto). Validada em: back.

## 5. Modelo de dados

Tabela nova **`pendencias_atendimento`**. Sem alteração em tabelas existentes. Schema detalhado para o `imedto-database` na §11.

- Colunas: `id`, `estabelecimento_id` (tenant, NOT NULL), `paciente_id` (NOT NULL), `evolucao_id` (origem, NOT NULL), `agendamento_id` (nullable), `acao` (enum string, NOT NULL), `status` (enum string, NOT NULL, default `Pendente`), `referencia_id` (bigint nullable — id do doc que concluiu), `concluida_em` (timestamptz nullable), `criado_por_usuario_id` (uuid NOT NULL), `criado_em` (timestamptz NOT NULL), `atualizado_em` (timestamptz nullable).
- **Sem PII e sem conteúdo clínico** (R4) — só tipo da ação e vínculos por id.
- UNIQUE `(evolucao_id, acao)` → idempotência da criação (R3).
- Índice de leitura do painel: `(estabelecimento_id, paciente_id, status)` → endpoint leve por paciente filtrando `status='Pendente'`.
- FKs: `estabelecimento_id`, `paciente_id`, `evolucao_id` com `ON DELETE RESTRICT` (preserva histórico). `agendamento_id`/`referencia_id` sem FK forte (referência fraca multi-origem).

## 6. UX e fluxo

**Referência visual obrigatória**: protótipo `Docs/Roadmap/prototipacao-financeiro/design-handoff/` — `Prontuario.html` → `components/ProntuarioModules.jsx` (`ConductChecklistModule`, `NextStepsModal`, `PendenciesPanel`) + `care-data.js`. Screenshots finais: `02-pront-nextsteps.png` (modal), `03-pront-panel-final.png` (painel), `pront-legacy.png` (conduta legada read-only). Recriar o **resultado visual** com o design system Vue, não copiar o React. Tokens tipográficos (CLAUDE.md §5) vencem o protótipo.

**Seção Conduta (no prontuário)**: lista de 6 checkboxes (rótulos exatos: "Criar receita", "Criar atestado", "Pedir exame", "Criar orçamento", "Marcar procedimento como realizado", "Agendar retorno") + textarea "Observação". Componentes do DS: reutilizar checkbox/label do design system, sem `<input>` cru.

**Modal pós-salvar "Próximos passos"** (abre só se ≥1 ação marcada): título, contador "0 de N concluídas", uma linha por ação marcada com **link direto** para o contexto (Criar receita → aba de receita do paciente; Agendar retorno → agenda no contexto do paciente; etc.), botão "Fazer depois" (fecha sem perder as pendências). **Se nenhuma ação marcada → modal NÃO abre** (salvamento segue normal).

**Painel persistente** em `PacienteDetalheView.vue`: card no topo/lateral da página do paciente listando pendências **abertas** (estado parcial mostra "X de N concluídas"; cada linha tem o link de ação + "Marcar como concluída"). Estados:
- **Vazio/completo** → painel **não renderiza** (some quando tudo concluído).
- **Parcial** → painel visível com as abertas.
- **Loading** → skeleton leve (`AppSkeleton` ou equivalente do DS).
- **Erro** → estado de erro discreto, não bloqueia o resto da página.

**Retrocompat (legado)**: evolução antiga com `conduta` texto livre → render read-only do texto (sem checklist, sem painel novo gerado retroativamente). Ver `pront-legacy.png`.

**Mobile-ready / responsivo** e centralizado via padrões `.app-page` já existentes.

## 7. Critérios de aceite (testáveis)

- **CA59 (caminho feliz — criação)**: Dado um profissional preenchendo a evolução, Quando marca "Criar receita" e "Agendar retorno" na Conduta e salva, Então são criadas 2 `PendenciaAtendimento` (acao=CriarReceita, acao=AgendarRetorno) com status=Pendente, vinculadas ao tenant+paciente+evolução, e a evolução é salva normalmente.

- **CA60 (modal pós-salvar)**: Dado o salvamento com 2 ações marcadas, Quando a resposta retorna, Então o modal "Próximos passos" abre listando exatamente as 2 ações com link correto, contador "0 de 2 concluídas" e botão "Fazer depois".

- **CA61 (modal não abre sem itens)**: Dado o salvamento de uma evolução sem nenhuma ação marcada na Conduta, Quando a resposta retorna, Então o modal "Próximos passos" NÃO abre e nenhuma pendência é criada.

- **CA62 (idempotência da criação)**: Dado uma evolução já salva com pendências criadas, Quando o salvamento/handler é reprocessado para a mesma evolução, Então nenhuma pendência é duplicada (UNIQUE `(evolucao_id, acao)` respeitado; total permanece igual).

- **CA63 (conclusão automática — receita)**: Dado uma pendência aberta acao=CriarReceita do paciente P, Quando uma receita é emitida para P no mesmo tenant (`ReceitaEmitidaEvent`), Então a pendência migra para Concluida com `referencia_id`=ReceitaId e `concluida_em` preenchido.

- **CA64 (conclusão automática — demais gatilhos)**: Dado pendências abertas acao=CriarAtestado / PedirExame / CriarOrcamento / AgendarRetorno, Quando o evento correspondente (`AtestadoEmitidoEvent` / `PedidoExameEmitidoEvent` / `OrcamentoCriadoEvent` / `AgendamentoCriadoEvent` com início futuro) é publicado para o mesmo paciente+tenant, Então cada pendência respectiva é concluída com a `referencia_id` do documento/agendamento.

- **CA65 (conclusão automática — idempotência / no-op)**: Dado nenhuma pendência aberta da ação para o paciente, Quando o evento correspondente é publicado, Então o handler é no-op (não cria pendência, não lança erro). E Dado uma pendência já concluída, Quando outro evento da mesma ação chega, Então ela permanece concluída com a `referencia_id` original (não sobrescreve).

- **CA66 (procedimento realizado — só manual nesta fase)**: Dado uma pendência aberta acao=MarcarProcedimentoRealizado, Quando nenhum gatilho automático existe (F4 ainda não implementada), Então ela só pode ser concluída pelo botão "Marcar como concluída" do painel, gravando status=Concluida e `referencia_id`=null.

- **CA67 (conclusão manual)**: Dado uma pendência aberta qualquer, Quando o usuário com `prontuario.editar` clica "Marcar como concluída" no painel, Então ela vira Concluida (referencia_id=null) e some do painel.

- **CA68 (painel — estados)**: Dado um paciente sem pendências abertas, Quando a página do paciente carrega, Então o painel NÃO renderiza. Dado pendências parciais (1 de 3 concluídas), Então o painel mostra as 2 abertas com contador. Dado todas concluídas, Então o painel some.

- **CA69 (multi-tenant)**: Dado um usuário do estabelecimento B, Quando tenta ler ou concluir uma pendência de um paciente do estabelecimento A, Então recebe lista vazia / "não encontrado" genérico, nada é concluído e nada é logado com PII.

- **CA70 (RBAC)**: Dado um usuário sem `prontuario.editar`, Quando a página do paciente carrega, Então o botão "Marcar como concluída" fica oculto/inativo no front E o endpoint de conclusão manual rejeita a tentativa no back (não confia só no front).

- **CA71 (LGPD — minimização)**: Dado o DTO/registro de uma pendência, Quando inspecionado (banco e endpoint do painel), Então ele contém apenas tipo da ação, vínculos por id, status e referência — sem nome de paciente, sem observação clínica, sem conteúdo da evolução.

- **CA72 (append-only intacto)**: Dado uma evolução já salva, Quando uma pendência dela é concluída (auto ou manual), Então o `ConteudoJson` e o snapshot da evolução permanecem byte-a-byte inalterados (só a tabela de pendências muda).

- **CA73 (retrocompat — conduta legada)**: Dado uma evolução antiga com `conduta` em texto livre, Quando o prontuário é aberto, Então o texto renderiza read-only sem checklist, sem erro, e nenhuma pendência é gerada retroativamente.

- **CA74 (performance — painel leve)**: Dado a página do paciente, Quando o painel carrega, Então dispara **um** GET leve por paciente filtrando `status='Pendente'` (usa o índice `(estabelecimento_id, paciente_id, status)`), retornando só os campos da tela — sem N+1 e sem consulta pesada de prontuário.

- **CA75 (regressão prontuário/agenda)**: Dado o fluxo de salvar evolução pré-existente (incl. exame físico, pool de variáveis), Quando salvo com a nova seção Conduta, Então o salvamento, o retorno do `evolucaoId` e o encadeamento de exame físico continuam funcionando, e a falha eventual na criação de pendência (falha-suave) não derruba a evolução.

## 8. Riscos e dependências

- **Dependência**: F3 (`2026-06-10_011`) — procedimentos indicados ligados ao catálogo — para dar sentido ao item "marcar procedimento realizado" (cujo gatilho real só vem na F4). A criação das pendências em si independe de F1/F2.
- **Risco — conclusão por engano**: `AgendamentoCriadoEvent` dispara em qualquer agendamento. Mitigação: só conclui acao=AgendarRetorno quando `InicioPrevisto > agora` (R11). Documentar que, se o paciente tem 2 retornos pendentes simultâneos (raro), conclui o **mais recente aberto** — comportamento determinístico e aceitável.
- **Risco — render do prontuário**: trocar o componente da seção `conduta` pode quebrar render de evoluções antigas. Mitigação: CA73 + flag de legado (texto livre → read-only).
- **Risco — idempotência sob reprocessamento**: a UNIQUE `(evolucao_id, acao)` é a trava dura; o domínio só evita o erro 23505 vazar. CA62 cobre.
- **Risco — falha-suave**: a criação de pendências segue o padrão `PoolExtrator` (nunca derruba a evolução). CA75 cobre.
- **Plug futuro (não-bloqueante)**: F4 plugará o handler de "procedimento realizado" (cobrança + baixa de estoque) usando a mesma pendência; F5 já é coberta por R10 (orçamento criado). Deixar o gancho documentado em ARQUITETURA.

## 9. Observações para execução

**Não-negociável**:
- A criação de pendências **mora no `RegistrarEvolucaoCommandHandler`**, no mesmo ponto e padrão do `PoolExtratorEvolucao.ExtrairECriar` (falha-suave, antes do publish dos eventos). NÃO criar transação/fluxo paralelo.
- Os handlers de conclusão automática são **novos `IEventHandler<TEvent>` que ouvem os eventos EXISTENTES** (`ReceitaEmitidaEvent`, `AtestadoEmitidoEvent`, `PedidoExameEmitidoEvent`, `OrcamentoCriadoEvent`, `AgendamentoCriadoEvent`). **NÃO criar eventos novos** — eles já existem e já carregam `PacienteId`+`EstabelecimentoId`. Seguir o padrão de `PacienteCadastradoEventHandler`.
- Minimização (R4/CA71) é design, não checklist: a entidade `PendenciaAtendimento` não tem campo de texto clínico nem nome de paciente.
- Multi-tenant em todas as queries/handlers de pendência; repo falha-fechada.
- Tipografia via tokens (CLAUDE.md §5); design system primeiro (checkbox/label/modal/card do DS, sem HTML cru).

**Liberdade técnica do dev**:
- Decidir se o "tipo checklist" da seção `conduta` vem de uma flag no catálogo `modeloProntuarioBuilder.ts` ou de um novo `tipo` na seção — desde que o legado (texto livre) continue renderizando read-only (CA73). Recomendação: novo `tipo: "conduta_checklist"` na seção `conduta`, mantendo fallback textarea.
- Formato exato do trecho de Conduta dentro do `ConteudoJson` (ex.: `{ acoesMarcadas: [...], observacao: "..." }`) — desde que append-only e parseável pelo handler de criação.
- Decidir o `referencia_id` como bigint genérico (sem FK forte) por ser multi-origem (receita/atestado/exame/orçamento/agendamento).

**Reuso obrigatório**: hook do `RegistrarEvolucaoCommandHandler`; padrão `IEventHandler`; eventos de domínio existentes; `prontuario.acesso_log` continua auditando o acesso à página do paciente (a F3B não adiciona audit novo — a pendência não é dado clínico novo, é projeção do que a evolução já registrou).

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar, na seção de bounded contexts/eventos de Prontuário (ou em "Eventos de domínio e handlers"), a entidade `PendenciaAtendimento` e o **padrão de conclusão automática por gatilho**: novos `IEventHandler` ouvindo eventos existentes (`ReceitaEmitidaEvent`, `AtestadoEmitidoEvent`, `PedidoExameEmitidoEvent`, `OrcamentoCriadoEvent`, `AgendamentoCriadoEvent`) para concluir pendências de forma idempotente/no-op, e o gancho **preparado** de "procedimento realizado" que a F4 plugará. Mudança incremental, surgical — só a seção de eventos/entidades de Prontuário.
- **`Docs/LGPD.md`** — *não precisa de nova seção*: a pendência é minimizada por design (sem PII/conteúdo clínico, R4/CA71) e o acesso ao paciente já é auditado pelo padrão existente. Registrar a premissa no próprio briefing basta; **nenhum delta em LGPD.md** (a entidade não expõe PII nova nem novo endpoint de dado sensível).
- **`Docs/DESIGN.md`** — *opcional/não obrigatório*: se o painel de pendências virar um componente reutilizável de design system (ex.: `AppPendenciesPanel`), o `imedto-developer` documenta na entrega; caso seja específico da página do paciente, não precisa. Decisão do dev conforme reuso real.

## 11. Schema para o `imedto-database`

Tabela nova `pendencias_atendimento` (migration SQL idempotente no padrão das F1/F2 — guard por `__ef_migrations_history`, sem BEGIN/COMMIT). **Não altera nenhuma tabela existente.**

```sql
CREATE TABLE public.pendencias_atendimento (
    id                     bigint GENERATED BY DEFAULT AS IDENTITY,
    estabelecimento_id     bigint NOT NULL,
    paciente_id            bigint NOT NULL,
    evolucao_id            bigint NOT NULL,
    agendamento_id         bigint,
    acao                   character varying(40) NOT NULL,   -- CriarReceita|CriarAtestado|PedirExame|CriarOrcamento|MarcarProcedimentoRealizado|AgendarRetorno
    status                 character varying(20) NOT NULL DEFAULT 'Pendente', -- Pendente|Concluida
    referencia_id          bigint,                            -- id do doc/agendamento que concluiu (nullable; null em conclusão manual)
    concluida_em           timestamp with time zone,
    criado_por_usuario_id  uuid NOT NULL,
    criado_em              timestamp with time zone NOT NULL,
    atualizado_em          timestamp with time zone,
    CONSTRAINT "PK_pendencias_atendimento" PRIMARY KEY (id),
    CONSTRAINT fk_pendencias_estabelecimento FOREIGN KEY (estabelecimento_id) REFERENCES public.estabelecimentos (id) ON DELETE RESTRICT,
    CONSTRAINT fk_pendencias_paciente        FOREIGN KEY (paciente_id)        REFERENCES public.pacientes (id)        ON DELETE RESTRICT,
    CONSTRAINT fk_pendencias_evolucao        FOREIGN KEY (evolucao_id)        REFERENCES public.prontuario_evolucoes (id) ON DELETE RESTRICT,
    CONSTRAINT uq_pendencias_evolucao_acao   UNIQUE (evolucao_id, acao)        -- idempotência da criação (R3/CA62)
);

-- Índice de leitura do painel (endpoint leve por paciente — CA74)
CREATE INDEX ix_pendencias_estab_paciente_status
    ON public.pendencias_atendimento (estabelecimento_id, paciente_id, status);
```

**Notas para o DB agent**:
- Confirmar o nome real da tabela de evoluções (`prontuario_evolucoes` ou equivalente — verificar via MCP/migration de prontuário) para a FK `fk_pendencias_evolucao`.
- `acao` e `status` como `varchar` (enum-string, padrão do projeto, ex.: `cobrancas.status`) — sem CHECK constraint (o domínio é a fonte da verdade; alinhado ao padrão `cobrancas`).
- Sem FK em `agendamento_id` e `referencia_id` (referência fraca multi-origem) — é intencional.
- A constraint UNIQUE é a trava de idempotência; o domínio trata o conflito sem vazar 23505.
