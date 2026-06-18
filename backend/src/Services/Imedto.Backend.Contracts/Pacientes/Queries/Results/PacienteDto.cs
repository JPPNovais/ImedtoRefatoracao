using System.Text.Json.Serialization;
using Imedto.Backend.SharedKernel.Json;

namespace Imedto.Backend.Contracts.Pacientes.Queries.Results;

public class PacienteDto
{
    public long Id { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public string DocumentoInternacional { get; set; }

    /// <summary>Data civil (sem hora/timezone). Serializada como "yyyy-MM-dd".</summary>
    [JsonConverter(typeof(DateOnlyAsYmdJsonConverter))]
    public DateTime? DataNascimento { get; set; }
    public string Genero { get; set; }
    public string Telefone { get; set; }
    public string Email { get; set; }
    public string Endereco { get; set; }
    // Mantido apesar de nao ser exibido no detalhe: o form de edicao precisa do
    // valor existente para round-trip (PUT envia campo de volta intacto se nao
    // alterado). Considerar separar em PacienteEdicaoDto numa proxima iteracao.
    public string Observacoes { get; set; }

    /// <summary>Tags clínicas/operacionais (chaves curtas, ex: <c>"vip"</c>, <c>"gestante"</c>).</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Alertas clínicos críticos (ex: alergias graves, comorbidades). Exibidos em
    /// destaque vermelho no detalhe do paciente.
    /// </summary>
    public string[] Alertas { get; set; } = Array.Empty<string>();

    public DateTime CriadoEm { get; set; }

    /// <summary>
    /// Consentimento do paciente para receber lembretes via WhatsApp (LGPD — R4).
    /// Exibido no form de edição para permitir marcar/desmarcar.
    /// </summary>
    public bool WhatsappLembreteOptIn { get; set; }
}

public class PacienteListaItemDto
{
    public long Id { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public string DocumentoInternacional { get; set; }

    /// <summary>Data civil (sem hora/timezone). Serializada como "yyyy-MM-dd".</summary>
    [JsonConverter(typeof(DateOnlyAsYmdJsonConverter))]
    public DateTime? DataNascimento { get; set; }
    public string Telefone { get; set; }
    public DateTime CriadoEm { get; set; }

    /// <summary>Tags clínicas para filtros e badges visuais na lista.</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>Quantidade de alertas clínicos do paciente (badge vermelho na lista).</summary>
    public int QtdAlertas { get; set; }
}

public class PaginaPacientesDto
{
    public IEnumerable<PacienteListaItemDto> Itens { get; set; } = Array.Empty<PacienteListaItemDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

/// <summary>
/// DTO minimizado para autocomplete (LGPD): apenas <c>id</c> + <c>nomeCompleto</c>.
/// Usado em seletores (novo agendamento, novo orçamento, etc.) onde a tela só
/// exibe o nome — sem CPF, telefone, data nascimento.
/// </summary>
public class PacienteBuscaRapidaDto
{
    public long Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
}
