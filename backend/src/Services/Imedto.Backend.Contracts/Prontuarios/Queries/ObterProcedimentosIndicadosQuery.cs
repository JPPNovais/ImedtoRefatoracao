using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Retorna o snapshot de procedimentos indicados de uma evolução para pré-preenchimento do form de orçamento (F5/R2).
/// Multi-tenant: o handler filtra por EstabelecimentoId (falha-fechada → "Não encontrado").
/// Itens sem catalogoCirurgiaId (legado texto-livre) são excluídos da resposta.
/// </summary>
public record ObterProcedimentosIndicadosQuery(
    long EvolucaoId,
    long EstabelecimentoId) : IQuery<IEnumerable<ProcedimentoIndicadoDto>>;
