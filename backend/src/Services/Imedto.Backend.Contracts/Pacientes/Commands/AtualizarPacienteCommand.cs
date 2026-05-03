using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Commands;

public class AtualizarPacienteCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: registrar quem editou os dados do paciente.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Genero { get; set; }
    public string Telefone { get; set; }
    public string Email { get; set; }
    public string Endereco { get; set; }
    public string Observacoes { get; set; }
}
