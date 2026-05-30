# Wave 5 — Admin global: builder visual de modelos de prontuário (extrair componente compartilhado)

**ID**: 2026-05-30_005
**Status**: Aprovado por usuário em 2026-05-30
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (extração de componente + 2 refatorações + retrocompat)
**Áreas regressivas tocadas**: configurações do tenant (`/configuracoes/modelos-prontuario`), admin global (`/admin/modelos`), nenhuma mudança em backend/schema
**Próximo agente**: `imedto-developer`

**Referências**:
- Wave 1 — `planejamentos/2026-05-30_001_admin-global-mvp.md`
- Wave 2 — `planejamentos/2026-05-30_002_admin-global-wave2.md`
- Wave 3 — `planejamentos/2026-05-30_003_admin-global-wave3-redesign.md`
- Wave 4 — `planejamentos/2026-05-30_004_admin-global-wave4-catalogos-livelink.md` (entregou CRUD admin de `modelo_de_prontuario` com `EhPadraoSistema=true` e live-link tenant)

---

## 1. Contexto e feedback literal do usuário

A Wave 4 destravou o admin global a editar modelos de prontuário padrão do sistema (gravados na mesma tabela `modelo_de_prontuario` que o tenant usa, com `EhPadraoSistema=true`, refletindo no tenant em tempo real). Porém, o formulário admin (`ModelosGlobaisFormView.vue`) ainda usa **textarea de JSON cru** — o admin precisa colar manualmente um array `[{ chave, titulo, tipo, ordem }]`, sujeito a erro de digitação e desalinhamento com o catálogo das 17 seções suportadas pelo prontuário.

O tenant, por outro lado, em `frontend/src/views/configuracoes/ModelosProntuarioView.vue`, já tem um **builder visual maduro**: lista de 17 seções predefinidas (queixa, HDA, HPP, h-familiar, h-social, exame-fisico, exames-realizados, procedimentos-indicados, evolucao-pos-op, desc-cirurgica, procedimento-consultorio, ficha-anestesica, equipe-cirurgica, fotos-paciente, anexos, cid10, conduta) com checkboxes para ativar, reorder via botões ↑/↓ e geração automática do `estruturaJson`.

> Feedback literal do usuário:
> "para a parte de modelo de prontuario, faça que a mudança dos itens padrões sejam com o mesmo design que o da plataforma normal, para que eu possa alterar de forma visual mais facil, e nao por tag q pode ter algum erro"

A demanda é cirúrgica: **dar ao admin o mesmo builder visual do tenant**, extraindo-o em componente compartilhado de design system (não duplicar markup), com retrocompatibilidade total para modelos já criados em Wave 4 via JSON manual.

---

## 2. Objetivo

Admin global edita modelos de prontuário padrão do sistema com o **mesmo builder visual usado pelo tenant** — extrair o builder em componente compartilhado `frontend/src/components/ui/ModeloProntuarioBuilder.vue`, refatorar tenant e admin para consumirem o mesmo componente, mantendo zero regressão funcional e retrocompatibilidade com modelos legados.

---

## 3. Escopo

### 3.1 Inclui
- Criar `frontend/src/components/ui/ModeloProntuarioBuilder.vue` extraindo o builder visual hoje hardcoded em `ModelosProntuarioView.vue` (tenant).
- Exportar do mesmo arquivo (ou de `frontend/src/components/ui/modeloProntuario/` com index re-export):
  - Constante `SECOES_MODELO_PRONTUARIO` (array com as 17 seções: `{ key, label, tipo, info }`).
  - Tipo `SecaoBuilderItem` (definição de uma seção do catálogo).
  - Helpers puros `gerarEstruturaJson(secoesAtivasOrdenadas)` e `parsearEstruturaJson(estruturaJson)`.
- Registrar o componente no barrel `frontend/src/components/ui/index.ts`.
- Refatorar `frontend/src/views/configuracoes/ModelosProntuarioView.vue` (tenant) para usar o componente — remover o markup duplicado, remover `secoesList` local, remover `sincronizarOrdem`/`moverSecao`/`emptySecoes` quando estiverem dentro do componente.
- Refatorar `frontend/src/modules/admin/views/ModelosGlobaisFormView.vue` (admin) para substituir o textarea de JSON pelo componente, mantendo o campo "Motivo da alteração" (≥10 chars) separado.
- Retrocompatibilidade: ao editar modelo existente cuja `estruturaJson` foi escrita à mão no Wave 4, o componente parseia, marca as seções correspondentes do catálogo e preserva seções customizadas (chaves fora das 17 conhecidas).
- Aviso visível no componente quando o JSON recebido contém seções desconhecidas: "Este modelo tem N seções customizadas que serão preservadas ao salvar."
- Atualizar `Docs/DESIGN.md` com nota sobre o componente compartilhado.

