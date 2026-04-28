using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

public class Orcamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual string Numero { get; protected set; } = string.Empty;
    public virtual OrcamentoStatus Status { get; protected set; }
    public virtual DateOnly Validade { get; protected set; }
    public virtual string? Observacoes { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    public virtual List<ItemOrcamento> Itens { get; protected set; } = new();
    public decimal Total => Itens.Sum(i => i.Subtotal);

    protected Orcamento() { }

    public static Orcamento Criar(
        long estabelecimentoId,
        long pacienteId,
        DateOnly validade,
        string? observacoes,
        Guid criadoPorUsuarioId,
        IEnumerable<ItemPayload> itens)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Validade não pode ser uma data passada.");

        var itensList = itens.ToList();
        if (itensList.Count == 0)
            throw new BusinessException("O orçamento deve ter ao menos um item.");

        var orc = new Orcamento
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Numero = string.Empty,
            Status = OrcamentoStatus.Pendente,
            Validade = validade,
            Observacoes = observacoes?.Trim(),
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow
        };

        foreach (var item in itensList)
            orc.Itens.Add(ItemOrcamento.Criar(0, item.Descricao, item.Quantidade, item.ValorUnitario, item.DescontoPercent));

        return orc;
    }

    public void DefinirNumero()
        => Numero = $"ORC-{CriadoEm:yyyyMM}-{Id:D4}";

    public void Aprovar()
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser aprovados.");
        if (Validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Orçamento expirado não pode ser aprovado.");

        Status = OrcamentoStatus.Aprovado;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrcamentoAprovadoEvent(Id, EstabelecimentoId, PacienteId, Total));
    }

    public void Recusar()
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser recusados.");
        Status = OrcamentoStatus.Recusado;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Expirar()
    {
        if (Status != OrcamentoStatus.Pendente)
            return;
        Status = OrcamentoStatus.Expirado;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Atualizar(
        DateOnly validade,
        string? observacoes,
        IEnumerable<ItemPayload> itens)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        if (validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Validade não pode ser uma data passada.");

        var itensList = itens.ToList();
        if (itensList.Count == 0)
            throw new BusinessException("O orçamento deve ter ao menos um item.");

        Validade = validade;
        Observacoes = observacoes?.Trim();
        Itens.Clear();
        foreach (var item in itensList)
            Itens.Add(ItemOrcamento.Criar(Id, item.Descricao, item.Quantidade, item.ValorUnitario, item.DescontoPercent));

        AtualizadoEm = DateTime.UtcNow;
    }

    public record ItemPayload(string Descricao, decimal Quantidade, decimal ValorUnitario, decimal DescontoPercent);
}
