# Fase 1 — Inventário de DTOs sem uso no frontend

**Data**: 2026-05-02
**Escopo**: `backend/src/Services/Imedto.Backend.Contracts/` × `frontend/src/`
**Objetivo**: identificar contratos órfãos e campos retornados ao front que ninguém lê — guia para minimização LGPD na Fase 3.

## Metodologia

1. Extraídos 82 DTOs (`*Dto.cs`) e seus campos via parse AST simplificado (regex sobre `public T Nome { get; set/init; }` e parâmetros de `record`).
2. Cada DTO testado em duas dimensões contra o blob completo de `frontend/src/**/*.{ts,vue}`:
   - **Tipo consumido?** Busca pelo nome (`PacienteDto` → `Paciente`/`PacienteListaItem`) + busca transitiva (DTO usado como campo de outro DTO consumido conta como consumido).
   - **Campos lidos?** Busca pelo identificador `camelCase` em três padrões fortes: `.campo`, `campo:`, `'campo'|"campo"|\`campo\``. Só conta como "lido" se o front efetivamente acessa a propriedade — um campo chamado `nome` em `front.ts` não conta como leitura de `EspecialidadeListadaDto.Nome` se não estiver no contexto correto.
3. Para Commands/Queries: como o front nunca referencia os nomes C# (chama HTTP por URL), o cruzamento foi feito via mapa `Controller route → service.ts call`.

PII = CPF, e-mail, telefone, data de nascimento, endereço, RG, CNPJ, razão social, nome completo, conselho/CRM, e nomes de pessoas (paciente, profissional, autor) que identificam diretamente um titular.

---

## 1. DTOs órfãos (não consumidos pelo front)

São DTOs declarados em `Contracts`, com handler/controller no backend, mas **sem nenhum service ou view do front que consuma o endpoint** que os retorna. Risco principal: código morto em `Application/Infrastructure` + endpoint exposto à internet sem cliente conhecido (superfície de ataque desnecessária). Risco LGPD = **médio** (apenas LGPD direto se o DTO contiver PII; reportado abaixo onde aplica).

### Catálogo
- `EspecialidadeListadaDto` — `Catalogo/Queries/Results/EspecialidadeListadaDto.cs` — endpoint `GET /api/catalogo/especialidades` sem consumidor. Risco: **médio**.
- `ProfissaoListadaDto` — `Catalogo/Queries/Results/ProfissaoListadaDto.cs` — endpoint `GET /api/catalogo/profissoes` sem consumidor. Risco: **médio**.
- `RegiaoCatalogoDto` — `Catalogo/Queries/Results/RegiaoCatalogoDto.cs` — endpoint `GET /api/catalogo/regioes` sem consumidor (front usa `/catalogo/regioes-anatomicas` que é outra rota com outro DTO legado). Risco: **médio**. **Atenção**: campo `SvgCoordsJson` é payload pesado (JSON cru) — não-PII mas waste de banda.

### Receitas
- `ConfiguracaoReceitaDto` — `Receitas/Queries/Results/ConfiguracaoReceitaDto.cs` — endpoint `GET /api/receitas/configuracao` existe mas o front não chama. Risco: **médio**. Contém `EmissorPadrao` (nome do profissional padrão — borderline PII).
- `MedicamentoFavoritoDto` — `Receitas/Queries/Results/ConfiguracaoReceitaDto.cs` — endpoint `GET /api/receitas/medicamentos-favoritos` sem consumidor. Risco: **médio** (não-PII).

### Automações
- `RegraAutomacaoDto` — `Automacoes/Queries/RegraAutomacaoDto.cs` — front consome apenas `ConfiguracaoAutomacaoDto` simples (`/api/automacoes/configuracao`). A query `ListarRegrasAutomacao` e o command `CriarRegraAutomacao` não têm UI. Risco: **médio** (campos `CondicoesJson`/`AcoesJson` são payloads pesados).
- `EventoAutomacaoDto` — mesmo arquivo — endpoint de listagem de eventos não é chamado. Risco: **baixo** (provável uso interno por job futuro).

