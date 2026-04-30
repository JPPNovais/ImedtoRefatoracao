# ETL Volumetria — Estimativa de carga

**Origem:** Supabase `rkgxcmubxkcvzhqhllev` (legado)
**Destino:** Supabase `kdoqflrmfgazdgekdbqc` (novo)
**Data:** 2026-04-30
**Método:** Estimativa baseada em premissas de produto (Imedto SaaS clínico, ~6 meses de produção, perfil de usuário típico de consultórios e clínicas pequenas/médias). **Sem acesso direto ao banco legado** — números são projeções, não medições.
**Fonte das tabelas:** [ETL_MAPEAMENTO.md](ETL_MAPEAMENTO.md) (66 tabelas legado vivas).

> **Aviso analítico**: este documento é estimativa de dimensionamento, não baseline factual. Ao obter acesso ao legado (mesmo que via `pg_dump` parcial), substituir as colunas por contagens reais e recalcular janela. Erro esperado da estimativa: ±50% no cenário conservador, ±30% no otimista — tamanho típico de modelos paramétricos sem validação empírica.

---

## Premissas

### Cenários

| Cenário | Estabelecimentos ativos | Justificativa |
|---|---|---|
| **Conservador** | 50 | Lower bound — produto em early stage, base orgânica ainda pequena. |
| **Otimista** | 300 | Upper bound — campanhas comerciais bem-sucedidas e penetração regional. |

### Perfil típico por clínica (ambos os cenários — varia só multiplicador)

| Dimensão | Valor médio | Observação |
|---|---|---|
| Profissionais ativos | 2,5 | 1-3 dentistas/médicos por clínica pequena. |
| Pacientes cadastrados | 600 | Mediana de cadastro acumulado em ~6 meses. |
| Agendamentos/dia | 15 | Distribuição assimétrica — alguns têm 30, muitos têm 5. |
| Receitas/dia | 4 | Apenas clínicas médicas/odontológicas com prescrição. ~60% das clínicas. |
| Orçamentos/dia | 1,5 | Concentrado em odonto/cirúrgicas. |
| Lançamentos financeiros/mês | 150 | Caixa diário + entradas/saídas. |
| Movimentações de estoque/mês | 100 | Concentrado em clínicas com inventário (60%). |
| Evoluções de prontuário/dia | 12 | ~80% dos atendimentos geram evolução. |

### Premissas técnicas

- **Throughput de INSERT em batch via `INSERT INTO ... SELECT FROM ...` no DB intermediário (mesma instância)**: 5.000 linhas/segundo para tabelas simples (1 KB/linha), caindo para ~1.000 linhas/segundo em tabelas com `jsonb` rico (>10 KB/linha) por custo de TOAST.
- **Período de operação**: ~180 dias úteis (~6 meses). Tabelas transacionais multiplicam por dias úteis; tabelas de cadastro acumulam o estoque total.
- **Sem rede entre origem e destino**: assume-se DB intermediário hospedado no mesmo region/AZ do destino (sa-east-1). Latência por INSERT remoto adicionaria 5-10x — fator considerado em "Riscos" abaixo.

---

## Estimativa por tabela

> Referências de tabela: [ETL_MAPEAMENTO.md](ETL_MAPEAMENTO.md) seções 1-14.

### 1. Identidade e tenant

| Tabela legado | Cons. (50 estab) | Otim. (300 estab) | KB/linha | MB total (otim.) | Tempo INSERT (s, otim.) |
|---|---:|---:|---:|---:|---:|
| `usuarios` | 125 | 750 | 1 | 0,7 | <1 |
| `estabelecimentos` | 50 | 300 | 2 | 0,6 | <1 |
| `unidades_estabelecimento` | 75 | 450 | 1 | 0,5 | <1 |

### 2. Profissional + vínculos

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `profissionais` | 125 | 750 | 1 | 0,7 | <1 |
| `vinculo_profissional_estabelecimento` | 200 | 1.200 | 1 | 1,2 | <1 |
| `solicitacao_vinculo_profissional_estabelecimento` (pendentes) | 25 | 150 | 1 | 0,2 | <1 |
| `modelo_permissao_estabelecimento` | 150 | 900 | 5 | 4,4 | 1 |

### 3. Pacientes

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `pacientes` | 30.000 | 180.000 | 3 | 540 | 36 |

### 4. Prontuário

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `modelo_de_prontuario` | 200 | 1.200 | 8 | 9,4 | 1 |
| `prontuario_variaveis_pool` | 10.000 | 30.000 | 1 | 29 | 6 |
| `prontuarios` | 30.000 | 180.000 | 1 | 176 | 36 |
| `evolucao_prontuario` → `prontuario_evolucoes` | **108.000** | **648.000** | **15** | **9.492** | **648** |
| `exame_fisico` | 30.000 | 180.000 | 8 | 1.406 | 180 |
| `exame_fisico_regioes` (catálogo customizado) | 50 | 300 | 2 | 0,6 | <1 |

