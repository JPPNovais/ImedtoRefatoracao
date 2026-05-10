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
    public int DuracaoConsultaPadraoMinutos { get; set; } = 30;
    public int IntervaloEntreConsultasMinutos { get; set; } = 0;
    public IReadOnlyList<int> DiasSemanaFuncionamento { get; set; } = Array.Empty<int>();
    public IReadOnlyList<HorarioBloqueadoDto> HorariosBloqueados { get; set; } = Array.Empty<HorarioBloqueadoDto>();
    public IReadOnlyList<DataBloqueadaDto> DatasBloqueadas { get; set; } = Array.Empty<DataBloqueadaDto>();

    /// <summary>
    /// Permissões granulares do usuário neste estabelecimento (formato "area.acao").
    /// Vazio para Dono — Dono tem todas. Vazio para Profissional sem modelo atribuído.
    /// </summary>
    public IReadOnlyList<string> Permissoes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Permissões finas (extras) do modelo do vínculo. Vazio para Dono — Dono tem todas.
    /// </summary>
    public IReadOnlyList<string> PermissoesExtras { get; set; } = Array.Empty<string>();
}

public record HorarioBloqueadoDto(Guid Id, TimeOnly Inicio, TimeOnly Fim, string Descricao);

public record DataBloqueadaDto(Guid Id, DateOnly Data, string Descricao);
