using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Unidades;

/// <summary>
/// Unidade física do estabelecimento (matriz/filial). Cada estabelecimento pode ter várias
/// unidades; exatamente uma deve ser marcada como <see cref="IsPrincipal"/>.
/// </summary>
public class UnidadeEstabelecimento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; }
    public virtual bool IsPrincipal { get; protected set; }

    // Endereço (campos opcionais, mantidos flat para refletir o legado).
    public virtual string Cep { get; protected set; }
    public virtual string Logradouro { get; protected set; }
    public virtual string Numero { get; protected set; }
    public virtual string Complemento { get; protected set; }
    public virtual string Bairro { get; protected set; }
    public virtual string Cidade { get; protected set; }
    public virtual string Estado { get; protected set; }
    public virtual string Telefone { get; protected set; }

    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected UnidadeEstabelecimento() { }

    public static UnidadeEstabelecimento Criar(
        long estabelecimentoId,
        string nome,
        bool isPrincipal,
        EnderecoUnidadeInput endereco,
        string telefone)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da unidade é obrigatório.");

        return new UnidadeEstabelecimento
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            IsPrincipal = isPrincipal,
            Cep = NormalizarCep(endereco.Cep),
            Logradouro = Trim(endereco.Logradouro),
            Numero = Trim(endereco.Numero),
            Complemento = Trim(endereco.Complemento),
            Bairro = Trim(endereco.Bairro),
            Cidade = Trim(endereco.Cidade),
            Estado = NormalizarUf(endereco.Estado),
            Telefone = string.IsNullOrWhiteSpace(telefone) ? null : SomenteDigitos(telefone),
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        };
    }

    public virtual void AtualizarDados(
        string nome,
        EnderecoUnidadeInput endereco,
        string telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da unidade é obrigatório.");

        Nome = nome.Trim();
        Cep = NormalizarCep(endereco.Cep);
        Logradouro = Trim(endereco.Logradouro);
        Numero = Trim(endereco.Numero);
        Complemento = Trim(endereco.Complemento);
        Bairro = Trim(endereco.Bairro);
        Cidade = Trim(endereco.Cidade);
        Estado = NormalizarUf(endereco.Estado);
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : SomenteDigitos(telefone);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void MarcarComoPrincipal()
    {
        if (IsPrincipal) return;
        IsPrincipal = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void RemoverFlagPrincipal()
    {
        if (!IsPrincipal) return;
        IsPrincipal = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static string Trim(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string NormalizarCep(string cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return null;
        var d = SomenteDigitos(cep);
        if (d.Length != 0 && d.Length != 8)
            throw new BusinessException("CEP deve conter 8 dígitos.");
        return d.Length == 0 ? null : d;
    }

    private static string NormalizarUf(string uf)
    {
        if (string.IsNullOrWhiteSpace(uf)) return null;
        var t = uf.Trim().ToUpperInvariant();
        if (t.Length != 2)
            throw new BusinessException("UF deve conter 2 caracteres.");
        return t;
    }

    private static string SomenteDigitos(string s) => new(s.Where(char.IsDigit).ToArray());
}

/// <summary>Bag de campos de endereço (não é value object — só agrupa parâmetros).</summary>
public record EnderecoUnidadeInput(
    string Cep,
    string Logradouro,
    string Numero,
    string Complemento,
    string Bairro,
    string Cidade,
    string Estado);