### 5. Agenda

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `evento_de_agendamento` → `agendamentos` | **135.000** | **810.000** | 2 | 1.582 | 162 |

### 6. Salas

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `sala_atendimento` | 100 | 600 | 1 | 0,6 | <1 |

### 7. Estoque/Inventário

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `estoque_produto` → `itens_inventario` | 6.000 | 36.000 | 2 | 70 | 8 |
| `movimento_estoque` → `movimentacoes_estoque` | **54.000** | **324.000** | 1 | 317 | 65 |

### 8. Orçamentos

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `orcamentos` | 13.500 | 81.000 | 4 | 317 | 17 |
| `orcamento_cirurgias` | 6.000 | 36.000 | 3 | 105 | 8 |
| `orcamento_internacao` | 3.000 | 18.000 | 2 | 35 | 4 |
| `orcamento_anestesia` | 3.000 | 18.000 | 2 | 35 | 4 |
| `orcamento_implante` → `orcamento_implantes` | 6.000 | 36.000 | 2 | 70 | 8 |
| `orcamento_equipe_especializada` (merge) | 4.000 | 24.000 | 1 | 24 | 5 |
| `orcamento_profissionais` (merge) | 8.000 | 48.000 | 1 | 47 | 10 |
| `orcamento_valor_profissional` (merge) | 4.000 | 24.000 | 1 | 24 | 5 |
| `orcamento_formas_pagamento` | 13.500 | 81.000 | 1 | 80 | 17 |

### 9. Receitas

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `receitas` | 21.600 | 129.600 | 2 | 253 | 26 |
| `receita_itens` | 65.000 | 390.000 | 1 | 381 | 78 |
| `receitas_configuracao_estabelecimento` | 50 | 300 | 1 | 0,3 | <1 |
| `medicamentos_favoritos` | 1.000 | 6.000 | 1 | 6 | 1 |

### 10. Financeiro

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `financeiro_categoria` → `categorias_financeiras` | 750 | 4.500 | 1 | 4 | 1 |
| `financeiro_forma_pagamento` → `formas_pagamento` | 400 | 2.400 | 1 | 2 | <1 |
| `financeiro_transacao` → `lancamentos` | **45.000** | **270.000** | 1 | 264 | 54 |

### 11. Automação

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `automation_rules` | 150 | 900 | 8 | 7 | 1 |
| `automation_events` | 5.000 | 30.000 | 5 | 147 | 30 |
| `automation_audit_logs` → `configuracoes_automacao` | 200 | 1.200 | 2 | 2 | <1 |

### 12. IA / Notificações / Audit

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `ai_audit_logs` | 5.000 | 30.000 | 4 | 117 | 30 |
| `ai_rate_limits` | 100 | 600 | 1 | 0,6 | <1 |
| `establishment_ai_settings` | 50 | 300 | 2 | 0,6 | <1 |
| `notifications` (90d) → `notificacoes` | 9.000 | 54.000 | 2 | 105 | 11 |

### 13. Subscription

| Tabela legado | Cons. | Otim. | KB/linha | MB (otim.) | Tempo (s) |
|---|---:|---:|---:|---:|---:|
| `assinaturas` | 50 | 300 | 1 | 0,3 | <1 |

---

## Totais agregados

| Métrica | Conservador (50 estab) | Otimista (300 estab) |
|---|---:|---:|
| **Linhas totais (DB)** | ~720 mil | ~4,3 milhões |
| **Tamanho total (DB, MB)** | ~2,6 GB | ~15,5 GB |
| **Tempo INSERT (s, sequencial)** | ~250 s (~4 min) | ~1.500 s (~25 min) |
| **Tempo INSERT com 4-way paralelismo** | ~80 s (~1,5 min) | ~480 s (~8 min) |

---

## Top 10 tabelas mais volumosas (cenário otimista)

Estas dominam o tempo total e devem ser priorizadas para paralelização e atenção:

1. **`evolucao_prontuario` → `prontuario_evolucoes`** — 648k linhas, ~9,5 GB, ~11 min sozinha.
2. **`agendamentos`** — 810k linhas, ~1,6 GB, ~3 min.
3. **`exame_fisico`** — 180k linhas, ~1,4 GB, ~3 min.
4. **`receita_itens`** — 390k linhas, ~380 MB, ~1,3 min.
5. **`movimentacoes_estoque`** — 324k linhas, ~317 MB, ~1 min.
6. **`lancamentos`** — 270k linhas, ~264 MB, ~1 min.
7. **`pacientes`** — 180k linhas, ~540 MB, ~36 s.
8. **`prontuarios`** — 180k linhas, ~176 MB, ~36 s.
9. **`receitas`** — 130k linhas, ~253 MB, ~26 s.
10. **`orcamentos`** — 81k linhas, ~317 MB, ~17 s.

