# FASE 3 — Diferenciação (o que faz o Imedto ser escolhido, não só aceito)

> Parte do roadmap [`README.md`](README.md). **Objetivo**: construir os diferenciais que sustentam o posicionamento e o preço — IA scribe nativa, profundidade no vertical cirúrgico e a camada de gestão que conquista o decisor. Especificação completa de cada diferencial em [`04_diferenciais.md`](../Discoverys/roadmap-melhorias-2026/04_diferenciais.md).
>
> **Duração estimada**: 1-2 trimestres (paralelizável com o fim da F2). **Pré-requisitos**: F2 (WhatsApp é dependência de vários itens; medidores de consumo prontos).

## Itens

### 3.1 IA scribe PT-BR (diferencial C1 — a aposta) — esforço G
- **Por quê**: maior diferencial ainda disponível no mercado (janela 12-24 meses); ataca a dor #4 (metade da consulta gasta documentando); concorrentes cobram R$199/mês como add-on — o Imedto inclui com franquia em todos os planos.
- **Base existente**: `IaController`, `EstabelecimentoIaSettings` (nível de minimização por tenant!), `ai_rate_limits`, `ai_audit_logs` com hash, modelos de prontuário estruturados (o destino da transcrição).
- **⚠️ Discovery obrigatório antes do briefing** (criar `Docs/Discoverys/ia-scribe/`): STT para PT-BR médico (Whisper API vs alternativas; custo/min real), LLM de estruturação (modelo/custo/prompt), consentimento do paciente (reusar módulo de termos), retenção do áudio (recomendação: descartar após transcrever, configurável), responsabilidade clínica e validação com 2-3 médicos reais.
- **Escopo MVP**: upload de áudio pós-consulta → transcrição → rascunho de evolução estruturado **no modelo do profissional** → revisão obrigatória antes de salvar (nunca auto-commit) → audit completo.
- **Evolução**: V2 gravação ao vivo no navegador; V3 sugestão de CID/conduta.
- **CAs-chave**: consentimento registrado antes de gravar; rascunho nunca vira evolução sem ação explícita do profissional; franquia/medidor (F2.4) aplicados; custo por consulta instrumentado por tenant; áudio não persiste além do processamento (default).

### 3.2 Financeiro completo + NFS-e — esforço M-G
- **Por quê**: dor #6 (gestão em planilha paralela); o decisor-gestor compra por isso; NFS-e é commodity esperada.
- **Base existente**: lançamentos/categorias/formas de pagamento/resumo; relatório financeiro; discovery [`nota-fiscal/`](../Discoverys/nota-fiscal/) com landscape de gateways já mapeado.
- **Escopo**: (a) contas a receber por atendimento (lançamento nasce do agendamento/procedimento realizado — liga agenda→financeiro); (b) inadimplência: vencidos, régua de cobrança via automações (e-mail/WhatsApp); (c) repasse por profissional (comissão % ou fixo — clínica multi-prof precisa); (d) NFS-e via gateway escolhido no discovery (emissão por atendimento/fatura, retry de falha).
- **CAs-chave**: lançamento gerado do atendimento não duplica; repasse calculado certo em edge cases (cancelamento/estorno); NFS-e idempotente; nada de PII desnecessária no payload da nota.

### 3.3 BI gerencial (evolução das 6 abas) — esforço M
- **Escopo**: taxa de no-show por profissional/dia/horário (prova o ROI do WhatsApp da F2!), ocupação de agenda (slots usados/disponíveis), produtividade e receita por profissional, funil de orçamentos (liga com 3.4), comparativo mês a mês. Export já existe (F1.5).
- **CAs-chave**: queries agregadas paginadas/limitadas (performance — padrão UNION já estabelecido); sem dado clínico em relatório gerencial (LGPD); cache leve (dashboard não recalcula a cada clique).

