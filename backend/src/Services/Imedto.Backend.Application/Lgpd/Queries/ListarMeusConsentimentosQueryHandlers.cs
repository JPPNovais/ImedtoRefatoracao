using Imedto.Backend.Contracts.Lgpd.Queries;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Lgpd.Queries;

public class ListarMeusConsentimentosQueryHandlers : IRequestHandler<ListarMeusConsentimentosQuery, IEnumerable<ConsentimentoDto>>
{
    private readonly ILgpdConsentimentoRepository _repo;

    public ListarMeusConsentimentosQueryHandlers(ILgpdConsentimentoRepository repo) => _repo = repo;

    public async Task<IEnumerable<ConsentimentoDto>> Handle(ListarMeusConsentimentosQuery query)
    {
        var consentimentos = await _repo.ListarPorUsuario(query.UsuarioId);
        return consentimentos.Select(c => new ConsentimentoDto
        {
            Tipo = c.Tipo.ToString(),
            Versao = c.Versao,
            AceitoEm = c.AceitoEm
        });
    }
}
