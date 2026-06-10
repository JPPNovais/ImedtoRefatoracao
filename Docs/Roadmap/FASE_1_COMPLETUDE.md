# FASE 1 — Completude (zero "ainda não funciona")

> Parte do roadmap [`README.md`](README.md). **Objetivo**: tudo que está a 70-95% chega a 100%; nenhum endpoint 501; demo de venda completa sem ressalva. É a fase que viabiliza mostrar o produto sem vergonha — pré-condição para publicar preço (ver [pricing §8.4](../Discoverys/roadmap-melhorias-2026/05_planos_e_pricing.md)).
>
> **Duração estimada**: 3-5 semanas. **Pré-requisitos**: nenhum (paralelizável com F0-E0).

## ✅ EXECUTADA em 2026-06-10 (exceto 1.3, em espera)

Fila executada pela pipeline (dev → QA) na sessão de 2026-06-10: **001** (`7ebe129`), **003** (`2263507`), **004** (`7fe16da`), **005** (`175fbc2`), **007** (`b11bd4c`), **008** (`2aab3d2`), **002** (`d8691a1`), **lint §5 + fix TS2550** (`dc98557`) e **006/2FA** (schema + implementação + docs — commit final da sessão). 4 devoluções Tipo A no ciclo (campo não-salvo no 005, teto de paginação no 007, literal tipográfico no 008, docs §10 no 006) — todas corrigidas e revalidadas. Validação visual em produção fica com o usuário (sandbox sem browser). **1.3/F1B segue em espera da Valid.**

## ⚡ Status de execução (2026-06-10) — pipeline carregada

Briefings imutáveis criados em `planejamentos/` (modo autônomo; decisões registradas no §11 de cada um). A investigação dos analistas corrigiu o estado real de vários itens — **ler a coluna de correção antes de executar**:

| Item | Briefing | Correção da investigação | Schema |
|---|---|---|---|
| 1.1 | `2026-06-10_001_pdf-receita-servidor.md` | Endpoint **já não é 501** (QuestPDF implementado); gap real = audit LGPD na exportação + Rascunho→422 + botão no front | não |
| 1.2 | `2026-06-10_002_pdf-termo-probatorio.md` | Stub 501 confirmado; novo `QuestPdfTermoService` + bloco de evidência do aceite | não |
| 1.3 | **FORA DA FILA ATUAL** → plano pronto em [`FASE_1B_ASSINATURA_DIGITAL_ICP.md`](FASE_1B_ASSINATURA_DIGITAL_ICP.md) | Aguardando respostas comerciais da Valid (e-mail 2026-06-10); executa quando confirmar | — |
| 1.4 | `2026-06-10_003_estoque-alertas-minimo-custo-medio.md` | Módulo está ~95% (não 70%): custo médio JÁ funciona; gap real = handler de alerta no-op + semântica de cruzamento | não |
| 1.5 | `2026-06-10_004_relatorios-export-csv.md` | São **5 abas (não 6, sem aba IA)**; datasets todos no front → CSV 100% frontend | não |
| 1.6 | `2026-06-10_005_orcamentos-fase6-configuracao.md` | Fase 6 do épico está CONCLUÍDA; gap real = sub-seções "Outras configurações" somente-leitura + placeholders | não |
| 1.7 | `2026-06-10_006_2fa-totp.md` | TOTP sem custo de serviço (confirmado); 21 CAs | **sim** → `imedto-database` |
| 1.8 | `2026-06-10_007_relatorio-acessos-lgpd-paciente.md` | Índices já existem; DB agent só confirma | não* |
| 1.9 | `2026-06-10_008_changelog-e-status-publicos.md` | Conteúdo em módulo TS versionado, zero backend | não |
| 1.10 | demanda técnica direta (sem briefing) | — | não |

**Ordem de execução recomendada**: 001 → 003 → 004 → 005 → 007 → 008 → 002 → 1.10 → 006 (2FA por último por ser o único com migration). Disparo: `"Execute o pipeline para o briefing planejamentos/2026-06-10_001_*.md"`.

## Itens

### 1.1 PDF de receita no servidor (destravar stub 501) — esforço P
- **Hoje**: `GET /api/receitas/{id}/pdf` retorna 501; QuestPDF já implementado no backend e jsPDF no front (padrão institucional do commit 5635619).
- **Escopo**: decidir a fonte única (recomendação: servidor/QuestPDF para documento oficial — consistente, auditável, não depende do browser; jsPDF permanece para visualização rápida se já em uso), integrar, registrar exportação no audit LGPD (endpoint de registrar-exportação já existe).
- **CAs-chave**: PDF gerado com identidade visual padronizada (`usePdfHeader` como referência); acesso filtra tenant; exportação auditada; receita cancelada marca o PDF.
- **Dependência**: nenhuma.

### 1.2 PDF de termo de consentimento on-the-fly (stub Fase 3 antiga) — esforço P-M
- **Hoje**: `GET /api/termos/{id}/pdf-gerado` 501; o aceite público funciona, mas o PDF final depende de upload manual.
- **Escopo**: gerar PDF do termo aceito (snapshot da versão + dados do aceite + trilha de auditoria do token) via QuestPDF.
- **CAs-chave**: PDF reproduz exatamente a versão aceita (versionamento já existe); hash/registro de aceite impresso no rodapé.

