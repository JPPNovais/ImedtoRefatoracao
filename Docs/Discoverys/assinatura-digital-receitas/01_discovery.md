# Discovery — Assinatura digital ICP-Brasil para receitas médicas

> Status: **draft / discovery** — não é plano de execução. Objetivo é mapear exigências legais, opções de implementação, custos reais e modelo de repasse antes de cravar arquitetura.
> Autor: Claude (Opus 4.8) · Data: 2026-05-31
> Escopo travado pelo usuário: assinatura **ICP-Brasil qualificada** (validade jurídica plena), modelo de **certificado em nuvem** (médico assina pelo celular/PIN, sem hardware A3), cenários de volume **pequeno/médio/grande**.

---

## 0. TL;DR (leia isto primeiro)

1. **Sim, dá pra fazer "de forma simples" — e barato.** Receita médica exige assinatura **ICP-Brasil qualificada** (não dá pra fazer assinatura "caseira" com validade legal). Mas você **não precisa virar uma autoridade certificadora**: o caminho enxuto é integrar a **API REST de um provedor de certificado em nuvem** (ex.: VIDaaS/Valid) e o médico assina pelo celular. A engenharia é uma integração OAuth2 + geração de PDF assinado em PAdES — viável dentro da stack .NET atual.

2. **O custo do certificado pode ser R$ 0 para você.** O **CFM dá certificado digital em nuvem gratuito** ao médico adimplente (válido 1 ano, renovável até 5 anos, serve até para controle especial). E o médico que já declara IR / acessa e-CAC normalmente **já tem e-CPF**. Ou seja: o modelo recomendado é **"médico traz o próprio certificado" (BYOC)** — você integra, ele assina, e o custo de certificado sai da sua conta.

3. **O custo marginal por receita tende a ~R$ 0,** porque os certificados em nuvem já incluem **5.000–6.000 assinaturas/ano**, e um médico emite ~500–600 receitas/ano. O único custo recorrente possível é o **carimbo do tempo** (centavos por assinatura, e nem sempre obrigatório). O grosso do custo é **engenharia one-time** (~2–4 semanas de dev).

4. **Quanto cobrar a mais?** Cuidado: o mercado já tem **Memed grátis** e **certificado CFM grátis**, então cobrar "pela assinatura" isolada é difícil. A jogada é **embutir no plano** como diferencial ("Receita Digital com assinatura ICP-Brasil inclusa") ou vender como **add-on de R$ 19–29/médico/mês** — não como taxa por receita. Como seu custo é quase zero, **quase qualquer preço é alta margem**; o limite é competitivo, não de custo.

5. **Atalho de validação (se quiser zero engenharia de assinatura):** integrar a **Memed** — ela já entrega prescrição + assinatura ICP-Brasil + envio ao paciente, de graça pro médico. Trade-off: menos controle de UX/dado e a receita vive parcialmente na Memed.

6. **Autorizações/prazos (lead time):** no caminho recomendado há **um único gate**: solicitar o **canal de integração à Valid/VIDaaS** (comercial, **sem custo**, dias a semanas). Você **não** precisa do credenciamento **ITI** (que leva **9 meses** — quem é PSC é o provedor) nem da **certificação SBIS** (opcional). **O grande bloqueador é o SNCR da ANVISA — e só para receitas controladas:** integração obrigatória por API, **requisitos técnicos ainda não publicados**, fase de integração começando em **jun/2026**, obrigatória até **30/set/2026**, regra ainda em alteração (minuta mai/2026). **Por isso o MVP deve cobrir só receita não-controlada** (que não passa pelo SNCR) e tratar controlados como track 2 dependente do cronograma da ANVISA. Detalhe completo no §9.

> ⚠️ **Confiança das informações:** a parte **legal e técnica** foi verificada com alta confiança (fontes primárias: CFM, Planalto, ITI, docs dos provedores, validação adversarial). Os **preços** foram coletados das páginas oficiais dos provedores em **maio/2026** — são ponto-no-tempo e devem ser reconfirmados em cotação antes de fechar contrato. Itens marcados com 🔸 precisam de **cotação comercial** (Lacuna Rest PKI, modelo B2B Memed, preço de carimbo do tempo em volume).

---

## 1. Contexto

O Imedto gera receitas médicas em PDF (já há redesign de PDF entregue, `usePdfHeader.ts` como fonte de verdade). Hoje essas receitas **não têm assinatura digital com validade jurídica** — são documentos visualmente formatados, mas sem a assinatura criptográfica ICP-Brasil que a lei exige para uma receita eletrônica substituir a receita em papel assinada de próprio punho.

A demanda: permitir que o **profissional médico assine digitalmente** a receita que gera na plataforma, com **validade jurídica plena**, de forma que:
- a farmácia aceite (incl. controle especial),
- o paciente possa validar a autenticidade (Validar ITI / validador CFM),
- o documento tenha autoria, integridade e não-repúdio garantidos.

### Por que não dá pra "fazer simples sem provedor"

