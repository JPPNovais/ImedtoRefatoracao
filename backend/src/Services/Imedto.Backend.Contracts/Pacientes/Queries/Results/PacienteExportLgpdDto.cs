namespace Imedto.Backend.Contracts.Pacientes.Queries.Results;

/// <summary>
/// Pacote do export LGPD Art. 18 (direito a portabilidade): contem TODA PII
/// do titular + metadados de tratamento (cadastro, atualizacao, soft-delete,
/// anonimizacao). Tipo forte (vs object anonimo) para documentar contrato no
/// Swagger e tipar o front quando ele consumir o blob.
/// </summary>
public class PacienteExportLgpdDto
{
    public DateTime ExportadoEm { get; set; }

    public PacienteExportPessoalDto Paciente { get; set; }

    // Reservado para fases futuras quando o export agregar dados ligados ao paciente.
    // Hoje so retorna os dados pessoais; prontuarios/agenda/financeiro entram nas suas fases.
    // public IEnumerable<ProntuarioExportDto> Prontuarios { get; set; }
    // public IEnumerable<AgendamentoExportDto> Agendamentos { get; set; }
    // public IEnumerable<LancamentoExportDto> Financeiro { get; set; }
}

public class PacienteExportPessoalDto
{
    public long Id { get; set; }
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public string DocumentoInternacional { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Genero { get; set; }
    public string Telefone { get; set; }
    public string Email { get; set; }
    public string Endereco { get; set; }
    public string Observacoes { get; set; }

    // Metadados LGPD obrigatorios no export — o titular tem direito de saber
    // quando e por quem cada operacao de tratamento ocorreu.
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? DeletadoEm { get; set; }
    public Guid? DeletadoPorUsuarioId { get; set; }
    public DateTime? AnonimizadoEm { get; set; }
    public Guid? AnonimizadoPorUsuarioId { get; set; }
}
