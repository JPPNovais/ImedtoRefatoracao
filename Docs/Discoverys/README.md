# Discovery

Pasta de **discoveries** — investigações de problema, mapeamento de opções, pesquisa de mercado e recomendações **antes** de cravar arquitetura ou abrir plano de execução.

> Discovery ≠ plano. O que está aqui é insumo de decisão, não compromisso de implementação.

## Organização

Cada feature/tema tem sua própria subpasta. Padrão de nomes dos arquivos dentro de cada subpasta:

| Arquivo | Conteúdo |
|---|---|
| `01_discovery.md` | Documento principal — contexto, escopo, opções, riscos, perguntas em aberto |
| `02_pesquisa_mercado.md` | Pesquisa de mercado / competitive analysis (quando relevante) |
| `03_adr.md` | ADR formal **se** o discovery virar decisão (opcional, gerado depois) |
| `preview.html` | Preview visual / mockups / fluxo (auto-contido, abre direto no navegador) |
| `assets/` | Imagens, diagramas, exports (opcional) |

## Subpastas atuais

- [`nota-fiscal/`](nota-fiscal/) — Emissão de NFS-e integrada com APIs do governo (gateways de mercado vs. Sistema Nacional NFS-e)

## Quando criar um novo discovery

Crie uma subpasta nova quando:

1. A feature toca um **novo bounded context** (faturamento, telemedicina, prescrição eletrônica, etc.).
2. Há **múltiplos caminhos viáveis** que dependem de fatores externos (mercado, regulação, integração com terceiros).
3. A decisão tem **custo alto de reverter** (vendor lock-in, modelagem de domínio, modelo de cobrança).

Para tarefas de implementação direta (bug, refactor pontual, melhoria de UX), use o fluxo normal — não precisa de discovery.

## Quando arquivar

Discovery virou ADR + plano de execução? Mover o conteúdo final para `Docs/` (no diretório-mãe) e deixar só um link de redirect aqui. Discoveries ativos ficam nesta pasta para serem retomados.

## Documentos relacionados

- [`../README.md`](../README.md) — índice geral da pasta `Docs/`
- [`../ARQUITETURA.md`](../ARQUITETURA.md) — quando o discovery resulta em decisão arquitetural
- [`../INFRA.md`](../INFRA.md) — quando o discovery resulta em decisão de infra
