# Pesquisa de mercado — Gateways de NFS-e (Brasil, 2025-2026)

> Status: pesquisa de mercado · Data: 2026-05-03
> Fontes: WebSearch + WebFetch direto nos sites dos provedores (ver §Fontes).
> Caveats: onde marcado "não verificado", a informação não foi confirmada via fonte primária pública. Antes de assinar contrato com qualquer provedor, validar diretamente com o vendor.

---

## TL;DR

- **POC: Focus NFe.** Preço transparente (R$ 89,90/mês tier Solo + R$ 0,10/NF excedente), sandbox imediato, SLA histórico 99,99%. Sem SDK .NET oficial — contornável via `HttpClient`.
- **Fallback estratégico: NFE.io.** Cobertura nacional confirmada nos top-10, webhook obrigatório (alinha com CQRS event-driven), cliente healthtech (Cuidas).
- **Não construir contra Nuvem Fiscal:** descontinuada em 31/07/2026.
- **Não apostar 100% no Sistema Nacional NFS-e (SNNFS-e) direto:** apenas ~52% das prefeituras aderiram em 2026, e SP usa modelo híbrido.
- **Nota Gateway** (ex-eNotas Gateway) merece call comercial: **iClinic e Doctoralia** (concorrentes diretos do Imedto) já são clientes — sinal forte de fit em saúde.

---

## Tabela 1 — Comparação de provedores

