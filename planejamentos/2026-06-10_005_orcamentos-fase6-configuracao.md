# Orçamentos — concluir a configuração (Fase 6): CRUD editável em "Outras configurações" e remoção de placeholders

**ID**: 2026-06-10_005
**Status**: Aprovado por usuário em 2026-06-10 (modo autônomo — escopo definido por descoberta a fundo do estado real; decisões e fatias em §3/§11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P-M (frontend-only; backend e service já prontos)
**Áreas regressivas tocadas**: orçamento (configuração). Não toca: schema, backend, formulário de orçamento, conversão em cirurgia, PDF.

## 1. Contexto e motivação

O roadmap (`Docs/Roadmap/FASE_1_COMPLETUDE.md`, item 1.6) pede "Orçamentos Fase 6 (configuração de abas/painéis): concluir o que o plano interno 'Fase 6' já definiu para `OrcamentoSettingsView` (catálogo + valores por profissional + configuração de abas)", marcando o módulo como "Partial".

**A descoberta a fundo (2026-06-10) mostrou que a "Fase 6" está, na prática, quase toda concluída** — este briefing define o escopo mínimo do que **de fato falta** para sair de "Partial" → "Complete", sem reinventar o que existe:

Já concluído e **fora de escopo** (não refazer):
- O épico **`Docs/06_MIGRACAO_ORCAMENTOS.md`** (Fases 6.0 a 6.4) está marcado **CONCLUÍDA (2026-04-30)**: catálogos, form tabbed, cálculos no backend, conversão em cirurgia, PDF rico.
- `OrcamentoCatalogoController` (`/api/orcamentos/configuracoes`) tem **CRUD completo** para: cirurgias/procedimentos, **valores-profissional**, local-cirurgia, equipes, implantes, pagamento, produtos, vínculo cirurgia×produto, team-roles, anestesistas, pacotes — tudo com `[FeatureGate(OrcamentoCompleto)]` + `[RequiresAcao("orcamento","configurar")]` + multi-tenant + `[RequiresPapel(Dono,Recepcionista)]` nas escritas.
- `OrcamentoSettingsView.vue` tem **7 abas** ligadas a componentes reais (300-440 linhas cada): Procedimentos, Produtos, Equipe, **Valores profissional** (feita no briefing `2026-06-04_008`), Anestesistas, Pacotes, **Outras configurações**.
- `orcamentoCatalogoService` já tem os métodos de **escrita** para implantes, equipes-legado e pagamento (`criarImplante/atualizarImplante/removerImplante`, `criarEquipe/...`, `criarConfigPagamento/atualizarConfigPagamento`, `salvarLocal`).

O que **realmente falta** (impede declarar "Complete"):
1. A aba **"Outras configurações"** (`OutrasConfigsTab.vue`) lista **Implantes, Equipes (legado) e Pagamento** em modo **somente-leitura**, com a mensagem "gerenciados no formulário antigo até a migração". Ou seja, o usuário **não consegue criar/editar/excluir** esses três pela configuração — ele é empurrado para um "formulário antigo". Só a sub-aba "Local cirurgia" é editável. **Os endpoints e os métodos de service já existem** — falta apenas a UI editável.
2. Dois botões do cabeçalho da view — **"Importar planilha"** e **"Exportar"** — são **placeholders** que só mostram toast "em breve". UX morta que destoa de um módulo "completo".

Fechar (1) elimina a dependência do "formulário antigo" e torna a configuração de orçamento **autossuficiente**; (2) remove a falsa promessa de funcionalidade.

## 2. Persona-alvo

- **Dono / Recepcionista** com a ação `orcamento.configurar` e plano com `OrcamentoCompleto` — no momento de **configurar o estabelecimento** para montar orçamentos cirúrgicos (cadastrar implantes, tabelas de pagamento, equipes). Frequência: baixa (setup inicial + ajustes pontuais). É pré-requisito operacional para orçamentos cirúrgicos completos.

## 3. Escopo

**Inclui (escopo mínimo viável para "Partial → Complete")**:
- Tornar **editáveis** (criar / editar / excluir) as três sub-seções somente-leitura de `OutrasConfigsTab.vue`, reusando o padrão de aba existente (drawer + `AppConfirmDialog` + `AppToast` + badge de contagem), consumindo os métodos **já existentes** do `orcamentoCatalogoService`:
  - **Implantes**: `criarImplante` / `atualizarImplante` / `removerImplante` (campos: descrição, custo unitário, vínculo opcional a item de inventário `itemInventarioId`).
  - **Equipes (legado)**: `criarEquipe` / `atualizarEquipe` / `removerEquipe` (campos: descrição, valor padrão).
  - **Pagamento**: `criarConfigPagamento` / `atualizarConfigPagamento` / `removerConfigPagamento` (campos: forma de pagamento, acréscimo %, entrada % padrão, taxa parcela, parcelas máximas).
- Remover a mensagem "gerenciados no formulário antigo" das sub-seções que passarem a ser editáveis (a sub-aba "Local cirurgia" já é editável e permanece).
- **Remover os botões placeholder** "Importar planilha" e "Exportar" do cabeçalho de `OrcamentoSettingsView.vue` (UX morta) — ver §11 para a alternativa considerada.
- Confirmar (CA) que a view e suas ações continuam atrás de `FeatureGate("orcamento_completo")` + RBAC `orcamento.configurar`.

**Não inclui (fora — registrar como próximo briefing se desejado)**:
- Qualquer mudança de **backend, schema, controller, command, handler ou migration** — tudo já existe. **Não acionar `imedto-database`.**
- Reescrita do **formulário de orçamento** (`OrcamentoFormView`), conversão em cirurgia, cálculo, PDF — todos concluídos na Fase 6.
- A aba **Valores profissional** (já entregue em `2026-06-04_008`).
- Features novas de **funil/CRM/pipeline de orçamentos** (são da Fase 3 do roadmap, não desta).
- **Importar planilha** (importação CSV/XLSX de catálogos) — funcionalidade nova de porte próprio; fica como backlog (próximo briefing) se o usuário quiser.
- **Exportar** catálogos para CSV — se desejado, pode reusar o padrão do briefing `2026-06-10_004` num briefing futuro; não entra aqui para manter o escopo cirúrgico.
- Renomear/reorganizar as 7 abas ou fundir a "Outras configurações" nas demais (a fusão prometida no aviso é refactor de UX maior — fora do MVP de completude; ver §11).

> **Nota de fatiamento**: a investigação confirmou que o "restante" da Fase 6 **não é grande** — resume-se aos itens acima. Importar planilha foi deliberadamente deixado de fora por ser feature nova (não "completude").

## 4. Regras de negócio

- **R1 — Multi-tenant**. Todas as operações (listar/criar/editar/excluir) de implantes, equipes e pagamento carregam `EstabelecimentoId` derivado do tenant claim no backend; o front **não** envia `estabelecimentoId`. Já garantido em `OrcamentoCatalogoController`. Mora em: **Controller/Handler** (back). Validada em: back (fonte da verdade) + front (httpClient usa o tenant ativo).

- **R2 — RBAC + FeatureGate**. A view e todas as ações de escrita exigem **plano com `OrcamentoCompleto`** e ação **`orcamento.configurar`**, e POST/PUT/DELETE exigem papel **Dono ou Recepcionista**. Já garantido no backend (`[FeatureGate(Features.OrcamentoCompleto)]` + `[RequiresAcao("orcamento","configurar")]` na classe; `[RequiresPapel(Dono,Recepcionista)]` nas escritas). Espelho no front: a aba e os botões seguem o mesmo gate visual das demais abas (a rota da view já é gateada). Mora em: **Controller** (back) + **gate visual** (front). Validada em: back (403/422) + front (UX).

- **R3 — Confirmar antes de excluir**. Excluir implante/equipe/pagamento usa `AppConfirmDialog` (`variante="danger"`), com a request disparando só no `@confirmar` e `:executando` bloqueando duplo disparo — padrão canônico do design system (`Docs/DESIGN.md`). Mora em: **Front**. Validada em: front. O backend é a fonte da verdade do efeito (DELETE).

- **R4 — Feedback pós-ação via AppToast**. Sucesso/erro de criar/editar/excluir usa `AppToast` (nunca `window.alert`), com a mensagem de erro vinda do backend (`e?.response?.data?.mensagem ?? "Falha ao ..."`). Mora em: **Front**. Validada em: front.

- **R5 — Vínculo opcional implante × item de inventário**. O implante pode referenciar um `itemInventarioId` (campo opcional, já no command `CriarImplanteDto(long? ItemInventarioId, ...)`). O seletor de item deve listar itens do estabelecimento (reuso de `inventarioService`/listagem existente) e ser **opcional** ("Sem item de inventário"). Mora em: **Front** (seletor) + **back** (já aceita null). Validada em: back + front.

- **R6 — Sem PII**. Catálogos de orçamento (implante, equipe, pagamento) não contêm dados de paciente; mensagens de erro genéricas. Não há audit clínico aqui (não é prontuário). Mora em: **Front/Back** (já genéricos). Validada em: back.

## 5. Modelo de dados

**Schema NÃO muda. Nenhum endpoint/contrato novo. `imedto-database` NÃO é acionado.**

Tabelas e endpoints já existentes e usados:
- Implantes: `GET/POST/PUT/DELETE /api/orcamentos/configuracoes/implantes` → `CatalogoImplanteDto` (`id, descricao, custoUnitario, itemInventarioId?, itemInventarioNome?, ativo`).
- Equipes (legado): `GET/POST/PUT/DELETE /api/orcamentos/configuracoes/equipes` → `CatalogoEquipeEspecializadaDto` (`id, descricao, valorPadrao, ativo`).
- Pagamento: `GET/POST/PUT/DELETE /api/orcamentos/configuracoes/pagamento` → `ConfiguracaoPagamentoCatalogoDto` (`id, formaPagamentoId, formaPagamentoNome?, acrescimoPercentual, entradaPercentualPadrao, taxaParcela, parcelasMaximas, ativo`).
- Formas de pagamento (para o seletor): `formaPagamentoService.listar()` (já usado em `OutrasConfigsTab`).
- Itens de inventário (para o seletor do implante): listagem já existente (`inventarioService` / `GET /api/inventario/itens`).

`orcamentoCatalogoService` já expõe todos os métodos de escrita necessários (confirmado: `criarImplante/atualizarImplante/removerImplante`, `criarEquipe/atualizarEquipe/removerEquipe`, `criarConfigPagamento/atualizarConfigPagamento`). Confirmar a existência de `removerConfigPagamento` no service; se faltar (o controller tem o DELETE), **adicionar o método no service** (mudança trivial de front, sem backend).

## 6. UX e fluxo

`OutrasConfigsTab.vue` mantém as 4 sub-abas (`Local cirurgia`, `Implantes`, `Equipes legado`, `Pagamento`). Local cirurgia permanece como está (já editável inline).

Para **Implantes / Equipes / Pagamento**, replicar o padrão de `EquipeTab.vue` / `ValoresProfissionalTab.vue`:
- **Cabeçalho da sub-seção**: título + contagem + botão `AppButton icon="fa-solid fa-plus"` "Novo {implante/equipe/configuração de pagamento}".
- **Lista**: a tabela já existe (read-only hoje) — adicionar, por linha, ações **Editar** (lápis) e **Excluir** (lixeira/ban), seguindo o padrão de ícones de linha do projeto.
- **Drawer de criar/editar** (`AppDrawer`): campos via `AppField`/`AppInput`/`AppSelect`:
  - Implante: Descrição (obrigatório), Custo unitário (R$), Item de inventário (select opcional).
  - Equipe: Descrição (obrigatório), Valor padrão (R$).
  - Pagamento: Forma de pagamento (select obrigatório, alimentado por `formaPagamentoService.listar()`), Acréscimo %, Entrada % padrão, Taxa parcela, Parcelas máximas (inteiro).
- **Excluir**: `AppConfirmDialog variante="danger"` (R3).
- **Toast** de sucesso/erro (R4).
- Após criar/editar/excluir, **recarregar a lista** da sub-seção (já há `carregarTudo`/listagens no componente) e atualizar a contagem exibida.
- Remover o texto "gerenciados no formulário antigo até a migração" das sub-seções agora editáveis. O aviso amarelo do topo da aba: ajustar/remover conforme as sub-seções deixem de ser "transitórias" (ver §11).

**Cabeçalho da view** (`OrcamentoSettingsView.vue`): remover os dois `AppButton` placeholder ("Importar planilha", "Exportar") e o `mostrarBreve`/`AppToast` associado se ficar órfão (limpar apenas o que esta mudança tornar não-usado — Surgical Changes).

**Estados** (por sub-seção):
- **Carregando**: indicador enquanto lista carrega (a aba já carrega no `onMounted`).
- **Vazio**: `AppEmptyState` já existe em cada sub-seção — manter, mas ajustar a descrição para apontar para o botão "Novo ..." (em vez de "no formulário antigo").
- **Erro de escrita**: toast com a mensagem do backend (R4).
- **Sucesso**: toast + lista atualizada.

**Mobile**: drawers e tabelas seguem o responsivo do design system já usado nas outras abas.

## 7. Critérios de aceite (testáveis)

- **CA1 (criar implante — caminho feliz)**: Dado a sub-aba Implantes, Quando o usuário clica em "Novo implante", preenche Descrição e Custo unitário e salva, Então o `POST /api/orcamentos/configuracoes/implantes` é chamado, a lista recarrega exibindo o novo implante e a contagem incrementa.
- **CA2 (editar implante)**: Dado um implante existente, Quando o usuário clica em Editar, altera o custo unitário e salva, Então o `PUT .../implantes/{id}` é chamado e a lista reflete o novo valor.
- **CA3 (excluir implante com confirmação)**: Dado um implante existente, Quando o usuário clica em Excluir, Então aparece `AppConfirmDialog` (danger); ao confirmar, `DELETE .../implantes/{id}` é chamado, a linha some e a contagem decrementa; ao cancelar, nada acontece.
- **CA4 (criar/editar/excluir equipe legado)**: Dado a sub-aba Equipes legado, Quando o usuário cria, edita e exclui uma equipe (descrição + valor padrão), Então cada operação chama o endpoint correspondente (`POST/PUT/DELETE .../equipes`) e a lista/contagem refletem o resultado.
- **CA5 (criar configuração de pagamento)**: Dado a sub-aba Pagamento, Quando o usuário clica em "Nova configuração", seleciona uma forma de pagamento e preenche acréscimo/entrada/taxa/parcelas e salva, Então `POST .../pagamento` é chamado e a lista exibe a nova configuração.
- **CA6 (editar/excluir pagamento)**: Dado uma configuração de pagamento existente, Quando o usuário a edita e depois a exclui (com confirmação), Então `PUT .../pagamento/{id}` e `DELETE .../pagamento/{id}` são chamados e a lista reflete cada mudança.
- **CA7 (vínculo opcional implante × inventário)**: Dado o drawer de implante, Quando o usuário deixa "Item de inventário" como "Sem item de inventário", Então o implante é criado com `itemInventarioId = null` sem erro; quando seleciona um item, o vínculo é enviado e o nome do item aparece na lista.
- **CA8 (multi-tenant)**: Dado um usuário do estabelecimento B, Quando ele tenta criar/editar/excluir um catálogo informando (via request forjado) um id de outro estabelecimento, Então o backend filtra por tenant e retorna erro genérico/não encontrado, sem afetar o estabelecimento A.
- **CA9 (RBAC — papel sem permissão)**: Dado um usuário com a aba acessível mas **sem** papel Dono/Recepcionista (ou sem ação `orcamento.configurar`), Quando tenta uma escrita, Então o backend retorna 403/422 e, no front, as ações de escrita não ficam disponíveis para quem não tem o gate.
- **CA10 (FeatureGate — plano sem orcamento_completo)**: Dado um estabelecimento cujo plano **não** inclui `orcamento_completo`, Quando o usuário tenta acessar a configuração de orçamento, Então o backend nega (FeatureGate) e a view não expõe as ações — coerente com as demais abas já gateadas.
- **CA11 (placeholders removidos)**: Dado o cabeçalho de `OrcamentoSettingsView`, Quando a view é renderizada, Então **não** existem mais os botões "Importar planilha" e "Exportar" que mostravam toast "em breve" (UX morta eliminada).
- **CA12 (sem texto "formulário antigo")**: Dado as sub-seções Implantes/Equipes/Pagamento agora editáveis, Quando renderizadas, Então não exibem mais a instrução de "gerenciado no formulário antigo" — o usuário cria/edita ali mesmo.
- **CA13 (estado vazio aponta para criar)**: Dado uma sub-seção sem registros, Quando carrega, Então o `AppEmptyState` orienta a usar o botão "Novo ..." (não mais "no formulário antigo").
- **CA14 (erro genérico via toast)**: Dado um erro 422 do backend ao salvar (ex.: campo obrigatório), Quando o front trata, Então exibe a mensagem do backend via `AppToast` sem PII e mantém o drawer aberto para correção.
- **CA15 (sem regressão de backend)**: Dado que a entrega é frontend-only, Quando o build/test do backend roda, Então nada muda nos controllers/queries/commands de orçamento (nenhum endpoint, DTO ou migration novo).
- **CA16 (regressão — abas existentes)**: Dado as 7 abas atuais, Quando "Outras configurações" passa a ter sub-seções editáveis, Então as demais abas (Procedimentos, Produtos, Equipe, Valores profissional, Anestesistas, Pacotes) e a sub-aba "Local cirurgia" continuam funcionando inalteradas.

## 8. Riscos e dependências

- **`removerConfigPagamento` no service**: o controller tem `DELETE .../pagamento/{id}`, mas confirmar se `orcamentoCatalogoService` já expõe o método (os de implante/equipe existem). Se faltar, adicionar (front trivial).
- **Aviso "transitório" da aba**: a aba "Outras configurações" se autodescreve como transitória. Tornar 3 das 4 sub-seções editáveis **não** completa a "fusão nas demais abas" prometida no aviso — isso é refactor de UX maior, fora do MVP. Ajustar o texto do aviso (ou removê-lo) para não prometer o que não está no escopo; ver §11.
- **Seletor de item de inventário**: reusar a listagem existente; cuidado para não disparar consulta pesada — usar a listagem paginada/buscável já existente do inventário (ou um endpoint enxuto). Não criar endpoint novo.
- **Consistência com `OrcamentoFormView`**: o "formulário antigo" mencionado pode ser o próprio `OrcamentoFormView`; ao tornar a config editável, garantir que o form continua **consumindo** esses catálogos normalmente (não alterar o form). Estes catálogos já alimentam o form — só passam a ser editáveis pela config.
- **Área regressiva — orçamento**: não tocar cálculo, conversão, PDF nem o form. Mudança restrita a `OutrasConfigsTab.vue`, `OrcamentoSettingsView.vue` (remover botões) e, se necessário, um método no `orcamentoCatalogoService`.

## 9. Observações para execução

**Não-negociável**:
- **Frontend-only**: zero backend/schema/migration (§5/CA15). Não acionar `imedto-database`.
- Reuso do padrão de aba editável (`EquipeTab.vue`/`ValoresProfissionalTab.vue`): drawer + `AppConfirmDialog` + `AppToast` + contagem.
- Excluir sempre com `AppConfirmDialog` (R3/CA3); feedback via `AppToast` (R4/CA14); **nunca** `window.alert/confirm`.
- Manter FeatureGate `orcamento_completo` + RBAC `orcamento.configurar` (R2/CA9-CA10) — não enfraquecer gates.
- Remover placeholders mortos (CA11) e o texto "formulário antigo" (CA12) — Surgical Changes: limpar só o que esta mudança tornar órfão.

**Liberdade técnica (dev decide)**:
- Estrutura interna de `OutrasConfigsTab`: um drawer compartilhado parametrizado por sub-seção vs. três drawers — o que ficar mais legível, mantendo tokens tipográficos (CLAUDE.md §5).
- Texto final do aviso da aba (ajustar vs. remover) — ver §11.
- Componente do seletor de item de inventário (reusar select existente).

**Reuso obrigatório (grep antes de criar)**:
- `EquipeTab.vue` e `ValoresProfissionalTab.vue` (padrão de CRUD em aba de config — drawer/confirm/toast/contagem).
- `orcamentoCatalogoService` (métodos de escrita já existentes).
- `formaPagamentoService.listar()` (seletor de pagamento — já usado em `OutrasConfigsTab`).
- `inventarioService`/listagem de itens (seletor de implante).
- `AppDrawer`, `AppConfirmDialog`, `AppToast`, `AppField`, `AppInput`, `AppSelect`, `AppButton`, `AppEmptyState`, `AppStatusPill` (todos já importados no módulo).

## 10. Atualização de documentação

- **`Docs/06_MIGRACAO_ORCAMENTOS.md`** — adicionar nota curta no fechamento (ou numa seção "pós-Fase 6"): a configuração de Implantes/Equipes-legado/Pagamento, antes somente-leitura na aba "Outras configurações" e dependente do formulário antigo, passou a ser **totalmente editável pela configuração** (briefing `2026-06-10_005`), removendo a dependência do "formulário antigo". Mudança incremental.
- **`Docs/DESIGN.md`** — sem alteração estrutural (reuso puro do padrão de aba editável já documentado: `AppDrawer` + `AppConfirmDialog` + `AppToast`). Não atualizar salvo se nascer componente novo.
- **`Docs/ARQUITETURA.md`** — sem alteração (frontend-only, sem padrão de back novo).
- **Sem `INFRA.md`/`COMANDOS.md`/`LGPD.md`** (sem infra/comando/PII novos).

## 11. Decisões e assunções (execução autônoma)

1. **Escopo = tornar editável o que está read-only + remover placeholders**. A descoberta confirmou que a Fase 6 está concluída; o único caminho real para "Partial → Complete" sem inventar feature é (a) Implantes/Equipes/Pagamento editáveis e (b) eliminar botões mortos. Catálogo e Valores-profissional já estão prontos (este último no briefing `2026-06-04_008`).
2. **"Importar planilha" fica de fora** (backlog/próximo briefing). É feature nova de porte próprio (parsing/validação de planilha), não "completude". Removemos o botão placeholder para não prometer.
3. **"Exportar" removido** (não implementado aqui). Se desejado, vira briefing futuro reusando o padrão CSV de `2026-06-10_004`. Manter este briefing cirúrgico.
4. **Aviso "transitório" da aba**: como a fusão completa nas demais abas é refactor maior fora do escopo, **ajustar o texto** do aviso (ou removê-lo) para refletir que as configurações agora são editáveis ali — sem prometer fusão. Default: remover o aviso das sub-seções que viraram editáveis; manter um aviso mínimo só se "Local cirurgia"/"Equipes legado" ainda forem conceitualmente legado.
5. **Sem reorganizar as 7 abas**. Renomear/fundir abas é UX maior; fora do MVP de completude.
6. **`removerConfigPagamento`**: se ausente no service, adicioná-lo (front trivial) — o DELETE já existe no controller.
7. **Equipes "legado" permanecem** como sub-seção própria (o modelo novo é a aba "Equipe" com papéis/honorários). Tornamos o legado editável para paridade/completude, sem migrar dados — fiel ao estado atual.