Assinatura digital com validade jurídica no Brasil **depende de uma cadeia de confiança (ICP-Brasil)** ancorada na raiz do ITI. Gerar um par de chaves próprio e "assinar" o PDF **não tem validade legal** para receita médica — qualquer um poderia gerar a sua. O que dá validade é a chave privada estar num **certificado ICP-Brasil emitido por uma AC credenciada**, vinculado ao CPF do médico. Isso significa que **sempre haverá um provedor de certificado** na jogada (o CFM, a Valid, a Soluti, a Certisign, etc.). O que **você escolhe** é apenas **como integrar** com essa cadeia — e é aí que mora a decisão de "simples vs. completo".

---

## 2. Exigências legais (o que realmente é obrigatório)

> Fontes primárias verificadas: Lei 14.063/2020 (Planalto), CFM Resolução 2.299/2021 (PDF oficial CFM), FAQ oficial da Prescrição Eletrônica CFM, requisitos SBIS-CFM, ITI (AD-RT), e RDC ANVISA 1.000/2025 (guia Mevo 2026).

### 2.1. Base normativa

| Norma | O que determina |
|---|---|
| **Lei 14.063/2020** | Define os 3 níveis: assinatura **simples**, **avançada** e **qualificada**. A **qualificada** (= certificado ICP-Brasil) tem "o mais elevado nível de confiabilidade" e presunção de veracidade (MP 2.200-2). |
| **CFM Res. 2.299/2021 (Art. 4)** | Documentos médicos emitidos por meio eletrônico (TDIC) devem ser assinados com certificado/chave **ICP-Brasil em nível NGS2**. Devem permitir validação pelo **Validar ITI** ou pelo validador do CFM. **Não** especifica formato (PAdES/CAdES) nem exige carimbo do tempo explicitamente. |
| **RDC ANVISA 1.000/2025** | Refina por tipo de medicamento (ver tabela 2.3). Exige **certificado ICP-Brasil** para requisição de numeração no **SNCR** (Sistema Nacional de Controle de Receituários) — vigência reforçada a partir de **jun/2026**. |
| **Requisitos SBIS-CFM (S-RES) V5.x** | Para sistemas que buscam **certificação SBIS** (opcional, não é mandato universal): assinatura em **CAdES/XAdES/PAdES**, **PAdES permitido para PDF**, carimbo do tempo no mínimo política **AD-RT** por ACT homologada ICP-Brasil, validação de longo prazo (LTV). |

### 2.2. A tensão "lei vs. prática" (decisão de produto)

A Lei 14.063 (Art. 14) **admite assinatura avançada** (sem ICP-Brasil, ex.: gov.br) para receitas **simples**. Mas a CFM Res. 2.299/2021 — norma do conselho de classe — na prática **exige ICP-Brasil para todo documento médico** emitido por TDIC. Juristas divergem (norma de conselho é hierarquicamente inferior à lei federal), mas:

> **Recomendação prudente para um SaaS de saúde: tratar TODA receita como exigindo ICP-Brasil qualificada.** Isso elimina o risco regulatório, alinha com a prática das farmácias/conselhos, **e simplifica o produto** (um único caminho de assinatura, sem ramificar por tipo de receita).

### 2.3. Diferença por tipo de receita (RDC 1.000/2025)

| Tipo de receita | Nível mínimo legal | Recomendação Imedto |
|---|---|---|
| **Controladas** (Notificação A amarela, B azul, Especial, C1, C5) | **Qualificada ICP-Brasil (obrigatório)** | ICP-Brasil qualificada |
| **Sujeitas a retenção** (antimicrobianos/antibióticos, GLP-1 tipo Ozempic) | Qualificada **ou** Avançada (gov.br) | ICP-Brasil qualificada |
| **Simples** (demais) | Avançada **ou** Qualificada | ICP-Brasil qualificada |
| **Atestados** | **Qualificada (obrigatório)** | ICP-Brasil qualificada |
| **Requisição numeração SNCR** | **ICP-Brasil (obrigatório)** | ICP-Brasil qualificada |

**Conclusão:** um único caminho — ICP-Brasil qualificada para tudo — cobre 100% dos casos sem ramificação.

### 2.4. Formato técnico recomendado

- **PAdES** (assinatura embutida no PDF) — é o formato natural para receita, pois o arquivo continua sendo um PDF visualizável e a assinatura viaja dentro dele.
- **Perfil PAdES_AD_RT** (com referência de tempo / carimbo do tempo) — é o que um sistema CFM-compliant com carimbo requer pelos requisitos SBIS. O perfil **AD_RB** (básico, sem carimbo) é o piso.
- **LTV (Long-Term Validation)** embutido no PDF permite validar a assinatura mesmo anos depois (CRL/OCSP congelados no documento).
- **Validação:** o documento precisa passar no **Validar ITI** (validar.iti.gov.br) ou no validador do CFM.

---

## 3. Como funciona a assinatura em nuvem (fluxo do médico, sem hardware)

O escopo travado é **certificado em nuvem**: a chave privada do médico fica num **HSM gerenciado pela AC** (não num token físico). O médico autoriza cada assinatura pelo celular. Juridicamente é equivalente ao A3 físico (mesma cadeia ICP-Brasil, mesmo NGS2).

**Fluxo típico (ex.: API VIDaaS / Valid):**

