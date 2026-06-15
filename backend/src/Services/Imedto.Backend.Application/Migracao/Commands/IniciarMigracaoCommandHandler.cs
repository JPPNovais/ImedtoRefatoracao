using Imedto.Backend.Contracts.Migracao.Commands;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Migracao.Commands;

/// <summary>
/// Inicia uma migração de dados.
///
/// Fluxo:
/// 1. Valida limite de 50MB (CA19, R11) — rejeita antes de qualquer I/O pesado.
/// 2. Cria o <see cref="MigracaoJob"/> (estado aguardando_arquivo).
/// 3. Faz upload do ZIP no S3 via <see cref="IMigracaoArquivoStorageService"/> (retenção 30 dias — CA24, R12).
/// 4. Registra o arquivo no job (transição → aguardando_mapa).
/// 5. Persiste o job.
///
/// Multi-tenant: estabelecimento_id vem do JWT via ICurrentTenantAccessor — não do body (CA2, CA3).
/// LGPD: nenhum PII nos logs; nome do arquivo não é logado (CA4).
/// </summary>
public sealed class IniciarMigracaoCommandHandler
{
    private const long LimiteBytes = 50L * 1024 * 1024; // 50 MB — R11

    private readonly IMigracaoJobRepository _repo;
    private readonly IMigracaoArquivoStorageService _storage;

    public IniciarMigracaoCommandHandler(
        IMigracaoJobRepository repo,
        IMigracaoArquivoStorageService storage)
    {
        _repo = repo;
        _storage = storage;
    }

    public async Task<IniciarMigracaoResult> Handle(IniciarMigracaoCommand cmd, CancellationToken ct = default)
    {
        // 1. Validar limite de arquivo (CA19) — antes de criar job ou fazer I/O.
        if (cmd.ArquivoTamanhoBytes > LimiteBytes)
            throw new BusinessException(
                "Arquivo acima de 50MB. Divida a migração em partes ou contate o suporte para migração assistida.");

        if (cmd.ArquivoTamanhoBytes <= 0)
            throw new BusinessException("Arquivo inválido.");

        // 2. Criar o job de migração.
        var job = MigracaoJob.Criar(
            estabelecimentoId: cmd.EstabelecimentoId,
            criadoPorUsuarioId: cmd.UsuarioId,
            origem: cmd.Origem);

        // Persiste para obter Id antes do upload S3 (chave usa o Id).
        await _repo.Salvar(job, ct);

        // 3. Upload do ZIP no S3 (retenção 30 dias configurada no bucket lifecycle).
        string s3Key;
        try
        {
            s3Key = await _storage.UploadArquivoAsync(
                cmd.EstabelecimentoId,
                job.Id,
                cmd.ArquivoStream,
                ct);
        }
        catch
        {
            // Upload falhou → rejeita o job (não deixa em estado aguardando_arquivo eternamente).
            job.Rejeitar();
            await _repo.Salvar(job, ct);
            throw new BusinessException("Falha ao processar o arquivo. Tente novamente.");
        }

        // 4. Registrar arquivo recebido → transição para aguardando_mapa.
        job.RegistrarArquivoRecebido(s3Key);
        await _repo.Salvar(job, ct);

        return new IniciarMigracaoResult
        {
            JobId = job.Id,
            Status = job.Status
        };
    }
}