### 3.2 NÃO inclui
- Mudança de contrato no backend (endpoints `/prontuario/modelos` e `/admin/global/catalogos/modelos-prontuario` continuam recebendo `estruturaJson` como string).
- Mudança em schema (`modelo_de_prontuario.estrutura_json` permanece `text`/`jsonb` conforme está; zero migration).
- Drag-and-drop (continua botões ↑/↓ — match com tenant atual).
- Adicionar novas seções ao catálogo das 17 (qualquer mudança no catálogo é briefing separado).
- Editor para o admin criar seções customizadas com chaves novas (admin usa só as 17; seções customizadas legadas são preservadas mas não editáveis pelo builder).
- Templates de texto pré-preenchidos por seção (extensão futura).
- Mudança no live-link Wave 4 (continua funcionando — só muda como o JSON é gerado no front).
- MFA TOTP, impersonate, painel `/admin/global` adicional.

---

## 4. Decisões cravadas (sem nova pergunta ao usuário)

| # | Decisão | Justificativa |
|---|---|---|
| D1 | Componente vai em `frontend/src/components/ui/ModeloProntuarioBuilder.vue` | Já é o local permitido para o módulo admin importar (isolamento físico do admin preservado — admin importa de `components/ui/` mas não acessa o `configuracoes/`). |
| D2 | Catálogo das 17 seções DENTRO do componente, exportado como `SECOES_MODELO_PRONTUARIO` | Fonte única de verdade. Tenant e admin importam da mesma constante. |
| D3 | Shape do JSON gerado continua **idêntico** ao do tenant hoje: array `[{ chave, titulo, tipo, ordem }]` (sem envelope `{ secoes: [...] }`) | Zero breaking change no backend, em `Evolucao.modeloSnapshot`, em `ProntuarioResumo.modeloEstrutura`. O placeholder enganoso no admin (`{ "secoes": [] }`) será corrigido. |
| D4 | Reorder por botões ↑/↓ (não drag-and-drop) | Match exato com tenant atual. Drag-and-drop fica como extensão futura — não introduz divergência visual nesta Wave. |
| D5 | Contrato do componente: `v-model:nome` + `v-model:descricao` + `v-model:estruturaJson` (string) | Mantém o componente neutro, sem conhecer payloads de admin (`motivo`) ou tenant. O wrapper de cada view monta seu próprio payload. `estruturaJson` como string deixa o componente ser fonte de verdade do JSON gerado e simplifica o parsing inicial. |
| D6 | Componente **opcionalmente** aceita prop `:mostrarNomeDescricao` (default `true`) | Permite ao admin esconder se preferir layout próprio (não vamos esconder nesta Wave, mas mantém flexibilidade). Tenant e admin nesta Wave usam `true`. |
| D7 | Seções customizadas (chaves fora das 17) são preservadas mas não editáveis no UI | Retrocompat para modelos Wave 4 escritos à mão. Componente mostra aviso e mantém o array original dessas seções, costurando-as ao salvar. |
| D8 | Aviso de seções customizadas usa `AppCallout` (ou markup leve com classe `--info` se `AppCallout` não existir hoje) | Reuso do design system; dev confere componente disponível e escolhe o mais próximo. |
| D9 | Validação backend permanece a mesma (Wave 4 já valida JSON parseável) | Builder visual sempre gera JSON válido, então a validação atual é satisfeita por construção. |
| D10 | Audit + motivo ≥10 chars no admin continua valendo, no campo separado do builder | Mesmo padrão Wave 4. |
| D11 | Tenant **não** usa motivo (não faz parte do contrato tenant) | Builder não exige motivo — campo motivo é responsabilidade da view admin, fora do componente. |

---

## 5. Arquitetura proposta (alto nível)

### 5.1 Estrutura de arquivos

