using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Queries;

/// <summary>
/// Solicitações recebidas pelo estabelecimento. Usuário solicitante deve ser o dono
/// — validado no handler.
/// </summary>
public class ListarSolicitacoesVinculoRecebidasQuery : IQuery<IEnumerable<SolicitacaoVinculoDto>>
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }

    /// <summary>Filtra por status. <c>null</c> = todos.</summary>
    public string Status { get; set; }
}
