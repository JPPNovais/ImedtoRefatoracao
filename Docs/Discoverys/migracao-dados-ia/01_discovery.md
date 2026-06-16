# Discovery — Central de Migração com mapeamento por IA

> **Status:** discovery refinado, pré-briefing. Investigação de viabilidade — ainda não é briefing executável.
> **Data:** 2026-06-15
> **Relação com o roadmap:** materializa a [FASE 2B — Central de Migração](../../Roadmap/FASE_2B_CENTRAL_DE_MIGRACAO.md), evoluindo a estratégia de "adaptadores determinísticos por origem" para **mapeamento de schema assistido por IA**.

---

> **Evolução pós-entrega (2026-06-15, addendum 4 — `planejamentos/2026-06-15_005`):** o épico foi entregue assumindo **1 arquivo = 1 entidade tabular** e **detecção da entidade pelo nome do arquivo** (`pacientes.json` → "paciente"). Um caso real (job #11 — dump JSON aninhado de sistema desconhecido, com objeto raiz contendo `pacientes[]`, `agendamentos[]`, `prontuarios[]` etc., além de mojibake) quebrou esse modelo. A estratégia evoluiu para:
> - **Decomposição de dump JSON aninhado em blocos-candidatos** — cada propriedade-array de objetos do objeto raiz vira um bloco (corrige o `EncontrarPrimeiroArray`, que só lia o primeiro array); objetos únicos de config (`estabelecimento{}`) são sinalizados, não migrados.
> - **Classificação semântica da entidade pela IA** (lista canônica fechada **ou** `"sem_equivalente"`) **substitui** a detecção por nome de arquivo. A IA passa a ter **duas tarefas sobre os mesmos metadados** — classificar a entidade **e** mapear as colunas — em **1 chamada por bloco-candidato** (custo por entidade, não por linha). Para arquivos tabulares, o nome do arquivo vira **hint**, não decisão.
> - **D1/D2 e D11 preservados:** a IA continua mapeando **schema** sobre **amostra mascarada por bloco** (nunca transforma registro a registro, nunca recebe o volume/PII real); id interno do dump **nunca** é gravado nem usado como chave (vínculo só por chave de negócio — agenda/prontuário sem paciente resolvível são rejeitados com motivo).
> - **Normalização de encoding determinística** na ingestão (corrige mojibake UTF-8↔Latin-1 quando seguro; sinaliza quando ambíguo — nunca corrompe nem usa IA).
> - **Backlog explícito:** XLSX multi-aba (o parser XLSX é stub hoje), resolução de FK por id interno do dump, auto-detecção de origem por fingerprint, explosão de sub-objetos em sub-registros.
> Os D1–D14 abaixo permanecem válidos como fonte original; esta nota registra a evolução incremental.

---

## 1. O problema

Cliente novo, vindo de outro sistema (iClinic, Feegow, Clinicorp, Ninsaúde, planilha solta…), precisa trazer os dados dele para o Imedto com o **menor atrito possível**. Hoje cada sistema exporta num formato diferente (CSV, XLSX, JSON, XML, ZIP), com nomes de coluna e estruturas próprias. Escrever um parser dedicado por origem (estratégia original da FASE 2B) não escala — são dezenas de sistemas, e o formato muda sem aviso.

**Ideia central:** usar IA para inferir o *de-para* entre o arquivo do cliente e o schema do Imedto, de forma que qualquer arquivo de qualquer origem possa ser mapeado sem um parser dedicado.

## 2. Distinção importante — esta funcionalidade ≠ ETL legado

Não confundir com [ETL_MAPEAMENTO.md](../../ETL_MAPEAMENTO.md): aquele é a migração **one-time** do nosso próprio legado (Supabase→RDS), com mapeamento fixo e conhecido. **Esta** é uma ferramenta **recorrente e de produto**, que importa dados de sistemas **de terceiros e desconhecidos** — entrada de cada novo cliente. Reaproveita conceitos (staging, idempotência, ordem de FK) mas o desafio é o mapeamento dinâmico.

## 3. Decisões tomadas no refino (2026-06-15)

| # | Decisão | Escolha |
|---|---|---|
| D1 | **Papel da IA** | IA mapeia o **schema** (de-para coluna→campo canônico) + garante **campos obrigatórios**. Quando um obrigatório não é preenchível, o registro é **rejeitado com motivo explícito** ao usuário. IA não transforma registro a registro. |
| D2 | **PII na IA** | Só **amostra mascarada** (cabeçalhos + poucas linhas com CPF/nome/etc. ofuscados). IA nunca recebe o volume de PII. |
| D3 | **Escopo do MVP** | **Onda 1** — pacientes, agendas, estoque (itens), fornecedores/categorias/fabricantes/locais de estoque, produtos de orçamento, procedimentos de orçamento. **Onda 2** — prontuário (vinculado aos pacientes da onda 1). Ver mapa de entidades e ordem de FK em §6. |
| D4 | **Canal + operador** | Cliente envia os arquivos pela **Configuração do Estabelecimento** (já vinculado ao `estabelecimento_id`). Operador Imedto vê no **painel de admin**, revisa, e **dispara a inserção com um clique**. Não é self-service de ponta a ponta — o disparo é do admin. |
| D5 | **Inserção** | Ao disparar, **todas as linhas** do arquivo são inseridas no banco vinculadas ao estabelecimento, **reusando os commands existentes** (`CriarPacienteCommand` + os de atualização). Nada de INSERT direto. |
| D6 | **Empacotamento** | Cliente sobe **um ZIP**; a pipeline detecta/separa os arquivos por entidade. |
| D7 | **Tenant pode já ter dados** | Duplicado (por chave natural) **atualiza** (upsert); novo, cria. Ver §5. |
| D8 | **Formatos do MVP** | **CSV, XLSX e JSON**. XML fica para depois. |
| D9 | **Ordem prontuário** | Pacientes primeiro; prontuário casa por **CPF ou documento internacional**. Ver §6 Onda 2. |
| D10 | **Chave do paciente** | 1º CPF/doc internacional; 2º fallback **nome + telefone** (obrigatórios, sem duplicar); senão **log + pula**. Ver §10. |
| D11 | **PKs e chaves** | Imedto gera as próprias PKs; **nunca** importa id técnico da origem; migra só dados pertinentes; dedupe por dado de negócio do registro. Ver §10. |
| D12 | **Desfazer** | Reverte **só os registros criados**; atualizados não são tocados (relatório avisa). |
| D13 | **Mapa por origem** | Reutilizável **dentro do MVP**: operador salva o de-para aprovado como template por origem; próximo job da mesma origem pré-carrega (operador ainda revisa). |
| D14 | **Limite** | ZIP ≤ **50MB** por job; acima, rejeita com mensagem clara. |

## 4. Por que D1 é a decisão que sustenta o resto

A IA atua **uma vez por arquivo**, sobre metadados, não sobre os dados:

```
Arquivo do cliente (CSV/XLSX/JSON/XML)
  → extrai cabeçalhos + amostra de N linhas (PII mascarada)   ← determinístico
  → 1 chamada à IA: "mapeie estas colunas para o modelo canônico"
  → IA devolve o MAPA: { col_origem → campo_canônico, confiança, dúvidas }
  → operador revisa/ajusta o mapa no painel admin               ← humano no loop
  → código aplica o mapa às N linhas (determinístico)           ← SEM IA por linha
  → valida obrigatórios por registro
        ├─ ok        → enfileira para importação via command existente
        └─ faltou X  → rejeita a linha com motivo ("CPF ausente", "data inválida")
  → relatório: importados / rejeitados (com motivo) / duplicados
```

**Ganhos:** custo baixo (1 chamada por arquivo, não por linha); auditável (mapa revisável antes de gravar nada); LGPD-safe (PII mascarada e em amostra); determinístico na carga (reimportar dá o mesmo resultado); **zero risco de a IA alucinar um CPF ou um medicamento** — ela nunca escreve o dado, só diz *para onde ele vai*.

## 5. Arquitetura — reuso de infra existente (não construir do zero)

As três peças que pareciam novas **já existem no projeto**:

| Peça imaginada | O que já existe | Onde |
|---|---|---|
| Lambda em background | `JobScheduler` nativo (`BackgroundService` + advisory lock no Postgres, multi-instância, retry) | [JobScheduler.cs](../../../backend/src/Services/Imedto.Backend.Infrastructure/Jobs/JobScheduler.cs) |
| "jogar numa IA" | Provider de IA atrás de porta, com rate-limit (Anthropic) | [AnthropicIaService.cs](../../../backend/src/Services/Imedto.Backend.Infrastructure/Ia/AnthropicIaService.cs), [RateLimitedIaService.cs](../../../backend/src/Services/Imedto.Backend.Infrastructure/Ia/RateLimitedIaService.cs) |
| Salvar arquivo no S3 | Provider de storage S3 já usado para anexos/fotos | Infrastructure (storage) |
| Painel de operação | Área de admin completa (backend + front) | `Controllers/Admin/*`, [frontend/src/modules/admin](../../../frontend/src/modules/admin) |

**Consequência:** não há Lambda nova, não há SDK de IA novo, não há infra de fila nova. O trabalho é **domínio + staging + UI de admin + a porta de mapeamento por IA**, tudo dentro do backend (respeita "regra de negócio sempre no backend" e "terceiros atrás de provider").

**Decisões estruturais herdadas da FASE 2B (mantêm):**
- **Staging tables** (`migracao_jobs`, `migracao_registros` com payload bruto + status + erro) — multi-tenant, auditáveis. Nada entra direto nas tabelas de domínio.
- **Importação roda pelos commands existentes** (`CriarPacienteCommand` etc.) — reusa validação, regra de negócio e audit. Nunca INSERT direto.
- **Upsert por chave de negócio** (D7/D11): o tenant **pode já ter dados**. Registro cuja chave de negócio já existe → **atualiza**; senão → cria. PK sempre gerada pelo Imedto; nunca usa id externo da origem. A carga usa **command de criação E de atualização** (não só `Criar*`) — a pipeline resolve a existência por chave antes de decidir qual chamar.
- **Desfazer**: o que um job **criou** carrega `migracao_job_id` → rollback enquanto o job for o único autor. **Atenção:** registros **atualizados** (já existiam) não são apagados no desfazer — no máximo revertidos ao snapshot anterior, se guardarmos o "antes". Decisão de produto sobre profundidade do desfazer em registros atualizados (ver §10).

**Porta nova a definir (na Application/Domain):**
```
IMapeadorDeMigracao
  PropostaDeMapa InferirMapa(EsquemaDeArquivo headersEAmostraMascarada, ModeloCanonico alvo)
```
Implementação concreta na Infrastructure usa o `IaService` existente. Domínio não conhece prompt nem provider.

## 6. Escopo — entidades e ordem de carga (FK)

D3 define duas ondas. Cada entidade-alvo já tem aggregate + command de inserção no domínio (mapa abaixo). **A ordem de carga importa**: cadastros-base (fornecedor, categoria, fabricante, local, catálogos) entram **antes** dos itens que dependem deles; paciente entra **antes** de agenda e prontuário.

### Onda 1

| Entidade do cliente | Destino (aggregate) | Depende de | Observação |
|---|---|---|---|
| Pacientes | `Paciente` → `pacientes` | — | base de tudo; chave natural CPF / nome+nascimento p/ dedupe |
| Cadastros de estoque | `FornecedorEstoque`, `CategoriaEstoque`, `FabricanteEstoque`, `LocalEstoque` | — | **carregar antes dos itens** — viraram entidades próprias (não mais coluna texto) |
| Itens de estoque | `ItemInventario` | cadastros de estoque acima | FK para fornecedor/categoria/fabricante/local |
| Produtos de orçamento | `CatalogoProduto` (Orçamentos/Catalogos) | — | catálogo do estabelecimento |
| Procedimentos de orçamento | `ProcedimentoCatalogo` (Catalogo) | — | catálogo do estabelecimento |
| Agendas | `Agendamento` → `agendamentos` | Paciente (e profissional) | mapear status; tratar paciente avulso |

### Envio parcial — migra só o que veio (regra confirmada)

O cliente **não precisa enviar todas as entidades**. Se mandar só pacientes, migra só pacientes; as demais são ignoradas sem erro. A regra de carga distingue três casos, e a **fonte da verdade do que é obrigatório são os próprios commands de domínio** (`BusinessException`):

1. **Entidade não enviada** → não migra, sem erro. (ex.: cliente manda só `pacientes.csv`.)
2. **Referência (FK) opcional ausente** → insere o registro sem ela. (ex.: `ItemInventario.FornecedorPadraoId`, `FabricanteId`, `LocalPadraoId` são `long?` opcionais — item de estoque entra mesmo sem o arquivo de fornecedores.)
3. **Campo/FK obrigatório ausente** → registro **rejeitado com motivo**. (ex.: `ItemInventario.CategoriaId` é obrigatório → item sem categoria é rejeitado; agenda sem paciente correspondente é rejeitada.)

Como a carga roda pelos commands existentes, "obrigatório" já está codificado no domínio — não há lista paralela a manter. O painel mostra os rejeitados (caso 3) com motivo antes/depois do disparo, e o operador decide se sobe o arquivo que falta ou segue.

### Onda 2 — Prontuário (vinculado aos pacientes da Onda 1)

- **Dependência forte de ordem (D8):** prontuário **só importa depois** dos pacientes. A pipeline carrega pacientes → confirma existência → só então casa os prontuários. Job de prontuário sem a Onda 1 concluída fica bloqueado.
- **Chave de vínculo:** o prontuário precisa de um **identificador do paciente** — **CPF ou documento internacional** — para localizar o paciente já migrado. Sem identificador que resolva o vínculo → registro **rejeitado com motivo** ("paciente não identificado"). Não se cria paciente a partir do prontuário.
- **Tratamento honesto da estrutura:**
  - origem **com campos identificáveis** → evolução estruturada via mapa.
  - origem **sem estrutura** (PDF/relatório corrido) → entra como **anexo/documento histórico pesquisável** no paciente, **não** como evolução estruturada falsa (decisão já firmada na FASE 2B §3).
  - **Receita controlada / ANVISA**: regra clínica forte (tipo+notificação A/B/C/Especial) validada pelos commands existentes; combinação inválida **bloqueia** o registro — a IA nunca "adivinha" o tipo.

## 7. Fluxo — do cliente ao disparo no admin (D4 + D5)

**Lado do cliente — Configuração do Estabelecimento (app do cliente):**
1. Cliente acessa uma seção "Migrar meus dados" nas Configurações do estabelecimento e **faz upload de um ZIP** (CSV/XLSX/JSON dentro). Como está logado no próprio tenant, o job já nasce vinculado ao **`estabelecimento_id` correto** — sem o operador precisar associar nada.
2. ZIP bruto → S3 (criptografado, retenção 30 dias). Sobe um `migracao_job` (status `aguardando_mapa`). A pipeline **descompacta e separa os arquivos por entidade**; `JobScheduler` dispara a inferência de mapa via IA (amostra mascarada) por arquivo.

**Lado do admin — painel da plataforma Imedto (`modules/admin`):**
3. **Fila de migrações** — operador vê os jobs já vinculados ao estabelecimento de origem.
4. **Revisão do mapa** — vê o de-para proposto pela IA por arquivo/entidade, com **confiança e dúvidas destacadas**, e ajusta o que for preciso. *(ver exatamente o que está acontecendo)*
5. **Preview** — "Encontramos 1.243 pacientes, 3.481 agendamentos, 512 itens de estoque; **37 registros serão rejeitados** (motivo por linha); 8 itens sem fornecedor correspondente". 
6. **Disparo (1 clique)** — operador clica **"Migrar"** → inserção assíncrona, em lotes, **de todas as linhas** via commands existentes, respeitando a ordem de FK (§6). Barra de progresso ao vivo.
7. **Relatório** — importados / rejeitados (com **motivo por registro**) / duplicados; botão **desfazer**; tudo no audit trail. *(ver os problemas que houve)*

**Estados do job:** `aguardando_arquivo → aguardando_mapa → mapa_em_revisão → preview_pronto → migrando → concluído (com erros?) → desfeito`. Cada transição auditada.

## 8. LGPD (premissa não-negociável)

- **Amostra mascarada** (D2): a IA só recebe cabeçalhos + poucas linhas com PII ofuscada. O volume real nunca sai do nosso ambiente para a LLM.
- **Arquivo bruto**: criptografado no S3, **retenção 30 dias**, apagado por job. Justificativa de base legal: CFM 1.821 (prontuário é do paciente) + LGPD Art. 18 (portabilidade).
- **Termo de responsabilidade** do cliente sobre a licitude dos dados enviados.
- **Audit trail** da migração inteira (quem aprovou, o que importou, o que rejeitou).
- **Multi-tenant**: todo `migracao_job`/`migracao_registro` carrega `estabelecimento_id`; importação só grava no tenant destino.
- **Mascaramento**: avaliar reuso de [IAnonimizacaoService](../../../backend/src/Services/Imedto.Backend.Domain/Lgpd/IAnonimizacaoService.cs) para gerar a amostra mascarada.

## 9. Riscos e mitigações

| Risco | Mitigação |
|---|---|
| IA erra o de-para | Humano no loop revisa o mapa **antes** de qualquer gravação; confiança + dúvidas destacadas no painel |
| Header genérico/ambíguo (`campo1`) | Amostra mascarada ajuda a desambiguar; operador corrige manualmente; mapa fica salvo por origem para reuso |
| Volume grande trava | Processamento 100% assíncrono em lotes (`JobScheduler`); IA só vê amostra, não o volume |
| Alucinação em dado clínico | IA não escreve dado — só mapeia colunas; carga é determinística; obrigatório ausente = rejeita com motivo |
| PII vaza para a LLM | Só amostra mascarada (D2); DPA com o provider de IA a confirmar |
| Reimportar duplica | Idempotência por chave natural |
| Receita/controlada com regra ANVISA | Validação pelos commands existentes; combinação inválida bloqueia o registro |

## 10. Pontos em aberto para o BA destravar antes do briefing

**Já decididos** — escopo em 2 ondas (§6, D3); canal = upload na Config do Estabelecimento (D4); disparo por 1 clique no admin reusando commands (D5); ZIP separado pela pipeline (D6); upsert sem duplicar em tenant que já tem dados (D7); formatos CSV/XLSX/JSON (D8); ordem + vínculo de prontuário por CPF/doc internacional (D9); IA mapeia schema com amostra mascarada (D1, D2).

### Regra inegociável — nunca duplicar (D7)

**Princípio das chaves (D11):** o Imedto **gera suas próprias PKs** no padrão dele. **Nunca** importa nem usa o identificador técnico do sistema de origem (o "id do agendamento no iClinic"). Migra apenas **dados pertinentes** (minimização — alinhado com LGPD). O dedupe usa **dado de negócio do próprio registro** (CPF do paciente, CNPJ do fornecedor, código do produto do estabelecimento), que pertence ao cliente — não ao sistema de origem.

Upsert por **chave de negócio confiável por entidade**, sempre dentro do tenant (`estabelecimento_id`). Se a chave existe → atualiza; se não → cria; **se o registro não tem chave que garanta unicidade → rejeita com motivo** (em vez de arriscar duplicar). Chaves:

| Entidade | Chave de negócio (nunca id externo) |
|---|---|
| Paciente | **hierarquia (D10):** 1º CPF ou documento internacional → 2º fallback **nome + telefone** (ambos obrigatórios, combinação única) → senão **log + pula** |
| Fornecedor de estoque | CNPJ → fallback Nome único no tenant → senão rejeita |
| Item de estoque | `Codigo` → fallback Nome único no tenant → senão rejeita |
| Categoria / Fabricante / Local de estoque | Nome único no tenant → senão rejeita |
| Produto / procedimento de orçamento | Código → fallback Nome único do catálogo → senão rejeita |
| Agenda | **paciente + profissional + data/hora** (dados de negócio) → senão rejeita |
| Prontuário | vínculo resolvido por paciente via CPF/documento (D9) |

#### Hierarquia de chave do paciente (D10)

1. **CPF** ou **documento internacional** → chave forte de dedupe.
2. **Sem CPF/documento:** usa **nome + telefone** como chave. Ambos **obrigatórios** nesse caminho; a combinação `nome+telefone` **não pode duplicar** (upsert atualiza se já existe). Entra como **pré-cadastro** (dados a completar depois).
3. **Sem CPF e sem (nome+telefone completos):** **não importa o registro — gera log e passa para o próximo.** Não bloqueia o job; o paciente simplesmente não entra e fica registrado no relatório/log para o operador conferir.

> A nota fica no relatório do job ("N pacientes pulados por falta de identificador"). Como prontuário (Onda 2) casa por CPF/documento (D9), pacientes que entraram só por nome+telefone podem não receber prontuário automático — sinalizar isso ao operador.

**Resiliência da inferência (addendum 5 — 2026-06-15_006):** após o job #12 perder 5 blocos bons quando o 6º recebeu 429 da Anthropic, a inferência por bloco ganhou resiliência **sem violar D1/D2/D11**: (a) **retry com backoff exponencial + jitter respeitando `Retry-After`** em 429/529/falha de rede, teto 5 tentativas, 4xx≠429 permanente (espelha `ResendEmailService`); (b) **pausa fixa ~1s entre blocos** (o mapeador não passa pelo `RateLimitedIaService`); (c) **truncamento de cada valor da amostra a 500 chars após a máscara** (corta `conteudo_html`/base64 que estouravam o TPM — D2 preservado); (d) **degradação graciosa por bloco** — falha de um bloco vira mapa de erro (`bloco_com_erro` no `mapa_json`) e a inferência continua; job só vai a `falhou` com **zero sucesso**, senão `mapa_em_revisao` com aviso e reprocessamento parcial (pula blocos OK). Sem migration. **Risco residual:** conta de IA tier muito baixo é dependência externa — o código fica resiliente, mas o tier ainda limita; mitigado por espaçamento configurável e degradação por bloco, resolvido subindo tier / integrando rate-limiter central (backlog).

**Ainda em aberto (não bloqueiam o briefing):**
1. **DPA com provider de IA**: acordo de tratamento de dados que cubra envio de amostra mascarada (jurídico — dependência externa).
2. **Self-service futuro**: confirmar que a porta `IMapeadorDeMigracao` e a staging servem a um wizard self-service depois (FASE 2B §5) sem retrabalho.

> Todos os pontos de regra de negócio estão fechados (D1–D14). O briefing pode ser cravado.

## 11. Próximos passos

1. Usuário confirma as **chaves naturais por entidade** (§10.1) — único ponto que ainda bloqueia regra de negócio.
2. **`imedto-business-analyst`** escreve o briefing imutável com CAs testáveis (multi-tenant, LGPD, estados do job, rejeição com motivo, upsert sem duplicar, ordem de FK, performance da carga).
3. **`imedto-database`** modela `migracao_jobs` / `migracao_registros` (staging multi-tenant + índices).
4. **`imedto-developer`** implementa porta `IMapeadorDeMigracao` + pipeline + painel admin + reuso dos commands de domínio.
5. Atualizar [FASE_2B_CENTRAL_DE_MIGRACAO.md](../../Roadmap/FASE_2B_CENTRAL_DE_MIGRACAO.md) registrando a evolução "adaptadores → mapa por IA" (documentação viva).
</content>
</invoke>
