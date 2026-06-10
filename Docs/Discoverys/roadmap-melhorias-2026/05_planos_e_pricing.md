# Planos e Pricing — estratégia de monetização

> Insumo de decisão do discovery [`01_discovery.md §10`](01_discovery.md). Benchmarks de preço em [`02_pesquisa_mercado.md §1 e §5`](02_pesquisa_mercado.md). Diferenciais que sustentam o preço em [`04_diferenciais.md`](04_diferenciais.md).
>
> A infraestrutura técnica de cobrança **já existe**: assinatura por estabelecimento, planos com feature flags JSON, trial com expiração por job, bloqueio 402, admin global para gerenciar planos. Implementar esta estratégia é configuração + flags novas, não construção.

---

## 1. Princípios (decididos pela pesquisa de dores)

1. **Previsibilidade contra a dor #7**: o mercado infla preço com add-ons (Amplimed) ou esconde preço (iClinic). Posição do Imedto: **preço público, tudo do tier incluído, sem surpresa**. Excedente de franquia nunca cobra sozinho — medidor visível + opt-in explícito.
2. **Não brigar no piso (R$75-99)** — lá a margem não paga suporte bom. Brigar por **valor por real**: IA nativa e WhatsApp inclusos onde os outros cobram add-on.
3. **Segurança e dados NUNCA são paywall**: 2FA, audit/LGPD, exportação de dados, backup, nº de pacientes — ilimitados em todos os planos. (Além de ético, é o diferencial D1/anti-lock-in.)
4. **Cobrar onde o valor aparece**: por profissional que atende (gera receita com a agenda). Secretária/recepção: **grátis e ilimitada** (padrão de mercado — e vale comunicar).
5. **Custo variável com franquia honesta**: WhatsApp e IA têm custo unitário real → franquias generosas por tier + pacotes adicionais com preço público.

## 2. Personas × disposição a pagar

| Persona | Quem decide | O que pesa na decisão | Referência de preço que já paga |
|---|---|---|---|
| **Consultório solo** (médico autônomo, 0-1 secretária) | o próprio médico | prontuário rápido, receita/atestado válidos, preço, simplicidade | R$75-110 (ProDoctor, HiDoctor, Ninsaúde) |
| **Clínica pequena** (2-10 profissionais, dono-gestor) | dono/administrador | no-show, agenda cheia, financeiro, controle da equipe | R$89-149/prof (Amplimed, Feegow) |
| **Clínica cirúrgica/estética** (nicho-alvo) | dono cirurgião | orçamentos/conversão, pós-op, fotos, imagem profissional | R$150-300/prof + ferramentas avulsas (CRM, foto, WhatsApp pago à parte) |

## 3. Estrutura recomendada — 3 planos

> Preços de lançamento a validar com pilotos (§8). Cobrança por **profissional ativo/mês**, por estabelecimento. Anual = 2 meses grátis (~-17%).

| | **Consultório** | **Clínica** | **Clínica Cirúrgica** |
|---|---|---|---|
| **Preço mensal** | **R$ 99**/prof | **R$ 149**/prof | **R$ 199**/prof |
| **Anual (equivalente/mês)** | R$ 82,50 | R$ 124 | R$ 166 |
| **Para quem** | autônomo/solo | clínica multi-prof | vertical cirúrgico/estético |
| **Mensagem** | "consultório completo e válido juridicamente" | "agenda cheia e gestão sem planilha" | "o sistema de quem opera" |

### Distribuição de funcionalidades

✅ incluído · — não incluído · (Fx) = fase do roadmap em que a feature nasce