```
1. Médico clica "Assinar receita" no Imedto
2. Backend Imedto monta o PDF e chama a API do provedor (OAuth2 + PKCE)
3. Provedor dispara PUSH no app do médico (ou exibe QR Code)
4. Médico abre o app, confere e autoriza com PIN/biometria
5. Provedor assina o hash do PDF com a chave privada no HSM
6. Backend recebe a assinatura, embute no PDF (PAdES_AD_RT) + carimbo do tempo
7. PDF assinado volta pro Imedto → entregue ao paciente (download/WhatsApp/e-mail)
```

Provedores de certificado em nuvem aceitos pela plataforma do CFM: **BirdID, VIDaaS, VaultID, SafeID, RemoteID, NeoID** (lista é ponto-no-tempo, pode mudar).

> Correção de premissa importante (verificada): **VIDaaS é da Valid S.A.**, não da Soluti. A Soluti opera **BirdID** e **SafeID(Safeweb)**. NeoID = Serpro, RemoteID = Certisign.

---

## 4. Caminhos de implementação

### Opção A — BYOC + API REST nativa do provedor (ex.: VIDaaS) ⭐ recomendado para começar

**Como funciona:** o médico traz o próprio certificado em nuvem (CFM grátis, ou VIDaaS/BirdID que ele já tem/compra). O Imedto integra a **API REST do provedor** (OAuth2 + PKCE), monta o PDF, dispara a assinatura, embute PAdES_AD_RT.

**Prós**
- **Custo de certificado = R$ 0 pra plataforma** (é do médico).
- **Custo marginal por assinatura ≈ R$ 0** — a cota anual do certificado (5–6 mil assinaturas) cobre de sobra (~500–600 receitas/médico/ano).
- Controle total da UX e do dado (a receita é sua, não fica num terceiro).
- API REST pura — integra bem com .NET 10.
- Sem dependência de operar infraestrutura PKI.

**Contras**
- Você suporta o onboarding do certificado do médico (orientar a tirar o CFM grátis ou cadastrar o e-CPF).
- Acoplamento à API de um provedor (mitigável com camada de abstração para suportar VIDaaS + BirdID).
- Carimbo do tempo pode ser custo à parte 🔸.

**Veredito:** **melhor ponto de partida.** Mais barato, mantém controle, viável na stack atual.

---

### Opção B — SDK de assinatura (Lacuna PKI SDK / Rest PKI) sobre o cert do médico

**Como funciona:** em vez de falar a API de cada provedor, você usa o **SDK da Lacuna** (líder de mercado em PKI no Brasil) que abstrai a assinatura em nuvem de **5 provedores** (BirdID, VIDaaS, NeoID, RemoteID, SafeID) por trás de uma única API.

- **Lacuna PKI SDK** (.NET) + pacote **BrazilTrustServices** para nuvem — `Lacuna.Pki.BrazilTrustServices` v1.7.3 já suporta **net10.0** (confirmado no NuGet).
- **Rest PKI** — serviço em nuvem da Lacuna com client libs oficiais p/ .NET; você não consome a REST crua.

**Prós**
- **Uma integração cobre os 5 provedores** — sem acoplar a um só.
- SDK .NET maduro, lida com PAdES/LTV/carimbo.
- Reduz risco técnico de implementar a criptografia "na mão".

**Contras**
- **Custo de licença/uso do SDK ou Rest PKI** 🔸 (preço não público — "veja os preços aqui", exige cotação). É um custo recorrente que a Opção A não tem.
- Mais uma dependência comercial.

**Veredito:** **ótimo upgrade da Opção A** quando quiser suportar múltiplos provedores sem manter N integrações, ou se a implementação PAdES_AD_RT crua se mostrar trabalhosa. Vale pedir cotação à Lacuna e comparar com o custo de engenharia de manter a integração direta.

---

### Opção C — Plataforma de prescrição pronta (Memed) — o atalho "buy"

**Como funciona:** integra a **Memed** como módulo dentro do Imedto. Ela já entrega prescrição completa + assinatura ICP-Brasil + banco de medicamentos + envio ao paciente.

- **Grátis para o médico** (assinatura digital gratuita via parceria Soluti).
- 350+ parceiros, 323+ integrações, ~4M receitas/mês, 210k+ médicos.

**Prós**
- **Zero engenharia de assinatura/PKI** — a Memed resolve tudo.
- Grátis na ponta do médico — forte argumento de adoção.
- Banco de medicamentos, interações, envio ao paciente já prontos.

**Contras**
- **Menos controle de UX e de dado** — a receita vive (parcialmente) na Memed; você fica dependente do produto deles.
- **Modelo B2B/parceiro tem custo não público** 🔸 (cotação).
- Diferenciação fraca — qualquer concorrente também pluga a Memed.
- Sua marca cede espaço à marca Memed no momento mais sensível (a prescrição).

**Veredito:** **bom para validar rápido** ou se prescrição não for core do produto. Ruim se você quer que a receita digital seja **diferencial seu**.

---

### Opção D — Assinatura "managed" por documento (Clicksign)

**Como funciona:** usa a Clicksign (ou similar) que faz a busca do certificado em nuvem do médico por CPF (Safeweb/Soluti/Valid) e gerencia a assinatura por documento.

**Prós**
- Integração simples, plataforma madura, certificado digital incluído nos planos.

