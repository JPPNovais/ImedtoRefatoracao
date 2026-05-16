using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class OrcamentoAnestesista : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual Guid? ProfissionalUsuarioId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? Crm { get; protected set; }
    public virtual string? Especialidade { get; protected set; }
    public virtual string? Telefone { get; protected set; }
    public virtual string? TabelaHonorarios { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    private readonly List<OrcamentoAnestesistaFaixa> _faixas = new();
    public virtual IReadOnlyCollection<OrcamentoAnestesistaFaixa> Faixas => _faixas.AsReadOnly();

    protected OrcamentoAnestesista() { }

    public static OrcamentoAnestesista Criar(long estabelecimentoId, string nome,
        Guid? profissionalUsuarioId = null, string? crm = null, string? especialidade = null,
        string? telefone = null, string? tabelaHonorarios = null)
    {
        Validar(estabelecimentoId, nome, crm, especialidade, telefone, tabelaHonorarios);
        return new OrcamentoAnestesista
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            ProfissionalUsuarioId = profissionalUsuarioId,
            Crm = N(crm), Especialidade = N(especialidade),
            Telefone = N(telefone), TabelaHonorarios = N(tabelaHonorarios),
            Ativo = true, CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, Guid? profissionalUsuarioId, string? crm,
        string? especialidade, string? telefone, string? tabelaHonorarios)
    {
        Validar(EstabelecimentoId, nome, crm, especialidade, telefone, tabelaHonorarios);
        Nome = nome.Trim();
        ProfissionalUsuarioId = profissionalUsuarioId;
        Crm = N(crm);
        Especialidade = N(especialidade);
        Telefone = N(telefone);
        TabelaHonorarios = N(tabelaHonorarios);
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    public virtual void SincronizarFaixas(IEnumerable<(string Descricao, decimal Valor)> faixasNovas)
    {
        var lista = faixasNovas?.ToList() ?? new();
        var descricoes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in lista)
        {
            var desc = (f.Descricao ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(desc))
                throw new BusinessException("Descrição da faixa é obrigatória.");
            if (!descricoes.Add(desc))
                throw new BusinessException($"Faixa duplicada: '{desc}'.");
        }

        _faixas.Clear();
        var ordem = 0;
        foreach (var f in lista)
        {
            _faixas.Add(OrcamentoAnestesistaFaixa.Criar(f.Descricao, f.Valor, ordem));
            ordem++;
        }
        AtualizadaEm = DateTime.UtcNow;
    }

    private static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static void Validar(long estab, string nome, string? crm, string? especialidade,
        string? telefone, string? tabela)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome)) throw new BusinessException("Nome do anestesista é obrigatório.");
        if (nome.Trim().Length > 200) throw new BusinessException("Nome não pode ter mais de 200 caracteres.");
        if (crm is not null && crm.Trim().Length > 40)
            throw new BusinessException("CRM não pode ter mais de 40 caracteres.");
        if (especialidade is not null && especialidade.Trim().Length > 120)
            throw new BusinessException("Especialidade não pode ter mais de 120 caracteres.");
        if (telefone is not null && telefone.Trim().Length > 40)
            throw new BusinessException("Telefone não pode ter mais de 40 caracteres.");
        if (tabela is not null && tabela.Trim().Length > 80)
            throw new BusinessException("Tabela de honorários não pode ter mais de 80 caracteres.");
    }
}
