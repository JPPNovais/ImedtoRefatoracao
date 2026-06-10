# Financeiro F3 — Procedimentos indicados ligados ao catálogo + atalho de criação

**ID**: 2026-06-10_011
**Status**: Aprovado por usuário em 2026-06-10 (execução autônoma autorizada — decisões de corte registradas na seção 11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (render das 17 seções + PDF de evolução), catálogo de orçamento (reuso de endpoint/serviço), permissionamento

## 1. Contexto e motivação

A seção **Procedimentos indicados** da evolução do prontuário hoje é texto livre: o profissional digita
`{descricao, observacao}` à mão (`SecaoProcedimentosIndicados.vue`). Isso impede que o procedimento
indicado vire cobrança automática (F4) ou pré-preencha orçamento de cirurgia (F5), porque não há vínculo
com o **catálogo de procedimentos** do estabelecimento — onde mora o valor, a duração e os insumos.

A F3 conecta essa seção ao catálogo existente (`orcamento_catalogo_cirurgia` / `CatalogoCirurgia`,
UI nova já chamada "Procedimento"), guardando referência ao item do catálogo **mais um snapshot** do que
foi indicado. O snapshot é não-negociável: a evolução é **append-only/imutável**, e o catálogo pode mudar
(ou ser inativado) depois — a evolução tem que continuar mostrando o que o profissional registrou naquele
dia, com aquele valor, mesmo que o catálogo mude amanhã.

Para não obrigar o profissional a sair do prontuário e ir até Configurações quando o procedimento ainda
não existe no catálogo, há um atalho **"+ Criar procedimento"** inline que cria no catálogo do tenant ativo
(reusando o command existente) e já seleciona o item recém-criado.

Esta fase **não** gera cobrança (F4) nem altera schema. É a fundação que destrava F3B/F4/F5.

## 2. Persona-alvo

**Profissional** (médico/dentista) durante o atendimento, preenchendo a evolução na aba "Consulta atual".
Secundariamente **recepção/dono**, quando preenchem/conferem a evolução. Frequência: toda consulta que
indica procedimento (alta em clínicas cirúrgicas/estéticas).

## 3. Escopo

**Inclui**:
- `SecaoProcedimentosIndicados.vue` deixa de ser texto livre e passa a um **seletor com busca** sobre o
  catálogo de procedimentos (`CatalogoCirurgia`) do **estabelecimento ativo**.
- Item selecionado vira **chip/linha** com: descrição (do catálogo), valor (do catálogo) e **observação
  opcional** editável. Guarda `catalogoCirurgiaId` + **snapshot** (`descricao`, `valor`) no `ConteudoJson`.
- Atalho **"+ Criar procedimento"** inline (mini-form: nome obrigatório, valor obrigatório, duração
  opcional) que cria no catálogo do tenant ativo via o endpoint/serviço **já existente**
  (`orcamentoCatalogoService.criarProcedimento` → `POST api/orcamentos/configuracoes/procedimentos`) e
  já adiciona o item recém-criado à lista da seção.
- Estado **busca-sem-resultado** destacando o atalho de criação (CTA "Criar \"<termo>\"").
- **Retrocompatibilidade**: evoluções antigas (formato texto-livre `{descricao, observacao}`) renderizam
  **read-only sem quebrar**; o discriminador convive no mesmo `ConteudoJson`.
- Render no **PDF da evolução** usa o snapshot (descrição + observação), sem N+1 ao catálogo.

**Não inclui**:
- Geração de cobrança de procedimento (F4).
- Checklist de conduta / pendências do atendimento (F3B).
- Qualquer mudança de schema SQL — `ConteudoJson` é `jsonb` e o formato novo cabe nele (ver seção 5).
- Edição do catálogo a partir do prontuário além do "criar" (sem editar/remover item do catálogo daqui).
- Vínculo de insumos/produtos ao procedimento pelo prontuário (já existe na config; F3B/F4 consomem).
- Migrar evoluções antigas para o formato novo (coexistência, não conversão).

## 4. Regras de negócio

- **R1 — Catálogo só do tenant ativo**: o seletor lista exclusivamente procedimentos do
  `estabelecimento_id` ativo. Reusa `GET api/orcamentos/configuracoes/procedimentos?ativas=true`, que já
  filtra por `_tenant.EstabelecimentoId` (`ListarCatalogoCirurgiasQuery`). Nunca lista de outros
  estabelecimentos. Mora em: Query (já existente, multi-tenant na origem) + Front (consome). Validada em:
  back (filtro de tenant na query) + front (UX). **Premissa explícita do usuário: nunca de outros tenants.**
- **R2 — Criação inline grava no tenant ativo**: o "+ Criar procedimento" reusa
  `CriarCatalogoCirurgiaCommand`, que seta `EstabelecimentoId = _tenant.EstabelecimentoId` no controller.
  O front **não** envia `estabelecimentoId` no payload (o back resolve do claim de tenant). Mora em:
  Handler/Controller (já existente). Validada em: back.
- **R3 — RBAC da criação inline = a mesma permissão do catálogo de orçamento** (opção mais segura — ver
  decisão D3 na seção 11). O endpoint de criação já exige, em camadas: `[FeatureGate(OrcamentoCompleto)]`
  + `[RequiresAcao("orcamento","configurar")]` + `[RequiresPapel(Dono, Recepcionista)]`. **Não criamos
  endpoint paralelo nem afrouxamos a regra.** Profissional sem essa permissão **não vê** o botão
  "+ Criar procedimento" (front), e se chamar o endpoint mesmo assim recebe **403** (back). Mora em:
  Controller (já existente) + Front (ocultar CTA). Validada em: back (403) + front (ocultar).
- **R4 — Listar para selecionar reusa o mesmo gate do catálogo**: o `GET .../procedimentos` está sob o
  mesmo `[RequiresAcao("orcamento","configurar")]` + `[FeatureGate(OrcamentoCompleto)]`. Logo, um
  profissional **sem** esse acesso não consegue listar o catálogo para selecionar. Decisão de produto
  (D4, seção 11): nesse caso a seção **degrada graciosamente** para entrada manual read-friendly — o
  profissional ainda registra texto livre `{descricao, observacao}` no formato legado (a seção continua
  funcionando, só não puxa do catálogo). **Não** abrimos um segundo endpoint de leitura sem gate (seria
  furo de produto/segurança). Mora em: Front (detecta 403/feature off e cai no modo manual) + Back
  (gate já existente). Validada em: front (fallback) + back (403).
- **R5 — Snapshot obrigatório no item selecionado**: ao adicionar um procedimento do catálogo, gravar no
  `ConteudoJson` `{ catalogoCirurgiaId, descricao, valor, observacao }`. `descricao` e `valor` são
  **cópia** do catálogo no momento da indicação. A evolução é imutável: alterações posteriores no catálogo
  **não** retroagem ao que foi salvo. Mora em: Front (monta o objeto ao salvar a evolução). Validada em:
  front. O back de evolução já persiste `ConteudoJson` opaco — não valida o conteúdo da seção.
- **R6 — Imutabilidade da evolução preservada**: a F3 **não** muda o fluxo append-only de
  `ProntuarioEvolucao`. Evolução salva nunca é reescrita; nova consulta = nova evolução. A seção só muda o
  *formato* do que vai dentro do `ConteudoJson` da evolução nova. Mora em: Domain (já existente, intocado).
- **R7 — Retrocompat / discriminador**: o `ConteudoJson` da seção `procedimentos-indicados` passa a
  aceitar **dois formatos** por item, distinguidos pela presença de `catalogoCirurgiaId`:
  - **Legado (texto livre)**: `{ descricao, observacao }` sem `catalogoCirurgiaId`.
  - **Novo (catálogo)**: `{ catalogoCirurgiaId, descricao, valor, observacao }`.
  A propriedade `observacoes` (observações gerais da seção) é mantida como está. Ao renderizar, item
  com `catalogoCirurgiaId` ausente → exibe como texto livre read-only; presente → exibe chip com valor.
  Mora em: Front (parse tolerante na seção). Validada em: front.
- **R8 — Nada de regra de negócio no front sem espelho**: a única validação de negócio nova
  (nome+valor obrigatórios no mini-form) já tem espelho no `CatalogoCirurgia.Validar` do back
  (`Descrição obrigatória`, `Valor não pode ser negativo`). Front faz UX, back é fonte da verdade (422).

## 5. Modelo de dados

**Sem migration.** `ProntuarioEvolucao.ConteudoJson` é `jsonb` e armazena o objeto da seção de forma opaca
ao banco. O novo formato de item cabe no mesmo campo. A referência ao catálogo é guardada **como dado
dentro do JSON** (`catalogoCirurgiaId` + snapshot), não como FK relacional — isso é intencional: FK
quebraria a imutabilidade quando o item do catálogo for removido, e o snapshot já preserva o valor/descrição.

Formato do `ConteudoJson["procedimentos-indicados"]` após a F3:
```jsonc
{
  "procedimentos": [
    // novo (catálogo):
    { "catalogoCirurgiaId": 123, "descricao": "Infiltração articular", "valor": 350.00, "observacao": "joelho D" },
    // legado (coexiste; sem catalogoCirurgiaId):
    { "descricao": "Curativo simples", "observacao": "" }
  ],
  "observacoes": "..."
}
```
- **Tabelas afetadas**: nenhuma (sem DDL).
- **Multi-tenant**: o catálogo lido/criado já carrega `estabelecimento_id` na origem (query/command).
- **LGPD/PII**: o snapshot guarda só `descricao` (clínica, não PII de identificação) e `valor`. Não
  introduz novo dado pessoal. Nenhum novo endpoint que exponha PII. Sem novo audit (o acesso à evolução
  já é auditado pelo fluxo de prontuário existente; a F3 não cria nova porta de leitura de paciente).

**→ DB agent: não há schema para esta fase. Nenhuma migration.**

## 6. UX e fluxo

Referência visual: `Docs/Roadmap/prototipacao-financeiro/design-handoff/` — `Prontuario.html`,
`components/ProntuarioModules.jsx` (`IndicatedProcModule`), screenshots `02-pront-catalog.png` (seletor
com resultado), `02-pront-noresult.png` (busca sem resultado + CTA criar), `pront-legacy.png` (render
legado read-only). Recriar o **resultado visual** com o design system Vue (`components/ui/`), tokens
tipográficos de `main.css` (CLAUDE.md §5) — não copiar a estrutura React do protótipo.

Wireframe textual (modo edição, profissional com permissão):
```
┌ Procedimentos indicados ────────────────────────────────┐
│ [ 🔍 Buscar procedimento do catálogo...            ]    │  ← AppInput + busca
│  ▸ resultados (lista de itens do catálogo, valor à dir.) │     (debounce decidido em D1)
│     Infiltração articular ............. R$ 350,00  [+]   │
│     Drenagem .......................... R$ 120,00  [+]   │
│  ── ou ──  [ + Criar procedimento ]  (só com permissão)  │
├ Selecionados ───────────────────────────────────────────┤
│  ● Infiltração articular  R$ 350,00                      │
│      obs: [ joelho D                         ]   [✕]     │  ← chip/linha + observação
├ Observações gerais ─────────────────────────────────────┤
│  [ textarea                                          ]   │
└──────────────────────────────────────────────────────────┘
```

Mini-form inline "+ Criar procedimento" (expande no lugar; não é modal pesado):
```
Nome*   [ _______________________ ]
Valor*  [ R$ ____ ]   Duração (min) [ ___ ]
[ Cancelar ]  [ Criar e adicionar ]
```

Estado **busca-sem-resultado** (`02-pront-noresult.png`): mostra "Nenhum procedimento encontrado para
\"<termo>\"" e o CTA destacado `+ Criar "<termo>"` (pré-preenche o nome do mini-form com o termo buscado).

Estados de UI:
- **loading**: skeleton/placeholder enquanto carrega a lista do catálogo (1 fetch por abertura da seção).
- **vazio (catálogo)**: catálogo sem nenhum procedimento → texto "Catálogo de procedimentos vazio" + CTA criar.
- **vazio (selecionados)**: "Nenhum procedimento indicado ainda." (mantém comportamento atual).
- **sem resultado de busca**: ver acima (destaca criar).
- **erro de carga**: AppEmptyState/aviso com retry; não derruba as outras seções.
- **sem permissão de catálogo** (R4/D4): seção cai no modo manual texto-livre (sem busca, sem criar).
- **sucesso de criação inline**: item entra em "Selecionados" e a busca limpa; toast discreto opcional.
- **readOnly** (ver evolução / consulta anterior / PDF): chips e textos sem editar, sem busca, sem criar.

Mobile-ready: reusar o grid responsivo já presente na seção (linha colapsa em coluna < 768px).
Atalho de teclado: Enter no campo de busca foca o primeiro resultado; Esc fecha o mini-form.

## 7. Critérios de aceite (testáveis)

- **CA43** (caminho feliz — selecionar do catálogo): Dado um profissional com acesso ao catálogo editando
  a seção Procedimentos indicados, Quando busca e clica em "+" num item do catálogo, Então o item entra em
  "Selecionados" como chip com descrição e valor do catálogo, e ao salvar a evolução o `ConteudoJson` grava
  `{ catalogoCirurgiaId, descricao, valor, observacao }` para esse item.

- **CA44** (observação opcional): Dado um procedimento selecionado, Quando o profissional digita uma
  observação e deixa o campo padrão de outro item vazio, Então ambos são salvos — o vazio com `observacao: ""`
  — sem erro e sem exigir preenchimento.

- **CA45** (criação inline + auto-seleção): Dado um profissional com permissão de catálogo que busca um
  termo inexistente, Quando aciona "+ Criar procedimento", preenche nome e valor e confirma, Então é feita
  **uma** chamada a `POST api/orcamentos/configuracoes/procedimentos`, o item é criado no catálogo do tenant
  ativo e já aparece selecionado na seção, sem recarregar a página.

- **CA46** (multi-tenant — leitura): Dado um usuário do estabelecimento B, Quando abre a seção no
  estabelecimento ativo B, Então o seletor lista **apenas** procedimentos de B; nenhum item de A aparece,
  mesmo que A tenha catálogo maior. (Reusa o filtro de tenant da `ListarCatalogoCirurgiasQuery`.)

- **CA47** (multi-tenant — criação): Dado o estabelecimento ativo B, Quando o usuário cria um procedimento
  inline, Então o registro nasce com `estabelecimento_id = B` (resolvido do claim no back), e o front não
  envia `estabelecimentoId` no payload.

- **CA48** (RBAC — criação oculta sem permissão): Dado um profissional **sem** a permissão do catálogo de
  orçamento (`orcamento.configurar` / papel ≠ Dono/Recepção / feature `OrcamentoCompleto` off), Quando abre
  a seção, Então o botão "+ Criar procedimento" **não** é renderizado.

- **CA49** (RBAC — back 403): Dado esse mesmo profissional sem permissão, Quando o cliente chama
  `POST .../procedimentos` diretamente, Então o back responde **403** e nenhum item é criado.

- **CA50** (degradação graciosa sem acesso ao catálogo — R4/D4): Dado um profissional cujo
  estabelecimento não tem `OrcamentoCompleto` ou que não tem `orcamento.configurar`, Quando o `GET
  .../procedimentos` retorna 403/feature-off, Então a seção exibe o modo **manual texto-livre**
  (`{descricao, observacao}`) e permanece utilizável, sem busca nem botão criar, e sem mensagem de erro
  técnico exposta.

- **CA51** (imutabilidade da evolução): Dado um procedimento indicado salvo numa evolução, Quando o item
  correspondente do catálogo é depois editado (valor) ou inativado, Então ao reabrir a evolução salva o
  chip ainda exibe a **descrição e o valor do snapshot** (não o valor atual do catálogo), e a evolução não
  é reescrita.

- **CA52** (retrocompat — render legado): Dada uma evolução antiga cuja seção tem itens texto-livre
  (`{descricao, observacao}` sem `catalogoCirurgiaId`), Quando é aberta em modo read-only (Ver / Consulta
  anterior), Então os itens renderizam como linhas de texto sem valor e sem quebra de layout, lado a lado
  com itens do formato novo se houver.

- **CA53** (estados de UI — sem resultado): Dada uma busca sem correspondência no catálogo, Quando os
  resultados retornam vazios, Então a seção mostra "Nenhum procedimento encontrado para \"<termo>\"" e o CTA
  `+ Criar "<termo>"` com o nome pré-preenchido (CTA só visível com permissão; senão, só a mensagem).

- **CA54** (performance — 1 carga, sem N+1): Dado o catálogo do estabelecimento, Quando a seção é aberta,
  Então o catálogo é carregado **uma única vez** por abertura da seção (ou abertura da evolução), a busca
  opera sobre a lista carregada/ com debounce conforme D1, e renderizar N procedimentos selecionados **não**
  dispara N requisições (usa o snapshot do `ConteudoJson`).

- **CA55** (LGPD — sem PII em erro): Dado um erro ao criar/listar procedimento, Quando o back retorna
  422/403/500, Então a mensagem exibida é genérica (ex.: "Não foi possível criar o procedimento.") e não
  contém PII nem detalhe técnico do tenant.

- **CA56** (validação espelhada do mini-form): Dado o mini-form de criação, Quando o usuário tenta criar
  com nome vazio ou valor negativo, Então o front bloqueia com mensagem de campo, **e** se a request chegar
  ao back ela é rejeitada com 422 (`BusinessException` de `CatalogoCirurgia.Validar`).

- **CA57** (regressão — demais seções intactas): Dado o prontuário com suas 17 seções, Quando a F3 altera
  só `procedimentos-indicados`, Então as outras seções (HPP, exame físico, exames, conduta etc.) renderizam
  e salvam exatamente como antes (chave/tipo/valor inalterados), tanto em edição quanto em read-only.

- **CA58** (regressão — PDF da evolução): Dada uma evolução com procedimentos do catálogo, Quando o PDF é
  gerado, Então a seção aparece com descrição (+ valor, se houver) e observação a partir do **snapshot**,
  sem chamada ao catálogo, e evoluções legadas continuam imprimindo o texto livre.

## 8. Riscos e dependências

- **Reuso vs. duplicação**: o serviço front (`orcamentoCatalogoService.listarProcedimentos` /
  `criarProcedimento`) e o endpoint back já existem. **Não** criar novo endpoint/serviço/DTO. Risco se o
  dev duplicar — vigiar no QA.
- **Gate de feature/permissão acoplado ao catálogo**: profissional sem `OrcamentoCompleto`/`orcamento.configurar`
  não lista nem cria. A degradação graciosa (D4/CA50) é o que evita que a seção quebre para esses usuários —
  é o ponto mais sutil da fase, validar com cuidado.
- **Imutabilidade**: tentação de re-resolver valor pelo `catalogoCirurgiaId` ao renderizar (live-link) —
  **proibido**, quebra a imutabilidade da evolução. Sempre snapshot. (CA51.)
- **Regressão do dispatcher de seções** (`SecaoProntuario.vue`): a chave `procedimentos-indicados` já é
  roteada; só o componente interno muda. Não tocar nas outras 16 ramificações.
- **Dependentes**: F3B (item "marcar procedimento realizado" do checklist lê estes procedimentos), F4
  (cobrança de procedimento usa `catalogoCirurgiaId` + valor do snapshot), F5 (orçamento pré-preenchido).
  Por isso o `catalogoCirurgiaId` no `ConteudoJson` é contrato — não renomear sem alinhar F4/F5.

## 9. Observações para execução

**Não-negociável**:
- Snapshot (`descricao`+`valor`) sempre gravado junto do `catalogoCirurgiaId` (imutabilidade).
- Reuso do endpoint/serviço de catálogo existente; zero endpoint paralelo.
- Discriminador por presença de `catalogoCirurgiaId`; formatos coexistem; legado nunca quebra.
- Tipografia por tokens (CLAUDE.md §5); design system primeiro (`components/ui/`).
- Sem migration; `ConteudoJson` é `jsonb`.

**Liberdade técnica do dev** (decidir o mais simples — ver D1):
- Estratégia de busca: client-side sobre a lista carregada uma vez **OU** remota com debounce
  (`useDebouncedRef`, ~300ms). Como o catálogo de procedimentos por estabelecimento tende a ser pequeno
  (dezenas, não milhares) e o `GET .../procedimentos` já devolve a lista completa filtrada por tenant, a
  **recomendação é client-side** sobre uma carga única (mais simples, sem novo endpoint de busca, satisfaz
  CA54). Usar `useDebouncedRef` apenas para suavizar o filtro do input, sem ir ao servidor a cada tecla.
- Forma do mini-form (inline expandido vs. AppDrawer leve) — preferir inline conforme protótipo.

**Acionar `imedto-database`?** Não. Sem schema nesta fase.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar nota curta na seção de prontuário/evolução documentando o
  **formato do `ConteudoJson` da seção `procedimentos-indicados`**: discriminador por `catalogoCirurgiaId`,
  snapshot `descricao`+`valor`, coexistência legado/novo, e a premissa de que a referência ao catálogo é
  guardada como dado no JSON (não FK) para preservar a imutabilidade da evolução. Mudança incremental,
  cirúrgica — não reescrever a seção.
- **`Docs/DESIGN.md`** — atualizar **somente se** o dev extrair um componente reutilizável novo para o
  design system (ex.: um seletor-com-busca-e-criar genérico). Se a implementação ficar contida em
  `SecaoProcedimentosIndicados.vue` reusando componentes existentes (`AppInput`, `AppButton`, etc.), **não**
  há delta de design system — o dev/QA decide na entrega e documenta se aplicável.
- **`Docs/LGPD.md`** — sem delta (não introduz novo PII, endpoint de PII, retenção ou audit novo).
- **`Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md`** — marcar F3 como "briefing escrito (2026-06-10_011)" no
  controle de status do roadmap (acompanhar padrão de F1/F2).

## 11. Decisões de corte (execução autônoma — registradas)

- **D1 — Estratégia de busca: client-side sobre carga única (recomendado), debounce só no input.**
  Motivo: catálogo por estabelecimento é pequeno; o `GET .../procedimentos` já entrega a lista completa
  filtrada por tenant; evita criar endpoint de busca paralelo e satisfaz "1 carga por abertura, sem N+1"
  (CA54). Decisão final de implementação fica com o dev, mas o briefing recomenda client-side.
- **D2 — Snapshot mínimo = `descricao` + `valor`** (não duração nem produtos vinculados). Motivo:
  minimização; F4/F5 re-resolvem produtos/insumos pelo `catalogoCirurgiaId` no momento da cobrança/orçamento.
  A duração não é necessária para indicar/render; fica fora do snapshot.
- **D3 — RBAC da criação inline = a mesma permissão do catálogo de orçamento (opção mais segura).**
  Motivo: criar item no catálogo do tenant é ato de configuração que repercute fora do prontuário (orçamentos,
  cobranças futuras). Reusar `[RequiresAcao("orcamento","configurar")]` + `[RequiresPapel(Dono,Recepcionista)]`
  já existente evita inventar permissão nova e fecha o furo de um profissional "criar catálogo" só por ter
  acesso ao prontuário. Front oculta o CTA; back garante 403.
- **D4 — Profissional sem acesso ao catálogo → degradação graciosa para texto livre** (não bloquear a seção).
  Motivo: o `GET .../procedimentos` está sob o mesmo gate; abrir um endpoint de leitura sem gate seria furo.
  Em vez disso, a seção continua usável no formato legado para quem não pode puxar do catálogo — preserva o
  trabalho clínico sem afrouxar segurança. Registrado como R4/CA50.
- **D5 — Referência guardada como dado no JSON, não FK relacional.** Motivo: imutabilidade da evolução; item
  do catálogo pode ser removido sem orfanizar/quebrar a evolução salva (o snapshot basta).
- **D6 — Sem migração de dados legados.** Formatos coexistem; evoluções antigas ficam read-only no formato
  texto-livre. Converter seria reescrever evolução imutável — proibido.
