using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class IniciarProntuarioCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>
    /// Null quando o caller (ex.: app mobile) não especifica modelo.
    /// O handler resolve o modelo padrão do estabelecimento antes de iniciar.
    /// </summary>
    public long? ModeloDeProntuarioId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
