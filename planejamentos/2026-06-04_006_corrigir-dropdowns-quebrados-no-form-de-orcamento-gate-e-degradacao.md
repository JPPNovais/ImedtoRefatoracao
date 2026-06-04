# Corrigir dropdowns quebrados no formulário de orçamento — alinhar gate de permissão e degradar carregamento de catálogos

**ID**: 2026-06-04_006
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento, orçamento

## 1. Contexto e motivação

No formulário de orçamento (`OrcamentoFormView.vue`, rotas `OrcamentoNovo` e `OrcamentoForm`), os dropdowns de catálogo (cirurgias, tabela de valor profissional, equipes, implantes, locais, formas de pagamento, profissionais) aparecem quebrados/vazios e a tela cai num erro genérico "Erro ao carregar.".

Há **dois problemas distintos** confundidos no mesmo sintoma:

1. **Descasamento de gate (RBAC).** A rota do formulário (`OrcamentoForm`/`OrcamentoNovo`) é liberada hoje por `orcamento.ver` (ver `frontend/src/router/routePermissions.ts`, linhas 61-64), mas o backend que serve os catálogos de configuração de orçamento exige `orcamento.configurar`. Resultado: um usuário com `orcamento.ver` mas **sem** `orcamento.configurar` entra na tela (router deixa passar), dispara as chamadas de catálogo e leva 422 do backend — que cai no `catch` genérico e quebra os 7 dropdowns de uma vez. O front "esconde no menu mas a URL abre a tela e estoura 422 lá dentro" — exatamente o anti-padrão que o `routePermissions.ts` foi criado para eliminar.

2. **Falha de carregamento sem degradação graciosa.** `carregarCatalogos()` usa `Promise.all` para 7 chamadas paralelas (linhas 505-522). Se **qualquer uma** falhar — seja o 422 de permissão, seja uma falha de rede isolada de **um** catálogo — o `Promise.all` rejeita inteiro, `carregar()` cai no `catch` (linha 542) e **nenhum** dropdown carrega. O usuário não distingue "não tenho permissão" de "um catálogo específico falhou".

A conta de teste `contato.imedto` reproduziu o caso por estar sem `orcamento.configurar`. **O ajuste da conta é setup manual do usuário e está FORA deste briefing** — a correção de código abaixo é o que resolve o problema estrutural.

Evidência adicional levantada: a tabela `orcamento_valor_profissional` aparece vazia para esse estabelecimento. Isso pode ser apenas falta de dado cadastrado **ou** um segundo bug no fluxo de cadastro de valores-profissional (honorários). Tratado como **follow-up de investigação na seção 8**, não como parte do conserto dos dropdowns.

## 2. Persona-alvo

- **Recepção / Financeiro / Profissional** com papel que inclui `orcamento.ver` mas **não** `orcamento.configurar` — hoje entram na tela e tomam erro genérico. Devem ser **bloqueados na entrada** com mensagem clara.
- **Dono / Admin / usuário com `orcamento.configurar`** — montando ou editando um orçamento. Para eles, uma falha de rede isolada de **um** catálogo não pode derrubar a tela inteira.

Momento da jornada: orçamento → criação/edição, antes do faturamento. Frequência: alta em clínicas cirúrgicas.

## 3. Escopo

**Inclui**:
- Alinhar `routePermissions.ts`: rotas `OrcamentoNovo` e `OrcamentoForm` passam a exigir `orcamento.configurar` (em vez de `orcamento.ver`). Bloqueia entrada por menu e por URL direta.
- Manter um **fallback dentro do form**: se a permissão for revogada com o form já aberto (ou o backend devolver 422 de permissão em qualquer catálogo), a tela exibe **uma única** mensagem de bloqueio em vez dos 7 dropdowns quebrados.
- Trocar `Promise.all` por `Promise.allSettled` em `carregarCatalogos()` e **distinguir a causa**:
  - Falha **422 de permissão** em qualquer catálogo → trata como bloqueio (mensagem única, não renderiza os dropdowns quebrados).
  - Falha **isolada de rede** (qualquer status que não 422-permissão) em parte dos catálogos → **degradação graciosa**: os que carregaram funcionam; o que falhou avisa apenas naquele select, sem derrubar a tela.
