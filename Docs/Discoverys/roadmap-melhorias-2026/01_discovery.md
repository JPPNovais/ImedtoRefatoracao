# Discovery — Roadmap de Melhorias 2026 (produto, UX, código e operação)

> **Data**: 2026-06-09 · **Método**: 3 análises independentes — (a) inventário funcional do código, (b) pesquisa de mercado healthtech BR ([`02_pesquisa_mercado.md`](02_pesquisa_mercado.md)), (c) auditoria técnica read-only do monorepo.
>
> **Pergunta que este discovery responde**: o que melhorar/construir no Imedto — e em que ordem — para que o sistema fique escalável e entregue valor suficiente para alguém **assinar e permanecer assinando**.

---

## 1. Resumo executivo

1. **O núcleo clínico está pronto e é acima da média** — agenda, pacientes, prontuário, exame físico, receitas/atestados/pedidos, cirurgias+orçamentos (mais profundo que a média do mercado), equipe/RBAC, LGPD por arquitetura, admin global com planos e feature flags. A engenharia de aplicação (CQRS+DDD, testes com banco real, multi-tenant blindado) está pronta para cobrar.
2. **O que NÃO está pronto para cobrar é a operação de dados**: Postgres em container num `t3.micro` único, **sem backup automatizado**, sem alertas, observabilidade instrumentada mas sem destino. Para dado de saúde pago, isso é bloqueador absoluto — vem antes de qualquer feature.
3. **O mercado é consolidado (Afya/iClinic + Docplanner/Doctoralia-Feegow)** e o piso de preço (R$75-99/prof/mês) é apertado. Entrante não ganha por paridade de features: ganha por **nicho (cirúrgico), IA nativa, confiabilidade, anti-lock-in e atendimento**.
4. **As 3 dores nº1 do comprador** que o Imedto ainda não cobre: confirmação WhatsApp (no-show), agendamento online pelo paciente, e validade jurídica/prescrição digital (ICP + Memed). As duas últimas têm **base já construída** (BirdID stub, links públicos por token, automações) — são mais baratas do que parecem.
5. **A aposta de diferenciação é IA scribe nativa PT-BR** (consulta → evolução transcrita e estruturada). A janela competitiva fecha em 12-24 meses; a infra de IA do Imedto (settings, rate limit, audit, sugestão de seção) já existe e dá vantagem de partida.

---

## 2. O que o Imedto já tem (inventário — jun/2026)

Visão consolidada; detalhe completo no código (53 controllers, 33 bounded contexts, 43 views, 10 stores).

| Módulo | Maturidade | Observação |
|---|---|---|
| Auth & Onboarding | 🟢 Completo | JWT ES256, BFF cookies, confirmação e-mail, convites, reset |
| Agenda & Agendamentos | 🟢 Completo | Check-in, disponibilidade, salas, **link público de confirmação por token** |
| Pacientes | 🟢 Completo | CRUD, busca trigram, exportação LGPD, aba Documentos consolidada |
| Prontuário | 🟢 Completo | Evoluções, exame físico, anexos S3, modelos customizáveis, pool de variáveis |
| Receitas | 🟢 Completo* | Rascunho→Emitida, favoritos, validação; *PDF endpoint = stub 501; assinatura BirdID = stub |
| Atestados / Pedidos de exame | 🟢 Completo | Com modelos |
| Cirurgias + Orçamentos | 🟢/🟡 90% | Planejamento→realização, equipe, ficha anestésica, conversão orçamento→cirurgia. **Profundidade rara no mercado** |
| Termos de consentimento | 🟢 Completo* | Versionamento, aceite público por token; *PDF on-the-fly = stub |
| Financeiro | 🔶 Básico | Lançamentos, categorias, formas de pagamento, resumo. Sem conciliação/NFS-e/inadimplência |
| Estoque | 🟡 70% | CRUD + movimentação; sem custo médio/alertas de mínimo |
| Relatórios | 🟡 85% | 6 abas (financeiro, agendamentos, operacional, pessoas, orçamentos, IA); sem export/drill-down |
| Automações | 🟡 80% | Regras + eventos (lembrete e-mail, expirar orçamento); sem canais WhatsApp/SMS |
| IA | 🟡 75% | Sugestão por seção de prontuário, settings por tenant, rate limit, audit com hash |
| Notificações | 🟢 Completo | Realtime via Postgres listen/notify |
| Equipe & RBAC | 🟢 Completo | Vínculos multi-estabelecimento, papéis, modelos de permissão granulares |
| Assinaturas & Planos | 🟢 Completo | Feature flags por plano, trial, bloqueio 402, admin global |
| Admin global | 🟢 Completo | KPIs, estabelecimentos, planos, modelos globais |
| Mobile (Capacitor) | 🟡 30% | Agenda/pacientes/prontuário parciais |

