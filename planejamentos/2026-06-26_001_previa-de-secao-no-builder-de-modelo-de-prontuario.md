# Prévia de seção no builder de modelo de prontuário

**ID**: 2026-06-26_001
**Status**: Aprovado por usuário em 2026-06-26
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (somente o builder de modelos — montagem do modelo). Nenhuma área de dados/permissionamento/financeiro/estoque.

## 1. Contexto e motivação

Ao montar um modelo de prontuário (Configurações → Modelos de prontuário → "Novo modelo"), o profissional escolhe quais das 17 seções entram no modelo (Queixa principal, HDA, Exame físico, Conduta, etc.). Hoje os chips de "Adicionar seção" mostram apenas o **label** e um `title` (tooltip nativo) com uma descrição curta (`info`). Isso não comunica **como a seção realmente aparece e funciona dentro do prontuário** — algumas seções são textareas simples, outras são telas estruturadas ricas (Exame físico com mapa corporal, Conduta com checklist, História pregressa com listas de alergias/medicações).

Hoje, para "ver" uma seção, o profissional precisa: montar o modelo → salvar → abrir um prontuário com esse modelo → navegar até a seção. Custo alto e tardio para uma decisão que deveria ser informada **antes de montar**.

Dor da persona (verbatim do usuário): *"para cada uma dessas seções do modelo do prontuário, quero poder ver uma prévia dessas seções sem ter que abrir o prontuário depois de ser montado, pois quero poder ver antes de montar."*

Solução: cada seção ganha um botão de "olho" (prévia). Ao clicar, abre um modal por cima com a seção **renderizada com dados de exemplo, em modo somente-leitura** — exatamente a UI que o profissional verá no prontuário, sem precisar montar nem salvar nada.

## 2. Persona-alvo

- **Primária**: Profissional (médico) ou Dono montando/editando um modelo de prontuário no painel do tenant (Configurações → Modelos de prontuário).
- **Secundária**: Administrador global montando modelos-padrão no painel admin (o builder `ModeloProntuarioBuilder.vue` é compartilhado entre tenant e admin — a prévia precisa funcionar nos dois contextos).
- **Momento da jornada**: configuração/setup do consultório (antes do atendimento). Frequência baixa, mas alto impacto na confiança da decisão.

## 3. Escopo

**Inclui**:
- Botão de "olho" (prévia) por seção, tanto nos **chips de "Adicionar seção"** (seções ainda não adicionadas) quanto nos **itens da lista "Seções do modelo"** (seções já adicionadas) — ver R2.
- Um **modal sobreposto** (componente do design system) que renderiza a seção escolhida com **dados de exemplo fictícios, em modo somente-leitura**.
- Reaproveitamento do dispatcher real `SecaoProntuario.vue` em modo `readOnly` para renderizar a prévia (mesma UI do prontuário).
- Um conjunto de **dados de exemplo por chave de seção** (string para seções de texto; objeto no shape esperado por cada componente estruturado), morando na fonte única ao lado do catálogo (`modeloProntuarioBuilder.ts`).
- Funcionamento idêntico no painel do tenant e no painel admin.
- Acessibilidade básica: fechar com ESC, foco gerenciado pelo modal do DS, botão de olho com `aria-label`/`title`.

**Não inclui**:
- Qualquer alteração no que é salvo no modelo. A prévia é só visualização — não adiciona, não remove, não reordena seção (ver R5). Para adicionar a seção, o usuário continua usando o chip "+" existente.
- Edição dos dados de exemplo pelo usuário (são estáticos e read-only).
- Prévia de seções **customizadas** (chaves fora do catálogo das 17). Customizadas não têm componente próprio nem exemplo — o botão de olho não aparece nelas (ver R6).
- Mudança de schema, endpoint, query, migration ou qualquer ida ao backend. **Feature 100% frontend.**
- Persistência de "qual seção foi pré-visualizada" ou telemetria.

## 4. Regras de negócio

- **R1 — Reuso obrigatório do dispatcher real**: a prévia renderiza a seção usando o componente existente `frontend/src/components/prontuario/SecaoProntuario.vue` com `:read-only="true"` e `:model-value="<dado de exemplo da chave>"`. **Proibido reimplementar a UI das seções** — a prévia tem que ser fiel ao prontuário real. Mora em: Front. Validada em: Front.

