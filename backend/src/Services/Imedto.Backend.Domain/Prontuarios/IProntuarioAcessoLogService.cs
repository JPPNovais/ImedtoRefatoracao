namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Serviço de auditoria — registrado automaticamente pelos handlers de prontuário.
/// Abstraído como serviço (não apenas repository) porque pode virar async fire-and-forget
/// no futuro (ex.: enviar para um coletor externo de logs LGPD).
/// </summary>
public interface IProntuarioAcessoLogService
{
    Task RegistrarAsync(long prontuarioId, Guid usuarioId, long estabelecimentoId, TipoAcessoProntuario tipoAcesso);
}
