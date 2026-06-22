# Alertas clínicos visíveis apenas no prontuário — ADDENDUM: reconciliação dos códigos HTTP com a arquitetura de erros real

**ID**: 2026-06-22_002-addendum
**Refere-se a**: `2026-06-22_002_alertas-clinicos-visiveis-so-no-prontuario-lgpd.md` (briefing original — IMUTÁVEL, não editado)
**Status**: Aprovado em 2026-06-22 — IMUTÁVEL
**Autor**: imedto-business-analyst
**Modo**: B (escalonamento de spec gap Tipo B detectado na validação)
**Estimativa de esforço incremental**: nenhum esforço adicional — apenas alinha os CAs ao mecanismo de erro que o projeto **já** usa (não muda comportamento de negócio nem postura de LGPD)
**Áreas regressivas tocadas**: nenhuma nova (mesmas do original — permissionamento de alertas / prontuário-paciente). Este addendum só corrige a **expectativa de código HTTP** nos critérios de aceite.

---

## 1. Motivo (spec gap)

Na validação, identificou-se que o briefing original especificou **códigos HTTP que conflitam com a arquitetura real de erros do projeto**:

- **CA12** (gestão negada) diz que o backend retorna **403** genérico para "sem permissão".
- **CA13** (cross-tenant) diz que o backend retorna **404 genérico** ("não encontrado").

A arquitetura real do projeto (confirmada no código) é:

- **`ForbiddenException`** (`backend/src/Core/Imedto.Backend.SharedKernel/Domain/ForbiddenException.cs`) → mapeada pelo `GlobalExceptionFilter` (`backend/src/Core/Imedto.Backend.SharedKernel/Filters/GlobalExceptionFilter.cs`) para **HTTP 403**. É o mecanismo padrão de "sem permissão / autorização negada" do projeto (ex.: `RequiresPermissaoExtraAttribute`, `AtualizarExigirDono2faCommandHandler` lançam `ForbiddenException` → 403).
- **`BusinessException`** (`backend/src/Core/Imedto.Backend.SharedKernel/Domain/BusinessException.cs`) → `UseExceptionHandler` em `Program.cs:520` → **HTTP 422**. É o padrão para regra de negócio **e** para "não encontrado" / cross-tenant. **Não existe `NotFoundException` / 404 idiomático** no projeto — o `ProntuarioController` já documenta "Multi-tenant → 422 genérico", e o CLAUDE.md afirma: *"422 do `BusinessException` é a fonte da verdade"*.

Ou seja: o **404 do CA13** não tem mecanismo equivalente no projeto; seguir o briefing à risca levaria o dev a inventar um padrão fora da arquitetura (anti-padrão). O **403 do CA12** está correto e tem mecanismo (`ForbiddenException`), mas precisa ser explicitado para o dev usar o caminho idiomático.

> **A postura de LGPD permanece exatamente a mesma.** Este addendum **não** afrouxa nem altera nenhuma regra de minimização, não-vazamento, mensagem genérica ou indistinguibilidade. Só reconcilia o **código HTTP** com o que o projeto realmente emite.

## 2. Decisões

1. **CA12 (gestão negada — sem permissão):** mantém **403**, implementado via **`ForbiddenException`** (caminho idiomático do projeto). Confirma a intenção do CA original; só fixa o mecanismo. A mensagem segue genérica, sem PII, e nada é persistido.

2. **CA13 (cross-tenant) e qualquer "não encontrado":** passa de **404** para **422** via **`BusinessException("Não encontrado.")`**, conforme o padrão estabelecido do projeto (não há 404 idiomático). **A postura de LGPD permanece idêntica:** mensagem genérica, indistinguível de "não existe", nada devolvido/persistido, sem PII em log. **Só o código HTTP muda (404 → 422).**

3. **CA8 / CA10 (leitura negada):** reafirma-se que **não é erro HTTP**. A leitura negada **não** é 403, **não** é 404, **não** é 422 — é **HTTP 200 com payload sem alertas** (conteúdo de alerta vazio, ex.: array `[]` / campo ausente), **indistinguível** de "paciente sem alertas" (R5/R7 do original). Negar leitura com um status de erro vazaria a existência do dado — exatamente o que a R7 proíbe. Quem não pode ler simplesmente **recebe o prontuário sem o conteúdo de alerta**, como se não houvesse alerta.

4. **Desambiguação de "pode gerir" para o front (nota nova, sem mudar regra):** a decisão de **renderizar ou não o controle de gestão** (criar/editar/remover alerta) no cabeçalho do prontuário (CA12, front) é feita por um **flag booleano no DTO do prontuário — `PodeGerirAlertas`** — derivado do **mesmo gating** da R3 (Dono **OU** atende/atendeu), e **não** pelo papel sozinho. Isso atende o CA12 ("controle não renderizado para quem não pode") **sem** usar o array de alertas como sinal de permissão (o array é **opaco**: vazio tanto para "sem permissão de leitura" quanto para "sem alertas", logo não serve para o front decidir gestão). O flag é a fonte da UX; o backend continua sendo a fonte da verdade do enforcement (R5) — o flag **não substitui** a trava no comando de gestão, que segue lançando `ForbiddenException` (403) para quem não pode (CA12 ajustado).

   > Observação de minimização: `PodeGerirAlertas` é um booleano derivado de permissão — **não é PII** e não revela a existência de alerta (é a capacidade de gerir, não o conteúdo). Coerente com a minimização do DTO.

## 3. CAs ajustados

