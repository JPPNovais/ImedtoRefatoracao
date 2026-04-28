using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Registro de auditoria LGPD — toda leitura e escrita de dados de prontuário
/// (contendo dados pessoais sensíveis de saúde, Art. 5º II da LGPD) é logada
/// aqui para permitir rastreabilidade de acessos.
///
/// Append-only — não há métodos de alteração.
/// </summary>
public class ProntuarioAcessoLog : Entity
{
    public virtual long ProntuarioId { get; protected set; }
    public virtual Guid UsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual TipoAcessoProntuario TipoAcesso { get; protected set; }
    public virtual DateTime OcorridoEm { get; protected set; }

    protected ProntuarioAcessoLog() { }

    public static ProntuarioAcessoLog Registrar(
        long prontuarioId,
        Guid usuarioId,
        long estabelecimentoId,
        TipoAcessoProntuario tipoAcesso) =>
        new()
        {
            ProntuarioId = prontuarioId,
            UsuarioId = usuarioId,
            EstabelecimentoId = estabelecimentoId,
            TipoAcesso = tipoAcesso,
            OcorridoEm = DateTime.UtcNow
        };
}
