namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>DTOs internos para leitura de configuração de funcionamento — usados apenas na camada de infra.</summary>

public class ConfiguracaoFuncionamentoDto
{
    public TimeOnly HorarioInicio { get; set; }
    public TimeOnly HorarioFim { get; set; }
    public List<int> DiasSemanaFuncionamento { get; set; } = new();
    public List<HorarioBloqueadoDispo> HorariosBloqueados { get; set; } = new();
    public List<DataBloqueadaDispo> DatasBloqueadas { get; set; } = new();
}

public record HorarioBloqueadoDispo(TimeOnly Inicio, TimeOnly Fim, string Descricao);

public record DataBloqueadaDispo(DateOnly Data, string Descricao);