### Prontuário / Exame Físico
- `ExameFisicoDto`, `ExameFisicoResumoDto`, `RegiaoExameFisicoDto`, `PaginaExamesFisicosDto` — `Prontuarios/Queries/Results/ExameFisicoDto.cs` — **caso especial**. O backend expõe os endpoints (`GET /api/exame-fisico/{id}`, timeline, etc.) e o front os chama (`exameFisicoService.ts`), **mas o front declara interfaces locais com shape legado em `snake_case`** (`paciente_id`, `regioes_examinadas`, `dados_gerais`) — incompatíveis com o novo `ExameFisicoDto` (`pacienteId`, `regioes`, `dadosGeraisJson`). Hoje a integração está **provavelmente quebrada em runtime ou intermediada por outra camada legada**. Risco: **alto** — investigar antes de tocar; pode haver bug latente. PII envolvida (`PacienteId`, `RealizadoPorNome`).

### LGPD (caso especial: download blob)
- `MeusDadosLgpdDto`, `ProfissionalResumidoDto`, `VinculoResumidoDto`, `NotificacaoResumidaDto` — `Lgpd/Queries/MeusDadosLgpdDto.cs` — não aparecem como tipos no front, mas são **legitimamente "consumidos" via download blob** em `lgpdService.ts > exportarDados()` (responseType `blob`, salva como `meus-dados.json`). O front não parseia nada — usuário recebe JSON cru. Tecnicamente não-órfãos, mas **toda PII que contiverem vai num arquivo público no disco do usuário** — verificar minimização (Art. 18 LGPD: titular tem direito ao próprio dado, mas o arquivo deve ser mínimo e claro). Risco: **alto** se incluir mais que o necessário.

---

## 2. Campos PII vazando sem leitura no front (PRIORIDADE LGPD)

Cada item abaixo é um campo que o backend retorna ao front (DTO efetivamente consumido), mas que **nenhum `.ts`/`.vue` lê** — ou seja, sai da database, atravessa rede e API, e cai no JSON do navegador sem destino. **Esses campos devem ser removidos do DTO** (e do projection do `*QueryRepository`) na Fase 3.

### Risco ALTO (PII clínica/identificadora vazando)

| Campo | DTO | Arquivo | Justificativa |
|---|---|---|---|
| `ProfissionalEmail` | `SolicitacaoVinculoDto` | `Vinculos/Queries/Results/SolicitacaoVinculoDto.cs` | E-mail é identificador direto do profissional. Front usa só `profissionalNome`. |
| `EstabelecimentoNomeFantasia` | `SolicitacaoVinculoDto` | mesmo arquivo | Razão social/fantasia em listagem expõe relacionamento que pode não ser pretendido. |
| `Crm` | `ProfissionalResumidoDto` (LGPD export) | `Lgpd/Queries/MeusDadosLgpdDto.cs` | Inclui CRM no JSON de export sem que o front leia — só vai no arquivo baixado. Verificar se o titular **deve** receber o próprio CRM (provavelmente sim, porém revisar Art. 9º). |
| `RealizadoPorNome` | `ExameFisicoDto`, `ExameFisicoResumoDto` | `Prontuarios/Queries/Results/ExameFisicoDto.cs` | Nome do profissional que realizou exame — sai mas não é exibido na timeline atual. |
| `RealizadoPorUsuarioId` | `ExameFisicoDto` | mesmo arquivo | UUID interno de usuário não deve trafegar se não há uso. |

### Risco MÉDIO (não-PII, mas ruído + waste)

DTOs com campos calculados/derivados que o front não lê. Cada um é candidato a remoção. Resumo (lista completa em "Apêndice A"):

