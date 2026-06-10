# Diferenciais do Imedto — especificação e priorização

> Insumo de decisão do discovery [`01_discovery.md`](01_discovery.md), aprofundando o §7. Evidências de mercado em [`02_pesquisa_mercado.md`](02_pesquisa_mercado.md). Implementação fase a fase em [`../../Roadmap/`](../../Roadmap/).
>
> Critério de inclusão: ou **ninguém faz bem** (diferencial de produto), ou **todos fazem mal** (diferencial de execução), ou **o Imedto já tem base construída** que barateia fazer melhor.

---

## Grupo A — Diferenciais que JÁ EXISTEM no código (polir + comunicar; custo baixo, retorno imediato)

### A1. Profundidade cirúrgica (orçamento → cirurgia → ficha anestésica)
- **O que é**: fluxo completo que o Imedto já tem — catálogo de procedimentos, orçamento com equipe/anestesia, conversão em cirurgia planejada, ficha anestésica, equipe cirúrgica, realização.
- **Evidência**: os líderes tratam cirurgia como item genérico de agenda; nenhum dos 6 grandes tem ficha anestésica nem orçamento cirúrgico com equipe. Especialidades cirúrgicas (plástica, dermato, bucomaxilo) são mal servidas e têm ticket alto.
- **Como amplificar**: posicionamento "o sistema de quem opera"; templates por especialidade cirúrgica; landing própria do vertical.
- **Monetização**: âncora do plano topo (Clínica Cirúrgica). **Esforço**: P (é marketing + polimento). **Risco**: nenhum técnico; risco é não comunicar.

### A2. Multi-estabelecimento nativo do profissional
- **O que é**: médico que atende em 2-3 clínicas tem **uma conta**, com vínculos, papéis e permissões por estabelecimento (já implementado: vínculos + RBAC + convites).
- **Evidência**: nos concorrentes o profissional multi-clínica vive com logins separados; é reclamação recorrente e nenhum grande resolve bem (o modelo deles cobra por clínica isolada).
- **Como amplificar**: UX de troca rápida de contexto; visão "minha semana" agregando agendas dos vínculos (cuidado LGPD: mostrar só blocos ocupado/livre de outros tenants, nunca dados do paciente cross-tenant); comunicar na página de preços ("uma conta, todas as suas clínicas").
- **Monetização**: incluído — é argumento de aquisição viral (o médico que usa numa clínica puxa a outra). **Esforço**: P-M. **Risco**: vazamento cross-tenant na visão agregada — CAs de isolamento obrigatórios.

### A3. LGPD auditável como selo comercial
- **O que é**: o que já é arquitetura (audit trail de acesso a paciente/prontuário, minimização de DTO, anonimização, exportação de dados, mensagens genéricas) vira **prova vendável**: relatório de auditoria de acessos por paciente, página de privacidade comercial, resposta pronta a titular.
- **Evidência**: nos incumbentes LGPD é checklist de marketing; nenhum entrega "quem acessou o prontuário do meu paciente e quando" como relatório de 1 clique. Clínica é controladora e responde por isso — dor jurídica real.
- **Como amplificar**: tela "Relatório de acessos" no detalhe do paciente (dados já estão em `paciente_acesso_log`); seção comercial "Privacidade por arquitetura".
- **Monetização**: incluído em todos (confiança não se vende por tier). **Esforço**: P. **Risco**: nenhum.

### A4. Motor de automações como plataforma ("Zapier da clínica")
- **O que é**: o motor regras+eventos existente (gatilhos: novo paciente, agendamento criado, orçamento expirado...) evolui para matriz gatilho × condição × ação (e-mail, WhatsApp quando existir, tarefa, notificação) montável pelo dono sem código.
- **Evidência**: concorrentes têm automações fixas (lembrete on/off); regra configurável é raro fora de CRM caro.
- **Como amplificar**: biblioteca de receitas prontas ("no-show → reagendamento", "pós-consulta → pesquisa", "orçamento parado 7d → follow-up").
- **Monetização**: nº de regras ativas escala por plano (flag `automacoes_ilimitadas` já existe). **Esforço**: M (canais novos são o grosso). **Risco**: complexidade de UX — mitigar com receitas prontas em vez de builder livre.

