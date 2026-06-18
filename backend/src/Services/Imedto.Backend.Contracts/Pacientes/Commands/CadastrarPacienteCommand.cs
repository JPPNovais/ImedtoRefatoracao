using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Commands;

public class CadastrarPacienteCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public string DocumentoInternacional { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Genero { get; set; }
    public string Telefone { get; set; }
    public string Email { get; set; }
    public string Endereco { get; set; }
    public string Observacoes { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Alertas { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Consentimento explícito do paciente para receber lembretes via WhatsApp (LGPD — R4).
    /// Opcional no cadastro: null = false (sem consentimento no cadastro).
    /// </summary>
    public bool? WhatsappLembreteOptIn { get; set; }

    /// <summary>Audit LGPD: quem registrou o consentimento no cadastro.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
}
