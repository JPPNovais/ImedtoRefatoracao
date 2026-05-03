using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

/// <summary>
/// LGPD Art. 18 — direito à portabilidade. Retorna todos os dados pessoais
/// do paciente + metadados de tratamento (cadastro/atualizacao/exclusao/anonimizacao).
/// Fases futuras agregarao prontuarios/agenda/financeiro neste mesmo blob.
/// </summary>
public class ExportarDadosPacienteQuery : IQuery<PacienteExportLgpdDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: registrar quem disparou o export.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
}
