using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario.Cadastros;

/// <summary>
/// Fornecedor (PJ) do estoque: distribuidor, representante, laboratório que
/// emite a NF de compra. CNPJ é validado com dígito verificador.
/// </summary>
public class FornecedorEstoque : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string RazaoSocial { get; protected set; } = string.Empty;
    public virtual string? NomeFantasia { get; protected set; }
    /// <summary>CNPJ apenas dígitos (14 chars). Pode ser null para fornecedor sem PJ formal.</summary>
    public virtual string? Cnpj { get; protected set; }
    public virtual string? ContatoNome { get; protected set; }
    public virtual string? ContatoTelefone { get; protected set; }
    public virtual string? ContatoEmail { get; protected set; }
    public virtual int PrazoEntregaDias { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected FornecedorEstoque() { }

    public static FornecedorEstoque Criar(
        long estabelecimentoId,
        string razaoSocial,
        string? nomeFantasia,
        string? cnpj,
        string? contatoNome,
        string? contatoTelefone,
        string? contatoEmail,
        int prazoEntregaDias)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        ValidarRazaoSocial(razaoSocial);
        ValidarNomeFantasia(nomeFantasia);
        var cnpjNormalizado = NormalizarECnpjValido(cnpj);
        ValidarContatoNome(contatoNome);
        ValidarContatoTelefone(contatoTelefone);
        ValidarContatoEmail(contatoEmail);
        ValidarPrazo(prazoEntregaDias);

        return new FornecedorEstoque
        {
            EstabelecimentoId = estabelecimentoId,
            RazaoSocial = razaoSocial.Trim(),
            NomeFantasia = string.IsNullOrWhiteSpace(nomeFantasia) ? null : nomeFantasia.Trim(),
            Cnpj = cnpjNormalizado,
            ContatoNome = string.IsNullOrWhiteSpace(contatoNome) ? null : contatoNome.Trim(),
            ContatoTelefone = string.IsNullOrWhiteSpace(contatoTelefone) ? null : contatoTelefone.Trim(),
            ContatoEmail = string.IsNullOrWhiteSpace(contatoEmail) ? null : contatoEmail.Trim().ToLowerInvariant(),
            PrazoEntregaDias = prazoEntregaDias,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        string razaoSocial,
        string? nomeFantasia,
        string? cnpj,
        string? contatoNome,
        string? contatoTelefone,
        string? contatoEmail,
        int prazoEntregaDias)
    {
        if (!Ativo) throw new BusinessException("Fornecedor inativo não pode ser alterado.");
        ValidarRazaoSocial(razaoSocial);
        ValidarNomeFantasia(nomeFantasia);
        var cnpjNormalizado = NormalizarECnpjValido(cnpj);
        ValidarContatoNome(contatoNome);
        ValidarContatoTelefone(contatoTelefone);
        ValidarContatoEmail(contatoEmail);
        ValidarPrazo(prazoEntregaDias);

        RazaoSocial = razaoSocial.Trim();
        NomeFantasia = string.IsNullOrWhiteSpace(nomeFantasia) ? null : nomeFantasia.Trim();
        Cnpj = cnpjNormalizado;
        ContatoNome = string.IsNullOrWhiteSpace(contatoNome) ? null : contatoNome.Trim();
        ContatoTelefone = string.IsNullOrWhiteSpace(contatoTelefone) ? null : contatoTelefone.Trim();
        ContatoEmail = string.IsNullOrWhiteSpace(contatoEmail) ? null : contatoEmail.Trim().ToLowerInvariant();
        PrazoEntregaDias = prazoEntregaDias;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Fornecedor já está inativo.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Fornecedor já está ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void ValidarRazaoSocial(string razao)
    {
        if (string.IsNullOrWhiteSpace(razao))
            throw new BusinessException("Razão social é obrigatória.");
        if (razao.Trim().Length > 200)
            throw new BusinessException("Razão social deve ter no máximo 200 caracteres.");
    }

    private static void ValidarNomeFantasia(string? nome)
    {
        if (!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length > 150)
            throw new BusinessException("Nome fantasia deve ter no máximo 150 caracteres.");
    }

    private static string? NormalizarECnpjValido(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var normalizado = CnpjValidator.Normalizar(cnpj);
        if (normalizado is null || normalizado.Length != 14)
            throw new BusinessException("CNPJ deve conter 14 dígitos.");
        if (!CnpjValidator.EhValido(normalizado))
            throw new BusinessException("CNPJ inválido (dígitos verificadores não conferem).");
        return normalizado;
    }

    private static void ValidarContatoNome(string? nome)
    {
        if (!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length > 150)
            throw new BusinessException("Nome do contato deve ter no máximo 150 caracteres.");
    }

    private static void ValidarContatoTelefone(string? tel)
    {
        if (!string.IsNullOrWhiteSpace(tel) && tel.Trim().Length > 40)
            throw new BusinessException("Telefone do contato deve ter no máximo 40 caracteres.");
    }

    private static void ValidarContatoEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        var trimmed = email.Trim();
        if (trimmed.Length > 200)
            throw new BusinessException("E-mail deve ter no máximo 200 caracteres.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed,
                @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            throw new BusinessException("E-mail do contato é inválido.");
    }

    private static void ValidarPrazo(int prazoDias)
    {
        if (prazoDias < 0)
            throw new BusinessException("Prazo de entrega não pode ser negativo.");
        if (prazoDias > 365)
            throw new BusinessException("Prazo de entrega não pode passar de 365 dias.");
    }
}
