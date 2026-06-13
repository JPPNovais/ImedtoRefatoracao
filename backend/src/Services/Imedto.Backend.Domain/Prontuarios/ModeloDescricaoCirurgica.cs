using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Modelo de texto reutilizável para a seção "Descrição cirúrgica" da evolução do prontuário.
///
/// Segue o mesmo escopo-duplo de <see cref="ProntuarioVariavelPool"/>:
/// - Padrão-sistema (<see cref="EstabelecimentoId"/> null) → aparece para todos os tenants;
/// - Do estabelecimento (<see cref="EstabelecimentoId"/> NOT NULL) → só para o dono.
///
/// Corpo de texto longo (sem limite de coluna) — texto puro, sem placeholders dinâmicos.
/// </summary>
public class ModeloDescricaoCirurgica : Entity
{
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual string Titulo { get; protected set; }
    public virtual string Corpo { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual bool EhPadraoSistema { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ModeloDescricaoCirurgica() { }

    public static ModeloDescricaoCirurgica CriarDoEstabelecimento(
        long estabelecimentoId,
        string titulo,
        string corpo)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        Validar(titulo, corpo);

        return new ModeloDescricaoCirurgica
        {
            EstabelecimentoId = estabelecimentoId,
            Titulo = titulo.Trim(),
            Corpo = corpo.Trim(),
            Ativo = true,
            EhPadraoSistema = false,
            CriadoEm = DateTime.UtcNow
        };
    }

    public static ModeloDescricaoCirurgica CriarPadraoSistema(string titulo, string corpo)
    {
        Validar(titulo, corpo);
        return new ModeloDescricaoCirurgica
        {
            EstabelecimentoId = null,
            Titulo = titulo.Trim(),
            Corpo = corpo.Trim(),
            Ativo = true,
            EhPadraoSistema = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Editar(string titulo, string corpo)
    {
        // Guarda explícita de profundidade (R3): padrão-sistema não é editável pelo tenant.
        // O repositório de escrita (ObterPorIdOuNulo) já impede que o tenant alcance
        // registros com EstabelecimentoId NULL, mas esta guarda permanece como defesa-em-profundidade.
        if (EhPadraoSistema)
            throw new BusinessException("Modelos padrão do sistema não podem ser alterados.");

        Validar(titulo, corpo);
        Titulo = titulo.Trim();
        Corpo = corpo.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Modelo já inativo.");
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Modelo já ativo.");
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void Validar(string titulo, string corpo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new BusinessException("Título é obrigatório.");
        if (titulo.Length > 200)
            throw new BusinessException("Título deve ter no máximo 200 caracteres.");
        if (string.IsNullOrWhiteSpace(corpo))
            throw new BusinessException("Corpo do modelo é obrigatório.");
    }
}
