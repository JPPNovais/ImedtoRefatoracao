using System.Text.Json;

namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

public class ProntuarioDto
{
    public long Id { get; set; }
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long ModeloDeProntuarioId { get; set; }
    public string ModeloNome { get; set; }
    public JsonElement ModeloEstrutura { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class EvolucaoDto
{
    public long Id { get; set; }
    public long ProntuarioId { get; set; }
    public Guid AutorUsuarioId { get; set; }
    public string AutorNome { get; set; }
    public string ModeloNome { get; set; }
    public JsonElement Conteudo { get; set; }
    public JsonElement ModeloSnapshot { get; set; }
    public long ModeloDeProntuarioIdOrigem { get; set; }
    public DateTime CriadaEm { get; set; }
}

public class ProntuarioCompletoDto
{
    public ProntuarioDto Prontuario { get; set; }
    public IEnumerable<EvolucaoDto> Evolucoes { get; set; } = Array.Empty<EvolucaoDto>();

    /// <summary>
    /// Alertas clínicos do paciente — gated por papel/vínculo de atendimento (R2/R5 LGPD).
    /// Retornado apenas quando o solicitante é Dono, ou atendeu/está atendendo o paciente.
    /// Para quem não tem direito, o campo retorna vazio — indistinguível de "sem alertas".
    /// </summary>
    public string[] Alertas { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Indica se o solicitante pode gerir (adicionar/remover) alertas clínicos (CA12).
    /// True apenas para Dono (sempre) ou Profissional com vínculo de atendimento verificado.
    /// Derivado do mesmo predicado de gating que decide o preenchimento de <see cref="Alertas"/>.
    /// </summary>
    public bool PodeGerirAlertas { get; set; }
}

public class PaginaEvolucoesDto
{
    public IEnumerable<EvolucaoDto> Itens { get; set; } = Array.Empty<EvolucaoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
