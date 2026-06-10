# Roadmap de Implementação — Imedto 2026

> Plano de execução em fases derivado do discovery [`Docs/Discoverys/roadmap-melhorias-2026/`](../Discoverys/roadmap-melhorias-2026/01_discovery.md) (pesquisa de mercado, inventário funcional e auditoria técnica de 2026-06-09). Cada arquivo desta pasta é uma fase auto-contida, pronta para virar demandas da pipeline.

## Princípio de sequenciamento

**Operação antes de venda → completude antes de novidade → conversão antes de diferenciação → diferenciação antes de paridade.**

```
FASE 0  Fundação/infra (gatilho: 1º pagante)      ─┐
FASE 1  Completude (zero stubs, demo vendável)     │   FASE TRANSVERSAL
FASE 2  Conversão (WhatsApp, online, Memed)        ├── Qualidade de código/BD
  └─ 2B Central de migração de concorrentes        │   (~20% da capacidade,
FASE 3  Diferenciação (IA scribe, financeiro, BI)  │    contínua em paralelo)
FASE 4  Paridade e expansão (tele, portal, mobile)─┘
```

## Arquivos

| Fase | Arquivo | Objetivo | Pré-requisito |
|---|---|---|---|
| **F0** | [`FASE_0_FUNDACAO_INFRA.md`](FASE_0_FUNDACAO_INFRA.md) | Infra AWS escalável por estágios (E0→E3), backup, alertas, gates de CI, Lambda para migrations | — (E0 pode já; E1 no 1º pagante) |
| **F1** | [`FASE_1_COMPLETUDE.md`](FASE_1_COMPLETUDE.md) | Destravar stubs 501, completar módulos a 70-90%, 2FA — demo sem "ainda não funciona" | — |
| **F1B** | [`FASE_1B_ASSINATURA_DIGITAL_ICP.md`](FASE_1B_ASSINATURA_DIGITAL_ICP.md) | Assinatura ICP-Brasil via IntegraICP — **em espera** (aguarda confirmação comercial da Valid) | F1 item 1.1 + aceite da proposta |
| **F2** | [`FASE_2_CONVERSAO.md`](FASE_2_CONVERSAO.md) | O combo que fecha assinatura: WhatsApp, agendamento online, Memed, medidores de consumo | F1 (e F0-E1 para vender) |
| **F2B** | [`FASE_2B_CENTRAL_DE_MIGRACAO.md`](FASE_2B_CENTRAL_DE_MIGRACAO.md) | Importadores dos principais concorrentes — "mude para o Imedto em 1 dia" | F1 |
| **F3** | [`FASE_3_DIFERENCIACAO.md`](FASE_3_DIFERENCIACAO.md) | IA scribe PT-BR, financeiro+NFS-e, BI, CRM de orçamentos, pós-op, galeria | F2 |
| **F4** | [`FASE_4_PARIDADE_EXPANSAO.md`](FASE_4_PARIDADE_EXPANSAO.md) | Telemedicina, portal do paciente, mobile, API pública, TISS (condicional) | tração da F2-F3 |
| **FT** | [`FASE_TRANSVERSAL_QUALIDADE.md`](FASE_TRANSVERSAL_QUALIDADE.md) | Correções de BD e backend (auditorias 2026-06-09), god components, design system, tipografia | contínua |

### Épicos com plano mestre dedicado

Épicos grandes demais para um item de fase ganham plano próprio (visão + fases internas + questões abertas). Cada fase interna vira um briefing imutável em `planejamentos/`, refinada com o usuário antes de executar.

| Épico | Plano mestre | Materializa | Estado |
|---|---|---|---|
| **Financeiro / Cobranças** | [`MODULO_FINANCEIRO_COBRANCAS.md`](MODULO_FINANCEIRO_COBRANCAS.md) | item "financeiro" da [F3](FASE_3_DIFERENCIACAO.md) | plano mestre — em refinamento por fase (F1→F2→F3→F3B→F4→F5→F6→F7); inclui conduta-checklist (F3B) e caixa/comissão (F7) |

## Documentos de estratégia (insumo, não execução)

- [Pesquisa de mercado](../Discoverys/roadmap-melhorias-2026/02_pesquisa_mercado.md) — concorrentes, preços, dores ranqueadas (válida ~6 meses)
- [Diferenciais especificados](../Discoverys/roadmap-melhorias-2026/04_diferenciais.md) — 16 diferenciais com ficha e matriz de priorização
- [Planos e pricing](../Discoverys/roadmap-melhorias-2026/05_planos_e_pricing.md) — 3 tiers, franquias, modelo de cobrança

## Como executar cada item (processo)

1. **Item de produto** → demanda para o `imedto-business-analyst` → briefing imutável em `planejamentos/` com CAs Dado/Quando/Então (multi-tenant + RBAC + LGPD + estados + performance obrigatórios) → `imedto-developer` (→ `imedto-database` se schema muda) → `imedto-qa` (único que commita/pusha).
2. **Item técnico/infra** (F0 e parte da FT) → demanda direta dev+QA, sem BA.
3. **Item com decisão externa cara** (vendor, regulação, custo variável) → discovery antes do briefing. Já existem: [`whatsapp-envio/`](../Discoverys/whatsapp-envio/), [`nota-fiscal/`](../Discoverys/nota-fiscal/), [`assinatura-digital-receitas/`](../Discoverys/assinatura-digital-receitas/). A criar quando a fase chegar: IA scribe, telemedicina (build vs buy).
4. **Feature nova nasce atrás de feature flag** de plano (infra de planos/flags já existe) — habilita o empacotamento comercial sem retrabalho.
5. **Medir para refinar**: conversão trial→pago, churn, taxa de no-show dos clientes, tempo até 1º agendamento, uptime. Reavaliar prioridades ao fim de cada fase.

## Critérios de saída por fase (resumo)

- **F0-E0**: restore testado e registrado; pipeline vermelho bloqueia deploy; porta 22 fechada.
- **F1**: zero endpoints 501; receita assinada digitalmente válida; demo completa sem ressalva.
- **F2**: clínica piloto medindo queda de no-show; agendamento online publicado; medidores de consumo funcionando.
- **F2B**: importar 1.000 pacientes de um CSV de concorrente em <15 min com relatório de erros.
- **F3**: % de evoluções via IA scribe nos pilotos; funil de orçamentos em uso.
- **F4**: decidido por tração, não por calendário.