- **R2 — Botão de olho em dois lugares**: o ícone de prévia aparece (a) em cada chip de "Adicionar seção" e (b) em cada linha da lista "Seções do modelo" já adicionadas. Motivo: depois que o usuário adiciona uma seção, o chip dela some da lista de disponíveis; sem o olho na lista de adicionadas, ele perderia o acesso à prévia daquela seção. Mora em: Front (`ModeloProntuarioBuilder.vue`). Validada em: Front.

- **R3 — Dados de exemplo na fonte única**: o mapa `chave → exemplo` vive junto do catálogo, em `frontend/src/components/ui/modeloProntuarioBuilder.ts` (ex.: `export const EXEMPLOS_SECAO_MODELO: Record<string, unknown>`), mantendo a fonte única de verdade do builder. Seções de texto (`cid10`, e fallback `texto`/`texto_longo`) usam **string**; seções estruturadas usam **objeto** no shape exato que o componente filho espera (ver Seção 5). Mora em: Front. Validada em: Front.

- **R4 — Dados fictícios, sem PII real**: os exemplos são clinicamente plausíveis porém claramente fictícios (ex.: "Paciente refere dor abdominal há 3 dias…"). Não contêm dados de paciente real, nem nome/CPF/contato. Como a feature não toca backend nem audit, não há risco de PII em log/erro — mas o conteúdo de exemplo não pode parecer um caso real identificável. Mora em: Front. Validada em: Front.

- **R5 — Prévia não altera o modelo em construção**: abrir/fechar a prévia de qualquer seção **não** altera o estado do builder — não marca/desmarca seção, não muda a ordem, não emite `update:estruturaJson`/`update:nome`/`update:descricao`/`update:valido`. O modal é puramente de leitura sobre dados estáticos locais. Mora em: Front. Validada em: Front.

- **R6 — Só seções do catálogo têm prévia**: o botão de olho só aparece para as 17 chaves conhecidas (`SECOES_MODELO_PRONTUARIO`). Seções customizadas (chaves desconhecidas, já tratadas como `customizadas` no builder) não recebem botão de olho. Mora em: Front. Validada em: Front.

- **R7 — Estático, sem loading/erro de rede**: a prévia não dispara request. Os estados de loading e erro de rede **não se aplicam**. Único estado relevante: modal aberto vs. fechado (e a renderização imediata da seção). Mora em: Front. Validada em: Front.

## 5. Modelo de dados

**Nenhuma alteração de schema, tabela, coluna, índice ou migration.** Feature frontend-only.

Os "dados de exemplo" são constantes estáticas no front. Shapes a respeitar (extraídos dos componentes reais — o dev deve abrir cada `.vue` e conferir o shape completo antes de preencher; abaixo o essencial confirmado):

| Chave | Tipo do exemplo | Componente que renderiza | Shape essencial (campos-chave) |
|---|---|---|---|
| `queixa`, `hda`, `procedimento-consultorio`, `ficha-anestesica`, `equipe-cirurgica`, `fotos-paciente`, `anexos` | **string** | fallback `AppTextarea` (texto_longo) | texto livre |
| `cid10` | **string** | fallback `AppInput` (texto) | ex.: `"K35.8 — Apendicite aguda, outra e a não especificada"` |
| `hpp` | **objeto** | `SecaoHistoriaPregressa` | `{ alergiasTem, alergias: [{nome, observacao}], medicacoesTem, medicacoes: [{nome, dose, frequencia, motivo, observacoes}], cirurgias: [{nome, ano, observacao}], doencas: [{nome, observacao}], ... }` |
| `h-familiar` | **objeto** | `SecaoHistoriaFamiliar` | conferir shape no `.vue` |
| `h-social` | **objeto** | `SecaoHistoriaSocial` | conferir shape no `.vue` |
| `exame-fisico` | **objeto** | `SecaoExameFisico` | `{ paSistolica, paDiastolica, fc, fr, temperatura, spo2, glicemia, peso, altura, estadoGeral, consciencia, ..., regioes: [RegiaoAnatomicaSelecionada] }` — ver R-EXFIS abaixo |
| `exames-realizados` | **objeto** | `SecaoExamesRealizados` | conferir shape no `.vue` |
| `procedimentos-indicados` | **objeto** | `SecaoProcedimentosIndicados` | conferir shape no `.vue` |
| `evolucao-pos-op` | **objeto** | `SecaoEvolucaoPosOperatoria` | conferir shape no `.vue` |
| `desc-cirurgica` | **objeto** | `SecaoDescricaoCirurgica` | conferir shape no `.vue` |
| `conduta` | **objeto** | `SecaoCondutaChecklist` | `{ acoesMarcadas: string[], observacao: string }` |

