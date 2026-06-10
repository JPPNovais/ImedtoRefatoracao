namespace Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;

public class EstabelecimentoDto
{
    public long Id { get; set; }
    // DonoUsuarioId removido (LGPD — minimizacao). O front diferencia Dono x
    // Profissional via PapelDoUsuario; nao ha nenhum cenario de UI que precise
    // do Guid auth interno do dono. Manter este campo no DTO vazava o id do
    // proprietario para qualquer membro do tenant (e ainda saia mascarado como
    // Guid.Empty quando o solicitante nao era Dono — "valor morto" sem motivo).
    public string NomeFantasia { get; set; }
    public string RazaoSocial { get; set; }
    public string Cnpj { get; set; }
    public string Telefone { get; set; }
    public string Endereco { get; set; }
    /// <summary>Cidade onde o estabelecimento opera. Pode ser null (legado pré-feature de Termos).</summary>
    public string Cidade { get; set; }
    /// <summary>UF do estabelecimento (2 letras). Pode ser null.</summary>
    public string Estado { get; set; }
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
    /// Toggle de exigência de 2FA para o papel Dono (R9/CA13).
    /// Exposto no DTO para que o bootstrap possa computar a flag deveConfigurar2fa.
    /// </summary>
    public bool ExigirDono2fa { get; set; }

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