```
frontend/src/components/ui/
  ModeloProntuarioBuilder.vue        ← novo componente compartilhado
  index.ts                            ← registrar barrel

frontend/src/views/configuracoes/
  ModelosProntuarioView.vue           ← refatorado para usar o componente

frontend/src/modules/admin/views/
  ModelosGlobaisFormView.vue          ← refatorado: textarea JSON → componente
```

### 5.2 API do componente

**Props/v-model**:
- `v-model:nome: string` — nome do modelo.
- `v-model:descricao: string` — descrição opcional.
- `v-model:estruturaJson: string` — JSON serializado das seções ativas + ordem (formato idêntico ao tenant hoje).
- `:mostrarNomeDescricao?: boolean = true` — esconde inputs de nome/descrição se a view-pai já cuida.
- `:disabled?: boolean = false` — bloqueia interação enquanto salvando.

**Emits**:
- `update:nome`, `update:descricao`, `update:estruturaJson` — implícitos do `v-model`.
- `update:valido` (boolean) — true quando há pelo menos uma seção ativa e nome não-vazio. Permite à view-pai habilitar/desabilitar submit.

**Comportamento interno**:
1. Ao montar, parseia `estruturaJson` recebido. Para cada item:
   - Se `chave` está em `SECOES_MODELO_PRONTUARIO`: marca o checkbox correspondente, adiciona à `ordem`.
   - Se `chave` é desconhecida: guarda em `secoesCustomizadas[]` interno, mostra aviso.
2. Watcher em `secoes` (record de checkboxes) + `ordem` (array) sincroniza ordem ao marcar/desmarcar (lógica idêntica ao `sincronizarOrdem` do tenant atual).
3. Sempre que muda, emite novo `estruturaJson` via `update:estruturaJson` — concatenando seções ativas conhecidas (na ordem do usuário) com seções customizadas legadas (no fim, preservando ordem original).
4. Se `estruturaJson` recebido (prop) está vazio ou inválido, componente arranca limpo (todos checkboxes false, ordem vazia) sem erro.

**Helpers exportados (mesmo arquivo)**:
```ts
export const SECOES_MODELO_PRONTUARIO: SecaoBuilderItem[] = [...]
export interface SecaoBuilderItem { key: string; label: string; tipo: "texto" | "texto_longo"; info: string }
export function parsearEstruturaJson(json: string): { conhecidas: SecaoModelo[]; customizadas: SecaoModelo[] }
export function gerarEstruturaJson(secoesAtivas: SecaoModelo[], customizadas: SecaoModelo[]): string
```

`SecaoModelo` continua sendo a interface já exportada em `services/prontuarioService.ts` (`{ chave, titulo, tipo, ordem }`) — componente importa de lá para evitar duplicação.

### 5.3 Refatoração do tenant (`ModelosProntuarioView.vue`)

- Remover `secoesList`, `emptySecoes`, `sincronizarOrdem`, `moverSecao` (vão para o componente).
- Remover blocos de markup `.secoes-bloco`, `.secoes-grid`, `.ordem-bloco`, `.ordem-lista` + CSS associado.
- `form.secoes` e `form.ordem` somem do `FormState` — o componente é dono. `FormState` fica com `id`, `nome`, `descricao`, `estruturaJson`.
- `preencherForm(modelo)` apenas seta `estruturaJson` (do `JSON.stringify(modelo.estrutura)`) — componente parseia.
- `salvar()` usa `form.estruturaJson` diretamente (já é a string).
- Botão "Salvar" desabilita com base em `valido` emitido pelo componente.

### 5.4 Refatoração do admin (`ModelosGlobaisFormView.vue`)

- Remover `estruturaJson` textarea, `formatarJson`, `validarJson`.
- Renderizar `<ModeloProntuarioBuilder v-model:nome=... v-model:descricao=... v-model:estruturaJson=... @update:valido="..."/>`.
- Manter `AppField` "Motivo da alteração" com validação ≥10 chars **fora** do componente.
- `validar()` simplifica para `nome.trim() && motivo.trim().length >= 10 && builderValido`.
- Payload de submit continua `{ nome, descricao, estruturaJson, motivo }`.

---

## 6. Critérios de Aceite (Dado/Quando/Então)