Os CAs abaixo **substituem** as versões correspondentes do briefing original **apenas no que diz respeito ao código HTTP / mecanismo**. Todo o resto de cada CA (personas, multi-tenant, não-vazamento, sem PII) permanece como no original.

- **CA12 (ajustado) — gestão negada (sem vínculo / Recepção):** Dado um usuário **Recepção** ou **Profissional sem vínculo (não-Dono)**, Quando (por chamada direta à API) tenta criar/editar/remover um alerta de um paciente do **seu próprio tenant**, Então o backend lança **`ForbiddenException` → HTTP 403** com mensagem **genérica** ("sem permissão"), **nada é persistido**, e **nenhum log contém PII**; e no front o controle de gestão **não é renderizado** para essa persona (decidido pelo flag `PodeGerirAlertas = false` do DTO do prontuário — §2.4). *(Único ajuste vs. original: fixa o mecanismo como `ForbiddenException`; o 403 já estava correto.)*

- **CA13 (ajustado) — multi-tenant:** Dado um usuário do estabelecimento **B**, Quando tenta ler ou gerir alertas (via UI ou chamada direta) de um paciente do estabelecimento **A**, Então o backend lança **`BusinessException("Não encontrado.")` → HTTP 422** genérico, **nada é devolvido/persistido** e **nenhum log contém PII** do paciente alheio. *(Ajuste vs. original: **404 → 422** via `BusinessException`, conforme o padrão do projeto — não há 404 idiomático. A postura LGPD — mensagem genérica, indistinguível, sem PII — é idêntica.)*

- **CA8 (esclarecido) — leitura negada (Profissional sem vínculo, não-Dono):** Dado um **Profissional** que **nunca atendeu** o paciente, **não está atendendo** e **não é Dono**, Quando abre o prontuário desse paciente (do seu tenant), Então a resposta é **HTTP 200** e o payload do prontuário **não inclui** o conteúdo de alertas (campo vazio/ausente) — **não é 403, não é 404, não é 422** — sendo **indistinguível** de "paciente sem alertas" (não revela alerta oculto), e o cabeçalho **não exibe** o bloco de alertas. *(Esclarecimento, não mudança: leitura negada nunca foi erro HTTP — é 200 com payload sem alertas, R5/R7 do original.)*

- **CA10 (esclarecido) — enforcement no backend (chamada direta):** Dado um usuário **sem direito de leitura** (Recepção, ou Profissional sem vínculo não-Dono), Quando chama **diretamente a API** (sem a UI) o endpoint/fluxo que serve o prontuário, Então a resposta é **HTTP 200** com o **mesmo formato genérico de "sem dado"** (sem conteúdo nem contagem de alertas) — **não** um status de erro — validado por **chamada direta**, não só pela tela. *(Esclarecimento: o enforcement de leitura se manifesta como ausência do dado em uma resposta 200, não como erro; usar erro vazaria a existência do alerta.)*

> **Nota de coerência (não é CA novo):** o flag `PodeGerirAlertas` (booleano no DTO do prontuário, derivado de Dono OU atende/atendeu) é o sinal que o front usa para renderizar/ocultar o controle de gestão (CA12, lado front). Ele **não** flexibiliza o enforcement do comando de gestão (que segue 403 via `ForbiddenException` para quem não pode) e **não** sinaliza a existência de alertas (não é PII). Não cria CA novo — apenas dá ao dev/QA o mecanismo concreto para validar "controle não renderizado para quem não pode" sem inferir permissão a partir do array opaco de alertas.

## 4. Impacto na implementação

- **Backend — gestão (CA12):** o comando/endpoint de gestão de alerta deve lançar **`ForbiddenException`** (→ 403) quando o usuário não satisfaz a R3 (Dono OU atende/atendeu). Não criar status custom.
- **Backend — cross-tenant (CA13):** paciente fora do tenant ativo → **`BusinessException("Não encontrado.")`** (→ 422), mesmo padrão já usado no `ProntuarioController`. Não introduzir 404 / `NotFoundException`.
- **Backend — leitura negada (CA8/CA10):** **não** lançar exceção; **omitir** o conteúdo de alertas do DTO do prontuário (campo vazio/ausente), mantendo **200**. A negativa de leitura é ausência de dado, não erro.
- **Backend — DTO do prontuário:** expor o flag derivado **`PodeGerirAlertas`** (booleano, = Dono OU atende/atendeu, mesmo gating da R3) para o front decidir a renderização do controle de gestão. Sem PII; não revela existência de alerta.
- **Front:** controle de gestão renderizado **sse `PodeGerirAlertas === true`** (não pelo papel sozinho, nem pela presença de alertas no array). Bloco de leitura renderizado conforme o conteúdo que o backend devolver (R5/CA8). Tratamento de erro do front: 403 na gestão → feedback genérico "sem permissão"; 422 "não encontrado" → feedback genérico, sem PII (espelha o padrão já existente do projeto).
- **QA:** validar por **chamada direta** que gestão negada = **403** (`ForbiddenException`), cross-tenant = **422** (`BusinessException`), e leitura negada = **200 sem alertas** (indistinguível de "sem alerta"). Confirmar que o front oculta o controle de gestão via `PodeGerirAlertas`, não via papel.

**Tudo o mais do briefing original (R1–R9, CA1–CA7, CA9, CA11, CA14–CA19, §5 modelo de dados, §10 documentação) permanece inalterado e em vigor.** Nenhuma migration nova é introduzida por este addendum.