**Contras**
- **Modelo por documento fica caro em volume:** excedente **R$ 2,40–6,90/documento**. A 10.000 receitas/mês isso é proibitivo.
- Pensada para contratos/assinaturas gerais, não para o fluxo de receita médica em alto volume.

**Veredito:** **não recomendado** para receita em volume. O custo por documento mata a margem. Faz sentido só para baixíssimo volume ou outros documentos (termos de consentimento, contratos).

---

## 5. Quem paga o certificado?

| Modelo | Quem paga | Custo p/ plataforma | Implicações |
|---|---|---|---|
| **BYOC — CFM grátis** ⭐ | CFM (médico adimplente) | **R$ 0** | Melhor custo. Você orienta o médico a emitir o cert gratuito do CFM (em nuvem, ICP-Brasil, serve p/ controle especial). Limitação: depende do CRM regional e de o médico estar adimplente. |
| **BYOC — médico já tem e-CPF** ⭐ | Médico | **R$ 0** | Muitos médicos já têm e-CPF (IR, e-CAC, eSocial). Reusa o que ele já tem. |
| **BYOC — médico compra** | Médico | **R$ 0** | Você só integra. Cert ~R$ 130–250/ano (ver §6). |
| **Plataforma fornece/revende** | Plataforma → repassa | R$ ~100–250/médico/ano | Você vira intermediária do cert. Mais fricção (custódia, expiração, revenda), mas pode virar receita/conveniência. |

> **Realidade de mercado:** o médico **tipicamente já tem ou consegue de graça**. O caminho de menor atrito e menor custo é **BYOC** — você **não** entra na cadeia de fornecimento de certificado no MVP.

---

## 6. Custos detalhados (R$, maio/2026)

### 6.1. Certificado em nuvem (e-CPF, por médico) — fontes primárias

| Provedor | Produto | Preço | Cota de assinaturas | Obs. |
|---|---|---|---|---|
| **CFM** | Cert. digital em nuvem | **R$ 0** | — | Médico adimplente. 1 ano, renovável até 5 anos. Serve p/ controle especial. |
| **Valid (VIDaaS)** | e-CPF A3 nuvem 1 ano | **R$ 129,90** | 6.000/ano | 2 anos R$ 208 · 2,5 anos R$ 258 |
| **Soluti (BirdID)** | e-CPF A3 nuvem 5.000 transações | **R$ 149,90** | 5.000/ano | "Bird ID Pro" = assinaturas ilimitadas (preço à parte). Recarga disponível. |
| **Certisign** | e-CPF A3 nuvem 12 meses | **R$ 249,90** | conforme plano | A3 token: R$ 186,90 (1 ano) / R$ 252,90 (3 anos) |

### 6.2. Plataformas/APIs de assinatura

| Provedor | Modelo | Preço | Obs. |
|---|---|---|---|
| **Clicksign** | Assinatura por documento | Planos R$ 39–85/mês (20–200 docs); excedente **R$ 2,40–6,90/doc**; Avançado (300+ docs, API/SDK) custom | Cert digital incluído. Cara em volume. |
| **SuperSign** | Por documento | Pro **R$ 119/mês** (100 docs) + R$ 0,70/doc adicional | Referência de mercado. |
| **Lacuna Rest PKI / SDK** | Licença/créditos | **não público** 🔸 | Cotação direta. SDK .NET maduro. |
| **VIDaaS API nativa** | Usa cota do cert do médico | **R$ 0 marginal** (dentro da cota) | Só o custo do cert (§6.1) + carimbo. |
| **Carimbo do tempo (AD-RT)** | Por carimbo, ACT ICP-Brasil | **centavos/un** 🔸 (estimativa R$ 0,05–0,50) | Cotação com ACT/Clicksign/Lacuna. Nem sempre obrigatório. |
| **Memed** | B2B parceiro | grátis p/ médico; parceria **não pública** 🔸 | Prescrição completa. |
| **Receita digital genérica (mercado)** | Por documento | R$ 1,50–5,00/doc | Referência de teto. |

### 6.3. Custo unitário por receita — 3 cenários (arquitetura recomendada: BYOC + VIDaaS API)

Premissa-chave: **certificado é do médico** (R$ 0 p/ plataforma) e **assinaturas cabem na cota anual** (médico emite ~500–600 receitas/ano vs. 5–6 mil incluídas). Logo, o **único custo recorrente** é o carimbo do tempo (se exigido).

| Cenário | Médicos | Receitas/mês | Receitas/médico/ano | Cert (plataforma) | Assinatura | Carimbo do tempo* | **Custo/receita** |
|---|---|---|---|---|---|---|---|
| **Pequeno** | 50 | 2.000 | ~480 | R$ 0 (BYOC) | R$ 0 (na cota) | ~R$ 0,20 → R$ 400/mês | **~R$ 0 a R$ 0,20** |
| **Médio** | 200 | 10.000 | ~600 | R$ 0 (BYOC) | R$ 0 (na cota) | ~R$ 0,15 → R$ 1.500/mês | **~R$ 0 a R$ 0,15** |
| **Grande** | 1.000 | 50.000 | ~600 | R$ 0 (BYOC) | R$ 0 (na cota) | ~R$ 0,10 → R$ 5.000/mês | **~R$ 0 a R$ 0,10** |