- `DashboardKpisDto`: 8 dos 10 campos não lidos (`TotalAgendamentos`, `AgendamentosConcluidos`, `AgendamentosCancelados`, `TaxaOcupacao`, `TaxaCancelamento`, `Despesas`, `LucroLiquido`, `TicketMedio`, `NovosPacientes`). Front consome apenas `Faturamento`. **Investigar**: o relatório operacional possivelmente consume isso indiretamente — verificar `RelatorioOperacionalView.vue` antes de cortar.
- `DesempenhoProfissionalDto`: `Atendimentos`, `AtendimentosConcluidos`, `TaxaOcupacao` não lidos.
- `PacientesResumoDto`: `Novos`, `Retornos`, `PorFaixaEtaria` não lidos.
- `InventarioResumoDto`: `ValorTotalEstoque`, `TopMovimentacoes` não lidos.
- `AgendaResumoDto`: `PorProfissional`, `PorDiaSemana` não lidos.
- `RelatorioOrcamentosDto`: `TotalEmitidos`, `ValorMedio` não lidos (front usa `valorMedioAprovado` — campo diferente que vem de outro endpoint).
- `ReceitaDto` / `ReceitaResumoDto`: `TipoNotificacao`, `RequerRetencao`, `CanceladaEm`, `QuantidadeItens` não lidos — mas estes são campos com valor clínico/regulatório (Portaria 344/98) e o comentário no DTO diz que o front exibirá; **provável feature em construção**, não cortar sem avisar dono do domínio.
- `ProcedimentoCirurgicoDto`: `CanceladoEm`, `CirurgiaCodigo` não lidos.
- `ItemReceitaDto.Via` e `MedicamentoFavoritoDto.Via` não lidos (mas DTO `MedicamentoFavoritoDto` está órfão como um todo).
- `VinculoResumidoDto`: `NomeEstabelecimento`, `VinculadoEm` não lidos (vai no JSON do export LGPD, mas titular provavelmente quer essa info — manter, é por design do export).
- `RegiaoExameFisicoDto`, `TopPacienteDto.Atendimentos`, `RegraAutomacaoDto` campos JSON, `EspecialidadeListadaDto.ProfissaoId/Nome`, `ProfissaoListadaDto.ConselhoSigla` — pertencem a DTOs já listados como órfãos.

### Apêndice A — listagem completa (gerada do parser)

Ver arquivo `/tmp/dto_field_misses.txt` no disco local (não commitado) ou rodar `python3 /tmp/analyze_dtos3.py` para regerar. Os blocos abaixo são a saída literal:

