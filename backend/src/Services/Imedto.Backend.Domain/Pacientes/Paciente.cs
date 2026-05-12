using Imedto.Backend.Domain.Pacientes.Events;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Text;

namespace Imedto.Backend.Domain.Pacientes;

/// <summary>
/// Aggregate root de Paciente. Sempre escopado a 1 estabelecimento — o mesmo CPF
/// pode aparecer em estabelecimentos diferentes (registros independentes) mas nunca
/// duas vezes no mesmo. Dados sensíveis de saúde → LGPD Art. 5º II.
/// Documento: CPF (com validação de dígito verificador) ou DocumentoInternacional
/// (passaporte/RNE/etc) — nunca os dois ao mesmo tempo.
/// </summary>
public class Paciente : Entity, ISoftDeletable
{
    public const int DocumentoInternacionalMaxLen = 30;
    public const int TagMaxLen = 40;
    public const int AlertaMaxLen = 200;
    public const int TagsMaxCount = 10;
    public const int AlertasMaxCount = 10;

    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string NomeCompleto { get; protected set; }
    public virtual string Cpf { get; protected set; }
    public virtual string DocumentoInternacional { get; protected set; }
    public virtual DateTime? DataNascimento { get; protected set; }
    public virtual GeneroPaciente Genero { get; protected set; }
    public virtual string Telefone { get; protected set; }
    public virtual string Email { get; protected set; }
    public virtual string Endereco { get; protected set; }
    public virtual string Observacoes { get; protected set; }

    /// <summary>
    /// Tags clínicas/operacionais (chaves curtas, ex: <c>"vip"</c>, <c>"gestante"</c>, <c>"cronico"</c>).
    /// Usadas para filtros e badges visuais. Catálogo definido pelo frontend.
    /// </summary>
    public virtual IReadOnlyList<string> Tags { get; protected set; } = Array.Empty<string>();

