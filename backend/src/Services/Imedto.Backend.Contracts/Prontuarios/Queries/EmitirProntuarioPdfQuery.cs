using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Gera o PDF do histórico completo do prontuário de um paciente.
/// Retorna os bytes do arquivo PDF pronto para ser servido como download.
/// Multi-tenant: falha-fechada — prontuário de outro tenant retorna null → 422 genérico.
/// LGPD: registra audit de Exportacao no prontuario_acesso_log (best-effort).
/// </summary>
public class EmitirProntuarioPdfQuery : IQuery<byte[]>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