### W5-CA1 — Componente compartilhado criado
**Dado** que a Wave 5 foi entregue,
**Quando** se inspeciona `frontend/src/components/ui/ModeloProntuarioBuilder.vue`,
**Então** o arquivo existe, exporta `SECOES_MODELO_PRONTUARIO` (array de 17 seções com chaves idênticas à `secoesList` do tenant pré-Wave-5: `queixa, hda, hpp, h-familiar, h-social, exame-fisico, exames-realizados, procedimentos-indicados, evolucao-pos-op, desc-cirurgica, procedimento-consultorio, ficha-anestesica, equipe-cirurgica, fotos-paciente, anexos, cid10, conduta`), está registrado em `components/ui/index.ts`, e usa apenas componentes do design system (`AppField`/inputs/etc) — sem markup HTML solto além do mínimo necessário para grid de checkboxes + lista de ordem (estilos podem migrar tal-qual da view tenant atual).

### W5-CA2 — Tenant continua idêntico (zero regressão visual e funcional)
**Dado** que `ModelosProntuarioView.vue` foi refatorado para usar o componente,
**Quando** o usuário (papel Dono) acessa `/configuracoes/modelos-prontuario`, cria um modelo novo, marca 3 seções (ex: queixa, HDA, conduta), reordena via ↑/↓ e salva,
**Então** o comportamento visual (grid de checkboxes 2-colunas, lista de ordem com botões ↑/↓, layout de 2 painéis) e o resultado final (modelo salvo com `estruturaJson` no mesmo shape de antes) são IDÊNTICOS ao comportamento pré-Wave-5. Captura de tela do antes/depois confirma equivalência visual.

### W5-CA3 — Admin usa o builder visual
**Dado** que `ModelosGlobaisFormView.vue` foi refatorado,
**Quando** o admin global acessa `/admin/modelos/novo` (ou rota equivalente da Wave 4),
**Então** vê o mesmo builder visual do tenant (grid de checkboxes das 17 seções, lista de ordem com ↑/↓), o textarea de JSON cru desapareceu, e o campo "Motivo da alteração" (≥10 chars) continua visível abaixo, separado do builder.

### W5-CA4 — Criação admin via builder gera shape idêntico
**Dado** que o admin abre o formulário de novo modelo,
**Quando** marca 5 seções (ex: queixa, HDA, exame-fisico, cid10, conduta) na ordem padrão e clica em salvar com motivo "Criação modelo padrão consulta",
**Então** o request `POST /admin/global/catalogos/modelos-prontuario` (ou rota Wave 4) carrega `estruturaJson` igual a um array JSON `[{ chave: "queixa", titulo: "Queixa principal (QP)", tipo: "texto_longo", ordem: 0 }, ...]` com 5 itens — mesma forma que o tenant geraria com as mesmas seções marcadas. Verificável via DevTools Network ou log de teste.

### W5-CA5 — Edição de modelo legado (criado em Wave 4 via textarea JSON)
**Dado** que existe um modelo padrão do sistema cadastrado na Wave 4 cujo `estruturaJson` é JSON válido com chaves das 17 seções (ex: `[{"chave":"queixa","titulo":"Queixa","tipo":"texto_longo","ordem":0},{"chave":"conduta","titulo":"Conduta","tipo":"texto_longo","ordem":1}]`),
**Quando** o admin abre esse modelo para editar,
**Então** o builder mostra os checkboxes "Queixa principal (QP)" e "Conduta" marcados, a lista de ordem mostra os dois na ordem `0,1`, o usuário pode editar normalmente (marcar nova seção, reordenar) e ao salvar, o backend recebe o `estruturaJson` atualizado sem perda de informação.

### W5-CA6 — Seções customizadas (chaves desconhecidas) são preservadas
**Dado** que existe um modelo cuja `estruturaJson` contém uma chave fora das 17 conhecidas (ex: `[{"chave":"queixa","titulo":"Q","tipo":"texto_longo","ordem":0},{"chave":"avaliacao-custom","titulo":"Avaliação Custom","tipo":"texto","ordem":1}]`),
**Quando** o admin abre esse modelo para editar,
**Então** o builder mostra apenas "Queixa principal (QP)" como checkbox marcado, exibe um aviso visível (ex: faixa de info) dizendo "Este modelo tem 1 seção customizada que será preservada ao salvar", e ao salvar (mesmo sem mexer em nada além de marcar uma seção nova), a seção `avaliacao-custom` continua presente no `estruturaJson` resultante, no fim do array, preservando `titulo`, `tipo` e ordem relativa original.

