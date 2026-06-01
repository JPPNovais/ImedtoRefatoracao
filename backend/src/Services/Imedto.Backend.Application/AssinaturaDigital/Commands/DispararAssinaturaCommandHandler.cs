using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Application.AssinaturaDigital.Commands;

public class DispararAssinaturaCommandHandler : ICommandHandler<DispararAssinaturaCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IAssinaturaCertificadoRepository _certRepo;
    private readonly IAssinaturaDigitalProvider _provider;
    private readonly IAssinaturaAuditLogRepository _auditRepo;
    private readonly IDataProtector _protector;
    private readonly ILogger<DispararAssinaturaCommandHandler> _logger;

    public DispararAssinaturaCommandHandler(
        IReceitaRepository receitaRepo,
        IAssinaturaCertificadoRepository certRepo,
        IAssinaturaDigitalProvider provider,
        IAssinaturaAuditLogRepository auditRepo,
        IDataProtectionProvider dataProtection,
        ILogger<DispararAssinaturaCommandHandler> logger)
    {
        _receitaRepo = receitaRepo;
        _certRepo = certRepo;
        _provider = provider;
        _auditRepo = auditRepo;
        _protector = dataProtection.CreateProtector("assinatura.refresh_token");
        _logger = logger;
    }

    public async Task Handle(DispararAssinaturaCommand cmd)
    {
        // Multi-tenant: obtém receita filtrando por estabelecimento.
        var receita = await _receitaRepo.ObterPorIdOuNulo(cmd.ReceitaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Receita não encontrada.");

        // CA-03: apenas o prescritor pode assinar.
        if (receita.ProfissionalUsuarioId != cmd.CallerUsuarioId)
            throw new BusinessException("Somente o médico prescritor pode assinar esta receita.");

        // CA-12: receita já assinada é imutável. CA-11: falha/expirada pode re-disparar.
        // IniciarAssinatura já lança BusinessException se já AssinadaIcp.
        var statusAnterior = receita.AssinaturaDigitalStatus.ToString();

        var cert = await _certRepo.ObterPorMedicoAsync(cmd.CallerUsuarioId)
            ?? throw new BusinessException("Nenhum certificado digital vinculado. Vincule seu certificado antes de assinar.");

        string refreshTokenDecifrado;
        try
        {
            refreshTokenDecifrado = _protector.Unprotect(cert.RefreshToken);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Chave de Data Protection mudou (ex.: deploy sem persistência configurada).
            // O médico precisa revincular o certificado.
            throw new BusinessException("Certificado inválido ou expirado. Acesse Minha Conta e vincule o certificado novamente.");
        }

        receita.IniciarAssinatura();
        await _receitaRepo.Salvar(receita);

        var auditLog = AssinaturaAuditLog.Registrar(
            receitaId: receita.Id,
            estabelecimentoId: cmd.EstabelecimentoId,
            usuarioId: cmd.CallerUsuarioId,
            acao: "DISPARO_ASSINATURA",
            statusAnterior: statusAnterior,
            statusNovo: receita.AssinaturaDigitalStatus.ToString());
        await _auditRepo.SalvarAsync(auditLog);

        // Disparo assíncrono — resposta via webhook.
        var resultado = await _provider.DispararAssinaturaAsync(
            receita, cmd.CallerUsuarioId, refreshTokenDecifrado);

        if (!resultado.Sucesso)
        {
            // Disparo falhou imediatamente (ex.: token inválido) — reverte para FalhaAssinatura.
            receita.RegistrarFalhaAssinatura();
            await _receitaRepo.Salvar(receita);

            var auditFalha = AssinaturaAuditLog.Registrar(
                receitaId: receita.Id,
                estabelecimentoId: cmd.EstabelecimentoId,
                usuarioId: cmd.CallerUsuarioId,
                acao: "DISPARO_ASSINATURA",
                statusAnterior: StatusAssinaturaDigital.AssinaturaPendente.ToString(),
                statusNovo: receita.AssinaturaDigitalStatus.ToString());
            await _auditRepo.SalvarAsync(auditFalha);

            _logger.LogWarning(
                "[AssinaturaDigital] Falha imediata no disparo da receita {ReceitaId}: {Erro}",
                cmd.ReceitaId, resultado.MensagemErro);
        }
        else if (resultado.ModoHomologacao)
        {
            _logger.LogInformation(
                "[AssinaturaDigital] Disparo em modo homologação para receita {ReceitaId}.",
                cmd.ReceitaId);
        }
    }
}