**R-EXFIS (atenção do dev)**: `SecaoExameFisico` carrega o catálogo de regiões anatômicas do estabelecimento (tipo `ExameFisicoRegiao`) para desenhar o mapa corporal. Na prévia (read-only, sem contexto de prontuário/estabelecimento garantido), o componente **não pode quebrar** se esse catálogo não carregar. O exemplo deve focar em sinais vitais + 1-2 regiões já no formato `RegiaoAnatomicaSelecionada` (campos `id`, `nome`, etc., conforme a interface no `.vue`), e o `readOnly` deve impedir qualquer interação de seleção no mapa. Se houver risco de erro em runtime por catálogo ausente, o dev decide a abordagem técnica (ex.: o componente já tolera `regioes` preenchidas sem o catálogo carregado) — o critério é: **abrir a prévia do Exame físico nunca lança erro no console nem renderiza tela quebrada** (CA validável no QA com o app rodando).

## 6. UX e fluxo

**Onde**: dentro do `ModeloProntuarioBuilder.vue`, em dois pontos:
1. **Lista "Seções do modelo"** (`.mpb-ord-row`): cada linha já tem grip + número + nome + botão remover (X). Adicionar um botão de olho antes/junto ao X.
2. **Chips "Adicionar seção"** (`.mpb-chip-add`): cada chip tem `+ {label}`. O olho aqui precisa ser um alvo de clique **separado** do clique no chip (clicar no corpo do chip continua adicionando a seção; clicar no olho abre a prévia, sem adicionar). Decisão técnica do dev sobre a marcação (ex.: botão de olho adjacente ao chip, ou ícone interno com `@click.stop`), respeitando R5 (prévia não adiciona).

**Wireframe textual do modal de prévia**:
```
┌────────────────────────────────────────────────┐
│  Prévia — {label da seção}                  [X] │   ← cabeçalho do modal DS
├────────────────────────────────────────────────┤
│  [aviso sutil] Exemplo somente leitura — não    │
│  altera o modelo.                                │
│                                                  │
│  <SecaoProntuario readOnly :modelValue=exemplo>  │   ← UI real da seção
│   (ex.: Exame físico com sinais vitais + mapa,   │
│    Conduta com checklist marcado, etc.)          │
│                                                  │
├────────────────────────────────────────────────┤
│                                       [ Fechar ] │
└────────────────────────────────────────────────┘
```

- **Componentes do DS**: usar `AppModal` (`frontend/src/components/ui/AppModal.vue`) para o sobreposto. O botão de olho usa `AppButton` (variante ícone/ghost) ou o padrão de ícone do projeto (FontAwesome `fa-eye`, coerente com os ícones já usados no builder — `fa-plus`, `fa-xmark`, `fa-grip-vertical`). Não criar modal/popover novo do zero.
- **Estados**:
  - **Aberto**: modal com a seção renderizada. Conteúdo sempre presente (estático) — sem skeleton/spinner.
  - **Fechado**: estado default.
  - **Loading / erro de rede**: **não se aplica** (R7) — registrar isso explicitamente no QA como "N/A".
  - **Vazio**: não se aplica — todo exemplo é preenchido.
- **Tipografia**: respeitar §5 do CLAUDE.md — nenhum `font-size`/`font-weight` literal em CSS scoped novo; usar tokens. O título do modal segue o padrão do `AppModal`; o aviso "somente leitura" usa classe/tokens do DS.
- **Acessibilidade**: fechar com **ESC** e com o botão de fechar/Fechar do `AppModal`. Botão de olho com `aria-label="Pré-visualizar seção {label}"` (e `title` para tooltip). Foco gerenciado pelo `AppModal` (conferir se já faz focus-trap/restore — se sim, herdar; não reinventar).
- **Mobile/responsivo**: o modal do DS já é responsivo. Seções grandes (Exame físico) podem exigir scroll interno no corpo do modal — garantir que o corpo rola e o cabeçalho/rodapé do modal ficam fixos (comportamento padrão do `AppModal`; só validar).

