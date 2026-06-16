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
/// 4. Registra o arquivo no job (transição → aguardando_aprovacao — addendum 003, R-A1).
/// 5. Persiste o job + grava evento de transição (CA53 — forward-only, addendum 004).
///
/// Multi-tenant: estabelecimento_id vem do JWT via ICurrentTenantAccessor — não do body (CA2, CA3).
/// LGPD: nenhum PII nos logs; nome do arquivo não é logado (CA4).
/// </summary>
public sealed class IniciarMigracaoCommandHandler
{
    private const long LimiteBytes = 50L * 1024 * 1024; // 50 MB — R11

    private readonly IMigracaoJobRepository _repo;
    private readonly IMigracaoArquivoStorageService _storage;
    private readonly IMigracaoJobEventoRepository _eventoRepo;

    public IniciarMigracaoCommandHandler(
        IMigracaoJobRepository repo,
        IMigracaoArquivoStorageService storage,
        IMigracaoJobEventoRepository eventoRepo)
    {
        _repo       = repo;
        _storage    = storage;
        _eventoRepo = eventoRepo;
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
            origem: cmd.Origem,
            onda: cmd.Onda);

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

        // 4. Registrar arquivo recebido → transição para aguardando_aprovacao (addendum 003, R-A1).
        var statusAnterior = job.Status; // aguardando_arquivo
        job.RegistrarArquivoRecebido(s3Key);
        await _repo.Salvar(job, ct);

        // CA53 — gravar evento de transição forward-only (addendum 004, R-T2).
        // UsuarioId null: upload é ação do tenant, não do admin — nenhum admin autenticado aqui.
        var evento = MigracaoJobEvento.Criar(
            migracaoJobId: job.Id,
            estabelecimentoId: job.EstabelecimentoId,
            statusAnterior: statusAnterior,
            statusNovo: job.Status,
            usuarioId: null);
        await _eventoRepo.Gravar(evento, ct);

        return new IniciarMigracaoResult
        {
            JobId = job.Id,
            Status = job.Status
        };
    }
}