```
[AgendaResumoDto]  (Relatorios/Queries/Results/RelatorioOperacionalDto.cs)
   - PorProfissional: IList<RowSummary>
   - PorDiaSemana: IList<RowSummary>

[ConfiguracaoReceitaDto]  (Receitas/Queries/Results/ConfiguracaoReceitaDto.cs)
   - CabecalhoHtml: string?
   - RodapeHtml: string?
   - ModeloPadraoId: long?
   - EmissorPadrao: string?

[DashboardKpisDto]  (Relatorios/Queries/Results/RelatorioOperacionalDto.cs)
   - TotalAgendamentos: int
   - AgendamentosConcluidos: int
   - AgendamentosCancelados: int
   - TaxaOcupacao: decimal
   - TaxaCancelamento: decimal
   - Despesas: decimal
   - LucroLiquido: decimal
   - TicketMedio: decimal
   - NovosPacientes: int

[DesempenhoProfissionalDto]  (Relatorios/Queries/Results/RelatorioPessoasDto.cs)
   - Atendimentos: int
   - AtendimentosConcluidos: int
   - TaxaOcupacao: decimal

[EspecialidadeListadaDto]  (Catalogo/Queries/Results/EspecialidadeListadaDto.cs)
   - ProfissaoId: long
   - ProfissaoNome: string

[EventoAutomacaoDto]  (Automacoes/Queries/RegraAutomacaoDto.cs)
   - RegraId: long
   - TentativaN: int
   - ExecutarEm: DateTime
   - ExecutadoEm: DateTime?
   - UltimaFalha: string?

[ExameFisicoDto]  (Prontuarios/Queries/Results/ExameFisicoDto.cs)
   - RealizadoEm: DateTime
   - RealizadoPorUsuarioId: Guid
   - RealizadoPorNome: string?  PII!

[ExameFisicoResumoDto]  (Prontuarios/Queries/Results/ExameFisicoDto.cs)
   - RealizadoEm: DateTime
   - RealizadoPorNome: string?  PII!
   - TotalRegioes: int
   - TemDadosGerais: bool
   - SeveridadeMaxima: string?

[InventarioResumoDto]  (Relatorios/Queries/Results/RelatorioOperacionalDto.cs)
   - ValorTotalEstoque: decimal
   - TopMovimentacoes: IList<RowSummary>

[ItemReceitaDto]  (Receitas/Queries/Results/ReceitaDto.cs)
   - Via: string?

[MedicamentoFavoritoDto]  (Receitas/Queries/Results/ConfiguracaoReceitaDto.cs)
   - Via: string?
   - UsoCount: int
   - UltimoUso: DateTime?

[PacientesResumoDto]  (Relatorios/Queries/Results/RelatorioPessoasDto.cs)
   - Novos: int
   - Retornos: int
   - PorFaixaEtaria: IList<RowSummary>

[ProcedimentoCirurgicoDto]  (Cirurgias/Queries/Results/ProcedimentoCirurgicoDto.cs)
   - CanceladoEm: DateTime?

[ProcedimentoCirurgicoResumoDto]  (Cirurgias/Queries/Results/ProcedimentoCirurgicoDto.cs)
   - CirurgiaCodigo: string?

[ProfissaoListadaDto]  (Catalogo/Queries/Results/ProfissaoListadaDto.cs)
   - ConselhoSigla: string?

[ProfissionalResumidoDto]  (Lgpd/Queries/MeusDadosLgpdDto.cs)
   - Crm: string  PII!

[ReceitaDto]  (Receitas/Queries/Results/ReceitaDto.cs)
   - TipoNotificacao: string?
   - RequerRetencao: bool
   - CanceladaEm: DateTime?

[ReceitaResumoDto]  (Receitas/Queries/Results/ReceitaDto.cs)
   - TipoNotificacao: string?
   - RequerRetencao: bool
   - QuantidadeItens: int

[RegiaoCatalogoDto]  (Catalogo/Queries/Results/RegiaoCatalogoDto.cs)
   - PaiCodigo: string?
   - TemplateTexto: string?
   - SvgCoordsJson: string?

[RegiaoExameFisicoDto]  (Prontuarios/Queries/Results/ExameFisicoDto.cs)
   - RegiaoCodigo: string
   - RegiaoPaiCodigo: string?
   - Severidade: string?

[RegraAutomacaoDto]  (Automacoes/Queries/RegraAutomacaoDto.cs)
   - EventoGatilho: string
   - CondicoesJson: string
   - AcoesJson: string

[RelatorioOrcamentosDto]  (Relatorios/Queries/Results/RelatorioOrcamentosDto.cs)
   - TotalEmitidos: int
   - ValorMedio: decimal

[SolicitacaoVinculoDto]  (Vinculos/Queries/Results/SolicitacaoVinculoDto.cs)
   - ProfissionalEmail: string  PII!
   - EstabelecimentoNomeFantasia: string  PII!

[TopPacienteDto]  (Relatorios/Queries/Results/RelatorioPessoasDto.cs)
   - Atendimentos: int

[VinculoResumidoDto]  (Lgpd/Queries/MeusDadosLgpdDto.cs)
   - NomeEstabelecimento: string
   - VinculadoEm: DateTime
```

---

## 3. Commands sem consumidor no front

Mapeados via rota do controller × URL chamada por `frontend/src/services/*.ts`. Risco padrão: **baixo** (podem ser invocados por job/event handler). Onde houver evidência de uso interno, marcar.

### Sem consumo aparente no front (revisar se existe gatilho interno)

- **Automacoes**:
  - `CriarRegraAutomacaoCommand` (`Automacoes/Commands/CriarRegraAutomacaoCommand.cs`) — sem UI; pode ser usado em seed/admin futuro. Risco: **baixo**.
  - `EnviarLembretesAgendamentosCommand`, `ExpirarOrcamentosVencidosCommand` — chamados por endpoints `POST /automacoes/enviar-lembretes` e `/automacoes/expirar-orcamentos`, esses **são** consumidos pelo `automacaoService.ts`. **Não-órfãos**.
- **Receitas**:
  - `AtualizarConfiguracaoReceitaCommand` (`Receitas/Commands/AtualizarConfiguracaoReceitaCommand.cs`) — endpoint existe, sem UI. Risco: **baixo**.
- **LGPD**:
  - `RegistrarConsentimentoCommand` — endpoint `POST /api/lgpd/consentimentos` existe. Buscar consumo no front (provável: aceite de termos no onboarding). Se não, risco **baixo**.

