# Visualizar prontuário em tela (drawer de leitura da evolução)

**ID**: 2026-05-25_001
**Status**: Aprovado por usuário em 2026-05-25
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: prontuário (timeline de evoluções), permissionamento (RBAC do botão), LGPD (decisão consciente de não auditar abertura)

## 1. Contexto e motivação

Hoje, para ler o conteúdo completo de uma evolução do prontuário, o profissional precisa exportar o PDF — o que cria atrito (download, abrir no leitor, descartar arquivo) e gera registro de exportação. Em muitos momentos clínicos o usuário só quer **conferir o que foi escrito**, sem necessidade de gerar documento.

A timeline (`EvolucaoTimelineItem.vue`) já entrega um card compacto com metadados (data, profissional, modelo). O payload de `prontuarioService.listarEvolucoes` já trafega o `modeloSnapshot` + `conteudo` por evolução — todos os dados necessários para renderizar a leitura completa já estão no cliente.

A demanda é abrir uma visão **somente leitura** desse conteúdo em tela, sem sair do contexto da timeline, sem gerar PDF, e sem registrar audit adicional.

## 2. Persona-alvo

- **Profissional autor** da evolução: revisar o que registrou em consultas anteriores, em retornos do mesmo paciente.
- **Administrador do estabelecimento**: auditoria operacional pontual sem precisar gerar documento.

Momento da jornada: durante atendimento (retorno) ou pré-atendimento, navegando histórico do paciente.

## 3. Decisões fechadas com o usuário

| # | Decisão | Escolha |
|---|---------|---------|
| 1 | Modo de exibição | Drawer lateral (reuso do `AppDrawer` do design system). Timeline permanece visível atrás. |
| 2 | Escopo das seções | Apenas seções preenchidas do `modeloSnapshot`. Seções vazias são ocultadas. |
| 3 | Botão "Editar" no drawer | Não. Visão é estritamente leitura. Edição segue fluxo existente fora do drawer. |
| 4 | Audit LGPD ao abrir | Não registrar. Racional: `listarEvolucoes` já trafega `conteudo` no payload da timeline; abrir o drawer só re-renderiza dados já presentes no cliente, não há novo evento de acesso. PDF continua registrando `Exportacao` (canal separado, distribuível). |
| 5 | RBAC do botão "Ver" | Mais restrito que o PDF: somente **autor da evolução** + **admin do estabelecimento**. Usuário ciente de que isso cria inconsistência com o PDF (outros profissionais com permissão geral ainda conseguem exportar PDF e ler o conteúdo). Tratar a brecha do PDF é demanda separada futura. |

## 4. Escopo

**Inclui**:
- Botão "Ver" (ou equivalente) no card de evolução da timeline, ao lado de "Ver PDF".
- Drawer lateral somente leitura exibindo: cabeçalho (data, profissional, modelo) + seções preenchidas do `modeloSnapshot` com seus respectivos valores do `conteudo`.
- Filtro de seções vazias (não exibir).
- Empty state quando todas as seções estão vazias.
- Espelho back+front da regra de RBAC (autor || admin).
- Multi-tenant garantido (filtro `estabelecimento_id` no back, botão escondido no front).

**Não inclui**:
- Edição da evolução pelo drawer.
- Anexos, prescrições, atestados, exames — apenas conteúdo da evolução em si.
- Comparação entre evoluções (diff lado-a-lado).
- Histórico de versões da mesma evolução.
- Mudança no fluxo/RBAC do PDF (a inconsistência consciente fica como backlog).
- Audit trail de abertura do drawer.

## 5. Regras de negócio

- **R1 — RBAC do botão "Ver"**: o botão "Ver" só aparece no card quando o usuário corrente é (a) autor da evolução, OU (b) admin do estabelecimento. Mora em: Domain (regra de quem pode ver) + Query/DTO (campo derivado ou comparação no front com claims). Validada em: back (se houver endpoint dedicado) + front (visibilidade do botão).
- **R2 — Multi-tenant**: toda query/comando que toca a evolução filtra `estabelecimento_id` do tenant claim. Repositório falha-fechada: sem tenant → vazio/exception. Mora em: Repositório (filtro obrigatório) + Handler (premissa). Validada em: back (filtro SQL/EF) + front (não há como abrir drawer cross-tenant porque a timeline já é tenant-scoped).
- **R3 — Espelho de RBAC**: se o dev optar por criar endpoint dedicado `GET /evolucoes/{id}` para hidratar o drawer, esse endpoint **exige** `autor || admin`. Front escondendo o botão não basta — back retorna 403/404 genérico se a regra for violada. Mora em: Handler/Query + Authorization. Validada em: back (teste de handler) + front (botão escondido).
- **R4 — Seções vazias ocultas**: para cada chave do `modeloSnapshot`, o drawer só renderiza a seção se o valor correspondente em `conteudo` for **não vazio**. Considerar vazio: `null`, `undefined`, string em branco (`""`/whitespace), array vazio, objeto sem chaves significativas. Mora em: Front (renderização). Sem reflexo no back (regra de UX).
- **R5 — Sem audit ao abrir drawer**: não chamar `registrar-exportacao` nem criar registro de audit ao abrir/fechar o drawer. PDF mantém seu fluxo atual de audit. Mora em: Front (não dispara endpoint) + Back (não há endpoint novo de audit para drawer). Validada em: QA observando ausência de chamada de audit + ausência de linha nova em tabela de audit.

