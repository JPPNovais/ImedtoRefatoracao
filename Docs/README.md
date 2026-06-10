# Docs — referência viva do projeto

Esta pasta é a **fonte de verdade** da documentação do Imedto. O `CLAUDE.md` na raiz é o ponto de entrada com princípios e premissas; **todo o resto** (arquitetura, design, infra, comandos, LGPD, fases de migração, discoveries) vive aqui e é carregado **sob demanda** pelos agentes da pipeline.

## Documentação modular (ler sob demanda)

Use esta tabela para decidir o que carregar antes de cada tarefa:

| Vou tocar... | Ler antes |
|---|---|
| Código backend (.NET, CQRS, handler, EF, Dapper) | [ARQUITETURA.md](ARQUITETURA.md) |
| Código frontend (Vue, store, service, view) | [ARQUITETURA.md](ARQUITETURA.md) + [DESIGN.md](DESIGN.md) |
| UI, componente, layout, design system | [DESIGN.md](DESIGN.md) |
| Autenticação (JWT, BFF, cookies, refresh) | [ARQUITETURA.md §Autenticação](ARQUITETURA.md#autenticação-bff--localjwt) |
| Migration, schema, índice, function SQL | [COMANDOS.md §Migrations](COMANDOS.md#migrations-ef-core-autora-pipeline-aplica-em-rds) + [ARQUITETURA.md](ARQUITETURA.md) |
| Deploy, EC2, RDS, S3, SSM, e-mail, DNS, CI/CD | [INFRA.md](INFRA.md) |
| Build, testes, lint, rodar dev local | [COMANDOS.md](COMANDOS.md) |
| Paciente, prontuário, PII, audit, mensagem de erro | [LGPD.md](LGPD.md) |
| Investigar viabilidade de nova feature/integração | [Discoverys/](Discoverys/) |
| Priorizar/implementar melhorias do roadmap 2026 (fases) | [Roadmap/](Roadmap/README.md) |

## Estrutura

```
Docs/
├── README.md             ← este arquivo (índice)
├── ARQUITETURA.md        ← backend, frontend, auth, conexão Postgres
├── DESIGN.md             ← padrão de produto, design system, UX, componentização
├── INFRA.md              ← AWS (EC2, RDS, S3, SSM, e-mail, DNS, CI/CD)
├── COMANDOS.md           ← build, test, migrations, dev local
├── LGPD.md               ← privacidade, multi-tenant, segurança, audit
│
├── Discoverys/           ← investigações de viabilidade (antes de cravar arquitetura)
│   ├── README.md
│   └── nota-fiscal/
│
├── Roadmap/              ← roadmap 2026 em fases de implementação (F0 infra → F4 expansão + transversal)
│   └── README.md
│
├── DESIGN_SYSTEM.md      ← referência completa do design system (componentes, tokens)
│
├── 00_PLANO_MIGRACAO.md  ← plano-mãe da migração Supabase → AWS RDS
├── 01_FASE_1_HARDENING.md
├── 02_FASE_2_PLATAFORMA.md
├── 03_FASE_3_DOMINIO_CLINICO.md
├── 03A_FASE_3_UX_ADR.md
├── 04_FASE_4_GAPS_SECUNDARIOS.md
├── 05_FASE_5_MIGRACAO_DADOS.md
├── 06_MIGRACAO_ORCAMENTOS.md
│
├── PLANO_*.md            ← planos específicos (CICD, dominio/SSL/email, limpeza, etc)
├── ETL_*.md              ← mapeamento, volumetria, plano de execução do ETL legado
└── baseline/             ← snapshot do estado do código nas auditorias
```

## Documentação viva — premissa

Toda mudança importante em **estrutura, arquitetura, infra, design system ou regra cross-cutting** deve atualizar o documento correspondente desta pasta. Documentação parada vira documentação errada — e prompt errado fica caro em produção.

### Quem atualiza

- **`imedto-business-analyst`** é o responsável primário pela documentação viva. Ao receber demanda que altera arquitetura/infra/design/LGPD, ele atualiza o doc correspondente no mesmo briefing — não como passo opcional, mas como parte da entrega.
- **`imedto-developer`** atualiza quando introduz componente novo no design system, padrão novo de service/store, ou comando recorrente novo.
- **`imedto-database`** atualiza `INFRA.md` quando muda extensions, índices estratégicos, ou padrão de migration; e `COMANDOS.md` quando muda fluxo de migration.
- **`imedto-qa`** valida nos CAs que o doc foi atualizado quando a feature exige.

### Discoveries

Investigações de viabilidade (antes de cravar arquitetura) vão em [Discoverys/](Discoverys/). Cada feature/tema tem subpasta própria. Quando o discovery vira decisão, o conteúdo finalizado migra para `Docs/` (ARQUITETURA / INFRA / ADR formal) e a subpasta de discovery fica como histórico.

## Outros documentos relevantes na raiz do repo

- [`CLAUDE.md`](../CLAUDE.md) — princípios, premissas, pipeline de agentes e índice modular. **Sempre carregado.**
- [`planejamentos/`](../planejamentos/) — briefings imutáveis produzidos pelo `imedto-business-analyst`. Uma demanda = um briefing.
- [`infra/aws-resources.md`](../infra/aws-resources.md) — inventário completo de IDs/ARNs AWS (complementar a `INFRA.md`).
- [`.claude/agents/`](../.claude/agents/) — definição dos 4 agentes da pipeline e o `PIPELINE.md`.