### 3.4 CRM de orçamentos (diferencial B5 — vertical cirúrgico) — esforço M
- **Base existente**: máquina de estados completa de orçamentos, envio por e-mail, automação de expiração, relatório.
- **Escopo MVP**: tracking de visualização do orçamento enviado, kanban do funil (enviado→visto→aprovado/recusado), follow-up automático de orçamento parado X dias (via automações+WhatsApp), motivo de recusa, conversão por procedimento/profissional (alimenta 3.3).
- **CAs-chave**: tracking sem expor dados de outro tenant; follow-up respeita opt-out; estados novos não quebram a máquina existente.

### 3.5 Galeria clínica antes/depois com consentimento (diferencial C3) — esforço M
- **Base existente**: anexos S3 presigned, catálogo de regiões anatômicas, termos versionados com aceite auditado, audit de acesso.
- **Escopo MVP**: captura/upload guiado por região anatômica + sessão fotográfica datada, comparador lado a lado entre datas, termo de uso de imagem específico (prontuário vs divulgação) vinculado a cada foto, marca d'água na exportação autorizada.
- **CAs-chave**: foto sem termo de divulgação **nunca** exportável; acesso à galeria auditado (mesmo padrão de alertas clínicos: conteúdo só no detalhe com audit); limite de storage por plano.

### 3.6 Fila de espera que se preenche sozinha (diferencial B2) — esforço M
- **Base existente**: `listaEsperaService` (front), eventos de cancelamento no domínio, WhatsApp (F2.1), disponibilidade.
- **Escopo MVP**: cancelamento/no-show dispara oferta sequencial do slot aos pacientes da lista (WhatsApp, expiração de 15 min por oferta, ordem configurável); aceite agenda automaticamente; trilha de quem recebeu/aceitou.
- **CAs-chave**: lock no slot durante oferta (anti double-booking — EXCLUDE GiST é a rede final); paciente sem WhatsApp cai para e-mail/fila manual.

### 3.7 No-show score (diferencial B3) — esforço P-M
- **Escopo MVP**: score heurístico transparente por agendamento (histórico de faltas, antecedência, 1ª consulta vs retorno, dia/horário) com motivos visíveis; ações sugeridas (confirmação extra, política de sinal); **sem ML** até ter volume.
- **CAs-chave**: exibido como risco do agendamento (nunca etiqueta pejorativa do paciente); cálculo barato (sem query pesada na agenda).

### 3.8 Pré-consulta digital (diferencial B4) — esforço M
- **Base existente**: links públicos por token com audit, modelos de prontuário (o formulário é derivado do modelo), termos.
- **Escopo MVP**: link enviado junto do lembrete T-24h (carona no WhatsApp): paciente preenche queixa/histórico/medicamentos/alergias + assina termos pendentes; resposta vira seção pré-preenchida (rascunho) da evolução.
- **CAs-chave**: token de uso único com expiração; dado do paciente entra como rascunho revisável; minimização (só campos do modelo).

### 3.9 Visão agregada multi-estabelecimento do profissional (diferencial A2) — esforço P-M
- **Escopo**: "minha semana" agregando agendas de todos os vínculos do profissional logado.
- **CAs-chave (crítico)**: dados de paciente **só** do tenant ativo; dos demais vínculos, apenas blocos ocupado/livre sem qualquer PII cross-tenant; trocar de contexto continua explícito.

## Critérios de saída da fase

- [ ] ≥30% das evoluções dos pilotos geradas via IA scribe (com revisão) e custo/consulta dentro do projetado.
- [ ] Funil de orçamentos em uso com follow-up automático disparando.
- [ ] Lançamento financeiro nascendo do atendimento + primeira NFS-e emitida em produção.
- [ ] Dashboard de no-show/ocupação usado na conversa de renovação dos pilotos (prova de valor).
- [ ] Tier "Clínica Cirúrgica" vendável (3.4 + 3.5 + módulo cirúrgico existente).

## Execução

3.1 e o gateway NFS-e (3.2d) exigem discovery antes do briefing. Todos os demais vão direto à pipeline. Schema novo em praticamente todos → `imedto-database` presente. Ordem sugerida dentro da fase: 3.3+3.7 (baratos, dados já existem) → 3.4+3.6 → 3.2 → 3.1 (a aposta, começa o discovery no início da fase) → 3.5+3.8+3.9.