**Insight crítico:** as 3 primeiras tabelas (`prontuario_evolucoes`, `agendamentos`, `exame_fisico`) sozinhas representam ~17 min do tempo total no cenário otimista — **80% do trabalho está em 4% das tabelas** (Pareto clássico em sistemas clínicos: dado transacional em jsonb domina).

---

## Estimativa de janela de manutenção

Soma sequencial dos tempos por etapa (DB+Storage+validação), aplicando paralelização onde possível.

### Cenário conservador (50 estabelecimentos)

| Etapa | Duração estimada |
|---|---|
| Pre-cutover: snapshot legado + freeze writes | 10 min |
| `pg_dump` legado + restore intermediário | 15 min |
| ETL DB (sequencial estrito) | 5 min |
| Storage (anexos + fotos, ver abaixo) | 10 min |
| Validador de paridade + smoke tests | 15 min |
| DNS/redirect + comunicação cutover | 5 min |
| **Total bruto** | **~60 min** |
| **Com buffer 2x** | **~2 horas** |

### Cenário otimista (300 estabelecimentos)

| Etapa | Duração estimada |
|---|---|
| Pre-cutover: snapshot legado + freeze writes | 15 min |
| `pg_dump` legado + restore intermediário | 45 min |
| ETL DB (com 4-way paralelismo nas top 10) | 25 min |
| Storage (anexos + fotos) | 45 min |
| Validador de paridade + smoke tests | 25 min |
| DNS/redirect + comunicação cutover | 5 min |
| **Total bruto** | **~160 min (~2h40)** |
| **Com buffer 2x** | **~5 horas** |

### Janela proposta

- **Cenário conservador:** **3 horas** (02:00 – 05:00 BRT) — buffer de 50% sobre estimativa.
- **Cenário otimista:** **5 horas** (01:00 – 06:00 BRT) — buffer de 100% sobre estimativa.
- **Recomendação prática:** dimensionar para o cenário otimista mesmo que a base atual esteja perto do conservador. Custo de janela maior é baixo (madrugada de domingo); custo de estourar janela e abrir o app sem terminar é catastrófico.

---

## Considerações operacionais

### Storage (anexos e fotos)

Bucket `imedto-anexos` (legado) e `imedto-fotos` legado são o **maior risco de timing imprevisível**, porque o tempo depende de:
- Número de arquivos (não bytes — cada arquivo tem overhead de request).
- Tamanho médio (anexos de prontuário podem ter PDFs de 5-20 MB).
- Throughput de download/upload da Supabase Storage API (estimar ~50-100 MB/s sustentado).

**Estimativa de storage:**

| Cenário | # anexos prontuário | Tamanho médio | Total | Tempo @ 75 MB/s |
|---|---:|---:|---:|---:|
| Conservador | 15.000 | 2 MB | 30 GB | ~7 min |
| Otimista | 90.000 | 2 MB | 180 GB | ~40 min |

**Mitigação**: copiar Storage **antes do freeze** (cópia "online" enquanto app legado ainda escreve), e fazer **delta sync** durante a janela só para arquivos criados nas últimas 24h. Reduz janela em 30-40 min no cenário otimista.

### Throughput de download/upload de Storage durante janela

Limitação real do Supabase Storage hosted: ~100 req/s sustentado por bucket. Para 90k arquivos sequenciais isso seria 15 min só de overhead de requests — paralelizar com 10 workers concorrentes.

---

## Recomendações de paralelização

Tabelas que podem rodar em paralelo (sem FK entre si):

**Wave A (paralelo, depois de tenant base):**
- `pacientes`
- `modelo_de_prontuario`
- `prontuario_variaveis_pool`
- `categorias_financeiras` + `formas_pagamento`
- `automation_rules`

**Wave B (paralelo, depois de Wave A):**
- `prontuarios` (depende de `pacientes`)
- `agendamentos` (depende de `pacientes` + `profissionais`)
- `itens_inventario`
- `medicamentos_favoritos`

**Wave C (paralelo, depois de Wave B — alta demanda):**
- `prontuario_evolucoes` (única, ocupa 1 worker — não paralelizar internamente sem cuidado para preservar ordem temporal)
- `exame_fisico`
- `receitas` + `receita_itens` (sequencial entre si, paralelo com outras)
- `movimentacoes_estoque`
- `lancamentos`
- `orcamentos` + filhas

**Não paralelizar:**
- `usuarios` → `estabelecimentos` → `vinculos` (FK chain crítica).
- `prontuario_evolucoes` internamente (triggers de imutabilidade re-habilitam ao final — ordem importa para `template_snapshot`).

