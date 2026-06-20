using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Commands;

/// <summary>
/// Atualização parcial dos dados básicos de identificação do paciente — consumida pelo app
/// mobile (edição rápida). Campos null = manter valor atual. Não mexe em genero, endereco,
/// observacoes, tags, alertas, consentimento WhatsApp nem documentoInternacional.
/// </summary>
public class AtualizarDadosBasicosPacienteCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: registrar quem editou os dados do paciente.</summary>
    public Guid SolicitanteUsuarioId { get; set; }

    /// <summary>Null = manter nome atual.</summary>
    public string NomeCompleto { get; set; }
    /// <summary>Null = manter telefone atual.</summary>
    public string Telefone { get; set; }
    /// <summary>Null = manter e-mail atual.</summary>
    public string Email { get; set; }
    /// <summary>Null = manter data de nascimento atual.</summary>
    public DateTime? DataNascimento { get; set; }
    /// <summary>
    /// Null = manter CPF atual. String vazia ("") = remover CPF (limpar o campo).
    /// O CPF não pode coexistir com DocumentoInternacional já preenchido no cadastro.
    /// </summary>
    public string Cpf { get; set; }

    /// <summary>
    /// Quando true, indica que DataNascimento foi explicitamente enviada no request
    /// (inclusive como null para limpar). Quando false, campo DataNascimento é ignorado.
    /// </summary>
    public bool DataNascimentoFoiEnviada { get; set; }

    /// <summary>
    /// Quando true, indica que Cpf foi explicitamente enviado no request
    /// (inclusive como null/vazio para limpar). Quando false, campo Cpf é ignorado.
    /// </summary>
    public bool CpfFoiEnviado { get; set; }
}
