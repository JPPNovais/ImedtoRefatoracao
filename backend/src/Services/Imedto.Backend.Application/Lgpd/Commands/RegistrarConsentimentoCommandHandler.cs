using Imedto.Backend.Contracts.Lgpd.Commands;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Lgpd.Commands;

public class RegistrarConsentimentoCommandHandler : ICommandHandler<RegistrarConsentimentoCommand>
{
    private readonly ILgpdConsentimentoRepository _repo;

    public RegistrarConsentimentoCommandHandler(ILgpdConsentimentoRepository repo) => _repo = repo;

    public async Task Handle(RegistrarConsentimentoCommand command)
    {
        if (!Enum.TryParse<TipoConsentimentoLgpd>(command.Tipo, ignoreCase: true, out var tipo))
            throw new BusinessException($"Tipo de consentimento inválido: '{command.Tipo}'.");

        var consentimento = LgpdConsentimento.Aceitar(
            command.UsuarioId,
            tipo,
            command.Versao,
            command.IpOrigem,
            command.UserAgent);

        await _repo.Salvar(consentimento);
    }
}
