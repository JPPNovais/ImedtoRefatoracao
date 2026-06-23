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
    /// Null = não alterar o opt-in existente; true/false = gravar consentimento com audit.
    /// </summary>
    public bool? WhatsappLembreteOptIn { get; set; }

    // Responsável (R3/R7 briefing 2026-06-23_002). PII de terceiro — não expor em lista/busca rápida.
    public string ResponsavelNome { get; set; }
    public string ResponsavelParentesco { get; set; }
    public string ResponsavelTelefone { get; set; }
}
