using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

/// <summary>
/// LGPD Art. 18 — direito à portabilidade. Retorna todos os dados pessoais
/// do paciente + referências futuras (prontuários, agenda) quando essas fases
/// forem implementadas.
/// </summary>
public class ExportarDadosPacienteQuery : IQuery<object>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
}