*Carimbo do tempo: estimativa 🔸, decrescente com volume. **Se você usar perfil PAdES_AD_RB (sem carimbo), esse custo zera** — e a Res. 2.299 não exige carimbo explicitamente (a exigência vem dos requisitos SBIS, que são opcionais). Decisão de produto: começar sem carimbo (AD_RB) e adicionar AD-RT depois se buscar certificação SBIS.

**Comparativo se a plataforma REVENDER certificado** (em vez de BYOC): + R$ 130/médico/ano ≈ **R$ 0,22–0,27/receita** nos cenários pequeno/médio. Ainda baixo, mas sem vantagem clara sobre BYOC.

**Comparativo Clicksign por documento** (managed, sem BYOC): a R$ 3/doc → **R$ 6.000/mês (pequeno)**, R$ 30.000/mês (médio). Inviável em volume.

**Custo one-time de engenharia (Opção A/B):** ~2–4 semanas de 1 dev backend para integração OAuth2 + PAdES + UI de assinatura. Estimativa **R$ 30–60k**, amortizada (não recorre).

> **Leitura do gestor:** o custo recorrente real dessa feature é **quase zero** se você for de BYOC. O investimento é **engenharia one-time**. Isso muda completamente a conta de pricing (§7).

---

## 7. Pricing / repasse — quanto cobrar a mais

### 7.1. A restrição não é custo, é mercado

Seu custo é ~R$ 0/receita (BYOC). **Logo, qualquer preço é alta margem.** O teto **não** é o custo — é a **disposição a pagar**, fortemente ancorada por dois fatos:

- **Memed dá assinatura de graça** ao médico.
- **CFM dá o certificado de graça.**

→ Cobrar "pela assinatura" como item isolado é **frágil**. O médico pergunta: "por que pagar se a Memed é grátis?". A resposta tem que ser **valor de workflow**, não a assinatura em si.

### 7.2. Três modelos de repasse

| Modelo | Como cobra | Quando usar | Risco |
|---|---|---|---|
| **A) Embutido no plano** ⭐ | Receita digital + assinatura ICP-Brasil é **feature do plano** (sobe o valor do plano ou vira diferencial de retenção) | Default. Alinha com Memed/iClinic. | Nenhum óbvio — é o padrão do mercado. |
| **B) Add-on por médico/mês** | Módulo "Receita Digital" a **R$ 19–29/médico/mês** | Quando quer linha de receita explícita e o valor entregue (prontuário integrado, histórico, templates, envio) justifica | Comparação com Memed-grátis na objeção de venda. |
| **C) Por receita assinada** | R$ 0,50–1,00/receita | Quase nunca — fricção alta, e Memed-grátis derruba | Pior dos três p/ receita médica. |

### 7.3. Números concretos (modelo B, add-on R$ 25/médico/mês)

| Cenário | Receita add-on/mês | Custo/mês | Margem bruta |
|---|---|---|---|
| Pequeno (50) | R$ 1.250 | ~R$ 0–400 | **68%–100%** |
| Médio (200) | R$ 5.000 | ~R$ 0–1.500 | **70%–100%** |
| Grande (1.000) | R$ 25.000 | ~R$ 0–5.000 | **80%–100%** |

### 7.4. Recomendação de pricing

> **Não venda a assinatura — venda a "Receita Digital".** Embuta a assinatura ICP-Brasil como **feature inclusa** de um tier (ou add-on de **R$ 19–29/médico/mês**) cujo valor percebido é o **fluxo completo**: prescrever dentro do prontuário, assinar em 2 toques no celular, entregar ao paciente por WhatsApp, histórico auditável, integração com a agenda. O custo subjacente (~R$ 0) significa que **toda essa receita é margem** — o trabalho é de **posicionamento**, não de cobrir custo.
>
> **"Quanto a mais adicionar ao plano?"** Se hoje seu plano por médico custa X, adicionar **R$ 19–29/médico/mês** pelo módulo de receita digital cobre o custo (quase nulo) com margem >80% **e** fica abaixo do que o médico gastaria montando isso sozinho. Se preferir não criar add-on, **suba o plano em ~R$ 15–20/médico/mês** e entregue a receita digital como valor que justifica o reajuste e reduz churn.

---

## 8. Recomendação final

### 8.1. Para começar (MVP juridicamente válido e barato)

1. **Arquitetura: Opção A — BYOC + API REST do VIDaaS** (Valid), com camada de abstração para depois plugar BirdID.
2. **Certificado: BYOC.** Orientar o médico a usar o **certificado gratuito do CFM** ou o e-CPF que já possui. Plataforma **não** revende cert no MVP.
3. **Formato: PAdES**, perfil **AD_RB (sem carimbo)** no MVP — válido pela Res. 2.299 (que não exige carimbo) — com caminho aberto para **AD_RT (com carimbo)** quando/se buscar certificação SBIS.
4. **Tratar toda receita como ICP-Brasil qualificada** — um único caminho, sem ramificar por tipo.
5. **Validação:** garantir que o PDF assinado passa no **Validar ITI**.
6. **Pricing:** embutir no plano OU add-on **R$ 19–29/médico/mês** — posicionar como "Receita Digital", não como "assinatura".

### 8.2. Caminho de evolução

