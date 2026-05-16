using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class OrcamentoTeamRole : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Papel { get; protected set; } = string.Empty;
    public virtual Guid? ProfissionalUsuarioId { get; protected set; }
    public virtual string? NomePadrao { get; protected set; }
    public virtual TipoHonorarioTeamRole TipoHonorario { get; protected set; }
    public virtual decimal Valor { get; protected set; }
    public virtual string BaseCalculo { get; protected set; } = string.Empty;
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected OrcamentoTeamRole() { }

    public static OrcamentoTeamRole Criar(long estabelecimentoId, string papel,
        Guid? profissionalUsuarioId, string? nomePadrao,
        TipoHonorarioTeamRole tipoHonorario, decimal valor, string baseCalculo)
    {
        Validar(estabelecimentoId, papel, tipoHonorario, valor, baseCalculo);
        return new OrcamentoTeamRole
        {
            EstabelecimentoId = estabelecimentoId,
            Papel = papel.Trim(),
            ProfissionalUsuarioId = profissionalUsuarioId,
            NomePadrao = N(nomePadrao),
            TipoHonorario = tipoHonorario,
            Valor = valor,
            BaseCalculo = baseCalculo.Trim(),
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string papel, Guid? profissionalUsuarioId, string? nomePadrao,
        TipoHonorarioTeamRole tipoHonorario, decimal valor, string baseCalculo)
    {
        Validar(EstabelecimentoId, papel, tipoHonorario, valor, baseCalculo);
        Papel = papel.Trim();
        ProfissionalUsuarioId = profissionalUsuarioId;
        NomePadrao = N(nomePadrao);
        TipoHonorario = tipoHonorario;
        Valor = valor;
        BaseCalculo = baseCalculo.Trim();
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static void Validar(long estab, string papel, TipoHonorarioTeamRole tipo, decimal valor, string baseCalculo)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(papel)) throw new BusinessException("Papel é obrigatório.");
        if (papel.Trim().Length > 80) throw new BusinessException("Papel não pode ter mais de 80 caracteres.");
        if (valor < 0) throw new BusinessException("Valor não pode ser negativo.");
        if (tipo == TipoHonorarioTeamRole.Percentual && valor > 100m)
            throw new BusinessException("Percentual não pode ser maior que 100%.");
        if (string.IsNullOrWhiteSpace(baseCalculo)) throw new BusinessException("Base de cálculo é obrigatória.");
        if (baseCalculo.Trim().Length > 40) throw new BusinessException("Base de cálculo não pode ter mais de 40 caracteres.");
    }
}
