using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Pacientes;

/// <summary>
/// Registro de auditoria LGPD de acesso a dados de paciente.
///
/// Append-only — nao ha metodos de alteracao. Cada acesso de
/// leitura/edicao/exclusao/export/anonimizacao gera uma linha.
///
/// Uso minimo: ler quando paciente foi exportado, quem edita prontuario,
/// trilha de quem aprovou anonimizacao. Nao serve para reconstrucao
/// historica de dados (eh log de acessos, nao de mutacoes).
/// </summary>
public class PacienteAcessoLog : Entity
{
    public virtual long PacienteId { get; protected set; }
    public virtual Guid UsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual TipoAcessoPaciente TipoAcesso { get; protected set; }
    public virtual DateTime OcorridoEm { get; protected set; }

    /// <summary>IP do solicitante quando disponivel — null para jobs/eventos.</summary>
    public virtual string? IpOrigem { get; protected set; }

    protected PacienteAcessoLog() { }

    public static PacienteAcessoLog Registrar(
        long pacienteId,
        Guid usuarioId,
        long estabelecimentoId,
        TipoAcessoPaciente tipoAcesso,
        string? ipOrigem = null) =>
        new()
        {
            PacienteId = pacienteId,
            UsuarioId = usuarioId,
            EstabelecimentoId = estabelecimentoId,
            TipoAcesso = tipoAcesso,
            OcorridoEm = DateTime.UtcNow,
            IpOrigem = ipOrigem
        };
}
