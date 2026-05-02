namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

/// <summary>
/// DTO completo do exame físico (com regiões) — usado nas telas de visualização e edição.
/// LGPD: campos minimizados — não retorna estabelecimento_id/paciente_id em listagens
/// que já são escopadas por contexto da tela; eles aparecem aqui porque o consumidor
/// (drawer no prontuário) precisa para navegação.
/// </summary>
public class ExameFisicoDto
{
    public long Id { get; set; }
    public long EvolucaoId { get; set; }
    // ProntuarioId/PacienteId removidos (LGPD): redundantes com a rota.
    // RealizadoPorUsuarioId removido (LGPD): Guid de auth interno permite enumeracao.
    public DateTime RealizadoEm { get; set; }
    public string? RealizadoPorNome { get; set; }
    public string? DadosGeraisJson { get; set; }
    public string? ObservacoesGerais { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public IEnumerable<RegiaoExameFisicoDto> Regioes { get; set; } = Array.Empty<RegiaoExameFisicoDto>();
}

public class RegiaoExameFisicoDto
{
    public long Id { get; set; }
    public string RegiaoCodigo { get; set; } = string.Empty;
    public string? RegiaoPaiCodigo { get; set; }
    public string? Lateralidade { get; set; }
    public string? Achados { get; set; }
    public string? Severidade { get; set; }
    public int Ordem { get; set; }
}

/// <summary>
/// Item resumido para listas paginadas e timeline (bem mais leve — sem regiões).
/// </summary>
public class ExameFisicoResumoDto
{
    public long Id { get; set; }
    public long EvolucaoId { get; set; }
    public DateTime RealizadoEm { get; set; }
    public string? RealizadoPorNome { get; set; }
    public int TotalRegioes { get; set; }
    public bool TemDadosGerais { get; set; }
    public string? SeveridadeMaxima { get; set; }
}

public class PaginaExamesFisicosDto
{
    public IEnumerable<ExameFisicoResumoDto> Itens { get; set; } = Array.Empty<ExameFisicoResumoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamanho { get; set; }
}
