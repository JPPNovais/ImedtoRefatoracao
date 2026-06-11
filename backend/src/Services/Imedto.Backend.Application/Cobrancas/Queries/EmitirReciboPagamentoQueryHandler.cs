using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Infrastructure.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Cobrancas.Queries;

/// <summary>
/// Handler que delega a geração do PDF do recibo ao <see cref="IReciboPagamentoPdfService"/>.
/// Scoped: recibo_emitido_em precisa de EF (escrita) via ICobrancaRepository injetado no serviço.
/// Audit LGPD e flag de 1ª emissão ficam no serviço (separação de concerns).
/// </summary>
public class EmitirReciboPagamentoQueryHandler : IRequestHandler<EmitirReciboPagamentoQuery, byte[]>
{
    private readonly IReciboPagamentoPdfService _pdfService;

    public EmitirReciboPagamentoQueryHandler(IReciboPagamentoPdfService pdfService)
        => _pdfService = pdfService;

    public Task<byte[]> Handle(EmitirReciboPagamentoQuery query)
        => _pdfService.GerarAsync(query.PagamentoId, query.EstabelecimentoId, query.UsuarioId);
}
