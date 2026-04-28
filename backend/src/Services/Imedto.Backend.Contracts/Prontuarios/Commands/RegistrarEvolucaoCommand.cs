using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Registra nova evolução em um prontuário. <b>Append-only</b> — não há command de update/delete.
/// O <paramref name="ConteudoJson"/> é o valor preenchido das seções do modelo ativo no momento.
/// </summary>
public class RegistrarEvolucaoCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid AutorUsuarioId { get; set; }
    public string ConteudoJson { get; set; }
    /// <summary>
    /// Substitui o modelo do prontuário apenas para esta evolução.
    /// Deve ser nulo (usa modelo do prontuário) ou um modelo ativo acessível pelo estabelecimento.
    /// </summary>
    public long? ModeloDeProntuarioId { get; set; }
}
