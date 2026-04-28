using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Item pré-cadastrado (de sistema ou do estabelecimento) que pode ser referenciado nas
/// seções do prontuário — ex.: "Dipirona", "Hipertensão", "Apendicectomia".
///
/// Segue o mesmo escopo-duplo do <see cref="ModeloDeProntuario"/>:
/// - Padrão-sistema (<see cref="EstabelecimentoId"/> null) → aparece para todos;
/// - Do estabelecimento (<see cref="EstabelecimentoId"/> NOT NULL) → só para o dono.
/// </summary>
public class ProntuarioVariavelPool : Entity
{
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual TipoVariavelPool Tipo { get; protected set; }
    public virtual string Nome { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual bool EhPadraoSistema { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ProntuarioVariavelPool() { }

    public static ProntuarioVariavelPool CriarDoEstabelecimento(
        long estabelecimentoId,
        TipoVariavelPool tipo,
        string nome)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        Validar(nome);

        return new ProntuarioVariavelPool
        {
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            Nome = nome.Trim(),
            Ativo = true,
            EhPadraoSistema = false,
            CriadoEm = DateTime.UtcNow
        };
    }

    public static ProntuarioVariavelPool CriarPadraoSistema(TipoVariavelPool tipo, string nome)
    {
        Validar(nome);
        return new ProntuarioVariavelPool
        {
            EstabelecimentoId = null,
            Tipo = tipo,
            Nome = nome.Trim(),
            Ativo = true,
            EhPadraoSistema = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Renomear(string nome)
    {
        Validar(nome);
        Nome = nome.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Item já inativo.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Item já ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void Validar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome é obrigatório.");
    }
}
