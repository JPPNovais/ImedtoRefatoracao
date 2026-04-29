using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Anexo de prontuário (imagem, PDF, etc.). Fica armazenado no Supabase Storage
/// em <see cref="StoragePath"/>; o backend emite URLs assinadas de leitura sob demanda.
///
/// Vinculado ao <see cref="ProntuarioId"/> (obrigatório) e OPCIONALMENTE à evolução
/// (<see cref="EvolucaoId"/>) que gerou o anexo. Append-only — para "remover" um anexo,
/// ele é marcado com <see cref="ArquivadoEm"/> mas o blob fica retido (LGPD).
/// </summary>
public class ProntuarioAnexo : Entity, ISoftDeletable
{
    public virtual long ProntuarioId { get; protected set; }
    public virtual long? EvolucaoId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string StoragePath { get; protected set; }
    public virtual string NomeOriginal { get; protected set; }
    public virtual string MimeType { get; protected set; }
    public virtual long TamanhoBytes { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? ArquivadoEm { get; protected set; }
    public virtual Guid? ArquivadoPorUsuarioId { get; protected set; }

    // Soft delete — separado de "arquivar". Arquivar é estado UX (anexo oculto, blob mantido);
    // deletado é estado LGPD (acionado por exclusão de prontuário/paciente, audit trail obrigatório).
    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    public virtual bool EstaArquivado => ArquivadoEm.HasValue;

    protected ProntuarioAnexo() { }

    public static ProntuarioAnexo Registrar(
        long prontuarioId,
        long estabelecimentoId,
        long? evolucaoId,
        string storagePath,
        string nomeOriginal,
        string mimeType,
        long tamanhoBytes,
        Guid criadoPorUsuarioId)
    {
        if (prontuarioId <= 0)
            throw new BusinessException("Prontuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new BusinessException("Caminho do Storage é obrigatório.");
        if (string.IsNullOrWhiteSpace(nomeOriginal))
            throw new BusinessException("Nome original é obrigatório.");
        if (tamanhoBytes <= 0)
            throw new BusinessException("Tamanho do arquivo inválido.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Autor é obrigatório.");

        return new ProntuarioAnexo
        {
            ProntuarioId = prontuarioId,
            EstabelecimentoId = estabelecimentoId,
            EvolucaoId = evolucaoId,
            StoragePath = storagePath,
            NomeOriginal = nomeOriginal.Trim(),
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType,
            TamanhoBytes = tamanhoBytes,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Arquivar(Guid porUsuarioId)
    {
        if (EstaArquivado)
            throw new BusinessException("Anexo já está arquivado.");
        ArquivadoEm = DateTime.UtcNow;
        ArquivadoPorUsuarioId = porUsuarioId;
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Anexo já está deletado.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }
}