### A5. Modelos de prontuário por especialidade (conteúdo como produto)
- **O que é**: a infra de modelos customizáveis + pool de variáveis (já existe, com admin global) vira **biblioteca curada por especialidade** — anamneses, exames dirigidos, evoluções padrão prontos para usar no dia 1.
- **Evidência**: "configurar o prontuário do zero" é fricção de onboarding em todos os concorrentes; conteúdo pronto reduz time-to-value.
- **Como amplificar**: 5-10 especialidades iniciais (alinhar com o nicho cirúrgico); seed no onboarding conforme a especialidade do profissional.
- **Monetização**: incluído (acelera ativação, reduz churn de trial). **Esforço**: M (é trabalho de conteúdo clínico, não de código — validar com profissionais reais). **Risco**: conteúdo clínico ruim queima credibilidade; revisar com médico parceiro.

---

## Grupo B — Relacionamento e receita da clínica (construir; alto impacto na decisão de compra)

### B1. Agente WhatsApp (além do lembrete)
- **O que é**: não só confirmar consulta (commodity), mas **conversar**: paciente responde "quero remarcar" e o agente oferece horários reais da agenda; responde preparo de exame e instruções pré/pós; escala para humano quando sai do script.
- **Evidência**: confirmação simples todos os grandes têm; agente que resolve reagendamento sozinho é raro (Carecode e similares vendem isso standalone, caro). No-show é a dor #1; reagendar na mesma conversa converte falta em consulta remarcada.
- **Base existente**: automações, disponibilidade de agenda (`/api/agendamentos/disponibilidade`), link público por token.
- **MVP → evolução**: MVP = confirmação 2 vias (sim/não/remarcar com 3 opções de slot); V2 = NLU para texto livre + preparo de exames; V3 = agendamento novo pelo WhatsApp.
- **Monetização**: franquia de conversas por plano (ver [`05_planos_e_pricing.md`](05_planos_e_pricing.md)). **Esforço**: M (MVP) / G (V2+). **Risco**: custo por conversa Meta + aprovação de templates — discovery [`whatsapp-envio/`](../whatsapp-envio/) crava provider e números.

### B2. Fila de espera que se preenche sozinha
- **O que é**: cancelou/faltou → o sistema oferece o slot vago automaticamente (WhatsApp) aos pacientes da lista de espera, por ordem/prioridade; primeiro que aceita, agenda — sem a secretária tocar no telefone.
- **Evidência**: lista de espera estática vários têm; **preenchimento automático do buraco** quase ninguém. ROI direto: cada slot recuperado é receita resgatada (R$200-500).
- **Base existente**: `listaEsperaService` **já existe no front**, eventos de cancelamento no domínio de Agendamentos, automações.
- **MVP**: oferta sequencial com expiração (15 min por paciente) + audit de quem recebeu/aceitou.
- **Monetização**: plano Clínica+. **Esforço**: M. **Risco**: corrida/duplo-booking — resolver com lock no slot (o domínio já valida conflito de horário).

### B3. No-show score (risco de falta por paciente)
- **O que é**: pontuação simples de risco por agendamento (histórico de faltas do paciente, antecedência da marcação, dia/horário, 1ª consulta vs retorno) que dispara ações: confirmação extra, pedido de sinal/pré-pagamento, sugestão de encaixe.
- **Evidência**: ninguém do mercado expõe isso de forma acionável; é "IA aplicada" com ROI mensurável e dados que o sistema já coleta.
- **Base existente**: histórico de agendamentos com status (confirmado/cancelado/no-show via check-in), automações para as ações.
- **MVP**: heurística transparente (sem ML) — score 0-100 com motivos visíveis; ML só depois com volume.
- **Monetização**: Clínica+. **Esforço**: P-M (heurística). **Risco**: falso rótulo "paciente faltoso" — mostrar como risco do agendamento, não etiqueta da pessoa; cuidado LGPD em como exibir.

### B4. Pré-consulta digital (anamnese preenchida pelo paciente)
- **O que é**: antes da consulta, o paciente recebe link (token público — infra já existe para termos/confirmação) e preenche queixa, histórico, medicamentos em uso, alergias + assina termos pendentes; tudo cai como rascunho estruturado na evolução.
- **Evidência**: reduz a dor #4 (tempo de documentação) sem IA; combina com IA scribe depois (o médico só completa). Players gringos (Phreesia) provam o valor; no BR é raro e mal feito.
- **Base existente**: links públicos por token com audit, modelos de prontuário (o formulário É um modelo), termos de consentimento.
- **MVP**: formulário derivado do modelo de prontuário da especialidade + alergias/medicamentos; resposta vira seção pré-preenchida da evolução (sempre revisável).
- **Monetização**: Clínica+. **Esforço**: M. **Risco**: adesão do paciente — enviar junto do lembrete de 24h (carona no WhatsApp).

