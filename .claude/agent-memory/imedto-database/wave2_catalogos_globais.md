---
name: wave2-catalogos-globais
description: Wave 2 admin (REVERTIDA em Wave 4): 3 tabelas paralelas dropadas; catálogos globais agora via EhPadraoSistema=true nas tabelas legado
metadata:
  type: project
---

Migration `20260530131404_CriarCatalogosGlobaisWave2` aplicada em dev em 2026-05-30.

**WAVE 4 (2026-05-30 — briefing 2026-05-30_004): abordagem de tabelas paralelas REVERTIDA.**

As 3 tabelas paralelas foram dropadas via `db/migrations/20260530200000_drop_catalogos_globais_wave2.sql`.
Novo modelo: live-link nativo via `EhPadraoSistema=true` + `EstabelecimentoId=NULL` nas tabelas legado.

**Tabelas REMOVIDAS (Wave 4)**:
- `imedto_modelo_prontuario_global` — dropada. Admin usa `modelo_de_prontuario` com `EhPadraoSistema=true`.
- `imedto_variavel_pool_global` — dropada. Admin usa `prontuario_variaveis_pool` com `EhPadraoSistema=true`.
- `imedto_regiao_anatomica_global` — dropada. Admin usa `regioes_anatomicas_catalogo` (já global por construção).

**Inspeção pré-drop (dev, 2026-05-30)**:
- imedto_modelo_prontuario_global: 2 registros (seeds), 0 FKs externas, 0 cópias em tenants
- imedto_variavel_pool_global: 3 registros (seeds), 0 FKs externas, 0 cópias em tenants
- imedto_regiao_anatomica_global: 15 registros (seeds), 0 FKs externas

**Tabelas legado que permanecem (estado pós-Wave 4)**:
- `modelo_de_prontuario`: 5 registros (4 padrão-sistema, 1 do tenant). Tem `eh_padrao_sistema bool`, `estabelecimento_id bigint NULL`.
- `prontuario_variaveis_pool`: 80 registros (0 padrão-sistema, 80 do tenant). Tem `eh_padrao_sistema bool`, `estabelecimento_id bigint NULL`, `tipo varchar`.
- `regioes_anatomicas_catalogo`: 144 registros ativos. Global por construção. Tem `codigo/pai_codigo/nivel/vista/template_texto/svg_coords/ordem/lateralidade/ativo`.

**Colunas adicionadas em `imedto_config` (Wave 2 — permanecem)**:
`tipo text NOT NULL DEFAULT 'texto'` e `secao text`. Seeds: 8 chaves (trial, assinatura, sistema, feature_flags, comunicacao, seguranca).

**Ponto de atenção para o developer (W4-CA24)**:
DbSets do AppDbContext ainda presentes (anotados com aviso), build passa.
Developer deve: (1) remover domain types/configurations/repositories/handlers C#, (2) remover DbSets, (3) criar migration EF de sincronização do snapshot.

**Arquivos de migration**:
- `db/migrations/20260530131404_criar_catalogos_globais_wave2.sql` (schema Wave 2 — histórico)
- `db/migrations/20260530131405_criar_catalogos_globais_wave2_indices.sql` (índices — histórico)
- `db/migrations/20260530131406_seed_catalogos_globais_wave2.sql` (seeds — histórico)
- `db/migrations/20260530200000_drop_catalogos_globais_wave2.sql` (drop Wave 4 — ativo)

[[area_admin_migration]]
