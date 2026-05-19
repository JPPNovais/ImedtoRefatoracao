---
name: project-orcamento-paridade-legado-2026-05-18
description: Paridade do orçamento com o legado — internação substituída por "Local Cirúrgico" (5 tipos), produtos consolidados, vínculo agendamento, permissões aprovar/configurar.
metadata:
  type: project
---

Entrega 2026-05-18 — orçamento ganhou paridade com o legado Vue+Supabase.

**Mudanças estruturais (não reverter sem motivo forte):**

- `OrcamentoInternacao` aggregate **removido** + tabela `orcamento_internacao` dropada.
  Substituído por 3 colunas embutidas em `orcamentos`: `tipo_local`, `tempo_local_minutos`, `valor_local`.
- Enum `TipoInternacao` (4 valores) **removido**. Novo `TipoLocalCirurgia` com 5 valores:
  `IntLocal`, `IntPeridural`, `IntGeral`, `SemInternacao`, `Ambulatorio`.
- `ConfiguracaoLocalCirurgia` migrou de `TipoInternacao` para `TipoLocalCirurgia`.
  Linhas com tipos antigos (Apartamento/Enfermaria/UTI/Ambulatorial) foram apagadas pela migration
  (não havia dados em produção segundo briefing).
- Novos campos em `Orcamento`: `Titulo` (opcional, 120 chars) e `AgendamentoId` (FK SET NULL + index parcial).
- Calculadora ganhou sobrecarga `CalcularValorLocal(TipoLocalCirurgia, tempo, ConfiguracaoLocalCirurgia?)`
  que ignora tempo para tipos fixos (SemInternacao/Ambulatorio).
- Novo serviço puro `ProdutosConsolidador` em `Domain/Orcamentos/Calculos/` — regra MAX (uso único)
  e SOMA (não único) entre cirurgias.
- Novos endpoints:
  - `POST /api/orcamentos/consolidar-produtos` — chama `ConsolidarProdutosOrcamentoQueryHandler` (singleton).
  - `GET /api/orcamentos/por-agendamento/{id}` — 204 quando não há orçamento ativo.
- Permissões granulares ampliadas: `orcamento.aprovar` e `orcamento.configurar` adicionadas
  ao `CatalogoPermissoes` (backend) e `PERMISSION_AREAS` (front). O `OrcamentoController` agora
  usa `[RequiresAcao("orcamento","criar")]` etc. por action.
- Front:
  - Rota nova `/orcamentos/novo` (`name: OrcamentoNovo`) — reutiliza `OrcamentoFormView.vue`.
  - `OrcamentoListaView` não cria mais orçamento eager — apenas `router.push("/orcamentos/novo")`.
    Tab "Quitados" removida (tinha statuses vazio, morta).
  - `OrcamentoFormView` reescrita do zero (`script setup ~590 linhas`, vs 864 do Frankenstein anterior),
    paritária com legado: paciente + título + cirurgias (com tempo somado + override manual) +
    produtos consolidados (chamada backend, debounce 300ms) + profissionais (via catálogo de
    valor — sem input livre de UUID) + equipes legado + implantes + local cirúrgico (5 cards radio) +
    anestesia opcional + formas de pagamento com indicador de diferença + sticky bar com resumo.
  - `OutrasConfigsTab` ganhou editor de Local Cirúrgico (5 cards com inputs editáveis,
    salvar individual por tipo).

**Pendências conscientes (registradas como out-of-scope desta entrega):**
- Botão "Criar orçamento" / "Ver orçamento vinculado" na ficha do `EditarAgendamentoModal` —
  endpoint backend existe, falta o link no UI.
- Uso de `Pacote` como snapshot no form — out-of-scope, infra do catálogo já pronta.

**Compatibilidade do read-side:** quando o `OrcamentoFormView` carrega para EDITAR um orçamento
existente, ele não consegue inferir o `catalogoCirurgiaId` (a coluna não existe em
`orcamento_cirurgias` — só persiste `procedimento_cirurgico_id`). O usuário precisa re-selecionar
a cirurgia para acionar consolidação de produtos. Mesmo padrão para equipe — só persistimos
`profissional_usuario_id + papel + valor`, não o `valor_profissional_id` do catálogo.
Se isso virar atrito, criar colunas auxiliares para snapshot do id de catálogo.

[[project-dtos-orfaos-fase1]] referenciava "ReceitaEmitidaEvent" — orçamento continua ativo.