## 6. Modelo de dados

Nenhuma alteração de schema. Reuso dos dados já existentes:
- Tabela de evoluções (já tem `autor_id`, `estabelecimento_id`, `modelo_snapshot`, `conteudo`).
- Claims/role de admin de estabelecimento (já existem no sistema de auth).

Se o dev optar por criar endpoint dedicado `GET /evolucoes/{id}`, ele:
- Filtra `estabelecimento_id` do tenant claim.
- Verifica `autor || admin`.
- Retorna 403/404 genérico em caso de violação (mensagem sem PII).
- Sem audit row.

## 7. UX e fluxo

**Wireframe textual**:

```
Timeline (atual)
┌─────────────────────────────────────────────┐
│ [card de evolução compacto]                 │
│  Dr. João — 22/05/2026 — Anamnese padrão    │
│                       [Ver] [Ver PDF]       │ ← botão "Ver" novo, condicional ao RBAC
└─────────────────────────────────────────────┘

AppDrawer (novo, à direita)
┌──────────────────────────────────────┐
│ Evolução de 22/05/2026          [×] │
│ Dr. João — Anamnese padrão          │
│ ────────────────────────────────────│
│ Queixa principal                    │
│   Dor abdominal há 3 dias.          │
│                                     │
│ História da doença atual            │
│   Iniciada após refeição...         │
│                                     │
│ [seções vazias do snapshot omitidas]│
│                                     │
│                                     │
│                            [Fechar] │
└──────────────────────────────────────┘
```

- **Componente**: reuso do `AppDrawer` do design system (lateral à direita). Dev escolhe se cria `EvolucaoDetalheDrawer.vue` dedicado ou compõe inline na view do prontuário.
- **Estados**:
  - **Carregando**: skeleton no corpo do drawer (somente se via b — endpoint dedicado — for adotada; se via a, abre instantâneo).
  - **Preenchido**: cabeçalho + seções preenchidas.
  - **Vazio (todas seções vazias)**: `AppEmptyState` com texto "Esta evolução não tem seções preenchidas".
  - **Erro de fetch** (só se via b): `AppEmptyState` com mensagem genérica e botão "Tentar novamente".
- **Mobile-ready**: drawer responsivo (já é premissa do `AppDrawer` do design system).
- **Atalho de teclado**: `Esc` fecha o drawer (comportamento padrão do `AppDrawer`).

## 8. Critérios de aceite (testáveis)

- **CA1 — Visualização (caminho feliz, autor)**
  Dado que sou o profissional autor de uma evolução com seções preenchidas,
  Quando clico em "Ver" no card da timeline,
  Então o drawer abre lateralmente exibindo cabeçalho (data, profissional, modelo) e somente as seções preenchidas do `modeloSnapshot` com seus valores; **não** existe botão "Editar" dentro do drawer.

- **CA2 — Filtro de seções vazias**
  Dado uma evolução com `modeloSnapshot` contendo 6 seções, das quais 3 têm valor em `conteudo` (não-vazio, não-whitespace) e 3 estão vazias (`null`/`""`/`[]`/`{}`),
  Quando abro o drawer,
  Então exatamente 3 seções são renderizadas, na ordem do snapshot; as 3 vazias não aparecem nem como título.

- **CA3 — RBAC autor vê botão**
  Dado um profissional autenticado que é o **autor** de uma evolução,
  Quando a timeline renderiza o card daquela evolução,
  Então o botão "Ver" aparece no card.

- **CA4 — RBAC admin vê botão**
  Dado um usuário autenticado com papel **admin do estabelecimento** ao qual a evolução pertence, e que **não** é o autor,
  Quando a timeline renderiza o card,
  Então o botão "Ver" aparece no card.

- **CA5 — RBAC outro profissional NÃO vê botão**
  Dado um profissional autenticado no mesmo estabelecimento, **não-autor** e **não-admin**, com permissão geral de leitura do prontuário,
  Quando a timeline renderiza o card,
  Então o botão "Ver" **não** aparece. O botão "Ver PDF" continua aparecendo (consistente com a decisão consciente do usuário).

