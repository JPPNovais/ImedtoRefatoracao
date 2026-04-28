namespace Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;

public class EstabelecimentoDto
{
    public long Id { get; set; }
    public Guid DonoUsuarioId { get; set; }
    public string NomeFantasia { get; set; }
    public string RazaoSocial { get; set; }
    public string Cnpj { get; set; }
    public string Telefone { get; set; }
    public string Endereco { get; set; }
    public string FotoUrl { get; set; }
    public string Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public string PapelDoUsuario { get; set; }

    // Funcionamento.
    public TimeOnly HorarioInicio { get; set; }
    public TimeOnly HorarioFim { get; set; }
    public IReadOnlyList<int> DiasSemanaFuncionamento { get; set; } = Array.Empty<int>();
    public IReadOnlyList<HorarioBloqueadoDto> HorariosBloqueados { get; set; } = Array.Empty<HorarioBloqueadoDto>();
    public IReadOnlyList<DataBloqueadaDto> DatasBloqueadas { get; set; } = Array.Empty<DataBloqueadaDto>();
}

public record HorarioBloqueadoDto(Guid Id, TimeOnly Inicio, TimeOnly Fim, string Descricao);

public record DataBloqueadaDto(Guid Id, DateOnly Data, string Descricao);
