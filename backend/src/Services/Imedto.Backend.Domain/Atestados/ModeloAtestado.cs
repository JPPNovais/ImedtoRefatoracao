using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Atestados;

/// <summary>
/// Modelo (template) de atestado salvo pelo profissional — quando emitir, o front
/// usa o modelo como ponto de partida (preenche tipo + conteudo). Sem soft delete:
/// modelos são metadados, não dado clínico do paciente.
/// </summary>
public class ModeloAtestado : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual TipoAtestado Tipo { get; protected set; }
    public virtual string Conteudo { get; protected set; } = string.Empty;
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ModeloAtestado() { }

    public static ModeloAtestado Criar(
        long estabelecimentoId,
        Guid profissionalUsuarioId,
        string nome,
        TipoAtestado tipo,
        string conteudo)
    {
        ValidarCampos(estabelecimentoId, profissionalUsuarioId, nome, conteudo);
        return new ModeloAtestado
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            Nome = nome.Trim(),
            Tipo = tipo,
            Conteudo = conteudo.Trim(),
            CriadoEm = DateTime.UtcNow,
        };
    }

    public virtual void Atualizar(string nome, TipoAtestado tipo, string conteudo)
    {
        ValidarCampos(EstabelecimentoId, ProfissionalUsuarioId, nome, conteudo);
        Nome = nome.Trim();
        Tipo = tipo;
        Conteudo = conteudo.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    private static void ValidarCampos(long estab, Guid usuario, string nome, string conteudo)
    {
        if (estab <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (usuario == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do modelo é obrigatório.");
        if (nome.Trim().Length > 120)
            throw new BusinessException("Nome do modelo excede 120 caracteres.");
        if (string.IsNullOrWhiteSpace(conteudo))
            throw new BusinessException("Conteúdo do modelo é obrigatório.");
        if (conteudo.Length > 4000)
            throw new BusinessException("Conteúdo excede 4000 caracteres.");
    }
}