**Ganho estimado de 4-way paralelismo nas Waves B/C**: redução de ~60% no tempo de DB ETL (de 25 min para ~10 min no otimista).

---

## Riscos de tamanho

Tabelas projetadas para **>100k linhas** (cenário otimista) — exigem estratégia especial:

| Tabela | Linhas (otim.) | Estratégia recomendada |
|---|---:|---|
| `agendamentos` | 810k | Chunks de 50k via `INSERT INTO ... SELECT ... LIMIT/OFFSET` para evitar lock longo. Indexes não-PK criados **após** carga. Constraint anti-overlap **desabilitada** durante carga (já decidido). |
| `prontuario_evolucoes` | 648k | **Maior risco**. `conteudo jsonb` com 5-50 KB cada → carga lenta por TOAST. Sugerido: copy via `COPY` binário em vez de `INSERT`. Triggers de imutabilidade desabilitadas (já decidido). Índice GIN no jsonb criado após carga. |
| `receita_itens` | 390k | Carga normal — linhas pequenas. Cuidado só com FK para `receitas` (carregar pais primeiro). |
| `movimentacoes_estoque` | 324k | Append-only, simples. Carga linear. |
| `lancamentos` | 270k | Carga linear. Atenção a `data_competencia` para validação pós-carga (não pode ter linhas com data futura). |
| `exame_fisico` | 180k | `regioes_examinadas jsonb` pode ser grande. Mesma estratégia de `prontuario_evolucoes` para tabelas com jsonb pesado. |
| `pacientes` | 180k | PII criptografada — confirmar que `pgsodium` está habilitado no destino antes de iniciar (caso contrário a carga falha em silêncio com null nas colunas cifradas). |
| `prontuarios` | 180k | Linhas pequenas, carga rápida. |
| `receitas` | 130k | Atenção ao mapping `(tipo, tipo_notificacao) → Tipo` — bloquear ETL se houver combinações inválidas (já documentado em `ETL_MAPEAMENTO.md`). |

---

## Limitações desta análise

O que esta volumetria **não** responde:

1. **Distribuição por estabelecimento (cauda longa)**: pode existir 1 estabelecimento "monstro" com 50% dos pacientes. A média esconde isso. Requer query no legado para identificar — sem acesso, assumimos uniformidade.
2. **Crescimento intra-janela**: se comunicação a clínicas falhar, pode haver writes durante a janela. Validador de paridade detecta, mas não previne.
3. **Volume real de jsonb em `prontuario_evolucoes`**: se conteúdo médio for 50 KB em vez de 15 KB, tempo dessa tabela triplica para ~33 min — estoura o orçamento da janela conservadora.
4. **Dependência de Storage**: tempo de cópia de buckets é o maior fator de incerteza (±50%).

**Recomendação analítica:** antes de marcar a janela em definitivo, fazer um **dry-run com `pg_dump` parcial** (10% das linhas das top 5 tabelas) para calibrar throughput real medido vs estimado. Isso reduz a incerteza de ±50% para ±10% e permite ajustar buffer.

---

## Recomendação de quando agendar

- **Dia da semana**: **domingo de madrugada** (02:00–06:00 BRT). Razões:
  - Volume mínimo de uso de clínicas (consultórios fechados, sem agendamentos, sem prescrições eletrônicas em curso).
  - Se janela estourar para 07:00–08:00, ainda é cedo para clínicas que abrem 09:00.
  - Equipe de plantão (devops + suporte) compatível com calendário de standby.
- **Mês**: evitar dezembro (recesso, mas também tráfego de fechamento contábil) e janeiro (volta de férias). **Sugestão: domingo de meio de mês em maio ou junho 2026** (após Dia das Mães e Corpus Christi, antes das férias de julho).
- **Comunicação**: banner no app legado **7 dias antes**, e-mail **3 dias e 24h antes** com instruções de reset de senha pós-cutover.

---

## Próximos passos analíticos

1. Quando obtiver acesso ao legado (mesmo via `pg_dump` parcial em DB intermediário): rodar `SELECT COUNT(*), pg_total_relation_size(...)` por tabela e substituir as estimativas.
2. Identificar **outliers de estabelecimento**: query `SELECT estabelecimento_id, COUNT(*) FROM pacientes GROUP BY 1 ORDER BY 2 DESC LIMIT 10` — se top 5 detém >40% dos dados, paralelizar por estabelecimento em vez de por tabela.
3. Medir **tamanho real de `conteudo jsonb`** em `evolucao_prontuario` (`avg(pg_column_size(conteudo))`) — fator de risco #1 da janela.
4. Após a primeira execução, capturar tempos reais para construir baseline e validar o modelo desta análise — útil para futuras migrações de tenants ou consolidações.