**Stubs prontos para "ligar"** (código existe, falta integração final): PDF de receita (QuestPDF pronto no back), PDF de termo, BirdID OAuth2+PAdES (aguarda canal comercial).

**Ausências confirmadas**: WhatsApp, agendamento online público, telemedicina, portal do paciente, Memed, TISS, NFS-e, 2FA, Google Calendar sync, API pública de integração.

> ⚠️ Nota de exatidão: documentação antiga menciona "RDS Multi-AZ + Auto Scaling". A realidade verificada (docker-compose + INFRA.md atual) é **EC2 `t3.micro` única com Postgres em container, app, nginx e Caddy co-residentes** — ver §5.

---

## 3. Mercado em uma página

Detalhe completo com fontes em [`02_pesquisa_mercado.md`](02_pesquisa_mercado.md).

- **Estrutura**: duopólio de ecossistemas — Afya (iClinic, Shosp) e Docplanner (Doctoralia 570k perfis + Feegow). Long tail de dezenas de players regionais/nicho.
- **Preço**: entrada R$75-99/prof/mês; Doctoralia R$429-679 (inclui aquisição de paciente); IA scribe vendida como add-on de ~R$199.
- **Dores ranqueadas**: (1) no-show 20-32% da agenda; (2) suporte ruim + migração dolorosa — campeãs do Reclame Aqui; (3) instabilidade durante atendimento (líder tem 6.5/10 no RA); (4) tempo de documentação clínica; (5) glosas TISS (só dói em quem fatura convênio); (6) financeiro fragmentado; (7) add-ons que inflam preço; (8) agenda telefônica.
- **Commodity em 2026**: telemedicina, Memed, ICP-Brasil, NFS-e, agendamento online — são tabela-estaca (faltar elimina, ter não diferencia).
- **Ainda diferencial**: IA scribe nativa, confiabilidade/velocidade, migração sem dor, atendimento humano, profundidade de nicho.

---

## 4. Gap analysis — funcionalidades (mercado × inventário real)

Ordenado por impacto na decisão de assinatura ÷ esforço, considerando a base já construída:

