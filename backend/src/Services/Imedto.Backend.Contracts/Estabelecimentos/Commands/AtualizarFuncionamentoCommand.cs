using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Estabelecimentos.Commands;

public class AtualizarFuncionamentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public TimeOnly HorarioInicio { get; set; }
    public TimeOnly HorarioFim { get; set; }
    public IReadOnlyList<int> DiasSemana { get; set; } = Array.Empty<int>();
    public IReadOnlyList<HorarioBloqueadoInput> HorariosBloqueados { get; set; } = Array.Empty<HorarioBloqueadoInput>();
    public IReadOnlyList<DataBloqueadaInput> DatasBloqueadas { get; set; } = Array.Empty<DataBloqueadaInput>();
}

public record HorarioBloqueadoInput(Guid? Id, TimeOnly Inicio, TimeOnly Fim, string Descricao);

public record DataBloqueadaInput(Guid? Id, DateOnly Data, string Descricao);
