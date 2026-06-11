using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

/// <summary>
/// Obtém o caixa do dia + resumo on-the-fly (R8).
/// Retorna null se não existe caixa para a data (estado NãoAberto — CA162).
/// </summary>
public class ObterCaixaDiarioQuery : IQuery<CaixaDiarioDto?>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly Data { get; set; }
}