- Espelho front+back mantido: o 422 do backend continua sendo a fonte da verdade; o front é UX.

**Não inclui**:
- Ajuste de permissão/role da conta `contato.imedto` (setup manual do usuário).
- Conserto do fluxo de cadastro de valores-profissional (segue como follow-up de investigação — seção 8).
- Alteração de regra de negócio do orçamento, cálculo, consolidação ou preview.
- Mudança nas rotas `Orcamentos`, `OrcamentoDetalhe`, `OrcamentoSettings` — permanecem em `orcamento.ver` (listar/ver é direito mais fraco que configurar). **Confirmar premissa na seção 9.**

## 4. Regras de negócio

- **R1 — Gate de entrada do formulário.** Acessar o formulário de orçamento (criar OU editar) exige `orcamento.configurar`. Mora em: `routePermissions.ts` (front) **espelhando** o `[RequiresAcao("orcamento.configurar")]` (ou equivalente) dos controllers de catálogo/criação/edição de orçamento. Validada em: **back (fonte da verdade, 422/403) + front (router gate + fallback no form)**. Dono passa sempre (helper `ehDono` já trata).
- **R2 — Bloqueio como estado único.** Quando o carregamento de catálogos retorna 422 de permissão (em qualquer das chamadas), o form NÃO renderiza os dropdowns; renderiza um estado de bloqueio único com mensagem genérica. Mora em: Front (`OrcamentoFormView.vue`). Validada em: front (UX); back já barra via 422.
- **R3 — Degradação graciosa por catálogo.** Falha isolada de rede de um subconjunto dos 7 catálogos (não-permissão) não impede o uso dos demais. Cada catálogo que falhou sinaliza no próprio select ("não foi possível carregar"); os que carregaram operam normalmente. Mora em: Front. Validada em: front.
- **R4 — Distinção de causa.** A decisão "bloqueio vs degradação" deriva do status: 422 de permissão → bloqueio (R2); qualquer outra falha → degradação (R3). Mora em: Front (classificação do erro do `allSettled`). O backend continua sendo a fonte: o front apenas interpreta o 422.
- **R5 — Multi-tenant.** Todos os catálogos já filtram `estabelecimento_id` no backend. Nenhuma mensagem do front pode revelar tenant alheio nem expor por que o registro "não existe". Mora em: Query/Handler (back, já existente) + Front (mensagem genérica). Validada em: back + front.

## 5. Modelo de dados

Sem alteração de schema. Nenhuma migration.

Tabelas apenas **lidas** (sem mudança): `orcamento_cirurgia_catalogo`, `orcamento_valor_profissional`, `orcamento_equipe_catalogo`, `orcamento_implante_catalogo`, `orcamento_local_cirurgia_config`, `forma_pagamento`, e o catálogo público de profissionais (vínculo). Todas filtradas por `estabelecimento_id` no backend.

Observação para o follow-up (seção 8): `orcamento_valor_profissional` está vazia para o estabelecimento de teste — verificar se é falta de dado ou bug de escrita no cadastro.

## 6. UX e fluxo

Wireframe textual:

- **Acesso sem `orcamento.configurar` (entrada bloqueada)**: usuário sem a permissão clica em "Novo orçamento" / abre a URL direta `/orcamentos/novo` ou `/orcamentos/:id/editar` → router redireciona para Home (comportamento atual de rota restrita), e o item de menu correspondente fica oculto. Não chega a renderizar o form.
- **Permissão revogada com form aberto / 422 em catálogo (fallback)**: o form, ao receber 422 de permissão em `carregarCatalogos()`, troca o conteúdo por um **estado de bloqueio único** — usar o componente de estado/aviso já presente no design system (preferência por `AppEmptyState` ou bloco de aviso equivalente; reusar, não criar novo). Mensagem genérica: "Você não tem permissão para configurar orçamentos." Sem PII, sem nome de tenant, sem detalhe técnico.
- **Falha de rede isolada (degradação)**: a tela carrega normalmente; o(s) select(s) cujo catálogo falhou exibem um aviso inline curto ("Não foi possível carregar. Tente recarregar.") e ficam sem opções, mas os demais selects funcionam e o usuário consegue prosseguir com o que carregou.
- **Estados padrão**: loading (spinner existente, linha 676-678); erro de bloqueio (R2); erro isolado por select (R3); vazio legítimo — catálogo carregou mas sem itens (ex.: nenhuma cirurgia cadastrada) → manter o `<option :value="0">Selecione...</option>` e, quando aplicável, o aviso de "não configurado" já existente (ex.: local cirúrgico, linhas 1012-1020).

