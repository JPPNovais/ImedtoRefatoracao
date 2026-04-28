using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Commands;

public class CadastrarPacienteCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Genero { get; set; }
    public string Telefone { get; set; }
    public string Email { get; set; }
    public string Endereco { get; set; }
    public string Observacoes { get; set; }
}
