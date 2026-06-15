using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Aggregate root — job de migração de dados de terceiros (Central de Migração — briefing 2026-06-15_001).
///
/// Multi-tenant: todo acesso filtra por <see cref="EstabelecimentoId"/> (CA2).
/// Arquivo bruto no S3 com retenção de 30 dias (CA24, R12).
/// Status auditado a cada transição (CA20).
/// </summary>
public class MigracaoJob : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }

    /// <summary>
    /// Status do job — valores válidos:
    /// aguardando_arquivo, aguardando_mapa, mapa_em_revisao, preview_pronto,
    /// migrando, concluido, concluido_com_erros, desfeito, rejeitado
    /// </summary>
    public virtual string Status { get; protected set; } = string.Empty;

    /// <summary>Nome do sistema de origem (ex.: "iClinic", "Feegow").</summary>
    public virtual string? Origem { get; protected set; }

    /// <summary>Key do ZIP no S3.</summary>
    public virtual string? ArquivoS3Key { get; protected set; }

    /// <summary>Data de expiração do arquivo no S3 (criado_em + 30 dias).</summary>
    public virtual DateTime? ArquivoExpiraEm { get; protected set; }

    /// <summary>True quando o job de expiração apagou o arquivo do S3.</summary>
    public virtual bool ArquivoExpirado { get; protected set; }

    /// <summary>Momento em que o cliente aceitou o termo de responsabilidade.</summary>
    public virtual DateTime? TermoAceitoEm { get; protected set; }

    /// <summary>Template de mapeamento usado como ponto de partida. Null = sem template.</summary>
    public virtual long? TemplateOrigemId { get; protected set; }

    /// <summary>Usuário que criou o job.</summary>
    public virtual Guid CriadoPorUsuarioId { get; protected set; }

    /// <summary>Usuário admin que disparou a execução. Null = job ainda não foi disparado.</summary>
    public virtual Guid? DisparadoPorUsuarioId { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime AtualizadoEm { get; protected set; }

    protected MigracaoJob() { }

    // ─── Status constants ────────────────────────────────────────────────────────
    public const string StatusAguardandoArquivo   = "aguardando_arquivo";
    public const string StatusAguardandoMapa      = "aguardando_mapa";
    public const string StatusMapaEmRevisao       = "mapa_em_revisao";
    public const string StatusPreviewPronto       = "preview_pronto";
    public const string StatusMigrando            = "migrando";
    public const string StatusConcluido           = "concluido";
    public const string StatusConcluidoComErros   = "concluido_com_erros";
    public const string StatusDesfeito            = "desfeito";
    public const string StatusRejeitado           = "rejeitado";

    // ─── Factory ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria um novo job de migração no estado <see cref="StatusAguardandoArquivo"/>.
    /// O arquivo ainda não foi enviado — o cliente faz o upload em seguida.
    /// </summary>
    public static MigracaoJob Criar(
        long estabelecimentoId,
        Guid criadoPorUsuarioId,
        string? origem = null)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário criador é obrigatório.");

        var agora = DateTime.UtcNow;
        return new MigracaoJob
        {
            EstabelecimentoId = estabelecimentoId,
            Status = StatusAguardandoArquivo,
            Origem = string.IsNullOrWhiteSpace(origem) ? null : origem.Trim(),
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
    }

    // ─── Transições de estado ────────────────────────────────────────────────────

    /// <summary>
    /// Registra que o arquivo ZIP foi recebido, salvo no S3 e termo aceito.
    /// Transição: aguardando_arquivo → aguardando_mapa.
    /// </summary>
    public virtual void RegistrarArquivoRecebido(string arquivoS3Key)
    {
        if (Status != StatusAguardandoArquivo)
            throw new BusinessException("O job não está aguardando arquivo.");
        if (string.IsNullOrWhiteSpace(arquivoS3Key))
            throw new BusinessException("Chave do arquivo no S3 é obrigatória.");

        ArquivoS3Key = arquivoS3Key;
        ArquivoExpiraEm = DateTime.UtcNow.AddDays(30);  // R12 — retenção 30 dias (CA24)
        TermoAceitoEm = DateTime.UtcNow;                  // R12 — termo registrado no momento do upload
        Status = StatusAguardandoMapa;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejeita o job antes do processamento (ex.: arquivo > 50MB — CA19, R11).
    /// </summary>
    public virtual void Rejeitar()
    {
        if (Status != StatusAguardandoArquivo && Status != StatusAguardandoMapa)
            throw new BusinessException("Apenas jobs pendentes podem ser rejeitados.");

        Status = StatusRejeitado;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o arquivo S3 como expirado após o job de limpeza (CA24, R12).
    /// </summary>
    public virtual void MarcarArquivoExpirado()
    {
        ArquivoExpirado = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Transição: aguardando_mapa → mapa_em_revisao.
    /// Chamada pelo job de inferência quando a IA termina de gerar o mapa.
    /// </summary>
    public virtual void MarcarMapaEmRevisao()
    {
        if (Status != StatusAguardandoMapa)
            throw new BusinessException("Job não está aguardando mapa.");

        Status = StatusMapaEmRevisao;
        AtualizadoEm = DateTime.UtcNow;
    }
}