### B5. CRM de orçamentos (funil de vendas clínico)
- **O que é**: para clínica cirúrgica/estética, orçamento é venda: funil enviado → visualizado → aprovado/recusado, follow-up automático de orçamento parado, motivo de recusa, taxa de conversão por procedimento e por profissional.
- **Evidência**: Clinicorp tem CRM (odonto); no médico-cirúrgico é raro. O gestor decide a compra por isso.
- **Base existente**: orçamentos com máquina de estados completa (Rascunho→Enviado→Aprovado→...), envio por e-mail, automação de expirar, relatório de orçamentos.
- **MVP**: tracking de visualização (pixel/link), kanban do funil, 1 automação de follow-up, motivo de recusa.
- **Monetização**: âncora do plano Cirúrgica. **Esforço**: M. **Risco**: baixo — é evolução de agregado existente.

---

## Grupo C — Clínico e nicho (construir; diferenciais de médio prazo)

### C1. IA scribe nativa PT-BR (a aposta principal)
- **O que é**: gravação da consulta (com consentimento do paciente via módulo de termos existente) → transcrição → evolução estruturada **no modelo de prontuário do profissional**, sempre como rascunho a revisar; sugestão de CID e conduta como apoio, nunca auto-commit.
- **Evidência**: dor #4 (metade do tempo de consulta em documentação); mercado vende como add-on caro (Doctoralia Noa Notes +R$199/mês) ou app separado (DoctorAssistant, Voa, Iara); embutido e incluído no plano ainda não existe nos grandes. Janela: 12-24 meses.
- **Base existente**: `IaController` + `EstabelecimentoIaSettings` (por tenant, com nível de minimização!), `ai_rate_limits`, `ai_audit_logs` com hash — a governança que os concorrentes não têm já está pronta.
- **MVP → evolução**: MVP = upload de áudio pós-consulta → rascunho de evolução (STT API + LLM estruturando no template); V2 = gravação ao vivo no navegador; V3 = sugestão de CID/prescrição a partir da transcrição.
- **Monetização**: franquia de consultas/mês em **todos** os planos (mensagem "IA nativa para todos", vs add-on dos concorrentes) — escalonada por tier.
- **Esforço**: G. **Risco**: custo por consulta (~R$0,80-1,10 estimado: STT ~R$0,65/20min + LLM) — franquias conservadoras no início; responsabilidade clínica — rascunho obrigatório + termo de consentimento; LGPD do áudio — reter só a transcrição, descartar áudio após processar (configurável). **Exige discovery próprio antes do briefing.**

### C2. Pós-operatório estruturado (matador no nicho cirúrgico)
- **O que é**: protocolos de acompanhamento por procedimento (D+1, D+7, D+30...): check-in automático via WhatsApp (escala de dor, foto da cicatriz), alertas de resposta preocupante para o profissional, registro automático no prontuário.
- **Evidência**: acompanhamento pós-op hoje é WhatsApp pessoal do cirurgião — desorganizado, sem registro, sem escala. Nenhum concorrente estrutura isso. Para plástica/dermato é argumento de compra sozinho (segurança do paciente + medicina defensiva documentada).
- **Base existente**: cirurgias com data de realização (gatilho), automações, termos, anexos de prontuário (fotos), WhatsApp (B1).
- **MVP**: 1 protocolo configurável por procedimento + check-in D+1/D+7 com escala de dor + alerta.
- **Monetização**: plano Cirúrgica. **Esforço**: M-G (depende de B1 pronto). **Risco**: resposta clínica urgente fora de hora — deixar claro que não é pronto-socorro (disclaimer + orientação de emergência).

### C3. Galeria clínica antes/depois com consentimento auditado
- **O que é**: fotos padronizadas por região anatômica (catálogo de regiões **já existe**), comparação lado a lado por data, consentimento específico por uso (prontuário vs divulgação) via termo versionado, marca d'água, exportação autorizada para portfólio.
- **Evidência**: clínicas de estética vivem de antes/depois e hoje guardam em celular/Drive — risco LGPD gigantesco e desorganização. Concorrentes genéricos não têm; apps de foto não têm consentimento jurídico.
- **Base existente**: anexos S3 com presigned URL, termos versionados com aceite auditado, regiões anatômicas, audit de acesso.
- **MVP**: captura/upload guiado por região + comparador 2 datas + termo de uso de imagem vinculado.
- **Monetização**: plano Cirúrgica. **Esforço**: M. **Risco**: armazenamento (fotos pesam — lifecycle S3 e limite por plano via flag `anexos_ilimitados`).