| Prioridade | Capacidade | Dor de mercado | Esforço | Vantagem de partida no Imedto |
|---|---|---|---|---|
| 🔴 P1 | **Lembrete/confirmação WhatsApp** | No-show (dor #1) | M | Discovery `whatsapp-envio/` feito; automações + link público de confirmação já existem — falta o canal |
| 🔴 P1 | **Ativar assinatura ICP-Brasil** | Validade jurídica de receita/atestado | P-M | BirdID stub + webhook HMAC prontos; alternativa VIDaaS (cert. CFM grátis); discovery feito |
| 🔴 P1 | **PDF de receita servidor (destravar stub 501)** | Completude básica | P | QuestPDF implementado; só integrar |
| 🟠 P2 | **Agendamento online pelo paciente** (link público da agenda) | Aquisição/agenda 24/7 | M | Disponibilidade + endpoints públicos por token já existem |
| 🟠 P2 | **Memed embed** (prescrição digital) | Expectativa básica | P-M | Receitas + favoritos existem; integração é SDK front + webhook |
| 🟠 P2 | **Migração assistida** (importar pacientes/histórico CSV/Excel) | Dor #2 do mercado — vira arma de aquisição | M | Exportação LGPD já existe (é o caminho reverso) |
| 🟡 P3 | **IA scribe PT-BR** (áudio consulta → evolução estruturada) | Tempo de documentação (dor #4) — **diferencial** | G | Infra IA completa (settings/rate-limit/audit); prontuário estruturado pronto para receber |
| 🟡 P3 | **Financeiro completo** (contas a receber por atendimento, inadimplência, conciliação) + **NFS-e** | Financeiro fragmentado (dor #6) | M-G | Lançamentos básicos existem; discovery `nota-fiscal/` feito |
| 🟡 P3 | **BI gerencial** (taxa no-show, ocupação de agenda, produtividade/prof, funil de orçamentos) | Decisor (gestor) compra por isto | M | 6 abas de relatórios existem; é evolução, não construção |
| 🟢 P4 | **Telemedicina** (vídeo + receita no fluxo) | Paridade (commodity) | M | Agenda + links públicos prontos; avaliar build (WebRTC/Daily) vs buy |
| 🟢 P4 | **Portal do paciente** (documentos, reagendamento) | Retenção/modernidade | M-G | Aba Documentos + tokens públicos são a fundação |
| ⚪ Adiar | **TISS/convênios** | Glosas (só clínica conveniada) | GG | Sem base; baixa alavanca para o ICP atual — reavaliar com tração |

Completar os parciais também conta como gap (são percepção de produto inacabado): estoque 70%→100% (alertas de mínimo), relatórios 85%→100% (export CSV), orçamentos Fase 6, automações (novos canais).

---

## 5. Melhorias técnicas — operação, segurança e escalabilidade

Da auditoria técnica (notas por área: backend 9/10, segurança 8.5/10, testes 8/10, frontend 6.5/10, performance 6/10, operação 6.5/10, DX 7/10). **Veredito: a aplicação está pronta para cobrar; a operação de dados não está.**

### Bloqueadores de monetização (fazer antes de vender)

| # | Item | Risco | Esforço |
|---|---|---|---|
| 1 | **Backup automatizado do Postgres** (`pg_dump` diário → S3 versionado + **teste de restore**) | Crítico — hoje uma falha de disco = perda total e irreversível de dados de saúde | P |
| 2 | **Separar Postgres do compute** (RDS pequeno ou instância dedicada) — banco compete por 1GB de RAM com o .NET | Crítico — quebra antes de qualquer otimização | M |
| 3 | **Snapshot/dump automático antes de cada migration no CI** | Alto — migration ruim hoje não tem rollback | P |
| 4 | **Alertas de host (CPU/RAM/disco) + OTel apontado para um coletor** | Alto — sistema instrumentado mas cego; degrada em silêncio | M |

### Segurança (hardening barato e de alto valor)

| # | Item | Esforço |
|---|---|---|
| 5 | **`FallbackPolicy` global de autenticação** (default-deny) — hoje controller novo sem `[Authorize]` nasce anônimo (51/53 cobertos) | P |
| 6 | **Restringir SSH `0.0.0.0/0`** no Security Group da EC2 | P |
| 7 | **Validação de MIME por magic bytes** no upload de anexos (hoje confia no header do cliente) | P |
| 8 | **2FA (TOTP)** — além de segurança, é argumento de venda para dado de saúde | M |

### Qualidade contínua (guard-rails de CI)

| # | Item | Esforço |
|---|---|---|
| 9 | **Ligar Vitest no CI como gate** — 59 arquivos de teste front existem e nunca rodam no pipeline | P |
| 10 | **Incluir integration tests (Testcontainers) no CI** — justamente os que provam isolamento multi-tenant só rodam localmente | P |
| 11 | **Lint rule que falha build em `font-size`/`font-weight` literais** — regra §5 do CLAUDE.md tem ~1.300 violações; sem gate, cresce a cada PR | P |

### Manutenibilidade (pagar aos poucos)

| # | Item | Esforço |
|---|---|---|
| 12 | **Quebrar god components** — `NovoAgendamentoModal.vue` (1.841 linhas), `OnboardingView` (1.717), `OrcamentoFormView` (1.467), `PacienteDetalheView` (1.408), `SecaoExameFisico` (1.142). 1 por ciclo, começando pelos de maior tráfego clínico | G |
| 13 | **Resolver duplicação de design system** — `@imedto/ui` (pacote, 185MB, ~3 imports) vs `components/ui/` local (37 componentes, fonte real). Decidir e matar um | M |
| 14 | **Registro de handlers por assembly scanning** — `Container.cs` com 1.282 linhas de registro manual; esquecer registro só estoura em runtime | M |
| 15 | **Higiene do monorepo** — `ReferenciaLegado/` (46MB), `mobile/` (152MB), `design-system/` (185MB) para repos próprios; reduzir 288 `any` (catch → `unknown`); limpar comentários de infra desatualizados | M |

---

## 6. Melhorias de UX

1. **Fluxo de agendamento**: o modal de 1.841 linhas indica fluxo sobrecarregado — redesenhar em etapas curtas (paciente → horário → confirmação), com criação rápida de paciente inline. É a tela de maior frequência de uso da recepção.
2. **Onboarding orientado a valor** (1.717 linhas hoje): meta = "primeiro agendamento criado em < 10 minutos", com seed de dados de exemplo e checklist de primeiros passos. Onboarding ruim é a dor #2 do mercado.
3. **Velocidade percebida como feature**: skeleton states consistentes, optimistic UI nas ações frequentes (check-in, confirmar), e orçamento de performance (p95 < 500ms nas telas de atendimento). "Não trava durante o atendimento" é diferencial real contra o líder (6.5/10 no RA).
4. **Atalhos e densidade para usuário recorrente**: secretária usa o sistema 8h/dia — command palette (Ctrl+K para buscar paciente/ação), navegação por teclado na agenda.
5. **Acessibilidade e responsivo**: auditar WCAG AA nas telas core; a agenda em tablet é cenário comum de recepção.
6. **Terminar a padronização tipográfica** (item 11 do §5) — consistência visual é percepção de qualidade.

---

## 7. Inovação — onde o Imedto pode ser diferente (não só igual)

> Especificação completa (ficha por diferencial + matriz de priorização, 16 itens) em [`04_diferenciais.md`](04_diferenciais.md).

1. **IA scribe nativa PT-BR** (a aposta): gravar a consulta (com consentimento via módulo de termos **já existente**), transcrever e estruturar a evolução no modelo de prontuário do profissional. Diferenciais possíveis sobre os add-ons do mercado: incluída no plano (não +R$199), audit trail LGPD nativo (infra já existe), e sugestão de CID/conduta como rascunho a revisar. Janela: 12-24 meses antes de virar commodity.
2. **Vertical cirúrgica como nicho de entrada**: o módulo orçamento→cirurgia→ficha anestésica é raro no mercado. Dobrar a aposta: templates por especialidade cirúrgica (plástica, dermato, bucomaxilo), fotos de antes/depois com consentimento, acompanhamento pós-operatório. Posicionar como "o sistema para quem opera" em vez de genérico.
3. **Anti-lock-in como marketing**: exportação completa 1-clique (base LGPD já existe) + migração assistida de entrada + cancelamento self-service. Ataca frontalmente a maior reclamação do mercado e custa pouco.
4. **Automações como plataforma**: o motor de regras existente pode evoluir para "Zapier da clínica" — gatilhos (no-show, orçamento parado, pós-consulta) × canais (WhatsApp, e-mail, tarefa) configuráveis pelo dono.
5. **Confiabilidade publicada**: status page pública + SLA declarado — nenhum concorrente pequeno faz; vira prova de seriedade.

---

## 8. O plano — sequência recomendada

> **Atualização 2026-06-10**: o plano abaixo foi detalhado em **arquivos de fase de implementação** em [`Docs/Roadmap/`](../../Roadmap/README.md) (F0, F1, F2, F2B central de migração, F3, F4 e fase transversal de qualidade) — usar os arquivos de lá para executar. A **Fase 0 está planejada com execução adiada** (produto em desenvolvimento); gatilho: antes do 1º cliente pagante ou de dado real em produção.

### Princípio de sequenciamento
**Operação antes de venda; completude antes de novidade; conversão antes de diferenciação; diferenciação antes de paridade.**

```
F0 Fundação (1-2 sem)  →  F1 Completude (3-5 sem)  →  F2 Conversão (1 trimestre)
                                                              ↓
            contínuo: qualidade/UX  ←  F4 Paridade & expansão  ←  F3 Diferenciação
```

### Fase 0 — Fundação operacional (bloqueia tudo; ~1-2 semanas)
Itens §5.1-6 e 9-10: backup+restore testado, snapshot pré-migration, separar Postgres do compute, alertas+OTel, FallbackPolicy, SSH, gates de CI (Vitest + integration).
**Critério de saída**: restore testado com sucesso; alerta dispara em simulação; CI falha se teste de isolamento falhar. *Sem pipeline de BA — são demandas técnicas diretas (dev+QA).*

### Fase 1 — Completude (destravar o que está a 90%; ~3-5 semanas)
PDF de receita (stub→produção), PDF de termos, ICP-Brasil (decidir BirdID vs VIDaaS e ativar), estoque 100% (alertas de mínimo), relatórios com export, orçamentos Fase 6, 2FA, lint tipográfico.
**Critério de saída**: zero endpoints 501; demo de venda sem "isso ainda não funciona".

### Fase 2 — Conversão (o combo que fecha assinatura; ~1 trimestre)
1. **WhatsApp** (lembrete/confirmação ligados às automações) — retomar discovery `whatsapp-envio/`.
2. **Agendamento online público** (página por estabelecimento, anti-spam, aprovação opcional da recepção).
3. **Memed** embed.
4. **Migração assistida** (importador CSV/Excel de pacientes + histórico).
**Critério de saída**: clínica piloto medindo queda de no-show; "migre em 1 dia" demonstrável.

### Fase 3 — Diferenciação (~1-2 trimestres, em paralelo ao final da F2)
1. **IA scribe PT-BR** (MVP: upload de áudio → evolução rascunho; depois gravação ao vivo). Exige discovery próprio (STT em PT-BR médico, custo/consulta, consentimento, LGPD).
2. **Financeiro completo + NFS-e** (retomar discovery `nota-fiscal/`) — abre o ICP de gestor.
3. **BI gerencial** (no-show, ocupação, produtividade, funil de orçamentos) — evolução das 6 abas.

### Fase 4 — Paridade e expansão (conforme tração)
Telemedicina (decidir build vs buy), portal do paciente, push mobile + evolução do app (30%→core), Google Calendar sync, API pública. **TISS só com demanda comprovada de clínicas conveniadas.**

### Trilha contínua (toda fase reserva ~20% da capacidade)
1 god component refatorado por ciclo · redução de `any` · decisão design system único · UX da agenda e onboarding · documentação viva.

---

## 9. Como executar (processo, no fluxo deste repo)

1. **Cada item vira demanda para o `imedto-business-analyst`** → briefing imutável em `planejamentos/` com CAs Dado/Quando/Então (multi-tenant + RBAC + LGPD + estados + performance obrigatórios). Itens de infra da F0 são exceção (demanda técnica direta dev+QA, sem BA).
2. **Discovery antes de briefing** quando há decisão externa cara de reverter (novo bounded context, vendor, regulação): WhatsApp provider (BSP), ICP (BirdID vs VIDaaS), IA scribe (STT/custos/consentimento), telemedicina (build vs buy), NFS-e (gateway vs Sistema Nacional). Três desses discoveries **já existem** — retomar, não recriar.
3. **Pipeline padrão**: BA → dev (→ database se schema muda) → QA (único que commita/pusha; 1 push por sessão).
4. **Feature flags por plano já existem** — cada feature nova nasce atrás de flag (`whatsapp`, `ia_scribe`, `agendamento_online`...), o que habilita o empacotamento comercial sem retrabalho.
5. **Medir para refinar** (instrumentar desde a F2): conversão trial→pago, churn mensal, taxa de no-show dos clientes (prova de valor do WhatsApp), % de evoluções via IA scribe, tempo até 1º agendamento (onboarding), uptime publicado.
6. **Cadência de revisão**: ao fim de cada fase, reavaliar este discovery contra os números — prioridade de F3/F4 pode mudar com feedback de clínica piloto.

## 10. Posicionamento e pricing (recomendação para validar)

> Estratégia completa (3 tiers, distribuição de features, franquias, modelo de cobrança, oferta fundadora) em [`05_planos_e_pricing.md`](05_planos_e_pricing.md).

- **Nicho de entrada**: clínicas/consultórios com perfil cirúrgico-estético (onde o produto já é mais fundo que o mercado), expandindo para clínicas gerais pequenas.
- **Preço**: R$99-129/prof/mês, **tudo incluído** (WhatsApp, agendamento online, financeiro, ICP) — contraposicionamento direto aos add-ons da Amplimed e à opacidade da iClinic. IA scribe incluída no tier superior (não como add-on).
- **Mensagens**: "o preço que você vê é o que você paga" · "seus dados são seus — entre e saia quando quiser" · "não trava no meio do atendimento".
- **Canal**: integrar (não competir) com Doctoralia; migração assistida como oferta de aquisição.

## 11. Riscos e questões em aberto

| Risco/questão | Mitigação |
|---|---|
| Canal comercial BirdID continua travado | Plano B já mapeado: VIDaaS/cert. CFM (discovery `assinatura-digital-receitas/`) |
| Custo por conversa WhatsApp + aprovação Meta BSP | Discovery `whatsapp-envio/` define provider; repassar custo no plano se necessário |
| IA scribe: custo de STT por consulta e responsabilidade clínica | MVP com revisão obrigatória do profissional (rascunho, nunca auto-commit); medir custo/consulta no piloto |
| Capacidade de execução (time pequeno, roadmap largo) | As fases são sequenciais por design; F2 só começa com F0+F1 fechadas; cortar escopo de F4 antes de F2 |
| Preços/dados de mercado envelhecem | Reverificar `02_pesquisa_mercado.md` antes de decisão comercial (válido ~6 meses) |