| Fase | O quê | Gatilho |
|---|---|---|
| **MVP** | BYOC + VIDaaS API + PAdES AD_RB | Agora |
| **F2** | Carimbo do tempo (AD_RT) + LTV + multi-provedor (add BirdID via abstração ou Lacuna SDK) | Buscar certificação SBIS / exigência de farmácia |
| **F3** | Integração **SNCR** (ANVISA) p/ controlados | Demanda de controle especial em volume (jun/2026+) |
| **F4** | Avaliar PSC próprio (Lacuna Amplia) | Só em **volume muito alto** — exige credenciamento/auditoria ITI (DOC-ICP-17). Provavelmente nunca compensa vs. integrar terceiros. |

### 8.3. Se quiser validar SEM engenharia de assinatura

Integrar **Memed** (Opção C) como atalho. Entrega rápida, grátis pro médico, mas abre mão de diferenciação e controle de dado. Bom para testar apetite do mercado antes de investir na engenharia da Opção A.

---

## 9. Autorizações, credenciamentos e prazos (lead time)

> Pergunta do usuário: *"preciso de alguma autorização/permissão que pode levar tempo de algum órgão ou empresa?"*
> **Resposta curta:** no caminho recomendado (Opção A — BYOC + API VIDaaS, **receita não-controlada**), há **um único gate real**: solicitar o **canal de integração à Valid** (comercial, **sem custo de integração**, dias a semanas). Os credenciamentos pesados — **ITI/PSC (9 meses)** e **certificação SBIS (auditoria paga, meses)** — são **evitados** ou **opcionais**. O único bloqueador grande, demorado e **fora do seu controle** é o **SNCR da ANVISA**, e **somente se você quiser emitir receitas de controlados**.

### 9.1. Mapa de gates

| Item | Quando é necessário | Quem concede | Lead time | Custo | Bloqueia o MVP? |
|---|---|---|---|---|---|
| **Canal de integração VIDaaS** | Sempre (Opção A) | Valid — contato comercial (`produtos.certificadora@valid.com`) | **Dias a semanas** | **Sem custo de integração** | ⚠️ **Sim — é o gate único do MVP** |
| **Registro do app (client_id/secret)** | Sempre (Opção A) | Self-service, após liberar o canal | Imediato | — | Não |
| **Integração SNCR (ANVISA)** | **Só p/ controlados/retenção** | ANVISA (via Gov.br) | **Alto — requisitos técnicos ainda não publicados** | a definir | 🚫 **Sim, p/ controlados** (ver §9.2) |
| **Certificação SBIS/CFM (S-RES)** | **Opcional** (selo de mercado) | SBIS + CFM | **Meses** (auditoria por amostragem) | **Auditoria paga** 🔸 | **Não** — selo ≠ validade jurídica |
| **Credenciamento ITI / PSC (DOC-ICP-17)** | **Só se operar PSC próprio** (F4) | ITI | **9 meses** + auditoria anual | Alto | **Não** — evitado no MVP |
| **Carimbo do tempo / ACT** | Só com perfil AD-RT | ACT homologada ICP-Brasil | Contrato comercial | Por carimbo 🔸 | **Não** — MVP usa AD_RB |
| **Certificado do médico** | Sempre (lado do **médico**) | CFM (grátis) ou AC | CFM: depende do CRM regional; compra de e-CPF: **1–3 dias** (validação por vídeo) | R$ 0 (CFM) ou §6.1 | **Não** — é do médico, não da plataforma |
| **Parceria Memed** | Só Opção C | Memed (comercial) | Comercial | 🔸 | **Não** |

### 9.2. 🚨 O grande bloqueador: SNCR da ANVISA (só controlados)

A **RDC 1.000/2025** criou o **SNCR (Sistema Nacional de Controle de Receituários)**. Para **Notificações de Receita, Receitas de Controle Especial e sujeitas à retenção**, a receita eletrônica deve ser gerada **exclusivamente em serviço de prescrição integrado ao SNCR via API**. Pontos críticos de prazo:

- **Os requisitos técnicos de integração AINDA SERÃO publicados pela ANVISA** — em jun/2026 ainda **não existe** manual técnico definitivo. A etapa de integração com plataformas **começa a partir de jun/2026**, com manuais/webinares prometidos pela ANVISA.
- **Operação plena obrigatória via SNCR até 30/set/2026.**
- **Alvo móvel:** há **minuta de mai/2026 alterando a RDC 1.000/2025** — a regra ainda está mudando.
- Acesso via **Login Único Gov.br**; pedido de número de série único exige **assinatura qualificada ICP-Brasil**; **CPF do paciente obrigatório**; receita de **uso único** (não reutilizável após dispensação).
- Portais: `sncr.anvisa.gov.br` e `api.anvisa.gov.br`.

> **Implicação de produto:** controle especial/retenção é um **track separado**, dependente de cronograma e spec da ANVISA que **você não controla e que ainda não está fechada**. **Recomendação: o MVP cobre apenas receita simples não-controlada** (que **não** passa pelo SNCR e já tem 100% da base legal resolvida). Controlados entram numa **Fase 3** (ver §8.2), acompanhando a liberação dos manuais técnicos da ANVISA — sem isso travar o lançamento da assinatura digital para o caso geral.

