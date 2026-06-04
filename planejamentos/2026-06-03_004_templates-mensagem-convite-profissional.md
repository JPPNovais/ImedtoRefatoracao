# Templates de mensagem no convite de profissional

**ID**: 2026-06-03_004
**Status**: Aprovado por usuário em 2026-06-03
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: equipe/vínculo (fluxo de convite) — nenhuma área de dado/schema

## 1. Contexto e motivação

Hoje, ao convidar um profissional para o estabelecimento, o gestor preenche os dados do convite e o sistema dispara o e-mail transacional padrão. Não há espaço para uma mensagem pessoal de boas-vindas/contexto ("Oi Dra., te chamei pra cobrir as terças, qualquer dúvida me chama").

A dor é de **tom e contexto**: o convite chega frio e impessoal. O gestor frequentemente quer adicionar uma linha pessoal, mas hoje não tem onde. Em vez de oferecer um campo em branco (que gera bloqueio de "o que escrever?"), oferecemos **3 templates prontos** que o gestor escolhe e pode usar como ponto de partida.

**Decisões de produto fechadas com o usuário (2026-06-03):**
1. **Não persistir a mensagem.** A mensagem escolhida/editada transita apenas para o corpo do e-mail. Não há coluna nova, não muda o DTO `ConvitePendente` nem a `MeusConvitesView`. O `imedto-database` **não** é acionado.
2. **Templates aprovados como estão** — usar exatamente os 3 textos abaixo, sem alteração.

**Importante — o que JÁ existe e será reusado (não duplicar):**
- O fluxo de convite já existe ponta a ponta: a tela/modal de convite no front (aba Convites / fluxo de convidar em `EquipeView.vue` e componentes de equipe), o `vinculoService` para disparar o convite, o command/handler de convite no backend e o envio de e-mail transacional (Resend, com SES alternativo).
- O envio de e-mail transacional já tem template/infra. Esta demanda **acrescenta um bloco de mensagem** ao corpo, não cria um novo canal.

A lacuna é **somente a entrada de UX + o transporte da mensagem até o e-mail**: um seletor de template + textarea editável no formulário de convite, e o repasse do texto final como campo opcional no command de convite, injetado no corpo do e-mail.

## 2. Persona-alvo

**Gestor da clínica** — Dono do estabelecimento. Momento da jornada: montagem/gestão de equipe, ao convidar um novo profissional. Frequência: pontual (a cada novo vínculo), mas alto valor percebido (primeira impressão do profissional com a clínica).

## 3. Escopo

**Inclui**:
- No formulário/modal de convite de profissional: um seletor dos 3 templates (Formal / Amigável / Curto e direto) + um `textarea` editável pré-preenchido com o template escolhido.
- A mensagem final (editada ou não) é enviada como **campo opcional** no command de convite e injetada no corpo do e-mail transacional como bloco de mensagem pessoal.
- Mensagem **vazia** (gestor apagou tudo ou não escolheu template) → o e-mail é enviado **sem o bloco de mensagem pessoal** (comportamento idêntico ao convite atual).
- Limite de **1000 caracteres** na mensagem, validado no front (contador/trava) e no back (422 genérico se exceder).

**Não inclui**:
- Persistência da mensagem (sem coluna, sem mudança em `ConvitePendente`, sem `MeusConvitesView`). Logo, **não** aparece em telas de "meus convites" nem em histórico.
- Edição/CRUD de templates pelo gestor — os 3 templates são fixos no código (front). Personalização de templates é backlog separado, se surgir demanda.
- Reenvio de convite com mensagem (o reenvio, se existir, mantém o comportamento atual — fora de escopo).
- Mudança no fluxo de aceite/recusa do convite.
- Internacionalização dos templates (PT-BR apenas).

## 4. Regras de negócio

