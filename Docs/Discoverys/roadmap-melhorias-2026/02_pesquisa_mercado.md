# Pesquisa de Mercado — Software de Gestão de Clínicas no Brasil (jun/2026)

> Insumo do discovery [`01_discovery.md`](01_discovery.md). Pesquisa conduzida em 2026-06-09 com fontes públicas (sites oficiais, comparativos, Reclame Aqui, imprensa especializada).
>
> **Nota de correção pós-inventário**: a pesquisa partiu da premissa de que o Imedto não tinha financeiro, relatórios e IA. O inventário do código mostrou que **financeiro básico (lançamentos/categorias/resumo), relatórios em 6 abas, automações e sugestões de IA já existem**. As tabelas abaixo foram corrigidas onde aplicável; o gap real está consolidado no `01_discovery.md §4`.

---

## A descoberta que muda o enquadramento de tudo

O mercado já está consolidado em dois grandes grupos, e o resto é long tail:

- **Afya** (NASDAQ: AFYA, grupo de educação médica com 60k+ médicos formados/ano na base): comprou **iClinic** (2021), **Shosp** (R$5,98M, sendo migrado para o iClinic), **Glic**, e tem Pebmed/Whitebook, Medcel, CardioPapers — ~10 healthtechs no ecossistema. O diferencial não é o software: é a **distribuição via base de estudantes e médicos** que já estão no funil educacional da Afya.
- **Docplanner / Doctoralia** (grupo espanhol global): dono do **Doctoralia** (570k+ perfis de profissionais no Brasil — o maior canal de aquisição de pacientes do país) e comprou a **Feegow** (líder de gestão). Estratégia: marketplace de demanda + software de gestão no mesmo guarda-chuva.

**Implicação para o Imedto:** competir de frente por paridade de features é uma guerra perdida — seria enfrentar capital de educação médica (Afya) e capital global com marketplace de aquisição embutido (Docplanner). A diferenciação **tem** que vir de nicho, UX, IA nativa, atendimento ou preço — não de "ter tudo que eles têm".