### 9.3. O que você NÃO precisa (de-risk importante)

- ❌ **Não precisa se credenciar no ITI** (processo de **9 meses**) — quem é o PSC credenciado é a Valid/Soluti; você é apenas integrador.
- ❌ **Não precisa de certificação SBIS** para ter validade jurídica — o selo é **opcional** (auditoria paga); um sistema pode atender NGS1/NGS2 sem o selo.
- ❌ **Não precisa virar Autoridade Certificadora** nem emitir certificado.
- ❌ **Não precisa de carimbo do tempo no MVP** (a Res. CFM 2.299 não o exige; usa-se AD_RB).

### 9.4. Sequenciamento recomendado (para não perder tempo)

1. **Agora:** abrir contato comercial com a **Valid** e pedir o **canal de integração VIDaaS** + acesso ao ambiente de **homologação** (`hml-certificado.vidaas.com.br`). É o item com lead time que está no caminho crítico do MVP.
2. **Em paralelo:** pedir as cotações 🔸 (Lacuna, carimbo do tempo, Memed) para decidir Opção A vs B sem esperar.
3. **MVP:** implementar **só receita não-controlada** — zero dependência de SNCR/ANVISA.
4. **Pós-MVP, monitorar:** acompanhar a publicação dos **manuais técnicos do SNCR** (jun–set/2026) e a **minuta que altera a RDC 1.000** antes de prometer controle especial.

### 9.5. Ambientes de teste / homologação (dá pra validar a integração de graça?)

**Resposta curta: sim.** Todos os provedores têm ambiente de homologação/sandbox, e dá pra testar a integração **sem pagar nada**. A regra geral: em homologação você assina com **certificado de teste** (gratuito, mas **sem validade jurídica** — serve só pra validar o fluxo técnico). Você só gasta quando quiser validar uma assinatura **real** ICP-Brasil de ponta a ponta — e mesmo aí pode usar **1 certificado grátis do CFM**, custo R$ 0.

| Provedor | Ambiente de homologação | Certificado de teste grátis? | Custo p/ testar | Precisa do comercial antes? |
|---|---|---|---|---|
| **BirdID / Soluti** ⭐ | `apihom.birdid.com.br` | **Sim** — "CERTIFICADO TESTE VAULTID" (AC SOLUTI) | **R$ 0** | Acesso ao repo BirdID Pro via gerente de conta; docs abertas em `docs.vaultid.com.br` |
| **Lacuna Rest PKI** | Registro em `pki.rest` gera token; samples no GitHub | Sim (certs de teste nos samples) | **R$ 0** no trial | Trial self-service / sob solicitação. Produção exige licença 🔸 |
| **VIDaaS / Valid** | `hml-certificado.vidaas.com.br` + `demo-certificado.vidaas.com.br` | A confirmar com o comercial 🔸 | **Sem custo de integração** | **Sim** — precisa solicitar o canal (via IntegraICP / `developers.integraicp.com.br`) |
| **Memed** | `integrations.memed.com.br` (homologação de parceiro) | n/a (assinatura é da Memed) | **R$ 0** (grátis p/ médico) | Onboarding de parceiro |
| **Clicksign** | Trial 14 dias (até 30 docs) | Usa cert real do usuário | **R$ 0** no trial | Não |

> **Recomendação para a POC (custo zero):** começar pelo **BirdID** (homologação aberta + certificado de teste grátis, sem gate comercial) **ou** pelo **trial da Lacuna** para validar o fluxo de assinatura PAdES no .NET. Em paralelo, **solicitar o canal da VIDaaS** (provedor de produção recomendado). No fim, validar **uma** assinatura real ICP-Brasil com o **certificado gratuito do CFM** — fechando o ciclo técnico + jurídico **sem desembolso**.
>
> ⚠️ Atenção: assinatura feita com **certificado de teste NÃO tem validade jurídica** e **não passa** no Validar ITI como válida — é só para desenvolvimento. A validação jurídica real exige um certificado ICP-Brasil de verdade (que pode ser o gratuito do CFM).

---

## 10. Riscos e perguntas em aberto

| # | Risco / pergunta | Como resolver |
|---|---|---|
| 1 | **Preços são ponto-no-tempo (maio/2026)** | Reconfirmar em cotação antes de contratar. |
| 2 | **Carimbo do tempo: preço em volume** 🔸 | Cotar com ACT homologada / Lacuna / Clicksign. Ou começar sem carimbo (AD_RB). |
| 3 | **Lacuna Rest PKI / SDK: preço** 🔸 | Cotação direta com Lacuna; comparar com custo de manter integração VIDaaS direta. |
| 4 | **Memed B2B: modelo de parceiro** 🔸 | Conversa comercial com a Memed (se Opção C entrar em jogo). |
| 5 | **Lista de provedores aceitos pelo CFM muda** | Tratar como config, não hardcode. Revalidar periodicamente. |
| 6 | **RDC 1.000/2025 + SNCR (jun/2026)** — bloqueador de prazo p/ controlados | Ver **§9.2**: spec técnica da ANVISA ainda não publicada, alvo móvel. MVP cobre só não-controlada. Discovery dedicado p/ controlados antes de prometer controle especial. |
| 9 | **Canal de integração VIDaaS tem lead time comercial** | Abrir contato com a Valid **já** (§9.4) — está no caminho crítico do MVP. |
| 7 | **Onboarding do certificado do médico** (UX) | Fluxo guiado: detectar se já tem cert em nuvem (busca por CPF na API), senão orientar CFM grátis. |
| 8 | **Cert do CFM depende do CRM regional e adimplência** | Ter fallback (médico compra VIDaaS/BirdID) no fluxo de onboarding. |

