using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.AspNetCore.DataProtection;

namespace Imedto.Backend.Application.AssinaturaDigital.Commands;

public class VincularCertificadoCommandHandler : ICommandHandler<VincularCertificadoCommand>
{
    private readonly IAssinaturaCertificadoRepository _repo;
    private readonly IDataProtector _protector;

    public VincularCertificadoCommandHandler(
        IAssinaturaCertificadoRepository repo,
        IDataProtectionProvider dataProtection)
    {
        _repo = repo;
        _protector = dataProtection.CreateProtector("assinatura.refresh_token");
    }

    public async Task Handle(VincularCertificadoCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            throw new BusinessException("Token do certificado é obrigatório.");

        var existente = await _repo.ObterPorMedicoAsync(cmd.MedicoId);
        var tokenCifrado = _protector.Protect(cmd.RefreshToken);

        if (existente is not null)
        {
            // Atualiza o token existente (renovação do vínculo).
            existente.AtualizarToken(tokenCifrado, cmd.ExpiraEm);
            await _repo.Salvar(existente);
        }
        else
        {
            var cert = AssinaturaCertificado.Vincular(
                cmd.MedicoId,
                cmd.Provedor,
                tokenCifrado,
                cmd.ExpiraEm);
            await _repo.Salvar(cert);
        }
    }
}