| Capacidade | Consultório | Clínica | Cirúrgica |
|---|---|---|---|
| Agenda + check-in + salas | ✅ | ✅ | ✅ |
| Pacientes + prontuário completo (modelos, exame físico, anexos) | ✅ | ✅ | ✅ |
| Receitas/atestados/pedidos com PDF + **assinatura ICP-Brasil** (F1) | ✅ | ✅ | ✅ |
| Termos de consentimento (aceite público) | ✅ | ✅ | ✅ |
| Documentos do paciente (aba consolidada) | ✅ | ✅ | ✅ |
| Financeiro (lançamentos, categorias, resumo) | ✅ | ✅ | ✅ |
| **Central de migração / importadores** (F2) + exportação total | ✅ | ✅ | ✅ |
| 2FA, audit LGPD, relatório de acessos (F1) | ✅ | ✅ | ✅ |
| Modelos de prontuário por especialidade (F2) | ✅ | ✅ | ✅ |
| **IA scribe** (F3) — franquia consultas/prof/mês | 10 | 30 | 60 |
| **WhatsApp confirmação/lembrete** (F2) — franquia confirmações/prof/mês | 50 | 150 | 300 |
| Agente WhatsApp 2 vias (remarcar pelo chat) (F2) | — | ✅ | ✅ |
| **Agendamento online público** (F2) | — | ✅ | ✅ |
| Fila de espera inteligente (F2-F3) | — | ✅ | ✅ |
| No-show score (F3) | — | ✅ | ✅ |
| Pré-consulta digital (F3) | — | ✅ | ✅ |
| Relatórios/BI gerencial avançado (F3) | básico | ✅ | ✅ |
| Estoque | — | ✅ | ✅ |
| Multi-unidade + salas avançado | — | ✅ | ✅ |
| Papéis de permissão customizados (RBAC pleno) | básico | ✅ | ✅ |
| Automações | 3 regras | ilimitadas | ilimitadas |
| Telemedicina (F4) | — | ✅ | ✅ |
| **Módulo cirúrgico completo**: orçamento cirúrgico + equipe + ficha anestésica + conversão | — | — | ✅ |
| CRM de orçamentos (funil/follow-up) (F3) | — | — | ✅ |
| Pós-operatório estruturado (F3-F4) | — | — | ✅ |
| Galeria antes/depois com consentimento (F3) | — | — | ✅ |
| API pública / integrações (F4) | — | — | ✅ |
| Armazenamento de anexos | 5 GB | 20 GB | ilimitado* |
| Suporte | e-mail/chat | chat prioritário | onboarding dedicado + prioridade |

*\*ilimitado com fair use; fotos da galeria com lifecycle S3.*

**Racional das quebras**: o Consultório resolve 100% do clínico (nada de mutilar prontuário — isso gera ódio e churn); a camada de **crescimento do negócio** (WhatsApp 2 vias, agendamento online, BI, equipe plena) é o que separa Clínica; o **vertical cirúrgico** inteiro ancora o topo. Upgrade acontece por evolução natural do cliente (contratou secretária → Clínica; opera → Cirúrgica), não por dor artificial.

## 4. Franquias e medidores (custo variável sob controle)

| Recurso | Custo unitário estimado | Franquia C/Cl/Cir | Custo máx/prof no teto | Pacote adicional (opt-in) |
|---|---|---|---|---|
| Confirmação WhatsApp (template utility) | ~R$0,05-0,15* | 50/150/300 | R$7,50-R$45 | +100 por R$19 |
| Conversa de agente (janela 24h) | ~R$0,35-0,50* | dentro da franquia 2:1 | — | idem |
| IA scribe (STT + estruturação) | ~R$0,80-1,10/consulta | 10/30/60 | R$11-R$66 | +10 por R$15 |

*\*Números do WhatsApp dependem do modelo de cobrança Meta vigente (por template vs janela) e do provider — o discovery [`whatsapp-envio/`](../whatsapp-envio/) crava. Calibrar franquias no piloto: meta = custo variável ≤25% do preço do plano no uso típico (não no teto).*

