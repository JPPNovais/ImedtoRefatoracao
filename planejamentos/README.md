# planejamentos/

Esta pasta guarda **briefings imutáveis** produzidos pelo `imedto-business-analyst`. Cada arquivo descreve uma demanda já refinada e aprovada pelo usuário, com critérios de aceite testáveis, e é a **única fonte de verdade** que `imedto-developer`, `imedto-database` e `imedto-qa` consultam para executar e validar.

## Convenção de nome

```
YYYY-MM-DD_NNN_titulo-em-kebab-case.md
```

- `YYYY-MM-DD` — data de criação do briefing (não a data de execução).
- `NNN` — número sequencial do dia, começando em `001`.
- `titulo-em-kebab-case` — resumo curto (3-6 palavras) do escopo.

Exemplos:
- `2026-05-25_001_bloqueio-agenda-profissional.md`
- `2026-05-25_002_relatorio-faturamento-convenio.md`
- `2026-05-26_001_filtro-prontuario-por-especialidade.md`

## Addendums

Quando o `imedto-qa` detecta **spec gap** (Tipo B) e devolve para o BA, o briefing original **não é editado**. Em vez disso, o BA cria um addendum:

```
YYYY-MM-DD_NNN_titulo-original-addendum.md
```

O addendum cita o briefing original no header (`## Refere-se a: 2026-05-25_001_bloqueio-agenda-profissional.md`), explica o gap, lista as decisões novas de produto e adiciona CAs incrementais. Briefings podem ter múltiplos addendums sequenciais (`-addendum`, `-addendum-2`, ...).

## Estrutura obrigatória do briefing

Todo briefing tem essa estrutura — sem exceção:

```md
# {{titulo legivel}}

**ID**: 2026-05-25_001
**Status**: Aprovado por usuário em 2026-05-25
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P / M / G

## 1. Contexto e motivação
Por que essa demanda existe. Qual dor da persona resolve. Que evidência sustenta (suporte, métrica, decisão de produto).

## 2. Persona-alvo
Quem usa, em que momento da jornada, com que frequência.

## 3. Escopo
**Inclui**: lista enxuta do que ENTRA.
**Não inclui**: lista do que sai para evitar scope creep.

## 4. Regras de negócio
Cada regra numerada (R1, R2, ...). Cada regra cita onde mora (Domain/Handler/Query/Front) e quem valida (front + back é o padrão).

## 5. Modelo de dados
Tabelas afetadas, colunas novas, índices, vínculo multi-tenant, audit, LGPD.

## 6. UX e fluxo
Wireframe textual ou descrição de tela-por-tela. Componentes do design system reutilizados. Estados (loading/erro/vazio/sucesso). Atalhos de teclado se aplicável.

## 7. Critérios de aceite (testáveis)
CA1, CA2, ... Cada CA é uma frase "Dado / Quando / Então" verificável. CAs cobrem:
- Caminho feliz
- Multi-tenant (usuário de outro estabelecimento não vê/edita)
- RBAC (papel sem permissão é bloqueado no back, escondido no front)
- LGPD (PII fora de log, mensagem genérica, audit trail)
- Estados (loading, erro 422 do back, vazio, sucesso)
- Performance subjetiva (lista > 1000 itens, debounce em busca)

## 8. Riscos e dependências
O que pode quebrar. Áreas regressivas a vigiar. Features dependentes.

## 9. Observações para execução
Notas para dev/db/qa — não negociáveis vs. liberdade técnica.

## 10. Atualização de documentação
Lista de docs em `Docs/` que serão atualizados nesta entrega (ou "nenhum"). Cada item explica o que muda.
Exemplo:
- `Docs/DESIGN.md` — adicionar componente `AppRecurrencePicker` à seção do design system.
- `Docs/ARQUITETURA.md` — atualizar §Frontend com novo padrão de store para entidades com recorrência.
```

## Por que isso existe

- **Carga cognitiva separada**: BA pensa; dev/db/qa executam contra texto imutável.
- **Imutabilidade reduz patch de sintoma**: se algo não foi previsto, gera addendum e revalida com o usuário, em vez de o dev "interpretar" no calor.
- **Auditoria de produto**: commits referenciam o briefing. Em 6 meses dá pra entender o porquê de cada feature.
- **Quality gate**: QA valida CA a CA, sem ambiguidade.

## Quando NÃO criar briefing

- Spike/exploração de viabilidade técnica (não muda código de produção).
- Refator interno puro (sem mudança observável para o usuário).
- Hotfix urgente trivial (1 linha, óbvio). Mesmo assim, descreva no commit.

Para qualquer outra coisa: **briefing primeiro, código depois**.