---

## 11. Fontes

**Legal / regulatório (primárias):**
- CFM Res. 2.299/2021 — https://sistemas.cfm.org.br/normas/arquivos/resolucoes/BR/2021/2299_2021.pdf
- Lei 14.063/2020 — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm
- FAQ Prescrição Eletrônica CFM — https://prescricaoeletronica.cfm.org.br/perguntas-frequentes/
- Certificado gratuito CFM — https://portal.cfm.org.br/noticias/cfm-inova-e-oferece-certificacao-digital-gratuito-aos-medicos-brasileiros/
- Requisitos assinatura SBIS-CFM (R. Sabbatini / SBIS V5.2) — https://medium.com/seguran%C3%A7a-da-informa%C3%A7%C3%A3o-em-sa%C3%BAde/os-novos-requisitos-de-assinatura-digital-para-a-certifica%C3%A7%C3%A3o-de-sistemas-de-registros-eletr%C3%B4nicos-4e018b9f665
- ITI — AD-RT — https://www.gov.br/iti/pt-br/assuntos/repositorio/assinatura-digital-com-referencia-de-tempo-ad-rt
- RDC 1.000/2025 (guia Mevo 2026) — https://mevo.com.br/blog/tudo-sobre-certificados-digitais-e-receitas-digitais

**Técnico (provedores):**
- Manual VIDaaS (Valid) API nuvem — https://valid-sa.atlassian.net/wiki/spaces/PDD/pages/958365697/
- Lacuna PKI SDK (cloud) — https://docs.lacunasoftware.com/pt-br/articles/pki-sdk/cloud-certificates/index.html
- Lacuna Rest PKI — https://docs.lacunasoftware.com/pt-br/articles/rest-pki/index.html
- Clicksign cert nuvem — https://ajuda.clicksign.com/article/973-certificado-digital-em-nuvem
- Memed parceiro software — https://memed.com.br/parceiro-software/

**Preços (páginas oficiais, maio/2026):**
- Valid e-CPF A3 nuvem — https://validcertificadora.com.br/products/e-cpf-a3-em-nuvem
- Soluti BirdID 5000 — https://www.soluti.com.br/produtos/bird-id-e-cpf-a3-5000-transacoes/
- Certisign e-CPF médicos — https://certisign.com.br/certificados/e-cpf/para-medicos
- Clicksign preços — https://www.clicksign.com/preco

**Autorizações / credenciamentos / prazos:**
- VIDaaS — integração via API (canal) — https://validcertificadora.com.br/pages/psc-integracao-via-api
- SNCR — portal ANVISA — https://sncr.anvisa.gov.br/ e https://www.gov.br/anvisa/pt-br/assuntos/medicamentos/controlados/sncr
- SNCR — o que muda (ANVISA, 2026) — https://www.gov.br/anvisa/pt-br/assuntos/noticias-anvisa/2026/SNCR-o-que-muda-para-farmacias-e-drogarias-com-o-novo-sistema-de-controle-de-receitas
- Minuta que altera a RDC 1.000/2025 (mai/2026) — https://sincofarmasp.com.br/2026/05/27/anvisa-publica-minuta-que-altera-a-rdc-1-000-2025/
- Certificação SBIS (software) — https://sbis.org.br/certificacoes/certificacao-software/
- SBIS é obrigatória? (ProDoctor) — https://prodoctor.net/blog/a-certificacao-sbis-no-prontuario-eletronico-do-paciente-e-obrigatoria/
- Credenciamento ITI / PSC — prazos (AARB) — https://www.aarb.org.br/portaria-adequa-prazos-para-analise-de-solicitacao-de-credenciamento-das-empresas-da-cadeia-icp-brasil/

**Ambientes de teste / homologação:**
- BirdID/VaultID — docs da API (homologação + cert de teste) — https://docs.vaultid.com.br/workspace/cloud/api
- Lacuna Rest PKI — registro/trial — https://pki.rest/ · samples — https://github.com/LacunaSoftware/PkiSuiteSamples
- VIDaaS — integração via API (IntegraICP) — https://validcertificadora.com.br/pages/psc-integracao-via-api · https://developers.integraicp.com.br/api-reference/v3/index.html
- Memed — homologação de parceiro (Memed Bridge) — https://legacy-doc.memed.com.br/referencia/memed-bridge

---

> **Próximo passo sugerido:** se este discovery virar decisão, abrir briefing na pipeline (`imedto-business-analyst`) para o MVP da Opção A — definindo CAs de assinatura (multi-tenant: cert vinculado ao médico do tenant; LGPD: PDF assinado é dado sensível; estados: assinatura pendente/concluída/falha; validação ITI). E pedir as 4 cotações 🔸 em paralelo (VIDaaS API, Lacuna, carimbo do tempo, Memed) para fechar os números antes de cravar.