Fontes: [Medicina S/A — Afya/Shosp](https://medicinasa.com.br/afya-shosp/), [Afya — consolidação de liderança](https://www.afya.com.br/noticias/grupo-consolida-lideranca-em-servicos-digitais-para-o-medico), [Medicina S/A — Doctoralia Panorama 2025](https://medicinasa.com.br/doctoralia-panorama-2025/).

---

## 1. Mapa competitivo BR

### Por segmento

| Segmento | Líderes | Posicionamento |
|---|---|---|
| **Autônomo / consultório solo** | ProDoctor, HiDoctor, Ninsaúde, iClinic Starter | Preço baixo, prontuário + agenda, pouca gestão |
| **Clínica pequena (2-10 profs)** | iClinic, Amplimed, Feegow, Shosp, GestãoDS | Multi-usuário, financeiro básico, TISS opcional |
| **Clínica média/grande, multiunidade** | Feegow, Clinicorp (odonto), Ninsaúde (franquias), Pixeon | TISS robusto, estoque, BI, multiunidade |
| **Aquisição de pacientes (camada acima)** | Doctoralia | Marketplace + agenda; não é "ERP", é canal |

### Pricing público (R$/profissional/mês)

| Player | Entrada | Topo | Modelo | Observação |
|---|---|---|---|---|
| **Ninsaúde Apolo** | R$79 | — | por prof | 2 meses grátis no anual; mais barato dos completos |
| **ProDoctor** | R$75 (Cloud) | R$195 | tiers | telemedicina avulsa R$2,95/teleconsulta |
| **Amplimed** | R$89-99 | — | **modular** | add-ons dobram/triplicam o valor real |
| **HiDoctor** | R$110 (promo 6m) | R$195 | flat | **funciona offline** (diferencial p/ internet ruim) |
| **Feegow** | Free / R$129 (Plus) | R$149 (Pro) | por prof | Free até 100 pacientes; >10 profs personalizado |
| **Shosp** | R$89 | R$299 | 4 tiers | sendo absorvido pela iClinic |
| **iClinic** | **não publica** | — | sales-led | Starter/Pro/Premium — opacidade é reclamação |
| **Doctoralia** | R$429 (Starter) | R$679 (VIP) | por prof | + Noa Notes IA R$199 + site R$99; **caro, mas inclui aquisição** |
| **Simples Dental** (odonto) | R$128,94 | — | por prof | essencial, p/ quem não planeja crescer |
| **Clinicorp** (odonto) | R$149,90 | — | por prof | escalável, multiunidade/franquia |

**Faixa de entrada típica do mercado: R$75-99/prof/mês.** O piso de preço já é apertado.

### Tabela comparativa de features — os 6 principais vs. Imedto

✅ completo · 🔶 parcial/add-on · ❌ ausente · 🔍 não confirmado

| Capacidade | iClinic | Amplimed | Feegow | ProDoctor | Ninsaúde | **Imedto (hoje)** |
|---|---|---|---|---|---|---|
| Prontuário (anamnese/evolução) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Agenda | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Receita/atestado/pedido PDF | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (PDF receita: stub a integrar) |
| Multi-estabelecimento/equipe | ✅ | ✅ | ✅ | 🔶 | ✅ (franquias) | ✅ |
| Cirurgias/orçamentos cirúrgicos | 🔶 | 🔶 | 🔶 | 🔶 | 🔶 | ✅ (diferencial!) |
| **Confirmação WhatsApp** | ✅ | ✅ | ✅ | 🔶 | ✅ | ❌ (discovery feito) |
| **Agendamento online (paciente)** | ✅ | ✅ | ✅ (+Doctoralia) | 🔶 | ✅ | ❌ |
| **Financeiro / fluxo de caixa** | ✅ | ✅ | ✅ | ✅ | ✅ | 🔶 (lançamentos/categorias/resumo básicos) |
| **TISS / convênios** | ✅ | ✅ | ✅ | 🔶 | ✅ | ❌ |
| **Prescrição digital integrada** | ✅ (Memed) | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Telemedicina** | ✅ | ✅ | ✅ | 🔶 (avulso) | ✅ | ❌ |
| **Assinatura ICP-Brasil** | ✅ | ✅ | ✅ | ✅ | ✅ | 🔶 (BirdID stub pronto, aguarda canal) |
| **NFS-e** | ✅ | ✅ | ✅ | 🔶 | ✅ | ❌ (discovery feito) |
| **BI / relatórios gerenciais** | ✅ | ✅ | ✅ | ✅ | ✅ | 🔶 (6 abas básicas) |
| **IA scribe (transcrição)** | 🔍 | ✅ | ✅ | 🔍 | 🔍 | ❌ (mas infra de IA já existe) |
| LGPD / audit nativo | 🔶 | 🔶 | 🔶 | 🔶 | 🔶 | ✅ (premissa de arquitetura) |

**Leitura crítica:** o Imedto cobre o **núcleo clínico** (prontuário + agenda + documentos + cirurgias/orçamentos — este último mais profundo que a média) e tem **LGPD/audit como premissa de design** — algo que nos incumbentes é checklist, não arquitetura. Mas está atrás em **toda a camada de relacionamento com paciente e integrações regulatórias.** O bloco vermelho da tabela é exatamente o que move a decisão de compra.

---

## 2. Dores reais dos usuários — ranqueadas

Ranking por **frequência × intensidade** (Reclame Aqui, reviews e dados de mercado):

**#1 — No-show (faltas): 20-32% da agenda.** A Federação Brasileira de Hospitais aponta até 20%/mês; dados de mercado falam em comprometer 32% da agenda; **48% das instituições** são afetadas. Com consulta a R$500, duas faltas/semana = ~R$50k/ano perdidos. **Confirmação automatizada reduz no-show em 50-70%.** É a dor #1 porque tem ROI quantificável e imediato. ([Doctoralia — desafios](https://pro.doctoralia.com.br/blog/clinicas/desafios-das-clinicas-no-brasil), [Meets — custo do no-show](https://blog.meets.com.br/o-custo-invisivel-do-no-show-quanto-sua-clinica-perde-por-mes-com-faltas-nao-avisadas/), [Agendar Saúde](https://agendarsaude.com.br/reducao-de-no-show-em-consultorios-medicos/))

**#2 — Suporte ruim + migração de dados falha no onboarding.** É o **campeão absoluto de reclamação** no Reclame Aqui. Feegow: tickets "abertos mas nunca resolvidos", complexidade excessiva, e **migração que só traz os pacientes e perde o resto** (cliente tem que baixar manualmente e mandar por e-mail). iClinic: "para assinar é rapidinho, para cancelar é impossível". Isto é estrutural — **o lock-in pós-venda é tática deliberada**, não acidente. ([Feegow no RA](https://www.reclameaqui.com.br/empresa/feegow/lista-reclamacoes/), [iClinic — cancelamento](https://www.reclameaqui.com.br/iclinic/nao-consigo-encerrar-minha-conta_UdvmWhJouQkoKBph/))

**#3 — Instabilidade / travamento durante o atendimento.** iClinic tem **6.5/10 no Reclame Aqui**: relatos de "prontuário em branco", sistema travando 30-40min na agenda durante o expediente. Para um sistema crítico de operação, confiabilidade é um diferencial subvalorizado pelos grandes. ([iClinic — sistema não funciona](https://www.reclameaqui.com.br/iclinic/sistema-nao-funciona_IFZwjYJDT4jnYHJF/))

**#4 — Tempo de documentação clínica.** Antes da pandemia, **mais da metade do tempo de consulta** era gasto preenchendo sistema; o "golden moment" da anamnese (escuta ativa) é interrompido por burocracia digital. É a justificativa direta para IA scribe. ([Voa Health — burocracia](https://blog.voa.health/blog/desafios-na-saude-5/burocracia-na-saude-sobrecarga-medico-tempo-paciente-29))

**#5 — Glosas de convênio (TISS).** Clínicas com glosa >7% perdem **R$6.000 a R$18.000/mês**; média de 10%/ano. **80% das glosas técnicas vêm de 6 erros** (TUSS desatualizado, CID incompatível, falta de autorização, XML). Dor cara — mas **só dói para quem fatura convênio.** ([ByDoctor](https://bydoctor.com.br/blog/erros-faturamento-tiss-glosas-como-evitar), [TISSXML](https://blog.tissxml.com.br/erros-e-glosas-tiss-como-evitar-rejeicoes/))

**#6 — Cobrança/inadimplência e financeiro fragmentado** (gestão acaba em planilha fora do sistema).

**#7 — Preço que infla com add-ons.** O modelo modular (Amplimed) começa em R$99 mas "dobra ou triplica" com adicionais — sensação de bait-and-switch. ([WE Marketing — Amplimed](https://wemarketingmedico.com.br/amplimed/))

**#8 — Agenda telefônica / ausência de agendamento online** (paciente quer marcar 24/7; 33% das clínicas citam aquisição de paciente como prioridade).

---

## 3. Features que decidem compra e churn

**Decide a compra (em ordem de peso):**
1. **Confirmação automática WhatsApp** — resposta direta à dor #1; primeira pergunta de quase toda clínica.
2. **Agendamento online + presença na Doctoralia** — aquisição de paciente é prioridade de 33% das clínicas.
3. **Financeiro integrado** — gestor não quer planilha paralela.
4. **Prescrição digital (Memed)** — virou expectativa básica; 2 em cada 8 médicos já usam.
5. **Telemedicina nativa** — 68% das instituições já oferecem; faltar é eliminatório em parte do mercado.

**Causa o churn:**
- **Suporte que não resolve** e **instabilidade no atendimento** — motivos mais citados de cancelamento.
- **Custo inflando com add-ons.**
- **Migração dolorosa** — a dor de sair também **segura** o cliente (lock-in). O Imedto pode explorar o reverso: **migração assistida de entrada + saída fácil** como argumento de aquisição.

Nota crítica: **TISS não está no top-5 de decisão para o público-alvo do Imedto** (clínicas pequenas e autônomos, muitos particular/híbrido). É decisivo para clínica média conveniada, mas é a feature de **maior custo de construção** e **menor alavanca de aquisição inicial**. Tratar como fase posterior.

---

## 4. Tendências e inovação 2025-2026 — commodity vs. diferencial

| Tendência | Estado | Veredito |
|---|---|---|
| **Telemedicina** | 68% das instituições já oferecem; +57% em agendamentos teleconsulta; CFM 2.314/2022 definitiva | **COMMODITY** — dói faltar, não diferencia |
| **Prescrição digital (Memed/Mevo)** | Memed em 2/8 médicos; só **15% do receituário** é digital ainda; Mevo (ex-Nexodata) com apoio à decisão clínica | **Commodity de integração** — todos integram Memed |
| **Assinatura ICP-Brasil (VIDaaS)** | Obrigatória p/ controlados/atestado (RDC Anvisa 1.000/2025); **certificado em nuvem do CFM é GRÁTIS** p/ médico adimplente | **Commodity** — e barato de habilitar |
| **WhatsApp Business API** | Confirmação reduz no-show 50-70%; conversa ~R$0,35; plataformas R$200-4.000/mês | **Esperado** — execução boa ainda diferencia |
| **Agendamento online** | Doctoralia (570k perfis) é o canal dominante de aquisição | **Commodity de feature, diferencial de canal** |
| **RNDS / interoperabilidade** | Portaria GM/MS 7.266/2025 = plataforma única; saúde suplementar integra ago-out/2025; "Open Health" | **Virando requisito regulatório** — FHIR/RNDS deixa de ser diferencial |
| **IA scribe (transcrição consulta→prontuário)** | Campo quente e **fragmentado**: DoctorAssistant.ai, Voa Health, Iara, Dr. Assistente, Scriba, Evalmind (standalone); HiDoctor LIVE, Doctoralia Noa Notes (R$199), Amplimed (embutidos). Sem regulação específica, só LGPD | **AINDA DIFERENCIAL** — janela aberta para embute nativo PT-BR; fecha em 12-24 meses |

**Síntese:** prontuário, agenda, telemedicina, prescrição Memed, assinatura ICP e NFS-e **viraram commodity** — são tabela-estaca, não vantagem. O que **ainda diferencia em 2026**: **IA scribe nativa**, **UX/velocidade real**, **onboarding/migração sem dor**, **atendimento humano rápido**, **confiabilidade (uptime)** e **profundidade em nicho de especialidade**.

Fontes: [Doctoralia — telemedicina](https://pro.doctoralia.com.br/blog/clinicas/telemedicina-o-que-diz-a-lei-brasileira), [Exame — Memed](https://exame.com/insight/memed-vendeu-farmacia-ao-mercado-livre-para-focar-em-prescricao-medica-digital/p), [Mevo](https://medicinasa.com.br/nexodata-mevo/), [RNDS — Min. Saúde](https://www.gov.br/saude/pt-br/composicao/seidigi/rnds), [Amplimed — cert. ICP](https://www.amplimed.com.br/blog/prescricao-com-assinatura-digital/), [Neural Saúde — DoctorAssistant](https://neuralsaude.com.br/doctor-assistant-ia-brasileira-ai-scribes/).

---

## 5. Pricing e empacotamento — onde há espaço

**Como o mercado cobra:**
- Modelo dominante: **por profissional de saúde/mês** (recepcionista geralmente não conta).
- Tiers por feature (Starter → Pro → Premium), com financeiro/TISS/telemedicina movendo entre tiers ou virando add-on.
- **Modular** (Amplimed) é o mais agressivo — entrada baixa, conta final alta.
- Free tier real só na **Feegow** (até 100 pacientes) e Doctoralia.

**Espaço de diferenciação para um entrante:**

1. **Preço previsível "tudo incluído"** — contra a dor #7 (add-ons que inflam). Tier único transparente que inclui WhatsApp + agendamento + financeiro básico, sem surpresa. Mensagem: *"o preço que você vê é o que você paga."*
2. **Não brigar no piso (R$75-99) — brigar no valor.** Posicionar em ~R$99-129 **com IA scribe nativa incluída** entrega mais valor/real do que ProDoctor a R$75 sem IA, e custa metade do Doctoralia+Noa Notes (R$529+R$199).
3. **Free/trial generoso com migração assistida** — transforma a dor #2 (migração) de barreira em **isca de aquisição**.

---

## 6. Oportunidades de diferenciação para o Imedto

1. **IA scribe nativa PT-BR como bandeira do produto** — não como add-on de R$199 (Doctoralia). Embutir transcrição consulta→evolução no prontuário existente ataca a dor #4 e é **o maior diferencial ainda disponível**. A janela fecha em 12-24 meses.
2. **"Anti-lock-in" como posicionamento** — exportação de dados livre (já existe via LGPD) + migração assistida de entrada + audit nativo. Mensagem comercial contra o "cancelar é impossível" da iClinic e a migração quebrada da Feegow. **Transforma a maior força arquitetural existente em argumento comercial.**
3. **Confiabilidade e UX/velocidade** — "não trava durante o atendimento" é diferencial real num mercado onde o líder tem 6.5/10 no Reclame Aqui. Stack moderna (.NET 10 + Vue 3) vira promessa de produto.
4. **Nicho de especialidade profundo** — em vez de "para todas as especialidades", ir fundo em 1-2 verticais mal servidas. O módulo de cirurgias/orçamentos cirúrgicos + ficha anestésica já aponta para **especialidades cirúrgicas (plástica, dermato, odonto-cirúrgico)** como vertical natural.
5. **Atendimento humano rápido** — a dor #2 é estrutural nos grandes; entrante pequeno tem vantagem temporária real e barata.

**Onde NÃO competir:** TISS robusto (custo altíssimo, alavanca baixa para o público-alvo), marketplace de aquisição (território da Doctoralia — melhor **integrar** do que competir), guerra de preço no piso.

---

## Ranking — capacidades mais pertinentes que o Imedto ainda não tem
### Ordenadas por impacto na decisão de assinatura (corrigido pós-inventário)

| # | Capacidade | Por que pesa | Dor | Esforço | Base existente |
|---|---|---|---|---|---|
| **1** | **Confirmação/lembrete automático WhatsApp** | Primeira pergunta de toda clínica; reduz no-show 50-70% | #1 | Médio | Discovery `whatsapp-envio/` + automações + link público de confirmação já existem |
| **2** | **Agendamento online pelo paciente** | Aquisição 24/7; prioridade de 33% das clínicas | #8 | Médio | Endpoint público de profissionais + disponibilidade já existem |
| **3** | **Assinatura ICP-Brasil ativada** | Obrigatória p/ controlados; cert. CFM grátis | Validade jurídica | Baixo | BirdID stub + webhook prontos; discovery `assinatura-digital-receitas/` |
| **4** | **Prescrição digital integrada (Memed)** | Expectativa básica | Burocracia | Baixo (integração) | Receitas + favoritos já existem |
| **5** | **IA scribe nativa PT-BR** | **Maior diferencial disponível**; janela 12-24m | #4 | Alto | Infra IA completa (settings, rate limit, audit, sugestão de seção) |
| **6** | **Financeiro completo + NFS-e** | Gestor não aceita planilha paralela | #6 | Médio-Alto | Lançamentos básicos existem; discovery `nota-fiscal/` |
| **7** | **Telemedicina** | Paridade — 68% já oferecem | — | Médio | Agenda + links públicos existem |
| **8** | **BI gerencial avançado** (no-show, ocupação, produtividade) | Decisor compra por isto | Visão gerencial | Médio | 6 abas de relatórios existem |
| **9** | **Migração assistida + onboarding guiado** | Anti-dor #2; arma de aquisição | #2 | Médio | Exportação LGPD existe (o reverso) |
| **10** | **Portal do paciente** | Retenção + modernidade | Comunicação | Médio-Alto | Aba Documentos + links públicos por token existem |

**Ressalva de confiança:** preços coletados em jun/2026 envelhecem rápido (reverificar antes de decisão comercial). iClinic não publica preço — faixas inferidas de comparativos de terceiros. Reduções de no-show (50-70%) e números de glosa vêm de fontes de fornecedores, com viés otimista — tratar como ordem de grandeza.
