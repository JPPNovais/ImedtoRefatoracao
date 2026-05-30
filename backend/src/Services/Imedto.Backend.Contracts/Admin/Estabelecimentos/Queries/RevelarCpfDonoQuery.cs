using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;

/// <summary>
/// Revela CPF completo do dono de um estabelecimento.
/// Motivo obrigatório (mín. 10 chars). Gera audit RevelarCpfDono (CA17–CA19).
/// </summary>
public class RevelarCpfDonoQuery : IQuery<CpfDonoReveladoDto>
{
    public long EstabelecimentoId { get; set; }
    public Guid AdminId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