| Provedor | Modelo | Preço entrada | Preço ~1k NF/mês | Preço ~10k NF/mês | Cobertura | Custódia A1 | API REST | Webhook | SDK .NET | Sandbox | SLA | Saúde |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Focus NFe** | Assinatura + excedente | R$ 89,90/mês (100 NF) | ~R$ 189,90 | ~R$ 1.089,90 (Solo) ou ~R$ 1.268 (Growth) | +1.400 municípios (top-10 ✅) | Não (cliente armazena) | Sim | Sim | Não (PHP/Java/Ruby/Python/JS) | Sim | 99,99% histórico | ? |
| **NFE.io** | Assinatura por volume | R$ 190/mês (250 NF) | R$ 375 (plano Escala) | Custom (Enterprise) | Nacional (top-10 ✅) | ? | Sim (Swagger) | Sim (obrigatório) | Não (Node/PHP/Ruby/Java) | ? | +99,9% | Cuidas |
| **PlugNotas (TecnoSpeed)** | Pay-per-use | Não publicado | Não publicado | Não publicado | +1.600 municípios | ? | Sim (JSON/REST) | Sim | Sim (C# listado) | ? | Não publicado | Medflynx (?) |
| **eNotas** | Assinatura + adesão | ~R$ 347 + plano (60 NF base) | ? | ? | "Centenas de prefeituras" | Sim (armazena A1) | Sim | ? | ? | ? | 1h suporte | ? |
| **Nota Gateway** (ex-eNotas Gateway) | Custom | ~R$ 129/mês (estimado por terceiros) | ? | ? | +190 cidades sem cert. + SNNFS-e | Sim | Sim | ? | ? | Sim | Não publicado | **iClinic, Doctoralia** ✅ |
| **WebmaniaBR** | Assinatura | R$ 199,90/mês | ? | ? | +2.000 municípios | ? | Sim (JSON/REST) | Sim | ? | ? | Não publicado | ? |
| **Nuvem Fiscal** | DEPRECATED — encerra 31/07/2026 | — | — | — | — | — | — | — | — | — | — | — |
| **Sistema Nacional NFS-e** | Gratuito (gov) | R$ 0 | R$ 0 | R$ 0 | ~52% das prefeituras (obrigatório jan/2026) | N/A | Sim (gov) | Não | Não | Sim | N/A | N/A |

---

## Tabela 2 — Preço modelado por volume

| Provedor | 100 NF/mês | 1.000 NF/mês | 10.000 NF/mês | Custo marginal/NF |
|---|---:|---:|---:|---:|
| **Focus NFe** | R$ 89,90 | R$ 189,90 | R$ 1.089,90 (Solo) ou ~R$ 1.268 (Growth) | R$ 0,10 |
| **NFE.io** | R$ 190 | R$ 375 | custom | tier-based |
| **PlugNotas** | n/p | n/p | n/p | n/p |
| **eNotas** | ~R$ 347 + plano | n/p | n/p | R$ 0,77 (excedente básico) |
| **Nota Gateway** | ~R$ 129 (estimado) | n/p | n/p | ~R$ 0,60 (estimado) |
| **WebmaniaBR** | R$ 199,90 | n/p | n/p | n/p |
| **SNNFS-e** | R$ 0 | R$ 0 | R$ 0 | R$ 0 (custo é dev/manutenção própria) |

---

## Tabela 3 — Cobertura nos top-10 municípios

Legenda: ✅ nativo · ⚠️ parcial / via SNNFS-e · ❌ não cobre · ? não verificado

| Provedor | SP | RJ | BH | DF | Curitiba | POA | Salvador | Fortaleza | Recife | Goiânia |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Focus NFe | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| NFE.io | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| PlugNotas | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ? | ? | ? | ? |
| eNotas | ✅ (sem cert) | ? | ? | ? | ? | ? | ? | ? | ? | ? |
| Nota Gateway | ✅ (sem cert) | ? | ? | ? | ? | ? | ? | ? | ? | ? |
| WebmaniaBR | ✅ | ✅ | ✅ | ? | ✅ | ? | ? | ? | ? | ? |
| SNNFS-e | ⚠️ híbrido | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ |

> SP usa modelo híbrido — parte SNNFS-e, parte Nota Fiscal Paulistana legada. Gateways absorvem essa complexidade.

---

## Tabela 4 — Pontuação final (0-5) para o caso Imedto

| Provedor | Preço | Cobertura | API | Saúde fit | Risco vendor | **Total** |
|---|:-:|:-:|:-:|:-:|:-:|:-:|
| **NFE.io** | 3 | 5 | 5 | 4 | 4 | **21** |
| **Focus NFe** | 5 | 4 | 4 | 3 | 4 | **20** |
| **Nota Gateway** | 2 | 3 | 4 | 5 | 4 | **18** |
| **PlugNotas** | 2 | 4 | 4 | 2 | 5 | **17** |
| **eNotas** | 2 | 3 | 3 | 2 | 3 | **13** |
| **WebmaniaBR** | 2 | 3 | 3 | 2 | 3 | **13** |
| **SNNFS-e** | 5 | 2 | 1 | 1 | 2 | **11** |
| **Nuvem Fiscal** | — | — | — | — | — | **0** (deprecated) |

**Justificativas por dimensão:**

- **Preço:** Focus NFe ganha pelo Solo R$ 89,90 transparente + excedente R$ 0,10 previsível. NFE.io competitivo em volume, mas Enterprise opaco. Demais não publicam tabela.
- **Cobertura:** NFE.io lidera com top-10 confirmado e declaração nacional explícita; Focus NFe próximo (1.400+ verificados); SNNFS-e limitado por adesão municipal (~52%).
- **Qualidade API:** NFE.io tem Swagger + webhook obrigatório + SDKs ativos; Focus NFe doc sólida + sandbox declarado; PlugNotas moderno mas preço opaco.
- **Saúde fit:** **Nota Gateway com iClinic + Doctoralia** publicamente listados — fit vertical direto. NFE.io tem Cuidas (healthtech). Focus NFe sem referência pública.
- **Risco vendor:** PlugNotas (TecnoSpeed) é o mais maduro (>20 anos). Focus NFe e NFE.io têm histórico sólido. Nota Gateway passou por rebranding em 2024. SNNFS-e depende de infraestrutura governamental (instabilidade documentada em jan/2026).

---

## Recomendação top-2

### POC: Focus NFe
Preço transparente, sandbox imediato, SLA 99,99% histórico, tier Solo R$ 89,90 sem fidelidade permite validar o fluxo de NFS-e do Imedto com risco financeiro **mínimo**. Ausência de SDK .NET oficial é contornável — `HttpClient` resolve.

### Fallback estratégico: NFE.io
Cobertura nacional confirmada nos top-10, webhook obrigatório em escala (alinha com a arquitetura CQRS event-driven do Imedto), cliente healthtech (Cuidas) e SLA >99,9% declarado. Migração natural se Focus NFe não escalar para volumes 10k+ NF/mês.

### Wildcard: Nota Gateway
Vale **call comercial** pedindo proposta numérica para 100/1.000/10.000 NF/mês + lista de saúde. Se o pricing for competitivo, o fato de iClinic e Doctoralia já usarem é prova social difícil de ignorar.

---

## Riscos críticos a validar antes do POC

1. **Focus NFe — custódia do certificado A1**: confirmar se o gateway armazena o `.pfx` server-side ou se o Imedto precisa repassar o arquivo a cada requisição (afeta arquitetura de segredos).
2. **Focus NFe — cobertura SP híbrida**: validar comportamento na transição SNNFS-e ↔ Nota Fiscal Paulistana legada.
3. **PlugNotas — tabela de preços**: exigir proposta escrita com volume 1k e 10k antes de qualquer avaliação. Preço opaco é risco contratual direto.
4. **Nota Gateway — SLA formal**: rebranding 2024. Solicitar contrato com SLA numérico + histórico de uptime últimos 12 meses.
5. **SNNFS-e — instabilidade jan/2026**: relatórios oficiais confirmaram instabilidade no início da obrigatoriedade. Qualquer gateway que dependa 100% do servidor nacional herda esse risco — confirmar fallback municipal.
6. **NFE.io — SDK .NET**: repo GitHub tem `poc-nfse-nacional` em C# mas não há SDK oficial. Confirmar com suporte se há client .NET mantido.
7. **Reforma Tributária IBS/CBS**: NFS-e Nacional passará por nova NT (NT-004/005 em piloto desde dez/2025). Confirmar road map de adequação antes de assinar.

---

## Considerações específicas para o caso Imedto

- **Multi-tenant**: cada estabelecimento tem seu CNPJ, regime, certificado, série. Gateway precisa permitir múltiplos emissores em uma conta-master (todos os top-3 permitem).
- **Descrição da NF e LGPD**: descrição **nunca** deve conter CID/diagnóstico. Usar texto controlado tipo "Consulta em <especialidade>" — implementação no domínio Imedto, não no gateway.
- **Tomador PF**: maioria das NFs do Imedto será para pessoa física. Validar se o gateway exige CPF do tomador (alguns municípios exigem; outros aceitam "consumidor não identificado").
- **Código de serviço**: predomina LC 116/2003 item 4 (saúde, assistência médica). Configurar default por estabelecimento, com possibilidade de override por procedimento.
- **ISS retido**: clínicas em Lucro Presumido podem ter ISS retido na fonte por algumas pessoas jurídicas tomadoras. Backend precisa modelar essa regra.
- **Reforma Tributária**: até 2033 o sistema precisa lidar com IBS/CBS coexistindo com ISS — gateway escolhido tem que ter road map claro.

---

## Padrão de abstração recomendado

Independente do provedor escolhido, isolar atrás de uma única interface:

```csharp
public interface INfsEmissaoGateway
{
    Task<EmissaoResult> EmitirAsync(EmissaoRequest request, CancellationToken ct);
    Task<CancelamentoResult> CancelarAsync(string idExterno, MotivoCancelamento motivo, CancellationToken ct);
    Task<ConsultaResult> ConsultarAsync(string idExterno, CancellationToken ct);
}
```

Implementações: `FocusNfeGateway`, `NfeIoGateway`, `NotaGatewayGateway`. Selecionar via configuração do estabelecimento (caso B2B exija provedor próprio do contador) ou via config global. Trocar provedor = nova classe, não reescrita.

---

## Fontes

1. https://focusnfe.com.br/precos/
2. https://focusnfe.com.br/cidades-integradas-nfse/
3. https://focusnfe.com.br/doc/
4. https://nfe.io/precos/emissao-nfse/
5. https://nfe.io/docs/prefeituras-integradas/cidades-integradas/
6. https://github.com/nfe
7. https://plugnotas.com.br/nfse/
8. https://notagateway.com.br/
9. https://suporte.nuvemfiscal.com.br/t/encerramento-do-suporte-nuvemfiscal-e-migracao-para-o-acbr/5347
10. https://www.gov.br/fazenda/pt-br/assuntos/noticias/2025/agosto/a-partir-de-janeiro-de-2026-a-nota-fiscal-de-servico-eletronica-nfs-e-sera-obrigatoria-a-fim-de-simplificar-cotidiano-das-empresas
11. https://blog.tecnospeed.com.br/adesao-nfs-e-nacional-novos-municipios/
12. https://notagateway.com.br/adesao-municipios-nfse/
13. https://webmania.com.br/planos/
14. https://www.transmitenota.com.br/site/api/precos.php
15. https://notas-fiscais.com/enotas-gateway/