- **R1 — Quem pode convidar (RBAC)**: apenas o **Dono** do estabelecimento pode convidar profissional e, portanto, enviar a mensagem. A autorização é checada **no handler do command de convite** (não só na rota). Se um usuário sem permissão chama o endpoint, recebe 403/erro de autorização e nada é enviado. No front, o acesso ao fluxo de convite já é restrito ao Dono; o campo de mensagem não cria nova superfície de permissão. Mora em: Handler (autorização) + Front (acesso já restrito). Validada em: back (autorização) + front (fluxo oculto a não-Dono).
- **R2 — Mensagem é opcional**: o convite funciona com ou sem mensagem. Mensagem vazia/whitespace → e-mail enviado sem o bloco pessoal. Mora em: Handler (monta corpo condicional) + Front (template pode ser limpo). Validada em: back + front.
- **R3 — Limite de 1000 caracteres**: mensagem com mais de 1000 caracteres é inválida. Front trava/conta; back valida e retorna **422 genérico** (`BusinessException`) — o 422 é a fonte da verdade, o front é UX. Mora em: Domain/Handler (validação de tamanho) + Front (contador + trava). Validada em: back (422) + front (impede submit / contador).
- **R4 — Mensagem não é persistida nem logada**: a mensagem transita apenas para o corpo do e-mail. **Não** é gravada em banco e **não** entra em log de aplicação (nem em log de erro). Em caso de falha de envio, a mensagem de erro é genérica e não contém o conteúdo da mensagem. Mora em: Handler (não loga conteúdo) + camada de e-mail. Validada em: back (ausência de persistência e de log do conteúdo).
- **R5 — Templates fixos (3)**: os textos são exatamente os aprovados (ver seção 6). O seletor pré-preenche o textarea; o gestor pode editar livremente dentro do limite. Mora em: Front. (Sem espelho no back — o back só recebe o texto final.)
- **R6 — Multi-tenant**: o convite resolve o estabelecimento a partir do contexto do Dono autenticado (claim de tenant), nunca de um tenant arbitrário. Usuário sem claim/tenant válido recebe erro genérico. O destinatário e o estabelecimento do convite respeitam o tenant do solicitante. Mora em: Handler + repositório falha-fechada. Validada em: back.

## 5. Modelo de dados

**Nenhuma mudança de schema.** Não há tabela/coluna nova. A mensagem é um campo **opcional** no command/DTO de convite (transporte em memória → corpo do e-mail), não persistido.

- LGPD: a mensagem pode conter texto livre do gestor; por minimização e por decisão de produto, **não é armazenada nem logada**. Sem PII nova em repouso. Sem necessidade de audit trail (não é acesso a prontuário/paciente; é operação de equipe e não há persistência do conteúdo).
- `imedto-database` **não** é acionado nesta demanda.

## 6. UX e fluxo

**Fluxo**: gestor abre o fluxo de convidar profissional → preenche os dados existentes → **escolhe um template** (seletor: Formal / Amigável / Curto e direto) → o `textarea` é pré-preenchido com o texto do template → gestor pode editar → envia. O e-mail chega com o bloco de mensagem pessoal (ou sem, se o textarea ficou vazio).

**Componentes**: reutilizar os componentes do design system já usados no formulário de convite (inputs/labels padrão) e um `textarea` com contador de caracteres. Conferir `frontend/src/components/ui/` antes de criar markup novo. Não introduzir componente novo no design system (a menos que o textarea com contador já não exista — nesse caso, o dev avalia reuso; é detalhe técnico do dev).

**Estados**:
- **Padrão**: nenhum template selecionado → textarea vazio → e-mail sem bloco pessoal.
- **Template selecionado**: textarea preenchido com o texto, editável.
- **Editando**: contador mostra `N/1000`; ao passar de 1000, trava/sinaliza e impede submit.
- **Vazio após edição**: gestor apaga tudo → válido → e-mail sem bloco pessoal.
- **Erro de envio**: toast genérico ("Não foi possível enviar o convite. Tente novamente."), sem expor o conteúdo da mensagem.
- **Sucesso**: feedback de convite enviado (igual ao atual).

**Responsivo/mobile**: o textarea e o seletor seguem o layout responsivo do formulário de convite existente.

**Os 3 templates (textos exatos, PT-BR):**

1. **Formal**
```
Olá! Você foi convidado(a) para integrar a equipe da nossa clínica em nosso sistema de gestão. Ao aceitar, você terá acesso à agenda e aos recursos necessários para o seu atendimento. Ficamos à disposição para qualquer dúvida.
```

2. **Amigável** (com o emoji 😊)
```
Oi! Que bom ter você com a gente 😊 Estamos te convidando para fazer parte da equipe da clínica. É só aceitar o convite para começar. Qualquer dúvida, estamos por aqui!
```

