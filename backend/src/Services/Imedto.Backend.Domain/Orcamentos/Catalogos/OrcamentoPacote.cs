using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class OrcamentoPacote : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? Descricao { get; protected set; }
    public virtual long? AnestesistaId { get; protected set; }
    public virtual decimal? ValorTotalSugerido { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    private readonly List<OrcamentoPacoteProcedimento> _procedimentos = new();
    private readonly List<OrcamentoPacoteProduto> _produtos = new();
    private readonly List<OrcamentoPacoteTeamRole> _teamRoles = new();

    public virtual IReadOnlyCollection<OrcamentoPacoteProcedimento> Procedimentos => _procedimentos.AsReadOnly();
    public virtual IReadOnlyCollection<OrcamentoPacoteProduto> Produtos => _produtos.AsReadOnly();
    public virtual IReadOnlyCollection<OrcamentoPacoteTeamRole> TeamRoles => _teamRoles.AsReadOnly();

    protected OrcamentoPacote() { }

    public static OrcamentoPacote Criar(long estabelecimentoId, string nome, string? descricao,
        long? anestesistaId, decimal? valorTotalSugerido)
    {
        Validar(estabelecimentoId, nome, descricao, valorTotalSugerido);
        return new OrcamentoPacote
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Descricao = N(descricao),
            AnestesistaId = anestesistaId,
            ValorTotalSugerido = valorTotalSugerido,
            Ativo = true, CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, string? descricao, long? anestesistaId, decimal? valorTotalSugerido)
    {
        Validar(EstabelecimentoId, nome, descricao, valorTotalSugerido);
        Nome = nome.Trim();
        Descricao = N(descricao);
        AnestesistaId = anestesistaId;
        ValorTotalSugerido = valorTotalSugerido;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    public virtual void Sincronizar(IEnumerable<long> procedimentoIds,
        IEnumerable<(long ProdutoId, decimal Quantidade)> produtos, IEnumerable<long> teamRoleIds)
    {
        var procs = procedimentoIds?.ToList() ?? new();
        var prods = produtos?.ToList() ?? new();
        var roles = teamRoleIds?.ToList() ?? new();

        if (procs.Distinct().Count() != procs.Count)
            throw new BusinessException("Procedimento duplicado no pacote.");
        if (prods.Select(p => p.ProdutoId).Distinct().Count() != prods.Count)
            throw new BusinessException("Produto duplicado no pacote.");
        if (roles.Distinct().Count() != roles.Count)
            throw new BusinessException("Papel de equipe duplicado no pacote.");

        _procedimentos.Clear();
        var i = 0;
        foreach (var pid in procs) _procedimentos.Add(OrcamentoPacoteProcedimento.Criar(pid, i++));

        _produtos.Clear();
        foreach (var p in prods) _produtos.Add(OrcamentoPacoteProduto.Criar(p.ProdutoId, p.Quantidade));

        _teamRoles.Clear();
        foreach (var rid in roles) _teamRoles.Add(OrcamentoPacoteTeamRole.Criar(rid));

        AtualizadaEm = DateTime.UtcNow;
    }

    private static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static void Validar(long estab, string nome, string? descricao, decimal? valor)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome)) throw new BusinessException("Nome do pacote é obrigatório.");
        if (nome.Trim().Length > 200) throw new BusinessException("Nome não pode ter mais de 200 caracteres.");
        if (descricao is not null && descricao.Trim().Length > 500)
            throw new BusinessException("Descrição não pode ter mais de 500 caracteres.");
        if (valor is { } v && v < 0) throw new BusinessException("Valor total sugerido não pode ser negativo.");
    }
}
