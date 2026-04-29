using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Achado clínico em uma região anatômica, child de <see cref="ExameFisico"/>.
///
/// <see cref="RegiaoCodigo"/> é uma string estável (ex.: <c>"torax"</c>, <c>"abdomen-quad-superior-direito"</c>)
/// que casa com a primitiva visual no <c>BodyMapSvg</c> do front. Quando a região
/// é uma sub-região (nível 2+ no legado), <see cref="RegiaoPaiCodigo"/> aponta para
/// a região de nível 1 (preserva a hierarquia anatômica do legado).
///
/// <see cref="Lateralidade"/> é guardada apenas para regiões que admitem lateralidade
/// — <see cref="Domain.Prontuarios.Lateralidade.NaoAplicavel"/> em regiões medianas (epigástrio etc.).
/// </summary>
public class RegiaoExameFisico : Entity
{
    public virtual long ExameFisicoId { get; protected set; }
    public virtual string RegiaoCodigo { get; protected set; }
    public virtual string? RegiaoPaiCodigo { get; protected set; }
    public virtual Lateralidade Lateralidade { get; protected set; }
    public virtual string? Achados { get; protected set; }
    public virtual SeveridadeExame? Severidade { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected RegiaoExameFisico() { }

    internal static RegiaoExameFisico Criar(
        string regiaoCodigo,
        string? regiaoPaiCodigo,
        Lateralidade lateralidade,
        string? achados,
        SeveridadeExame? severidade,
        int ordem)
    {
        if (string.IsNullOrWhiteSpace(regiaoCodigo))
            throw new BusinessException("Código da região é obrigatório.");
        if (regiaoCodigo.Length > 60)
            throw new BusinessException("Código da região excede 60 caracteres.");
        if (regiaoPaiCodigo is not null && regiaoPaiCodigo.Length > 60)
            throw new BusinessException("Código da região-pai excede 60 caracteres.");
        if (achados is not null && achados.Length > 2000)
            throw new BusinessException("Achados excedem 2000 caracteres.");

        return new RegiaoExameFisico
        {
            RegiaoCodigo = regiaoCodigo.Trim(),
            RegiaoPaiCodigo = string.IsNullOrWhiteSpace(regiaoPaiCodigo) ? null : regiaoPaiCodigo.Trim(),
            Lateralidade = lateralidade,
            Achados = string.IsNullOrWhiteSpace(achados) ? null : achados.Trim(),
            Severidade = severidade,
            Ordem = ordem < 0 ? 0 : ordem
        };
    }

    internal void Atualizar(string? achados, SeveridadeExame? severidade, Lateralidade lateralidade)
    {
        if (achados is not null && achados.Length > 2000)
            throw new BusinessException("Achados excedem 2000 caracteres.");

        Achados = string.IsNullOrWhiteSpace(achados) ? null : achados.Trim();
        Severidade = severidade;
        Lateralidade = lateralidade;
    }
}
