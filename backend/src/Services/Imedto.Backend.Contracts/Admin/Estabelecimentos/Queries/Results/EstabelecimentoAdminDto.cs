namespace Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;

/// <summary>
/// Item da lista paginada de estabelecimentos na área admin.
/// LGPD: CPF do dono mascarado. Zero campo de paciente.
/// </summary>
public class EstabelecimentoAdminListaItemDto
{
    public long Id { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DonoNome { get; set; } = string.Empty;
    public string DonoEmail { get; set; } = string.Empty;
    /// <summary>CPF mascarado: "123.***.***-45". Reveal via endpoint dedicado.</summary>
    public string DonoCpfMascarado { get; set; } = string.Empty;
    public string PlanoNome { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
    public int TotalProfissionaisAtivos { get; set; }
    public int TotalPacientes { get; set; }
    public int AgendamentosNoMes { get; set; }
}

/// <summary>
/// Detalhe completo de estabelecimento. Apenas metadados — zero campo de paciente (LGPD §4).
/// CPF do dono mascarado; revelar via POST .../revelar-cpf-dono.
/// </summary>
public class EstabelecimentoAdminDetalheDto
{
    public long Id { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public DateTimeOffset CriadoEm { get; set; }

    // Dono
    public Guid DonoUsuarioId { get; set; }
    public string DonoNome { get; set; } = string.Empty;
    public string DonoEmail { get; set; } = string.Empty;
    /// <summary>CPF mascarado: "123.***.***-45".</summary>
    public string DonoCpfMascarado { get; set; } = string.Empty;

    // Plano/assinatura vigente
    public string PlanoNome { get; set; } = string.Empty;
    public bool AssinaturaGratuita { get; set; }
    public DateTimeOffset? AssinaturaDataFim { get; set; }

    // Contagens (sem dados de paciente — só metadados agregados)
    public int TotalProfissionaisAtivos { get; set; }
    public int TotalPacientes { get; set; }
    public int AgendamentosNoMes { get; set; }
    public int TotalProntuarios { get; set; }
}

/// <summary>Resultado paginado da listagem admin.</summary>
public class PaginaEstabelecimentosAdminDto
{
    public IEnumerable<EstabelecimentoAdminListaItemDto> Itens { get; set; } = Array.Empty<EstabelecimentoAdminListaItemDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

/// <summary>CPF completo revelado (CA17–CA19). Retornado apenas após audit.</summary>
public class CpfDonoReveladoDto
{
    public string Cpf { get; set; } = string.Empty;
}
