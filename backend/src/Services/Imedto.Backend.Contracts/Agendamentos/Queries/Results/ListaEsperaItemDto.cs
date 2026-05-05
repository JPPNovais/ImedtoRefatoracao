namespace Imedto.Backend.Contracts.Agendamentos.Queries.Results;

public class PaginaListaEsperaDto
{
    public IEnumerable<ListaEsperaItemDto> Itens { get; set; } = Array.Empty<ListaEsperaItemDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class ListaEsperaItemDto
{
    public long Id { get; set; }
    public long PacienteId { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public string? PacienteTelefone { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public Guid? ProfissionalPreferidoId { get; set; }
    public string? ProfissionalPreferidoNome { get; set; }
    /// <summary>Rotina | Prioritario | Urgente.</summary>
    public string Prioridade { get; set; } = "Rotina";
    /// <summary>Qualquer | Manha | Tarde.</summary>
    public string PreferenciaPeriodo { get; set; } = "Qualquer";
    public DateTime CriadoEm { get; set; }
    /// <summary>Tempo desde criação em minutos — usado pelo front pra mostrar "há 2h".</summary>
    public int MinutosDesdeQueEntrou { get; set; }
}
