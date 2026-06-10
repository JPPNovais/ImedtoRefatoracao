# FASE 2B — Central de Migração ("Mude para o Imedto em 1 dia")

> Parte do roadmap [`README.md`](README.md). **Objetivo**: importar os dados do cliente vindo de outro sistema com o **menor número de cliques possível**, transformando a dor #2 do mercado (migração quebrada + lock-in) em arma de aquisição — diferencial D1 ([`04_diferenciais.md`](../Discoverys/roadmap-melhorias-2026/04_diferenciais.md)).
>
> Pesquisa de exports dos concorrentes (2026-06-10, fontes citadas inline): help centers oficiais + Reclame Aqui. **Caveat**: estruturas de arquivo mudam sem aviso — antes de codificar cada parser, obter exemplos reais do export (clínica piloto vinda daquele sistema).
>
> **Pré-requisitos**: F1 (produto completo para receber os dados). **Duração estimada**: MVP 3-4 semanas; +1-2 semanas por adaptador dedicado.

---

## 1. O argumento que ninguém usa (posicionamento)

- **CFM 1.821/2007**: o prontuário é do paciente; a clínica é fiel depositária — ao trocar de sistema, ela tem o direito (e o dever) de levar os registros. O sistema antigo não pode reter dados como refém.
- **LGPD Art. 18**: portabilidade gratuita mediante requisição; prazo razoável (~30 dias na prática).
- **Gap de mercado confirmado pela pesquisa**: nenhum concorrente posiciona "portabilidade garantida por lei" comercialmente. O Imedto pode entregar junto da central um **kit de saída**: modelo de carta de solicitação de dados com base legal (CFM+LGPD) para o cliente enviar ao sistema antigo quando o export self-service falhar — vira conteúdo de marketing e desbloqueador prático.

## 2. Realidade dos exports (resumo da pesquisa)

| Sistema | Export self-service | Formato | Dificuldade de sair (1-5) | Adaptador dedicado? |
|---|---|---|---|---|
| **iClinic** | pacientes, prontuário, agenda, financeiro (Config → Exportar Dados; chega por e-mail) | CSV/XLS | 3 (existe, mas instável) | ✅ **#1 — maior base (22k médicos), escopo completo** |
| **Simples Dental** | cadastro self-service; backup completo (16 planilhas) via suporte em ≤8 dias | Excel/ZIP | 2 (processo claro) | ✅ #2 — se atender odonto (100k dentistas) |
| **Feegow** | relatórios de agenda/atendimentos | Excel/CSV/ZIP | 4 (cancelamento travado) | ✅ #3 — cliente frustrado = lead quente |
| **Clinicorp** | 5 planilhas nomeadas (Patient.xlsx, Appointment.xlsx, TreatmentOperation.xlsx, PatientAnamnesis.xlsx, Dentist.xlsx) | XLSX | 4 | ✅ #4 — estrutura mais previsível do mercado |
| **Ninsaúde Apolo** | relatórios; estrutura de import deles é pública (CSV `;`, ISO-8859-1/UTF-8) | CSV/Excel | 3 | ✅ #5 — formato documentado publicamente |
| ProDoctor | pacientes, agenda, procedimentos | CSV | 3 (desktop, banco local) | genérico |
| HiDoctor | relatórios em XLS | XLS | **5 (relatos de retenção de prontuário)** | genérico + kit de saída |
| Amplimed | nada self-service documentado (CSV mediante solicitação) | CSV | 4 | genérico + kit de saída |
| Doctoralia | PDF de relatórios; prontuário com reclamação de export | PDF/CSV | 4 | genérico |
| GestãoDS | pacientes por filtro; CSV garantido no cancelamento | CSV/XLSX | 2 | genérico |

**Benchmark de prazo dos concorrentes que importam**: iClinic 5+18 dias úteis; Clinicorp 5-12 dias; Simples Dental 3 dias. **Meta do Imedto: self-service em minutos; assistida em ≤2 dias úteis** — só isso já é mensagem de marketing.

## 3. Arquitetura do importador (genérico + adaptadores)

Pipeline única, adaptadores por origem:

```
Upload (CSV/XLSX/ZIP, até ~50MB)
  → Detecção de origem (fingerprint: nomes de colunas/arquivos; ex.: Patient.xlsx+Dentist.xlsx ⇒ Clinicorp)
  → Parsing (adaptador da origem OU genérico com mapeador manual)
  → Normalização para o MODELO CANÔNICO DE IMPORT (paciente, agendamento, evolução, documento, lançamento)
  → Validação + dedupe (CPF/nome+nascimento) → relatório de preview linha a linha
  → Mapeamento assistido de campos (UI "de-para" pré-preenchida pelo adaptador; usuário só confirma)
  → Importação assíncrona em lotes (fila — sinergia com F0-E2.3 SQS/Lambda) com idempotência
  → Relatório final (importados / pulados / erros com motivo) + desfazer
```