Regras de medidor (anti-dor #7): consumo visível no app em tempo real · alerta em 80% · estourou = recurso pausa e oferece pacote/upgrade — **nunca cobrança automática surpresa** · precedente técnico já existe (`ai_rate_limits`).

## 5. Modelo de cobrança

- **Unidade**: profissional de saúde **ativo** (com agenda/atendimento no mês) por estabelecimento. Secretárias/recepção ilimitadas. Mínimo 1 prof.
- **Multi-estabelecimento**: cada estabelecimento assina o seu plano (modelo já implementado — assinatura por tenant). O profissional vinculado a 2 clínicas conta em cada uma (cada negócio paga pelo valor que extrai).
- **Trial**: 30 dias do plano **Cirúrgica** completo, sem cartão. Conversão guiada: ao fim, escolhe tier (downgrade automático suave para read-only de 7 dias se não decidir — nunca sequestro de dados).
- **Anual**: 2 meses grátis + migração assistida humana incluída (no mensal, a migração assistida custa R$299 one-time — a self-service da central é grátis sempre).
- **Oferta fundadora** (10 primeiros estabelecimentos pagantes): -50% por 12 meses + preço travado por 24 meses + canal direto com o fundador. Em troca: caso de sucesso + 2 calls de feedback/mês.

## 6. Ancoragem de valor (para a página de preços)

- **1 no-show evitado paga o mês**: consulta média R$250-500 vs plano R$149. A franquia de WhatsApp do plano Clínica cobre ~7 agendamentos/dia úteis.
- **IA scribe**: 30 consultas/mês transcritas ≈ 5-10h de digitação devolvidas ao médico — o concorrente cobra R$199/mês só por isso.
- **Comparação honesta**: Doctoralia Starter+Noa = R$628/prof; Imedto Cirúrgica = R$199 com pós-op e galeria que eles não têm (sem o marketplace deles — integrar, não competir).

## 7. Implementação técnica (mapa de flags)

| Flag existente | Plano(s) | Flag nova necessária | Plano(s) |
|---|---|---|---|
| `receitas` | todos | `whatsapp` (+ franquia) | todos (franquia escalonada) |
| `exame_fisico` | todos | `whatsapp_agente` | Clínica+ |
| `procedimentos_cirurgicos` | Cirúrgica | `agendamento_online` | Clínica+ |
| `orcamento_completo` | Cirúrgica | `ia_scribe` (+ franquia) | todos (franquia) |
| `ia` (sugestões) | todos | `fila_espera_auto` | Clínica+ |
| `relatorios_avancados` | Clínica+ | `pre_consulta` | Clínica+ |
| `automacoes_ilimitadas` | Clínica+ | `pos_operatorio`, `galeria_clinica` | Cirúrgica |
| `anexos_ilimitados` | Cirúrgica | `telemedicina`, `api_publica` | Clínica+ / Cirúrgica |

Medidores de consumo: generalizar o padrão de `ai_rate_limits` para `consumo_recursos` (tenant, recurso, período, usado, franquia) — uma demanda própria na F2.

## 8. Validação antes de cravar

1. **5-10 pilotos** (oferta fundadora) com mix consultório/clínica/cirúrgica; medir ativação (1º agendamento, 1ª receita), uso de WhatsApp/IA vs franquia, e churn de trial.
2. **Pesquisa de disposição a pagar** (Van Westendorp simplificado: "barato demais / caro demais / caro mas aceitável") nas entrevistas de piloto.
3. Revisar preço/franquia a cada 6 meses no 1º ano (o `02_pesquisa_mercado.md` envelhece rápido).
4. **Não lançar preço público antes da F1 completa** (zero stubs 501) — preço público com produto incompleto queima a credibilidade que sustenta o posicionamento.

## 9. Riscos

| Risco | Mitigação |
|---|---|
| Custo WhatsApp/IA mal calibrado corrói margem | Franquias conservadoras no lançamento + telemetria de consumo por tenant desde o dia 1; reajustar franquia (não preço) |
| Preço 30% acima do piso afasta o solo | O Consultório a R$99 está NO piso com mais valor (ICP + IA 10/mês inclusos); ancorar comparação |
| Tier Cirúrgica esperar features F3 para justificar R$199 | Lançar Cirúrgica quando módulo cirúrgico + CRM básico existirem; até lá, vender Clínica |
| Downgrade/cancelamento mal desenhado vira reclamação | Self-service total + read-only de cortesia + exportação sempre — é o diferencial D1, tratar como feature |
