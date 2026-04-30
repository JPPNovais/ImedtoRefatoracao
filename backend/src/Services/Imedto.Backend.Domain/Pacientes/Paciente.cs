using Imedto.Backend.Domain.Pacientes.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Pacientes;

/// <summary>
/// Aggregate root de Paciente. Sempre escopado a 1 estabelecimento — o mesmo CPF
/// pode aparecer em estabelecimentos diferentes (registros independentes) mas nunca
/// duas vezes no mesmo. Dados sensíveis de saúde → LGPD Art. 5º II.
/// </summary>
public class Paciente : Entity, ISoftDeletable
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string NomeCompleto { get; protected set; }
    public virtual string Cpf { get; protected set; }
    public virtual DateTime? DataNascimento { get; protected set; }
    public virtual GeneroPaciente Genero { get; protected set; }
    public virtual string Telefone { get; protected set; }
    public virtual string Email { get; protected set; }
    public virtual string Endereco { get; protected set; }
    public virtual string Observacoes { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    // Soft delete — LGPD: mantém histórico mínimo por período legal.
    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    public virtual bool EstaDeletado => DeletadoEm.HasValue;

    // Anonimização LGPD (item 4.3) — independente do soft delete.
    public virtual DateTime? AnonimizadoEm { get; protected set; }
    public virtual Guid? AnonimizadoPorUsuarioId { get; protected set; }

    public virtual bool EstaAnonimizado => AnonimizadoEm.HasValue;

    protected Paciente() { }

    public static Paciente Cadastrar(
        long estabelecimentoId,
        string nomeCompleto,
        string cpf,
        DateTime? dataNascimento,
        GeneroPaciente genero,
        string telefone,
        string email,
        string endereco,
        string observacoes)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new BusinessException("Nome do paciente é obrigatório.");

        var cpfDigitos = string.IsNullOrWhiteSpace(cpf) ? null : SomenteDigitos(cpf);
        if (cpfDigitos is { Length: > 0 and not 11 })
            throw new BusinessException("CPF deve conter 11 dígitos.");

        if (dataNascimento.HasValue && dataNascimento.Value > DateTime.UtcNow.Date)
            throw new BusinessException("Data de nascimento não pode estar no futuro.");

        return new Paciente
        {
            EstabelecimentoId = estabelecimentoId,
            NomeCompleto = nomeCompleto.Trim(),
            Cpf = cpfDigitos,
            DataNascimento = dataNascimento,
            Genero = genero,
            Telefone = SanitizeOpt(telefone, digitsOnly: true),
            Email = SanitizeOpt(email, digitsOnly: false)?.ToLowerInvariant(),
            Endereco = SanitizeOpt(endereco, digitsOnly: false),
            Observacoes = SanitizeOpt(observacoes, digitsOnly: false),
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void MarcarComoCadastrado()
    {
        if (Id == 0)
            throw new InvalidOperationException("Paciente ainda não foi persistido — Id é 0.");

        AddDomainEvent(new PacienteCadastradoEvent(Id, EstabelecimentoId, NomeCompleto));
    }

    public virtual void AtualizarDados(
        string nomeCompleto,
        string cpf,
        DateTime? dataNascimento,
        GeneroPaciente genero,
        string telefone,
        string email,
        string endereco,
        string observacoes)
    {
        if (EstaDeletado)
            throw new BusinessException("Paciente deletado não pode ser editado.");
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new BusinessException("Nome do paciente é obrigatório.");

        var cpfDigitos = string.IsNullOrWhiteSpace(cpf) ? null : SomenteDigitos(cpf);
        if (cpfDigitos is { Length: > 0 and not 11 })
            throw new BusinessException("CPF deve conter 11 dígitos.");

        if (dataNascimento.HasValue && dataNascimento.Value > DateTime.UtcNow.Date)
            throw new BusinessException("Data de nascimento não pode estar no futuro.");

        NomeCompleto = nomeCompleto.Trim();
        Cpf = cpfDigitos;
        DataNascimento = dataNascimento;
        Genero = genero;
        Telefone = SanitizeOpt(telefone, digitsOnly: true);
        Email = SanitizeOpt(email, digitsOnly: false)?.ToLowerInvariant();
        Endereco = SanitizeOpt(endereco, digitsOnly: false);
        Observacoes = SanitizeOpt(observacoes, digitsOnly: false);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Anonimiza os campos PII do paciente substituindo-os por valores neutros.
    /// Idempotente: se já anonimizado, lança exceção clara em vez de silenciar.
    /// Pode estar deletado E anonimizado — fluxos ortogonais.
    /// </summary>
    public virtual void Anonimizar(Guid? usuarioId)
    {
        if (EstaAnonimizado)
            throw new BusinessException("Paciente já está anonimizado.");

        // LGPD: substitui PII. Nunca logar os valores originais.
        NomeCompleto = $"Paciente Anonimizado #{Id}";
        Cpf = null;
        Email = null;
        Telefone = null;
        DataNascimento = null;
        Endereco = null;
        Observacoes = null;

        AnonimizadoEm = DateTime.UtcNow;
        AnonimizadoPorUsuarioId = usuarioId;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Soft delete. Não remove do banco — marca com <see cref="DeletadoEm"/>.</summary>
    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (EstaDeletado)
            throw new BusinessException("Paciente já está deletado.");

        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }

    private static string SomenteDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());

    private static string SanitizeOpt(string valor, bool digitsOnly)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        return digitsOnly ? SomenteDigitos(valor) : valor.Trim();
    }
}