**Decisões estruturais:**
- **Staging tables** (`import_jobs`, `import_rows` com status/erro/payload bruto) — multi-tenant, auditáveis; nada entra direto nas tabelas de domínio.
- **Importação roda pelos commands existentes** (CriarPaciente etc.) — reusa validação/regra de negócio/audit; nunca INSERT direto (premissa: regra no backend).
- **Idempotência**: chave natural por linha (origem+id externo ou CPF+nascimento); reimportar o mesmo arquivo não duplica.
- **Desfazer**: tudo que um job criou é marcado com `import_job_id` → rollback em 1 clique enquanto o job for o único autor do registro.
- **Prontuário legado sem estrutura** (PDF/relatório): honestidade técnica — entra como **anexo/documento histórico** pesquisável no paciente, não como evolução estruturada falsa. Evolução estruturada só quando a origem fornece campos (ex.: Ninsaúde, Clinicorp `PatientAnamnesis`).
- **LGPD**: arquivo bruto criptografado, retido por 30 dias e apagado (job); termo de responsabilidade do cliente sobre a licitude dos dados; importação inteira no audit trail.

## 4. Ondas de escopo

| Onda | O que importa | Por quê |
|---|---|---|
| **W1 (MVP)** | **Pacientes** via genérico CSV/XLSX com mapeador de campos + template público do Imedto | universal — todo sistema exporta pacientes; destrava qualquer trial |
| **W2** | Adaptador **iClinic** (pacientes+agenda+financeiro+prontuário-como-documento) + dedupe robusto + desfazer | maior base migrável do BR |
| **W3** | Agendamentos futuros + histórico de consultas (genérico + adaptadores) | agenda é o 2º dado mais pedido |
| **W4** | Adaptadores **Feegow** e **Ninsaúde**; evoluções estruturadas onde a origem permite | cobre os líderes de gestão |
| **W5** | **Simples Dental + Clinicorp** (se/quando o vertical odonto entrar); documentos/anexos em lote (ZIP) | opção de expansão de mercado |

## 5. UX — meta de cliques (o pedido central)

Wizard de 4 passos no onboarding e em Configurações → Migração:

1. **"De onde você vem?"** — grade de logos + "outro/planilha". Ao escolher, instruções ilustradas de como exportar **daquele** sistema (conteúdo da pesquisa §2 vira help contextual) + kit de saída LGPD se aplicável. *(1 clique)*
2. **Arraste os arquivos** — aceita o ZIP/CSV/XLSX como veio; detecção automática confirma a origem. *(1-2 cliques)*
3. **Preview** — "Encontramos 1.243 pacientes, 3.481 agendamentos; 12 linhas com problema (ver motivo)"; mapeamento de campos pré-resolvido pelo adaptador, editável só se quiser. *(1 clique se o adaptador resolveu; nunca tela de "de-para" obrigatória quando a origem é conhecida)*
4. **Importar** — barra de progresso assíncrona, e-mail ao concluir, relatório + botão desfazer. *(1 clique)*

**Meta mensurável: origem conhecida = ≤5 cliques e <15 min do upload ao relatório para 1.000 pacientes.** Genérico = +1 tela de mapeamento.

## 6. Migração assistida (serviço humano como produto)

- **Self-service**: grátis em todos os planos, sempre (diferencial D1).
- **Assistida** ("nós migramos para você"): incluída no plano anual / oferta fundadora; R$299 one-time no mensal ([pricing §5](../Discoverys/roadmap-melhorias-2026/05_planos_e_pricing.md)). Operacional: canal de upload seguro + operador usa a MESMA central (sem ferramenta paralela) + videochamada de validação final (benchmark Clinicorp).
- **SLA público: ≤2 dias úteis** — contra 18 dias do iClinic.

## 7. Métricas da fase

% de trials que importam dados na 1ª semana · tempo upload→relatório · % linhas com erro por origem (qualidade dos adaptadores) · conversão trial→pago de quem importou vs não importou (a tese: quem importa converte mais).

## 8. Riscos

| Risco | Mitigação |
|---|---|
| Estrutura de export muda sem aviso | Fingerprint tolerante + fallback automático para o mapeador genérico; testes com arquivos reais versionados como fixtures |
| Export da origem vem incompleto/corrompido (relatos no iClinic) | Relatório de preview transparente ("o arquivo não contém X") + kit de saída LGPD para exigir o resto |
| Dados ilícitos/da clínica errada importados | Termo de responsabilidade + dedupe + desfazer + audit |
| Volume grande trava o request | Processamento 100% assíncrono em lotes (fila); nunca síncrono no upload |
| Scraping/acesso direto ao sistema antigo | **Fora de escopo por princípio** — só arquivos que o cliente exporta legitimamente |

## Execução

W1 é uma demanda de briefing (BA) com schema novo (`import_jobs`/`import_rows`) → `imedto-database`. Cada adaptador subsequente é demanda própria pequena (parser + fixtures + mapeamento), sem mexer na pipeline central. O kit de saída LGPD é conteúdo (sem código) — pode sair junto do W1.