Componentes reutilizados: `AppSelect`, `AppField`, `AppCard`, `AppButton` e o componente de estado vazio/aviso do design system. Mobile-ready: o `.app-page--wide` e o grid responsivo já existentes permanecem.

Não há atalho de teclado novo.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — com permissão):** Dado um usuário com `orcamento.configurar` (ou Dono) e os 7 catálogos respondendo 200, Quando abre `/orcamentos/novo`, Então todos os dropdowns são populados e o form é utilizável normalmente.

- **CA2 (RBAC — gate de entrada, espelho rota+backend):** Dado um usuário com `orcamento.ver` mas **sem** `orcamento.configurar`, Quando tenta acessar `/orcamentos/novo` ou `/orcamentos/:id/editar` por menu ou URL direta, Então o router bloqueia (redireciona para Home) e o item de menu de criação fica oculto — sem nenhuma chamada de catálogo disparada e sem erro genérico na tela. (Espelho: `routePermissions.ts` exige `orcamento.configurar` para `OrcamentoNovo` e `OrcamentoForm`.)

- **CA3 (RBAC — fallback no form, permissão revogada em runtime):** Dado um usuário que entrou no form e tem a permissão revogada (ou um catálogo retorna 422 de permissão), Quando `carregarCatalogos()` recebe o 422, Então a tela exibe **um único** estado de bloqueio com mensagem genérica ("Você não tem permissão para configurar orçamentos.") e **não** renderiza os 7 dropdowns quebrados.

- **CA4 (degradação graciosa — falha de rede isolada):** Dado que 6 dos 7 catálogos respondem 200 e 1 falha por erro de rede (status != 422-permissão), Quando o form carrega via `Promise.allSettled`, Então os 6 dropdowns que carregaram funcionam normalmente e apenas o select do catálogo que falhou exibe aviso inline ("Não foi possível carregar."), sem derrubar a tela inteira nem cair no erro genérico.

- **CA5 (distinção de causa):** Dado o resultado do `Promise.allSettled`, Quando há pelo menos uma rejeição com 422 de permissão, Então o comportamento é bloqueio (CA3) e tem precedência sobre a degradação; Quando todas as rejeições são não-permissão, Então o comportamento é degradação (CA4).

- **CA6 (multi-tenant / LGPD — mensagem genérica):** Dado um catálogo que retorna 422/404, Quando o front exibe a falha, Então a mensagem é genérica, não contém PII, não revela id/nome de estabelecimento alheio nem detalhe técnico do backend, e nada é logado com PII.

- **CA7 (estado vazio legítimo):** Dado um catálogo que carregou com 200 mas sem itens (ex.: nenhuma cirurgia cadastrada), Quando o select renderiza, Então mostra apenas "Selecione..." (e o aviso de "não configurado" onde já existe, como no local cirúrgico) — sem confundir vazio legítimo com erro de carregamento.

- **CA8 (sem regressão de paridade):** Dado um usuário com permissão completa, Quando cria e salva um orçamento com cirurgias, profissionais, formas de pagamento e local cirúrgico, Então o fluxo de salvar/enviar e o preview continuam funcionando exatamente como antes da mudança (nenhuma alteração de cálculo/payload).

## 8. Riscos e dependências

