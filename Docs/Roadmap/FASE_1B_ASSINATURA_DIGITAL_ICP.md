# FASE 1B — Assinatura Digital ICP-Brasil (IntegraICP) · PRONTA PARA EXECUTAR

> Parte do roadmap [`README.md`](README.md), destacada da [`FASE_1_COMPLETUDE.md`](FASE_1_COMPLETUDE.md) (item 1.3) em 2026-06-10. **Status: EM ESPERA — aguardando respostas comerciais da Valid.** Este arquivo deixa tudo pronto para executar no dia em que a confirmação chegar, sem retrabalho de planejamento.

## Gatilho de execução

Respostas da Valid às 5 perguntas enviadas (e-mail de 2026-06-10 para Cléo Santos) + decisão do usuário de aceitar a proposta **IntegraICP nº 0350/2026** (R$ 1.800 one-time; consumo R$ 0 via VIDaaS/12 meses; R$ 0,10/assinatura outras ACs). Termos completos e as 5 perguntas: discovery [`assinatura-digital-receitas/ §12`](../Discoverys/assinatura-digital-receitas/01_discovery.md).

## O que já está pronto (não refazer)

| Peça | Estado |
|---|---|
| Discovery completo (legal, técnico, custos, BYOC) | ✅ `Docs/Discoverys/assinatura-digital-receitas/` |
| Proposta comercial recebida e analisada | ✅ §12 do discovery (2026-06-01) |
| Stub de assinatura no código | ✅ Domínio `AssinaturaDigital` + `ReceitaAssinaturaController` (POST /assinar, GET /status-assinatura, webhook HMAC) + tabela `assinatura_digital_receita` + `MedicoCertificadoController` (vincular certificado) — hoje apontando para BirdID em "modo homologação" |
| PDF oficial da receita no servidor (pré-requisito técnico) | 🔄 briefing `2026-06-10_001` (na fila da F1) — a assinatura PAdES é aplicada sobre esse PDF |
| Doc da API IntegraICP | ✅ https://developers.integraicp.com.br/api-reference/icp/v3/index.html |

## Plano de execução pós-confirmação (sequência)

### Passo 0 — Formalização (usuário, ~1 dia)
Aceite da proposta → V/Cert envia a **chave de API em até 2 dias úteis** → reunião de kickoff (remota) → guardar a chave no SSM Parameter Store (nunca em appsettings versionado).

### Passo 1 — Briefing de integração (BA, mesma sessão)
Acionar `imedto-business-analyst` para o briefing `2026-06-XX_NNN_assinatura-digital-integraicp.md` com as decisões já tomadas no discovery:
- **Arquitetura**: substituir/abstrair o provider BirdID do stub por um provider IntegraICP (agregador multi-PSC — uma integração cobre VIDaaS, BirdID, NeoID etc.). Manter a abstração de provider para não acoplar.
- **Fluxo técnico** (doc IntegraICP v3): consulta de *clearances* pelo CPF do médico → redirect 302 para autenticação no app do provedor (QR Code/OTP) → callback com `credentialId` → cálculo SHA-256 do PDF oficial (gerado pelo briefing 001) → `POST /signatures` com os hashes → receber e armazenar o PDF assinado (S3) → status `AssinadaIcp`.
- **Perfil**: PAdES **AD_RB** (sem carimbo do tempo — decisão do discovery §3; carimbo fica como evolução se buscarmos SBIS).
- **Escopo MVP**: receitas **não-controladas** + atestados. Controlados ficam para o track 2 (dependem do SNCR/ANVISA — cronograma jun-set/2026, requisitos ainda não publicados).
- **Onboarding do médico**: tela já existente de vincular certificado (`MedicoCertificadoController`) evolui para o fluxo de autorização IntegraICP; orientação in-app para tirar o certificado gratuito do CFM.
- **CAs obrigatórios além dos padrão**: documento assinado validável no validar.iti.gov.br; falha de assinatura **degrada com clareza** (emissão simples nunca bloqueia); status visível na lista de receitas; PDF assinado é o servido pelo endpoint do briefing 001 quando existir; multi-tenant no vínculo médico↔certificado; auditoria de cada assinatura; webhook validado (HMAC) e idempotente.
- **Medidor de consumo**: registrar assinaturas/mês por AC de origem (para conferir a fatura de R$ 0,10 das "outras ACs" — apuração mensal da Valid).

### Passo 2 — Execução (pipeline normal)
`imedto-developer` (→ `imedto-database` se o briefing apontar ajuste em `assinatura_digital_receita`/certificados) → `imedto-qa`. Estimativa do discovery: **2-4 semanas de engenharia**.

### Passo 3 — Homologação conjunta com a V/Cert (cronograma deles: ~10 dias úteis no total)
Homologação do cliente → homologação da V/Cert (eles validam a comunicação da API) → go-live. Agendar com a Cléo na reunião de kickoff.

### Passo 4 — Pós-go-live
- Validar 1 receita real no validar.iti.gov.br e arquivar a evidência.
- Atualizar `Docs/ARQUITETURA.md` (provider IntegraICP) e o discovery (status → decidido/ADR) — documentação viva.
- Marketing: "receita com assinatura ICP-Brasil inclusa" entra na página de planos (argumento do tier Consultório — [pricing](../Discoverys/roadmap-melhorias-2026/05_planos_e_pricing.md)).

## Riscos a observar na execução

| Risco | Mitigação |
|---|---|
| Resposta da pergunta 1 (cert CFM = R$ 0,10?) muda a economia do caminho comum | Mesmo no pior caso é ~R$ 50-60/ano/médico — absorvível; só ajustar o §6.3 do discovery |
| Chave única para SaaS multi-tenant não contemplada no contrato | Pergunta 4 do e-mail; se precisar de aditivo, resolver antes do kickoff |
| Assinatura em lote não suportada (pergunta 5) | UX degrada para 1 autorização por documento — aceitável no MVP |
| SNCR/ANVISA muda regra de controlados durante o desenvolvimento | Controlados já estão fora do MVP por decisão |

## Checklist de retomada (quando a confirmação chegar)

- [ ] Respostas da Valid anexadas ao discovery §12
- [ ] Proposta aceita e assinada; chave de API recebida e guardada no SSM
- [ ] Briefing de integração criado (Passo 1)
- [ ] Briefing `2026-06-10_001` (PDF receita servidor) já executado e em produção
- [ ] Kickoff com a V/Cert agendado
