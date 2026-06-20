using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Infrastructure.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Handler que delega a geração do PDF do prontuário ao <see cref="IProntuarioPdfService"/>.
/// Scoped: depende de IProntuarioAcessoLogService (audit LGPD) que é scoped.
/// Multi-tenant e audit ficam encapsulados no serviço.
/// </summary>
public class EmitirProntuarioPdfQueryHandler : IRequestHandler<EmitirProntuarioPdfQuery, byte[]>
{
    private readonly IProntuarioPdfService _pdfService;

    public EmitirProntuarioPdfQueryHandler(IProntuarioPdfService pdfService)
        => _pdfService = pdfService;

    public Task<byte[]> Handle(EmitirProntuarioPdfQuery query)
        => _pdfService.GerarAsync(query.PacienteId, query.EstabelecimentoId, query.SolicitanteUsuarioId);
}