3. **Curto e direto**
```
Você foi convidado(a) para a equipe da clínica. Aceite o convite para acessar o sistema.
```

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — com mensagem)**: Dado um Dono no fluxo de convite que escolhe o template "Amigável" e mantém o texto, Quando envia o convite, Então o command de convite carrega a mensagem final e o e-mail enviado contém o bloco de mensagem pessoal com o texto do template (incluindo o emoji 😊).
- **CA2 (caminho feliz — editado)**: Dado um Dono que escolhe um template e edita o texto, Quando envia, Então o e-mail contém exatamente o texto **editado** (não o template original).
- **CA3 (mensagem vazia → e-mail sem bloco)**: Dado um Dono que não escolhe template (ou apaga todo o texto), Quando envia o convite, Então o convite é criado normalmente e o e-mail é enviado **sem** o bloco de mensagem pessoal (idêntico ao convite atual).
- **CA4 (limite 1000 — back)**: Dado um command de convite com mensagem de 1001 caracteres, Quando o handler processa, Então retorna **422 genérico** (`BusinessException`) e o convite **não** é criado nem o e-mail enviado.
- **CA5 (limite 1000 — front)**: Dado o textarea de mensagem, Quando o gestor digita além de 1000 caracteres, Então o contador sinaliza o limite e o submit é impedido no front (espelho do 422).
- **CA6 (RBAC — handler)**: Dado um usuário **não-Dono** (ex.: recepção/profissional) que chama o endpoint de convite diretamente, Quando o handler processa, Então recebe erro de autorização (403/equivalente) e nenhum convite/e-mail é gerado — a checagem ocorre **no handler**, não apenas na rota.
- **CA7 (RBAC — front)**: Dado um usuário não-Dono, Quando acessa a tela de equipe, Então o fluxo de convidar (e portanto o campo de mensagem) não está disponível para ele.
- **CA8 (multi-tenant)**: Dado um Dono do estabelecimento A, Quando envia um convite, Então o vínculo/convite é criado para o estabelecimento A (resolvido pelo claim de tenant) e nunca para outro estabelecimento; usuário sem claim de tenant válido recebe erro genérico.
- **CA9 (LGPD — não persiste)**: Dado um convite enviado com mensagem preenchida, Quando se inspeciona o banco após o envio, Então **não** existe nenhuma coluna/registro com o conteúdo da mensagem (a mensagem não foi persistida).
- **CA10 (LGPD — não loga)**: Dado um envio de convite com mensagem (sucesso ou falha), Quando se inspecionam os logs de aplicação/erro, Então o **conteúdo da mensagem não aparece** em nenhum log; mensagens de erro são genéricas e sem PII.
- **CA11 (estado de erro)**: Dado uma falha no envio do e-mail, Quando o front recebe o erro, Então exibe toast genérico ("Não foi possível enviar o convite. Tente novamente.") sem expor o conteúdo da mensagem.
- **CA12 (performance/foco)**: Dado o fluxo de convite, Quando o gestor seleciona um template, Então a troca de template é instantânea no front (texto fixo em memória, sem request) — nenhuma chamada de rede é disparada pela seleção/edição de template.

## 8. Riscos e dependências

- **Risco baixo**: como não há schema nem mudança no DTO de listagem, a superfície de regressão é o **corpo do e-mail** e o **command de convite**. Vigiar: convite sem mensagem deve permanecer byte-a-byte equivalente ao comportamento atual (CA3).
- **Dependência**: infra de e-mail transacional (Resend/SES) e o template de e-mail de convite existente. A injeção do bloco condicional deve respeitar o template atual sem quebrar o layout.
- **Atenção**: garantir que o emoji 😊 do template "Amigável" seja preservado na codificação do e-mail (UTF-8) — não vire `?` ou mojibake.

## 9. Observações para execução

- **Não-negociável**: (a) mensagem **não persistida** e **não logada** (R4, CA9, CA10); (b) limite de 1000 com **422 no back** como fonte da verdade (R3, CA4); (c) autorização de convite **no handler**, não só na rota (R1, CA6); (d) convite sem mensagem permanece equivalente ao atual (CA3); (e) os 3 textos exatos da seção 6.
- **Liberdade técnica do dev**: nome do campo no command/DTO, como o seletor de template é renderizado (radio/select/chips), e como o bloco condicional é montado no corpo do e-mail. Preferência por **reuso** do textarea/contador e dos componentes de form já existentes.
- **Não acionar `imedto-database`** — sem mudança de schema.
- O command de convite ganha um campo opcional `mensagem` (string, nullable, ≤1000). Validação de tamanho no Domain/Handler.

## 10. Atualização de documentação

**Nenhum doc de `Docs/` precisa de atualização.** A demanda segue padrões já documentados (CQRS handler com validação de `BusinessException`, e-mail transacional existente, RBAC no handler, multi-tenant por claim). Não introduz: bounded context novo, componente novo no design system, recurso de infra, comando novo, tipo de dado pessoal persistido nem novo audit. Como a mensagem não é persistida nem logada, não há novo dado pessoal em repouso a registrar em `Docs/LGPD.md`.
