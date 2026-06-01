using Amazon.S3;
using Amazon.S3.Model;
using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Cqrs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imedto.Backend.Application.AssinaturaDigital.Commands;

public class ProcessarCallbackAssinaturaCommandHandler : ICommandHandler<ProcessarCallbackAssinaturaCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IAssinaturaDigitalProvider _provider;
    private readonly IAssinaturaAuditLogRepository _auditRepo;
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<ProcessarCallbackAssinaturaCommandHandler> _logger;

    public ProcessarCallbackAssinaturaCommandHandler(
        IReceitaRepository receitaRepo,
        IAssinaturaDigitalProvider provider,
        IAssinaturaAuditLogRepository auditRepo,
        IAmazonS3 s3,
        IOptions<StorageOptions> storageOptions,
        ILogger<ProcessarCallbackAssinaturaCommandHandler> logger)
    {
        _receitaRepo = receitaRepo;
        _provider = provider;
        _auditRepo = auditRepo;
        _s3 = s3;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task Handle(ProcessarCallbackAssinaturaCommand cmd)
    {
        // CA-07: validação HMAC obrigatória antes de qualquer mutação.
        var validacao = await _provider.ValidarCallbackAsync(cmd.PayloadJson, cmd.HeaderAssinatura);
        if (!validacao.AssinaturaValida)
        {
            _logger.LogWarning(
                "[AssinaturaDigital] Callback recebido com HMAC inválido para receita {ReceitaId}.",
                cmd.ReceitaId);
            // Lança para o controller retornar 401.
            throw new UnauthorizedAccessException("Assinatura do callback inválida.");
        }

        // Busca receita sem filtro de estabelecimento (webhook externo não tem claim de tenant).
        // A receita_id é chave suficiente — o HMAC garante autenticidade do provedor.
        var receita = await _receitaRepo.ObterSemTenantAsync(cmd.ReceitaId);
        if (receita is null)
        {
            // CA-08: tenant inativo ou receita inexistente → descarta silenciosamente.
            _logger.LogInformation(
                "[AssinaturaDigital] Webhook para receita {ReceitaId} não encontrada — ignorado.",
                cmd.ReceitaId);
            return;
        }

        var statusAnterior = receita.AssinaturaDigitalStatus.ToString();

        if (validacao.Sucesso && !string.IsNullOrWhiteSpace(validacao.PdfBase64))
        {
            // Persiste PDF assinado no S3 (bucket de anexos, prefixo receitas-assinadas/).
            var s3Key = $"receitas-assinadas/{receita.EstabelecimentoId}/{receita.Id}/receita-assinada.pdf";
            var pdfBytes = Convert.FromBase64String(validacao.PdfBase64);

            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _storageOptions.BucketAnexosProntuario,
                Key = s3Key,
                InputStream = new MemoryStream(pdfBytes),
                ContentType = "application/pdf",
            });

            receita.ConfirmarAssinatura(s3Key);
        }
        else
        {
            receita.RegistrarFalhaAssinatura();
        }

        await _receitaRepo.Salvar(receita);

        var audit = AssinaturaAuditLog.Registrar(
            receitaId: receita.Id,
            estabelecimentoId: receita.EstabelecimentoId,
            usuarioId: Guid.Empty, // Guid.Empty = sistema/provedor externo.
            acao: "WEBHOOK_CALLBACK",
            statusAnterior: statusAnterior,
            statusNovo: receita.AssinaturaDigitalStatus.ToString());
        await _auditRepo.SalvarAsync(audit);

        _logger.LogInformation(
            "[AssinaturaDigital] Callback processado para receita {ReceitaId}: {Status}.",
            cmd.ReceitaId, receita.AssinaturaDigitalStatus);
    }
}