Demais commands (criar/atualizar/cancelar etc. de Agendamento, Paciente, Profissional, Estabelecimento, Financeiro, Inventario, Orcamento, Sala, Unidade, Vinculo, Prontuario, Receita) **têm endpoint consumido** pelos respectivos services do front — não são órfãos.

---

## 4. Queries sem consumidor no front

### Sem consumo aparente

| Query | Endpoint | Risco | Observação |
|---|---|---|---|
| `ListarEspecialidadesQuery` | `GET /api/catalogo/especialidades` | **baixo** | DTO órfão (item 1). |
| `ListarProfissoesQuery` | `GET /api/catalogo/profissoes` | **baixo** | DTO órfão. |
| `ListarRegioesCatalogoQuery` | `GET /api/catalogo/regioes` | **baixo** | DTO órfão; rota legada `/regioes-anatomicas` é a usada. |
| `ObterProcedimentoPorCodigoQuery` | `GET /api/catalogo/procedimentos/{codigo}` | **baixo** | Usado só pelo lookup interno; verificar. |
| `ListarRegrasAutomacaoQuery` | `GET /api/automacoes/regras` | **baixo** | Sem UI. |
| `ObterConfiguracaoReceitaQuery` | `GET /api/receitas/configuracao` | **baixo** | Sem UI. |
| `ListarMedicamentosFavoritosQuery` | `GET /api/receitas/medicamentos-favoritos` | **baixo** | Sem UI. |

Demais queries têm consumo direto via `*Service.ts`.

---

## Recomendações para Fase 3 (minimização LGPD)

### Ações de risco ALTO (fazer primeiro)
1. **`SolicitacaoVinculoDto`**: remover `ProfissionalEmail` e `EstabelecimentoNomeFantasia` da projection do `VinculoQueryRepository`. Validar no controller que essa info não é necessária para a tela de "minhas solicitações" / "recebidas". Se for, mover `EstabelecimentoNomeFantasia` para um DTO específico de "recebidas" e manter o de "minhas" só com IDs.
2. **`MeusDadosLgpdDto`**: validar com produto se o JSON de export deve incluir `CRM` em `ProfissionalResumidoDto` — defensavelmente sim (Art. 18 dá ao titular acesso aos próprios dados), mas precisa documentação clara. Reportar no consentimento.
3. **`ExameFisicoDto`/`ExameFisicoResumoDto`**: investigar a discrepância shape novo (`pacienteId`, `regioes`) × consumo legado snake_case (`paciente_id`, `regioes_examinadas`) em `exameFisicoService.ts`. Antes de tocar campos, alinhar o front com o novo contrato — o "campo não lido" pode ser falso negativo se o consumo está quebrado.

### Ações de risco MÉDIO (lote único)
4. Cortar **DTOs órfãos do catálogo** (`EspecialidadeListadaDto`, `ProfissaoListadaDto`, `RegiaoCatalogoDto`) e respectivas queries/repositories — ou remover endpoints, ou expor um único endpoint enxuto se houver plano de uso futuro (decidir com produto).
5. Remover `ConfiguracaoReceitaDto`, `MedicamentoFavoritoDto`, `RegraAutomacaoDto`, `EventoAutomacaoDto` se não há roadmap de UI nas próximas 2 sprints — reduz superfície de API.
6. **`DashboardKpisDto`**, **`PacientesResumoDto`**, **`InventarioResumoDto`**, **`AgendaResumoDto`**, **`DesempenhoProfissionalDto`**: reduzir projeção SQL aos campos efetivamente lidos. Validar primeiro com `RelatorioOperacionalView.vue` e `RelatorioPessoasView.vue` se há features incompletas dependendo desses campos.
7. **`ReceitaDto`/`ReceitaResumoDto.RequerRetencao`/`TipoNotificacao`**: o comentário do DTO indica que o front "exibe badge RETER" — confirmar se a feature é planejada antes de remover. Se sim, abrir tarefa frontend; se não, remover.

### Ações de risco BAIXO (housekeeping)
8. Commands/queries sem consumidor: **não remover** automaticamente — verificar handlers, testes e jobs. Marcar com `[Obsolete]` por uma sprint, depois remover se não aparecer uso.
9. Rotas órfãs do controller: documentar e considerar remoção do `[HttpGet]` correspondente para reduzir superfície externa.