### W5-CA7 — Catálogo único entre tenant e admin
**Dado** que um modelo é criado pelo admin com seção "Exame físico" marcada,
**Quando** o tenant abre seu próprio formulário de modelo de prontuário e cria outro modelo marcando "Exame físico",
**Então** ambas as gravações carregam `chave: "exame-fisico"`, `titulo: "Exame físico"`, `tipo: "texto_longo"` — sem divergência. (Verifica que o catálogo é importado da mesma constante exportada — uma busca por outro array com as 17 seções no front retorna apenas o do componente.)

### W5-CA8 — Live-link Wave 4 preservado
**Dado** que o admin edita um modelo padrão do sistema (`EhPadraoSistema=true`) via builder visual, marca uma seção nova e salva com motivo válido,
**Quando** o tenant (qualquer usuário com permissão para usar modelos) abre o seletor de modelos no fluxo de novo prontuário/evolução logo após,
**Então** o modelo atualizado aparece com a estrutura nova — sem cache, sem republish, sem deploy. Comportamento idêntico ao Wave 4.

### W5-CA9 — Motivo ≥10 chars no admin continua obrigatório e fora do builder
**Dado** que o admin marca seções e preenche nome, mas digita motivo com 5 caracteres,
**Quando** clica em salvar,
**Então** o submit é bloqueado e a mensagem "Motivo deve ter ao menos 10 caracteres" aparece no campo motivo (mesma validação Wave 4). O builder em si não exige motivo.

### W5-CA10 — Audit em mutação admin continua
**Dado** que o admin salva qualquer modelo (criação ou edição) via builder visual,
**Quando** se inspeciona a tabela de audit (`auditoria_admin` ou equivalente Wave 4),
**Então** uma linha foi inserida com `{usuario_admin_id, acao, tipo_recurso: 'modelo_de_prontuario', recurso_id, motivo, payload_antes/depois, timestamp}` — exatamente como Wave 4. Backend não muda.

### W5-CA11 — Zero regressão em testes existentes
**Dado** que a Wave 5 foi entregue,
**Quando** se roda `npm run test` (frontend Vitest) e `dotnet test` (backend),
**Então** todos os testes que passavam antes continuam passando — incluindo qualquer teste que envolva `ModeloProntuarioView`, store admin, criação/edição de modelos.

### W5-CA12 — Documentação viva atualizada
**Dado** que a Wave 5 foi entregue,
**Quando** se lê `Docs/DESIGN.md`,
**Então** existe uma nota sobre `ModeloProntuarioBuilder.vue` em `components/ui/` registrando: (a) finalidade (builder visual de modelos de prontuário), (b) que é compartilhado entre tenant e admin, (c) que o catálogo das 17 seções é constante exportada `SECOES_MODELO_PRONTUARIO` (fonte única de verdade) e (d) que reorder é via botões ↑/↓ (drag-and-drop é extensão futura).

### W5-CA13 — Multi-tenant e RBAC inalterados
**Dado** que a Wave 5 só mexe em UI do front,
**Quando** um usuário não-admin tenta acessar a rota admin `/admin/modelos/novo`,
**Então** continua sendo redirecionado/bloqueado pelo mesmo guard de papel da Wave 1-4. Quando um usuário Dono (tenant) tenta editar um modelo padrão do sistema na sua tela `/configuracoes/modelos-prontuario`, continua impedido como antes (`editarModelo` retorna se `ehPadraoSistema`).

---

## 7. Pontos de extensão futura (NÃO implementar agora)

1. Drag-and-drop de reorder (substituir ↑/↓ por `vuedraggable` ou nativo HTML5). Briefing separado quando houver demanda.
2. Editor de seções customizadas no admin (adicionar `chave` + `titulo` + `tipo` arbitrários). Hoje, só preservamos legadas.
3. Templates de texto pré-preenchidos por seção (ex: "Exame físico" vem com headers de FC/PA/Saturação).
4. Catálogo dinâmico (configurável pelo admin), em vez de hardcoded de 17 seções.
5. Preview do modelo (mostrar como ficaria na tela de evolução).
6. MFA TOTP no admin, impersonate, painel global de métricas (já listados nas Waves anteriores).