### 1.3 Assinatura digital ICP-Brasil ativada — esforço M (decisão + integração)
- **Status 2026-06-10**: **proposta comercial IntegraICP (V/Cert/Valid) nº 0350/2026 recebida em 2026-06-01** (contato: Cléo Santos, executiva Health). Termos: integração R$ 1.800 one-time; consumo **sem custo** via certificado em nuvem VIDaaS (contrato 12 meses); R$ 0,10/assinatura via certificados de outras ACs; chave de API em até 2 dias úteis após aceite; cronograma sugerido ~10 dias com homologação conjunta. Carimbo do tempo fora de escopo (alinhado à decisão AD_RB do discovery). Detalhes e perguntas pré-assinatura no discovery [`assinatura-digital-receitas/`](../Discoverys/assinatura-digital-receitas/01_discovery.md) §12.
- **Vantagem do IntegraICP**: é agregador multi-PSC — uma única integração cobre VIDaaS, BirdID e demais provedores (substitui/abstrai o stub BirdID atual). Doc API: developers.integraicp.com.br (fluxo: clearances por CPF → redirect ao provedor → autorização no app → credentialId → POST signatures com hashes SHA-256).
- **Gate**: decisão comercial do usuário (aceitar a proposta). Após aceite → briefing de integração (BA) + execução.
- **Escopo**: fluxo real de assinatura nos PDFs de receita (depende do 1.1) e atestado, via IntegraICP.
- **CAs-chave**: receita assinada verificável no validar.iti.gov.br; falha de assinatura degrada com mensagem clara (não bloqueia emissão simples); status visível na lista.
- **Por que nesta fase**: é "commodity obrigatória" (RDC 1.000/2025 para controlados) e o esforço restante é pequeno — destrava validade jurídica, argumento central do plano Consultório.

### 1.4 Estoque 70% → 100% — esforço M
- **Escopo**: alertas de estoque mínimo (notificação interna já existe como canal), custo médio na movimentação, tela de reposição sugerida. Cortar o que não for essencial: integração com pedido de compra fica fora (registrar como F4+).
- **CAs-chave**: alerta dispara ao cruzar mínimo; custo médio recalcula em entrada; multi-tenant nos novos endpoints.

### 1.5 Relatórios 85% → 100% — esforço P-M
- **Escopo**: exportação CSV das 6 abas existentes; pequenos drill-downs pendentes. BI avançado fica na F3 (não confundir os dois).
- **CAs-chave**: export respeita os filtros ativos; sem PII além do necessário (LGPD: relatório agregado não lista CPF).

### 1.6 Orçamentos Fase 6 (configuração de abas/painéis) — esforço M
- **Escopo**: concluir o que o plano interno "Fase 6" já definiu para `OrcamentoSettingsView` (catálogo + valores por profissional + configuração de abas).
- **CAs-chave**: flag `orcamento_completo` controla acesso; catálogo por tenant.

### 1.7 2FA (TOTP) — esforço M
- **Escopo**: TOTP opcional por usuário (QR + códigos de recuperação), obrigatório configurável por estabelecimento para papéis administrativos. SMS fica fora (custo/complexidade).
- **CAs-chave**: login com TOTP; recuperação segura; auditoria de ativação/desativação; sem lockout permanente.
- **Por quê**: segurança vendável para dado de saúde + item barato do hardening (auditoria §segurança).

### 1.8 Relatório de acessos LGPD (diferencial A3) — esforço P
- **Escopo**: tela no detalhe do paciente listando quem acessou o quê e quando (dados já em `paciente_acesso_log` e correlatos); exportável em PDF para resposta a titular.
- **CAs-chave**: acesso ao relatório também é auditado; só papéis autorizados; linguagem leiga no PDF.

### 1.9 Confiabilidade publicada (diferencial D2, parte 1) — esforço P
- **Escopo**: changelog público de produto (página simples alimentada por releases) + página de status básica (pode usar o monitor externo da F0-E1 quando existir; até lá, manual).
- **Nota**: publicar uptime real só depois da F0-E1 (não publicar números de infra frágil).

### 1.10 Lint tipográfico (estanca a sangria da regra §5) — esforço P
- **Escopo**: regra de lint (Stylelint/ESLint custom) que **falha o build** em `font-size:`/`font-weight:` literais em CSS scoped novo; baseline de exceções para as ~1.300 violações existentes (a redução do estoque é da [FASE_TRANSVERSAL](FASE_TRANSVERSAL_QUALIDADE.md)).
- **CAs-chave**: PR com literal novo falha no CI; baseline não cresce.

## Critérios de saída da fase

- [ ] `grep` por `501` nos controllers = zero stubs ativos.
- [ ] Receita de controlado emitida, assinada (ICP) e validada externamente.
- [ ] Demo completa (agenda → atendimento → receita assinada → documento no paciente → relatório) sem nenhum "isso ainda não está pronto".
- [ ] 2FA ativável; relatório de acessos entregável a um titular.
- [ ] Nenhum módulo do inventário abaixo de 100% exceto os explicitamente movidos para F3/F4 (BI avançado, automações-plataforma, mobile).

## Execução

Todos os itens passam pela pipeline com briefing (BA → dev → DB se schema → QA). Itens 1.3 e 1.7 mudam schema (certificados/segredos TOTP) → `imedto-database` obrigatório. Item 1.10 é demanda técnica direta (sem BA).
