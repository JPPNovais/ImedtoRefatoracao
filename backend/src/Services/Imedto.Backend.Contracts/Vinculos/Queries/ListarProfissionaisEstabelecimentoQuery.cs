using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Queries;

public class ListarProfissionaisEstabelecimentoQuery : IQuery<IEnumerable<ProfissionalVinculadoDto>>
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }

    /// <summary>
    /// Quando true, inclui vínculos com status Inativo no resultado. Usado pela
    /// tela de gestão de equipe (precisa enxergar quem foi desativado para reativar).
    /// Default false: seletores de agenda/prontuário/orçamento só veem quem pode atuar.
    /// </summary>
    public bool IncluirInativos { get; set; }
}
