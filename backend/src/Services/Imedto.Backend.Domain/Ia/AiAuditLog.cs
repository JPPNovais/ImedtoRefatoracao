using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Ia;

/// <summary>
/// Registro de auditoria de cada chamada à IA (sucesso, erro, cache-hit).
///
/// LGPD: NUNCA armazena prompt ou resposta crus — apenas hash SHA256, para que
/// possamos correlacionar entradas idênticas / detectar padrões anômalos sem
/// preservar dado clínico fora do prontuário.
/// </summary>
public class AiAuditLog : Entity
{
    public virtual Guid UsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string PromptHash { get; protected set; } = string.Empty;
    public virtual string? ResponseHash { get; protected set; }
    public virtual int? TokensIn { get; protected set; }
    public virtual int? TokensOut { get; protected set; }
    public virtual string Modelo { get; protected set; } = string.Empty;
    public virtual string Endpoint { get; protected set; } = string.Empty;
    public virtual int? DuracaoMs { get; protected set; }
    public virtual bool Sucesso { get; protected set; }
    public virtual string? ErroMensagem { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    /// <summary>
    /// Item 2.13: FKs nullable para correlacionar a chamada com o conteúdo clínico
    /// que motivou (paciente/prontuário/evolução). Migration deve aplicar
    /// <c>ON DELETE SET NULL</c> — preserva trilha LGPD mesmo após exclusão do registro origem.
    /// </summary>
    public virtual long? PacienteId { get; protected set; }
    public virtual long? ProntuarioId { get; protected set; }
    public virtual long? EvolucaoId { get; protected set; }

    protected AiAuditLog() { }

    public static AiAuditLog Criar(
        Guid usuarioId,
        long estabelecimentoId,
        string promptHash,
        string? responseHash,
        string modelo,
        string endpoint,
        int? duracaoMs,
        bool sucesso,
        string? erroMensagem,
        int? tokensIn = null,
        int? tokensOut = null,
        long? pacienteId = null,
        long? prontuarioId = null,
        long? evolucaoId = null)
    {
        return new AiAuditLog
        {
            UsuarioId         = usuarioId,
            EstabelecimentoId = estabelecimentoId,
            PromptHash        = promptHash,
            ResponseHash      = responseHash,
            Modelo            = modelo,
            Endpoint          = endpoint,
            DuracaoMs         = duracaoMs,
            Sucesso           = sucesso,
            ErroMensagem      = Truncar(erroMensagem, 500),
            TokensIn          = tokensIn,
            TokensOut         = tokensOut,
            PacienteId        = pacienteId,
            ProntuarioId      = prontuarioId,
            EvolucaoId        = evolucaoId,
            CriadoEm          = DateTime.UtcNow
        };
    }

    private static string? Truncar(string? valor, int max)
    {
        if (string.IsNullOrEmpty(valor)) return valor;
        return valor.Length <= max ? valor : valor[..max];
    }
}
