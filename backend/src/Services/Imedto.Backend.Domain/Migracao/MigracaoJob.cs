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

    /// <summary>
    /// Identifica a onda de carga do job.
    /// Null = Onda 1 (pacientes, estoque, agenda, orçamento).
    /// "prontuario" = Onda 2 (CA13 — bloqueado até Onda 1 concluir).
    /// </summary>
    public virtual string? Onda { get; protected set; }

    // ─── Onda constants ──────────────────────────────────────────────────────────
    public const string OndaProntuario = "prontuario";

    /// <summary>
    /// Categoria genérica da falha (R-B2 addendum 002). Nunca contém PII.
    /// Preenchido quando <see cref="Status"/> == <see cref="StatusFalhou"/>.
    /// </summary>
    public virtual string? MotivoFalha { get; protected set; }

    /// <summary>
    /// Status imediatamente anterior à falha (R-B3/D-B3 addendum 002).
    /// Usado pelo Reprocessar para saber em qual estado recolocar o job.
    /// </summary>
    public virtual string? StatusAntesFalha { get; protected set; }

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

    /// <summary>
    /// Job falhou — motivo visível ao operador, sem PII (addendum 002 — CA25/CA26).
    /// O recorrente não re-seleciona jobs neste estado até o operador reprocessar.
    /// </summary>
    public const string StatusFalhou              = "falhou";

    // ─── Factory ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria um novo job de migração no estado <see cref="StatusAguardandoArquivo"/>.
    /// O arquivo ainda não foi enviado — o cliente faz o upload em seguida.
    /// </summary>
    public static MigracaoJob Criar(
        long estabelecimentoId,
        Guid criadoPorUsuarioId,
        string? origem = null,
        string? onda = null)
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
            Onda = string.IsNullOrWhiteSpace(onda) ? null : onda.Trim(),
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

    public virtual void MarcarPreviewPronto(Guid adminId)
    {
        if (Status != StatusMapaEmRevisao)
            throw new BusinessException("Job precisa estar em revisão para gerar preview.");
        if (adminId == Guid.Empty)
            throw new BusinessException("Admin é obrigatório.");
        DisparadoPorUsuarioId = adminId;
        Status = StatusPreviewPronto;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void MarcarMigrando(Guid adminId)
    {
        if (Status != StatusPreviewPronto)
            throw new BusinessException("Job precisa estar com preview pronto para migrar.");
        if (adminId == Guid.Empty)
            throw new BusinessException("Admin é obrigatório.");
        DisparadoPorUsuarioId = adminId;
        Status = StatusMigrando;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void MarcarConcluido()
    {
        if (Status != StatusMigrando)
            throw new BusinessException("Job precisa estar migrando para concluir.");
        Status = StatusConcluido;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void MarcarConcluidoComErros()
    {
        if (Status != StatusMigrando)
            throw new BusinessException("Job precisa estar migrando para concluir com erros.");
        Status = StatusConcluidoComErros;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o job como desfeito após rollback dos registros criados (CA17, R9, D12).
    /// Transição: concluido | concluido_com_erros → desfeito.
    /// </summary>
    public virtual void MarcarDesfeito()
    {
        if (Status != StatusConcluido && Status != StatusConcluidoComErros)
            throw new BusinessException("Apenas jobs concluídos podem ser desfeitos.");
        Status = StatusDesfeito;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Transição para <see cref="StatusFalhou"/> com motivo categórico genérico (sem PII).
    /// Válido apenas a partir de <see cref="StatusAguardandoMapa"/> ou <see cref="StatusMigrando"/>.
    /// Guarda o status atual em <see cref="StatusAntesFalha"/> para o reprocessar saber de onde retomar.
    /// O recorrente para de re-selecionar o job até que <see cref="Reprocessar"/> seja chamado (D-B4).
    /// (addendum 002 — R-B1, CA25, CA26)
    /// </summary>
    /// <param name="motivo">Categoria legível PT-BR, sem PII (R-B2). Ex.: "IA não configurada".</param>
    public virtual void MarcarFalhou(string motivo)
    {
        if (Status != StatusAguardandoMapa && Status != StatusMigrando)
            throw new BusinessException("Job só pode falhar a partir de 'aguardando_mapa' ou 'migrando'.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo da falha é obrigatório.");

        StatusAntesFalha = Status;
        Status = StatusFalhou;
        MotivoFalha = motivo.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Retoma o job de <see cref="StatusFalhou"/> para o estado anterior (R-B4, D-B3 addendum 002).
    /// O recorrente correspondente ao <see cref="StatusAntesFalha"/> reprocessa automaticamente.
    /// Somente jobs em <see cref="StatusFalhou"/> podem ser reprocessados.
    /// A carga é idempotente — apenas registros <c>pendente</c> serão reprocessados (CA30).
    /// </summary>
    public virtual void Reprocessar()
    {
        if (Status != StatusFalhou)
            throw new BusinessException("Apenas jobs que falharam podem ser reprocessados.");
        if (string.IsNullOrWhiteSpace(StatusAntesFalha))
            throw new BusinessException("Status anterior não registrado — não é possível reprocessar.");

        Status = StatusAntesFalha;
        StatusAntesFalha = null;
        MotivoFalha = null;
        AtualizadoEm = DateTime.UtcNow;
    }
}