## 7. Critérios de aceite (testáveis)

- **CA1 (olho na lista de adicionadas)**: Dado um modelo com a seção "Exame físico" já adicionada, Quando o usuário olha a linha dessa seção em "Seções do modelo", Então vê um botão de olho (prévia) junto ao botão de remover.

- **CA2 (olho nos chips disponíveis)**: Dado que a seção "Conduta" ainda não foi adicionada, Quando o usuário olha o chip "Conduta" em "Adicionar seção", Então há um alvo de prévia (olho) distinto do corpo do chip.

- **CA3 (abrir prévia pelo chip não adiciona)**: Dado o chip "Conduta" não adicionado, Quando o usuário clica no olho do chip, Então abre o modal de prévia E a seção "Conduta" **não** é adicionada ao modelo (continua na lista de disponíveis; o contador "N seções" não muda).

- **CA4 (caminho feliz — seção de texto)**: Dado o usuário clica no olho de "Queixa principal (QP)", Quando o modal abre, Então a seção é renderizada como textarea somente-leitura preenchida com o texto de exemplo, e o campo está desabilitado (não editável).

- **CA5 (estruturada — Exame físico com mapa, sem quebrar)**: Dado o usuário clica no olho de "Exame físico", Quando o modal abre, Então a seção renderiza sinais vitais e o mapa corporal com a(s) região(ões) de exemplo em modo somente-leitura, **sem erro no console** e sem tela quebrada, mesmo que o catálogo de regiões do estabelecimento não esteja disponível (R-EXFIS).

- **CA6 (estruturada — Conduta checklist)**: Dado o usuário clica no olho de "Conduta", Quando o modal abre, Então a seção renderiza o checklist com itens marcados de exemplo e a observação de exemplo, todos em modo somente-leitura (checkboxes não respondem ao clique).

- **CA7 (estruturada — História pregressa com listas)**: Dado o usuário clica no olho de "História pregressa (HPP)", Quando o modal abre, Então renderiza as listas (alergias/medicações/cirurgias/doenças) com os itens de exemplo em modo somente-leitura.

- **CA8 (read-only de verdade)**: Dado qualquer prévia aberta, Quando o usuário tenta digitar/clicar/alterar qualquer campo da seção renderizada, Então nada é editável (inputs `disabled`, sem mutação) — a prévia é estritamente de leitura.

- **CA9 (não altera o modelo em construção)**: Dado um modelo em montagem com nome "X" e 3 seções na ordem A,B,C, Quando o usuário abre e fecha a prévia de qualquer seção (adicionada ou disponível), Então nome, seções selecionadas, ordem e validade do modelo permanecem idênticos (nenhum `update:*` disparado pela prévia).

- **CA10 (fechar — botão e ESC)**: Dado uma prévia aberta, Quando o usuário clica em "Fechar"/X ou pressiona ESC, Então o modal fecha e o foco volta ao builder; reabrir outra prévia funciona normalmente.

- **CA11 (cobertura de todas as 17 seções)**: Dado cada uma das 17 chaves do catálogo, Quando o usuário abre a prévia de cada uma, Então cada seção renderiza seu conteúdo de exemplo sem erro de console e sem campo quebrado (textarea, input ou componente estruturado conforme a chave).

- **CA12 (sem prévia em customizada)**: Dado um modelo que contém uma seção customizada (chave fora do catálogo), Quando o usuário visualiza essa seção na lista, Então ela **não** exibe botão de olho (R6).

- **CA13 (funciona no admin)**: Dado o painel admin usando o mesmo `ModeloProntuarioBuilder.vue` para montar um modelo-padrão, Quando o admin clica no olho de qualquer seção, Então a prévia abre e funciona igual ao tenant (mesmo comportamento, mesmos exemplos).

- **CA14 (RBAC — N/A explícito)**: A feature não cria novo endpoint nem nova ação de domínio; o acesso ao builder de modelos já é controlado pela permissão existente de Modelos de prontuário (`modelos_prontuario`). Nada novo a permissionar. Registrar no QA como "RBAC N/A — sem endpoint/ação nova; acesso herdado da tela de Modelos".

- **CA15 (multi-tenant / LGPD — N/A explícito)**: A prévia usa dados fictícios estáticos no front, sem request ao backend, sem PII real, sem audit. Não há vazamento cross-tenant possível. Registrar no QA como "Multi-tenant/LGPD N/A — sem ida ao backend, dados de exemplo fictícios (R4)".

