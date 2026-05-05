using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Queries;

public class ConsultarDisponibilidadeQuery : IQuery<DisponibilidadeSemanaDto>
{
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    /// <summary>
    /// Duracao desejada do agendamento em minutos. Quando null, usa a duracao
    /// padrao do estabelecimento. Permite que o cliente regenere os slots
    /// conforme o usuario muda a duracao no formulario.
    /// </summary>
    public int? DuracaoMinutos { get; set; }
}
