using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Queries;

/// <summary>
/// Lista apenas modelos padrão do sistema (estabelecimento_id IS NULL, ativos).
/// Usado na tela de "Cadastros → Termos → Importar padrões".
/// </summary>
public class ListarModelosPadraoTermoQuery : IQuery<IReadOnlyList<TermoModeloDto>>
{
    public long EstabelecimentoId { get; set; } // só pra audit
    public Guid SolicitanteUsuarioId { get; set; }
}
