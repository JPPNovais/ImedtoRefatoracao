using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario.Cadastros;

/// <summary>
/// Local físico onde o estoque é guardado: "Armário azul - Sala 2", "Geladeira -
/// Recepção", "Cofre - Anestésicos controlados".
/// </summary>
public class LocalEstoque : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual TipoLocalEstoque Tipo { get; protected set; }
    public virtual string? AndarSetor { get; protected set; }
    public virtual string? Responsavel { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected LocalEstoque() { }

    public static LocalEstoque Criar(
        long estabelecimentoId,
        string nome,
        TipoLocalEstoque tipo,
        string? andarSetor,
        string? responsavel)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        ValidarNome(nome);
        ValidarTexto(andarSetor, "Andar/Setor", 80);
        ValidarTexto(responsavel, "Responsável", 150);

        return new LocalEstoque
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Tipo = tipo,
            AndarSetor = string.IsNullOrWhiteSpace(andarSetor) ? null : andarSetor.Trim(),
            Responsavel = string.IsNullOrWhiteSpace(responsavel) ? null : responsavel.Trim(),
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        string nome,
        TipoLocalEstoque tipo,
        string? andarSetor,
        string? responsavel)
    {
        if (!Ativo) throw new BusinessException("Local inativo não pode ser alterado.");
        ValidarNome(nome);
        ValidarTexto(andarSetor, "Andar/Setor", 80);
        ValidarTexto(responsavel, "Responsável", 150);

        Nome = nome.Trim();
        Tipo = tipo;
        AndarSetor = string.IsNullOrWhiteSpace(andarSetor) ? null : andarSetor.Trim();
        Responsavel = string.IsNullOrWhiteSpace(responsavel) ? null : responsavel.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Local já está inativo.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Local já está ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do local é obrigatório.");
        if (nome.Trim().Length > 120)
            throw new BusinessException("Nome do local deve ter no máximo 120 caracteres.");
    }

    private static void ValidarTexto(string? valor, string campo, int max)
    {
        if (!string.IsNullOrWhiteSpace(valor) && valor.Trim().Length > max)
            throw new BusinessException($"{campo} deve ter no máximo {max} caracteres.");
    }
}