---

## 8. Riscos e mitigações

| # | Risco | Mitigação |
|---|---|---|
| R1 | Extração do builder quebra comportamento sutil do tenant (CSS, ordem de eventos, watchers) | CA2 exige verificação visual antes/depois + suíte Vitest. Dev pode rodar `npm run test:e2e` se existir, ou QA valida via chrome-devtools. |
| R2 | `parsearEstruturaJson` falha em JSON inesperado (ex: envelope antigo `{ secoes: [...] }`) | Helper detecta os dois shapes (array direto OU `{ secoes: [...] }`); se nenhum bate, arranca com builder vazio + warning no console (não quebra a tela). |
| R3 | Componente em `components/ui/` cresce demais e vira "god component" | Escopo limitado a builder de modelo de prontuário. Helpers ficam exportados — se outro tipo de builder surgir, faz componente próprio. |
| R4 | Divergência entre seleção (record) e ordem (array) gera estado inconsistente | Migrar a lógica `sincronizarOrdem` atual do tenant intacta — já testada em produção. |
| R5 | Backend rejeita o JSON gerado pelo builder em modelos legados com customizadas | Shape continua o mesmo (`SecaoModelo[]`); validação backend é só `JsonSerializer.Deserialize<SecaoModelo[]>(...)` — passa direto. |
| R6 | Admin perde o "format JSON" do textarea (ferramenta era atalho útil) | Substituído pelo builder visual — não é mais necessário. Se algum modelo precisar edição manual em casos extremos, hotfix via SQL direto no RDS (raro). |

---

## 9. Observações para execução

- **Não-negociável**: shape do JSON gerado igual ao do tenant atual (array direto, sem envelope). Conferir gravando um modelo via tenant e via admin pós-Wave-5 e comparando os payloads byte-a-byte (ignorando whitespace).
- **Não-negociável**: catálogo das 17 seções importado da mesma constante. `grep` no front por outra cópia dos labels (ex: "Queixa principal") deve retornar só o componente.
- **Negociável (decisão do dev)**: nome do arquivo do componente (`ModeloProntuarioBuilder.vue` é a sugestão), nome dos helpers exportados, organização interna (1 arquivo vs subpasta), classe CSS prefix (recomendado `mpb-` ou `builder-` se houver risco de colisão com classes do `.app-page`).
- **Negociável (decisão do dev)**: usar `<AppCallout>` para o aviso de seções customizadas se existir; caso contrário, markup simples com classe variant `--info` no estilo do design system.
- **Reuso obrigatório**: `AppField`, `AppInput`, `AppTextarea`, `AppButton` (e `AppBadge` se já usado no tenant para indicar seções padrão). Não criar componentes novos só para isso.
- **Acionar `imedto-database`?** Não. Zero mudança em schema, zero migration.
- **Validação visual**: smoke test no QA via chrome-devtools com 3 cenários: (a) tenant cria modelo novo, (b) admin cria modelo novo, (c) admin edita modelo legado com seção customizada. Screenshots dos 3 anexos ao commit.

---

## 10. Atualização de documentação

| Doc | O que muda |
|---|---|
| `Docs/DESIGN.md` | Acrescentar entrada na seção de componentes UI (ou criar subseção "Componentes de domínio compartilhados" se ainda não existir) descrevendo `ModeloProntuarioBuilder.vue`: finalidade, compartilhamento tenant+admin, constante `SECOES_MODELO_PRONTUARIO` como fonte única do catálogo de 17 seções, reorder via ↑/↓, retrocompat com seções customizadas legadas. **2-4 frases**, surgical. |

Nenhuma atualização em `ARQUITETURA.md`, `INFRA.md`, `COMANDOS.md`, `LGPD.md` — Wave 5 não muda padrão de back, infra, comando recorrente nem regra de PII.

---

## 11. Hand-off

- **Próximo agente**: `imedto-developer`.
- **Não precisa**: `imedto-database` (zero schema/migration).
- **Após dev**: `imedto-qa` valida CAs W5-CA1..W5-CA13, commit + push (1 só, agrupando frontend + doc).
- **Critério de "pronto para QA"**: tenant + admin compartilhando o componente, retrocompat confirmada manualmente pelo dev em pelo menos 1 modelo legado de teste, `npm run test` verde, `dotnet test` verde.