---

## Grupo D — Confiança e posicionamento (baratos, ninguém faz)

### D1. Anti-lock-in operacionalizado ("seus dados saem com você")
- **O que é**: exportação completa self-service (já existe via LGPD — expor melhor), **central de migração de entrada** (importadores dos concorrentes — plano próprio em [`../../Roadmap/FASE_2B_CENTRAL_DE_MIGRACAO.md`](../../Roadmap/FASE_2B_CENTRAL_DE_MIGRACAO.md)) e cancelamento self-service sem retenção forçada.
- **Evidência**: dor #2 do mercado (migração quebrada, "cancelar é impossível"). Transformar a maior reclamação contra os líderes em promessa central de marca.
- **Monetização**: incluído em todos — é aquisição, não receita. **Esforço**: P (export/cancel) + M-G (importadores). **Risco**: nenhum — o churn que isso "facilita" aconteceria de qualquer forma, só que com ódio.

### D2. Confiabilidade publicada
- **O que é**: status page pública (uptime do `/health` — o monitor externo da Fase 0 já alimenta), changelog ativo de produto, compromisso de suporte com tempo de resposta declarado.
- **Evidência**: dor #3 (instabilidade) e #2 (suporte) — nenhum concorrente pequeno publica uptime; os grandes não podem (têm esqueletos).
- **Monetização**: incluído. **Esforço**: P. **Risco**: exige fazer a Fase 0 antes (não publicar uptime de infra frágil).

### D3. Preço transparente "tudo incluído"
- **O que é**: a estratégia completa está em [`05_planos_e_pricing.md`](05_planos_e_pricing.md) — sem add-on surpresa, franquias visíveis com medidor no app, excedente só com opt-in.
- **Evidência**: dor #7 (Amplimed modular que triplica; iClinic sem preço público).
- **Esforço**: P (é decisão, não código — flags já existem). 

---

## Matriz de priorização

| # | Diferencial | Impacto aquisição | Impacto retenção | Esforço | Fase do roadmap | Plano onde entra |
|---|---|---|---|---|---|---|
| A3 | LGPD auditável (relatório de acessos) | ▲▲ | ▲▲ | P | F1 | Todos |
| D2 | Confiabilidade publicada | ▲▲ | ▲ | P | F1 (pós-F0) | Todos |
| A1 | Vertical cirúrgico (posicionamento) | ▲▲▲ | ▲▲ | P | F1-F2 | Cirúrgica |
| D3 | Preço transparente | ▲▲▲ | ▲▲ | P | F2 (lançamento) | — |
| B1 | Agente WhatsApp | ▲▲▲ | ▲▲▲ | M | F2 | Franquia/tier |
| D1 | Anti-lock-in + central de migração | ▲▲▲ | ▲ | M-G | F2 | Todos |
| A5 | Modelos por especialidade | ▲▲ | ▲▲ | M | F2 | Todos |
| B2 | Fila que se preenche sozinha | ▲▲ | ▲▲▲ | M | F2-F3 | Clínica+ |
| B5 | CRM de orçamentos | ▲▲ | ▲▲▲ | M | F3 | Cirúrgica |
| B4 | Pré-consulta digital | ▲▲ | ▲▲ | M | F3 | Clínica+ |
| B3 | No-show score | ▲ | ▲▲ | P-M | F3 | Clínica+ |
| C1 | IA scribe PT-BR | ▲▲▲ | ▲▲▲ | G | F3 (discovery antes) | Franquia em todos |
| C3 | Galeria antes/depois | ▲▲ | ▲▲▲ | M | F3 | Cirúrgica |
| C2 | Pós-operatório estruturado | ▲▲ | ▲▲▲ | M-G | F3-F4 (depende B1) | Cirúrgica |
| A2 | Multi-estabelecimento (visão agregada) | ▲▲ | ▲▲ | P-M | F3 | Todos |
| A4 | Automações plataforma | ▲ | ▲▲ | M | F3-F4 | Por tier |

**Leitura**: o caminho de menor esforço/maior retorno imediato é A3+D2+A1+D3 (tudo P, viabiliza o lançamento com identidade). B1+D1 são o motor de aquisição da F2. C1 é a aposta que precisa começar discovery cedo para chegar na F3.