- **CA16 (tipografia — gate)**: Dado o CSS scoped novo introduzido, Quando o QA roda `npm run check:typography -- --ci`, Então passa sem novos literais de `font-size`/`font-weight` (§5 CLAUDE.md).

## 8. Riscos e dependências

- **Risco 1 — Exame físico depende do catálogo de regiões**: o maior risco técnico. `SecaoExameFisico` foi feito para o contexto de prontuário (carrega `ExameFisicoRegiao` do estabelecimento). Na prévia esse contexto pode não existir. Mitigação: dado de exemplo com `regioes` já materializadas + `readOnly`, e CA5 valida ausência de erro. Se o componente exigir o catálogo carregado para não quebrar, o dev reporta — não force.
- **Risco 2 — Shapes de objeto divergentes**: cada componente estruturado tem interface própria. Exemplo com shape errado renderiza vazio ou lança erro. Mitigação: dev confere a `interface` no topo de cada `.vue` antes de preencher o exemplo; CA11 cobre todas as 17.
- **Risco 3 — Clique no olho do chip acionando o "adicionar"**: o chip inteiro é um `<button>` que adiciona a seção. O olho precisa de `@click.stop` ou estar fora do botão. CA3 valida.
- **Dependência**: nenhuma de backend/DB. Depende apenas de `AppModal` e `SecaoProntuario.vue` existentes (ambos confirmados no repo).
- **Regressão a vigiar**: o builder emite `update:estruturaJson` via watch profundo em `secoesAtivas`/`customizadas`. A prévia não pode tocar esses refs (R5/CA9). Vigiar que nenhum estado reativo do builder seja mutado pela abertura do modal.

## 9. Observações para execução

- **Não-negociável**:
  - Reusar `SecaoProntuario.vue` em `readOnly` (R1) — proibido reimplementar UI de seção.
  - Dados de exemplo na fonte única `modeloProntuarioBuilder.ts` (R3) — não espalhar pelo componente.
  - Prévia não altera o modelo (R5/CA9).
  - Usar `AppModal` do DS — não criar modal novo.
  - Tipografia por tokens (§5).
- **Liberdade técnica do dev**:
  - Marcação exata do botão de olho no chip (ícone interno com `@click.stop` vs. botão adjacente) — desde que CA2/CA3 passem.
  - Como passar `pacienteSexo` ao `SecaoProntuario` na prévia (pode ser um valor de exemplo fixo, ex.: feminino ou masculino, só para o mapa escolher a silhueta) — não é regra de negócio.
  - Onde guardar o estado `secaoEmPrevia` (ref local no builder) e como passar para o `AppModal`.
- **Confira antes de codar** (reuso > duplicação): abrir cada `.vue` em `frontend/src/components/prontuario/secoes/` e copiar o shape exato da `interface` para o exemplo. Não inventar campos.
- **Validação local (QA)**: como é frontend-only e estático, o QA valida com o app rodando (`./dev.sh`) abrindo o builder em Configurações → Modelos de prontuário → Novo modelo, e repetindo no painel admin. Não há smoke de job/banco. Confirmar no console que abrir as 17 prévias (CA11) e especialmente o Exame físico (CA5) não gera erro.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar uma nota curta na seção de padrões/componentes do design system registrando o padrão **"prévia de seção do prontuário"**: o builder de modelos (`ModeloProntuarioBuilder.vue`) oferece um botão de olho que abre um `AppModal` renderizando `SecaoProntuario.vue` em `readOnly` com **dados de exemplo estáticos** (mapa `EXEMPLOS_SECAO_MODELO` em `modeloProntuarioBuilder.ts`, fonte única ao lado do catálogo das 17 seções). Mencionar que é o padrão para "pré-visualizar UI estruturada sem contexto real". Mudança incremental, surgical — não reescrever o doc. Responsável pela edição: `imedto-developer` durante a implementação; `imedto-qa` valida que a nota foi adicionada (CA de doc).
- Demais docs (`ARQUITETURA.md`, `INFRA.md`, `COMANDOS.md`, `LGPD.md`): **nenhuma atualização** — sem novo bounded context, sem mudança de infra/comando, sem novo dado pessoal/endpoint/audit (feature frontend-only com dados fictícios).