- **CA6 — Multi-tenant (botão escondido)**
  Dado um usuário do estabelecimento B,
  Quando navega para um paciente que (por bug ou rota forçada) entrega uma evolução do estabelecimento A,
  Então o botão "Ver" não aparece e, se houver endpoint dedicado e o usuário forçar a request, o backend retorna 403/404 genérico sem revelar a existência do recurso e sem logar PII.

- **CA7 — Estado vazio**
  Dado uma evolução existente cujo `conteudo` tem **todas** as seções vazias (null/whitespace/array vazio/objeto vazio),
  Quando abro o drawer,
  Então o corpo exibe `AppEmptyState` com a mensagem "Esta evolução não tem seções preenchidas", sem listar títulos sem valor.

- **CA8 — Performance / não-refetch desnecessário**
  Dado que a timeline já entregou o `conteudo` + `modeloSnapshot` no payload de `listarEvolucoes`,
  Quando abro e fecho o drawer 5 vezes em sequência em evoluções diferentes,
  Então não há nova chamada HTTP para hidratar conteúdo já presente em memória. (Se o dev justificar a via b — endpoint dedicado — porque o `conteudo` chega truncado, a justificativa deve estar nos comentários do PR e o cache deve evitar re-fetch para a mesma evolução já aberta na sessão.)

- **CA9 — LGPD: sem audit ao abrir drawer**
  Dado que abro e fecho o drawer N vezes,
  Quando inspeciono o tráfego de rede E a tabela de audit do prontuário,
  Então **nenhuma** chamada de `registrar-exportacao` é disparada e **nenhuma** nova linha de audit é inserida pela abertura do drawer. O fluxo de PDF continua registrando `Exportacao` quando exercitado (não regrediu).

- **CA10 — Documentação viva**
  Esta demanda **não exige** atualização de `Docs/`. Justificativa: o `AppDrawer` já existe no design system, não há novo padrão de auth, não há mudança de schema, não há novo recurso de infra, não há novo tipo de PII. Caso o dev introduza componente novo (ex: `EvolucaoDetalheDrawer.vue`) por opção de organização, isso permanece como detalhe interno do módulo prontuário e não exige doc cross-cutting.

## 9. Riscos e dependências

- **Inconsistência RBAC consciente entre drawer e PDF**: profissionais não-autor/não-admin continuam podendo exportar PDF e ler o conteúdo da evolução. O usuário foi avisado e aceitou. Recomenda-se abrir item de backlog separado para revisitar RBAC do PDF — não escopo desta entrega.
- **Mudança no payload de `listarEvolucoes`**: se hoje o `conteudo` vem completo no payload da timeline, qualquer redução futura (lazy/truncate por performance) quebra a via a do drawer. Se acontecer, será preciso introduzir endpoint dedicado retroativamente.
- **Áreas regressivas**:
  - Timeline do prontuário (`EvolucaoTimelineItem.vue`): inserção de novo botão não pode quebrar layout do card compacto (commit `c867914` é referência do estado atual a preservar).
  - Fluxo de PDF: nenhuma alteração esperada — QA precisa validar que o botão "Ver PDF" e o audit de `Exportacao` continuam funcionando.
- **Comportamento mobile**: `AppDrawer` em telas estreitas pode ocupar 100% da largura — validar que o conteúdo renderiza bem sem ultrapassar viewport.

## 10. Observações para execução

**Liberdade técnica do dev (não-prescritivo)**:

- **Via a (preferida, sem novo endpoint)**: usar `autorId` (já existente no payload) comparado ao `userId` da sessão + claim/role de admin para decidir visibilidade do botão. Drawer hidrata a partir dos dados já em memória do store/composable da timeline.
- **Via b (só se justificável)**: criar `GET /evolucoes/{id}` quando o `conteudo` na listagem estiver truncado ou houver razão concreta de performance. Nesse caso, o endpoint **exige** `autor || admin` + filtro `estabelecimento_id`, retorna 403/404 genérico em violação, sem audit row. Cache no front para evitar refetch ao reabrir a mesma evolução na sessão.

**Não-negociáveis**:
- Espelho back+front da RBAC se via b for adotada.
- Filtro `estabelecimento_id` no back é obrigatório (premissa não-negociável do CLAUDE.md).
- Mensagem de erro genérica, sem PII.
- Nenhum audit row pela abertura do drawer.
- Reuso do `AppDrawer` e do `AppEmptyState` do design system — não criar variantes locais.

**Pontos de atenção para QA**:
- Validar os 3 perfis distintos do RBAC (autor, admin não-autor, outro profissional) em cenários reais — não basta unit test do front.
- Inspecionar Network tab E tabela de audit do prontuário durante CA9.
- Confirmar que o card compacto da timeline não regrediu visualmente.

## 11. Atualização de documentação

Nenhum doc em `Docs/` será atualizado nesta entrega. Justificativa em CA10.
