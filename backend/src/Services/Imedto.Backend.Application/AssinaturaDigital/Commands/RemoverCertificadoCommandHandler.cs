using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.AssinaturaDigital.Commands;

public class RemoverCertificadoCommandHandler : ICommandHandler<RemoverCertificadoCommand>
{
    private readonly IAssinaturaCertificadoRepository _repo;

    public RemoverCertificadoCommandHandler(IAssinaturaCertificadoRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(RemoverCertificadoCommand cmd)
    {
        var cert = await _repo.ObterPorMedicoAsync(cmd.MedicoId);
        if (cert is null) return; // Idempotente — se não existe, nada a fazer.

        await _repo.Remover(cert);
    }
}