- **Risco — outras telas de orçamento.** Mudar o gate só de `OrcamentoNovo`/`OrcamentoForm` e manter `Orcamentos`/`OrcamentoDetalhe`/`OrcamentoSettings` em `orcamento.ver` é intencional (ver/listar é direito mais fraco que configurar). Atenção a links internos que levam ao form a partir dessas telas — o botão "Novo orçamento" e "Editar" devem ficar ocultos/inativos para quem não tem `orcamento.configurar`, evitando clique que redireciona para Home. **Verificar e ajustar visibilidade desses CTAs.**
- **Risco — `OrcamentoSettings`.** A tela de configuração de catálogos provavelmente já deveria exigir `orcamento.configurar` no backend. Confirmar se o gate da rota `OrcamentoSettings` precisa subir junto (hoje está em `orcamento.ver`). Se o backend de settings exige `configurar`, alinhar para não recriar o mesmo descasamento. **Item a confirmar pelo dev ao espelhar os controllers.**
- **Dependência — classificação do 422 de permissão no front.** O front precisa distinguir 422 "sem permissão" de 422 "regra de negócio" (ex.: validação). Verificar o shape do erro retornado (`e?.response?.status` e o corpo `mensagem`/código) para classificar com segurança. Se o backend não devolver um código distinguível, o dev deve sinalizar — pode virar follow-up de spec.

### Follow-up de investigação (item separado — NÃO faz parte do conserto dos dropdowns)

- **F1 — Cadastro de valores-profissional (honorários) possivelmente quebrado.** A tabela `orcamento_valor_profissional` está vazia para o estabelecimento de teste. Investigar se é (a) apenas falta de dado cadastrado, ou (b) um bug no fluxo de cadastro/escrita de valores-profissional. Verificar a tela/endpoint de cadastro de honorários (criar um registro e confirmar persistência multi-tenant). Se confirmar bug, abrir briefing próprio (não addendum deste). Se for só falta de dado, registrar o achado e encerrar. Este item pode virar discovery em `Docs/Discoverys/orcamento-valores-profissional/` se a causa não for cravável rapidamente.

## 9. Observações para execução

- **Não-negociável:** o gate de `OrcamentoNovo`/`OrcamentoForm` em `routePermissions.ts` deve usar a **mesma** chave que o backend exige nos controllers de catálogo/criação/edição de orçamento. Antes de cravar `orcamento.configurar`, **confirme via `grep` nos controllers** (`[RequiresAcao(...)]`) qual é a chave real exigida. Se o backend exigir uma chave diferente, espelhe a chave do backend — a fonte da verdade é o back. O briefing assume `orcamento.configurar` com base na decisão de produto; ajuste se a inspeção do back divergir e reporte.
- **Não-negociável:** trocar `Promise.all` por `Promise.allSettled` em `carregarCatalogos()` preservando os assignments individuais (`catCirurgias`, `catValores`, etc.) só para as promessas `fulfilled`; as `rejected` marcam o select correspondente como "falhou".
- **Liberdade técnica:** a forma de sinalizar falha por select (flag por catálogo, mapa de status) fica a critério do dev, desde que cumpra CA4/CA7.
- **Reuso:** usar o componente de estado vazio/bloqueio já existente no design system para o estado de R2 — **não** criar componente novo. Confirme em `frontend/src/components/ui/` (ex.: `AppEmptyState`) antes de montar HTML scoped.
- **Espelho front+back:** o front é UX; o 422 do backend permanece a fonte da verdade. Não remover nenhuma validação de backend.
- **Conta de teste:** ignorar qualquer ajuste de role da conta `contato.imedto` — é setup manual do usuário, fora do escopo.

## 10. Atualização de documentação

Nenhuma atualização de `Docs/` necessária — a demanda corrige um descasamento de gate e melhora tratamento de erro de carregamento dentro de padrões já documentados (`routePermissions.ts` como fonte de verdade do gate, multi-tenant e mensagem genérica já cobertos em `Docs/LGPD.md`, design system já em `Docs/DESIGN.md`). Não introduz componente novo, padrão novo nem novo tipo de PII.

Se o follow-up F1 evoluir para discovery, criar `Docs/Discoverys/orcamento-valores-profissional/01_discovery.md` naquele momento — não nesta entrega.