    /// <summary>
    /// Alertas clínicos críticos exibidos em destaque no detalhe do paciente
    /// (ex: <c>"Alergia grave a penicilina"</c>, <c>"Diabetes Tipo 1"</c>).
    /// LGPD: armazenam dado de saúde — restritos por papel/permissão de prontuário.
    /// </summary>
    public virtual IReadOnlyList<string> Alertas { get; protected set; } = Array.Empty<string>();

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
        string observacoes,
        string documentoInternacional = null,
        IReadOnlyList<string> tags = null,
        IReadOnlyList<string> alertas = null)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new BusinessException("Nome do paciente é obrigatório.");

        var (cpfNormalizado, docInternacionalNormalizado) = NormalizarDocumentos(cpf, documentoInternacional);

        if (dataNascimento.HasValue && dataNascimento.Value > DateTime.UtcNow.Date)
            throw new BusinessException("Data de nascimento não pode estar no futuro.");

        return new Paciente
        {
            EstabelecimentoId = estabelecimentoId,
            NomeCompleto = nomeCompleto.Trim(),
            Cpf = cpfNormalizado,
            DocumentoInternacional = docInternacionalNormalizado,
            DataNascimento = dataNascimento,
            Genero = genero,
            Telefone = SanitizeOpt(telefone, digitsOnly: true),
            Email = SanitizeOpt(email, digitsOnly: false)?.ToLowerInvariant(),
            Endereco = SanitizeOpt(endereco, digitsOnly: false),
            Observacoes = SanitizeOpt(observacoes, digitsOnly: false),
            Tags = NormalizarLista(tags, TagMaxLen, TagsMaxCount, "Tags"),
            Alertas = NormalizarLista(alertas, AlertaMaxLen, AlertasMaxCount, "Alertas"),
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
        string observacoes,
        string documentoInternacional = null,
        IReadOnlyList<string> tags = null,
        IReadOnlyList<string> alertas = null)
    {
        if (EstaDeletado)
            throw new BusinessException("Paciente deletado não pode ser editado.");
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new BusinessException("Nome do paciente é obrigatório.");

        var (cpfNormalizado, docInternacionalNormalizado) = NormalizarDocumentos(cpf, documentoInternacional);

        if (dataNascimento.HasValue && dataNascimento.Value > DateTime.UtcNow.Date)
            throw new BusinessException("Data de nascimento não pode estar no futuro.");

        NomeCompleto = nomeCompleto.Trim();
        Cpf = cpfNormalizado;
        DocumentoInternacional = docInternacionalNormalizado;
        DataNascimento = dataNascimento;
        Genero = genero;
        Telefone = SanitizeOpt(telefone, digitsOnly: true);
        Email = SanitizeOpt(email, digitsOnly: false)?.ToLowerInvariant();
        Endereco = SanitizeOpt(endereco, digitsOnly: false);
        Observacoes = SanitizeOpt(observacoes, digitsOnly: false);
        Tags = NormalizarLista(tags, TagMaxLen, TagsMaxCount, "Tags");
        Alertas = NormalizarLista(alertas, AlertaMaxLen, AlertasMaxCount, "Alertas");
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
        DocumentoInternacional = null;
        Email = null;
        Telefone = null;
        DataNascimento = null;
        Endereco = null;
        Observacoes = null;
        Tags = Array.Empty<string>();
        Alertas = Array.Empty<string>();

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

    /// <summary>
    /// Normaliza CPF e DocumentoInternacional, garantindo que apenas um esteja
    /// preenchido. CPF passa por validação de dígito verificador.
    /// </summary>
    private static (string cpf, string docInternacional) NormalizarDocumentos(
        string cpf,
        string documentoInternacional)
    {
        var cpfPreenchido = !string.IsNullOrWhiteSpace(cpf);
        var docPreenchido = !string.IsNullOrWhiteSpace(documentoInternacional);

        if (cpfPreenchido && docPreenchido)
            throw new BusinessException("Informe apenas um documento: CPF ou documento internacional.");

        string cpfNormalizado = null;
        if (cpfPreenchido)
        {
            var razao = CpfValidator.RazaoInvalidez(cpf);
            if (razao is not null)
                throw new BusinessException(razao);
            cpfNormalizado = TextSanitizer.SomenteDigitos(cpf);
        }

        string docNormalizado = null;
        if (docPreenchido)
        {
            docNormalizado = documentoInternacional.Trim();
            if (docNormalizado.Length > DocumentoInternacionalMaxLen)
                throw new BusinessException($"Documento internacional excede {DocumentoInternacionalMaxLen} caracteres.");
        }

        return (cpfNormalizado, docNormalizado);
    }

    private static string SanitizeOpt(string valor, bool digitsOnly) =>
        digitsOnly ? TextSanitizer.DigitosOuNulo(valor) : TextSanitizer.TrimOuNulo(valor);

    /// <summary>
    /// Normaliza uma lista de strings: trim, remove vazios e duplicados (case-insensitive),
    /// valida tamanho máximo de cada item e limite de itens. Retorna lista vazia para null.
    /// </summary>
    private static IReadOnlyList<string> NormalizarLista(
        IReadOnlyList<string> origem,
        int tamanhoItemMax,
        int quantidadeMax,
        string nomeCampo)
    {
        if (origem is null || origem.Count == 0) return Array.Empty<string>();

        var visto = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultado = new List<string>(origem.Count);
        foreach (var raw in origem)
        {
            var trim = raw?.Trim();
            if (string.IsNullOrEmpty(trim)) continue;
            if (trim.Length > tamanhoItemMax)
                throw new BusinessException($"{nomeCampo}: cada item deve ter até {tamanhoItemMax} caracteres.");
            if (visto.Add(trim)) resultado.Add(trim);
        }

        if (resultado.Count > quantidadeMax)
            throw new BusinessException($"{nomeCampo}: máximo de {quantidadeMax} itens.");

        return resultado;
    }
}
