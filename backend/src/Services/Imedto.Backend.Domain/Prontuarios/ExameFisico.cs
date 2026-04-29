using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Aggregate root — exame físico realizado durante uma evolução.
///
/// Cada exame guarda dados gerais antropométricos/sinais vitais (<see cref="DadosGeraisJson"/>),
/// observações livres e uma coleção de achados por região anatômica
/// (<see cref="Regioes"/>). Vinculado obrigatoriamente a uma evolução do prontuário —
/// não existe exame físico avulso, fora do contexto clínico.
///
/// Soft delete (LGPD): exame físico é dado clínico sensível, append-only após finalizar.
/// </summary>
public class ExameFisico : Entity, ISoftDeletable
{
    public virtual long EvolucaoId { get; protected set; }
    public virtual long ProntuarioId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual DateTime RealizadoEm { get; protected set; }
    public virtual Guid RealizadoPorUsuarioId { get; protected set; }
    public virtual string? DadosGeraisJson { get; protected set; }
    public virtual string? ObservacoesGerais { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    private readonly List<RegiaoExameFisico> _regioes = new();
    public virtual IReadOnlyCollection<RegiaoExameFisico> Regioes => _regioes.AsReadOnly();

    protected ExameFisico() { }

    /// <summary>Tupla de input ao registrar uma região.</summary>
    public record RegiaoInput(
        string Codigo,
        string? PaiCodigo,
        Lateralidade Lateralidade,
        string? Achados,
        SeveridadeExame? Severidade,
        int Ordem);

    public static ExameFisico Registrar(
        long evolucaoId,
        long prontuarioId,
        long pacienteId,
        long estabelecimentoId,
        Guid realizadoPorUsuarioId,
        DateTime? realizadoEm,
        string? dadosGeraisJson,
        string? observacoesGerais,
        IEnumerable<RegiaoInput> regioes)
    {
        if (evolucaoId <= 0)
            throw new BusinessException("Evolução é obrigatória.");
        if (prontuarioId <= 0)
            throw new BusinessException("Prontuário é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (realizadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional responsável é obrigatório.");
        if (observacoesGerais is not null && observacoesGerais.Length > 2000)
            throw new BusinessException("Observações excedem 2000 caracteres.");

        var agora = DateTime.UtcNow;
        var realizado = realizadoEm ?? agora;
        if (realizado > agora.AddMinutes(5))
            throw new BusinessException("Data de realização não pode ser futura.");

        var exame = new ExameFisico
        {
            EvolucaoId = evolucaoId,
            ProntuarioId = prontuarioId,
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            RealizadoPorUsuarioId = realizadoPorUsuarioId,
            RealizadoEm = realizado,
            DadosGeraisJson = string.IsNullOrWhiteSpace(dadosGeraisJson) ? null : dadosGeraisJson,
            ObservacoesGerais = string.IsNullOrWhiteSpace(observacoesGerais) ? null : observacoesGerais.Trim(),
            CriadoEm = agora
        };

        foreach (var r in regioes ?? Enumerable.Empty<RegiaoInput>())
            exame.AdicionarRegiaoInterno(r);

        return exame;
    }

    public virtual void AtualizarDadosGerais(string? dadosGeraisJson, string? observacoesGerais)
    {
        if (DeletadoEm is not null)
            throw new BusinessException("Exame físico deletado não pode ser alterado.");
        if (observacoesGerais is not null && observacoesGerais.Length > 2000)
            throw new BusinessException("Observações excedem 2000 caracteres.");

        DadosGeraisJson = string.IsNullOrWhiteSpace(dadosGeraisJson) ? null : dadosGeraisJson;
        ObservacoesGerais = string.IsNullOrWhiteSpace(observacoesGerais) ? null : observacoesGerais.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AdicionarRegiao(RegiaoInput input)
    {
        if (DeletadoEm is not null)
            throw new BusinessException("Exame físico deletado não pode ser alterado.");

        AdicionarRegiaoInterno(input);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AtualizarRegiao(string codigo, string? achados, SeveridadeExame? severidade, Lateralidade lateralidade)
    {
        if (DeletadoEm is not null)
            throw new BusinessException("Exame físico deletado não pode ser alterado.");

        var regiao = _regioes.FirstOrDefault(r => string.Equals(r.RegiaoCodigo, codigo, StringComparison.OrdinalIgnoreCase))
            ?? throw new BusinessException("Região não encontrada neste exame físico.");

        regiao.Atualizar(achados, severidade, lateralidade);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void RemoverRegiao(string codigo)
    {
        if (DeletadoEm is not null)
            throw new BusinessException("Exame físico deletado não pode ser alterado.");

        var regiao = _regioes.FirstOrDefault(r => string.Equals(r.RegiaoCodigo, codigo, StringComparison.OrdinalIgnoreCase))
            ?? throw new BusinessException("Região não encontrada neste exame físico.");

        _regioes.Remove(regiao);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Exame físico já está deletado.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }

    private void AdicionarRegiaoInterno(RegiaoInput input)
    {
        if (input is null) throw new BusinessException("Região inválida.");

        // Unicidade por código — a constraint do banco é defense-in-depth, validar aqui dá 422 cedo.
        if (_regioes.Any(r => string.Equals(r.RegiaoCodigo, input.Codigo, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException($"Região '{input.Codigo}' já foi adicionada a este exame.");

        _regioes.Add(RegiaoExameFisico.Criar(
            input.Codigo,
            input.PaiCodigo,
            input.Lateralidade,
            input.Achados,
            input.Severidade,
            input.Ordem));
    }
}
