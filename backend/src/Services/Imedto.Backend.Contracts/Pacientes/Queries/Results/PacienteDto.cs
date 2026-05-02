namespace Imedto.Backend.Contracts.Pacientes.Queries.Results;

public class PacienteDto
{
    public long Id { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Genero { get; set; }
    public string Telefone { get; set; }
    public string Email { get; set; }
    public string Endereco { get; set; }
    // Mantido apesar de nao ser exibido no detalhe: o form de edicao precisa do
    // valor existente para round-trip (PUT envia campo de volta intacto se nao
    // alterado). Considerar separar em PacienteEdicaoDto numa proxima iteracao.
    public string Observacoes { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class PacienteListaItemDto
{
    public long Id { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Telefone { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class PaginaPacientesDto
{
    public IEnumerable<PacienteListaItemDto> Itens { get; set; } = Array.Empty<PacienteListaItemDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
