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

    // Item 3.3.B — extensões para orçamento completo. Defaults preservam o orçamento
    // simples (Tipo=Simples, sem procedimento, collections vazias).
    public virtual TipoOrcamento Tipo { get; protected set; } = TipoOrcamento.Simples;
    public virtual long? ProcedimentoCirurgicoId { get; protected set; }
    /// <summary>
    /// Configuração extra de pagamento (item 7). Schema fechado (POCO) — substitui o
    /// antigo <c>config_pagamento_json</c> opaco. Persistido como jsonb via conversão no
    /// <c>OrcamentoConfiguration</c>. <c>null</c> quando o orçamento não tem regra extra
    /// (orçamento simples).
    /// </summary>
    public virtual ConfigPagamentoOrcamento? Configuracao { get; protected set; }
    public virtual decimal CustoImplantesTotal { get; protected set; }

    public virtual List<ItemOrcamento> Itens { get; protected set; } = new();
    public virtual List<OrcamentoEquipe> Equipe { get; protected set; } = new();
    public virtual List<OrcamentoImplante> Implantes { get; protected set; } = new();
    public virtual List<OrcamentoFormaPagamento> FormasPagamento { get; protected set; } = new();
    // Item 6 — paridade com legado: cirurgias múltiplas + internação 1:1 + anestesia 1:1.
    public virtual List<OrcamentoCirurgia> Cirurgias { get; protected set; } = new();
    public virtual OrcamentoInternacao? Internacao { get; protected set; }
    public virtual OrcamentoAnestesia? Anestesia { get; protected set; }

    /// <summary>
    /// Total bruto = soma de itens + total de implantes + comissões da equipe + cirurgias +
    /// internação + anestesia. Não aplica desconto/juros — para o total efetivo (que casa
    /// com formas de pagamento) use <see cref="CalcularTotalEfetivo"/>.
    ///
    /// Comissão da equipe entra como custo do orçamento (legado tratava como dedução
    /// interna apenas para relatórios — aqui mantemos no bruto, igual ao comportamento
    /// existente antes do item 6).
    /// </summary>
    public decimal Total =>
        Itens.Sum(i => i.Subtotal)
        + Implantes.Sum(i => i.CustoTotal)
        + Equipe.Sum(e => e.Valor)
        + Cirurgias.Sum(c => c.ValorTotal)
        + (Internacao?.ValorTotal ?? 0m)
        + (Anestesia?.Valor ?? 0m);

    protected Orcamento() { }

    /// <summary>
    /// Fábrica simples — preserva o comando legado <c>CriarOrcamentoCommand</c>. Tipo = Simples,
    /// sem equipe/implantes/formas/configuração de pagamento. Não chama
    /// <c>ValidarIntegridade</c> — orçamento simples não exige conferência de pagamento.
    /// </summary>
    public static Orcamento Criar(
        long estabelecimentoId,
        long pacienteId,
        DateOnly validade,
        string? observacoes,
        Guid criadoPorUsuarioId,
        IEnumerable<ItemPayload> itens)
    {
        ValidarBasico(estabelecimentoId, pacienteId, validade);

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
            CriadoEm = DateTime.UtcNow,
            Tipo = TipoOrcamento.Simples
        };

        foreach (var item in itensList)
            orc.Itens.Add(ItemOrcamento.Criar(0, item.Descricao, item.Quantidade, item.ValorUnitario, item.DescontoPercent));

        return orc;
    }

    /// <summary>
    /// Fábrica completa — orçamento cirúrgico ou simples-extendido. Aceita itens, equipe
    /// (com comissões), implantes, formas de pagamento, cirurgias múltiplas, internação,
    /// anestesia e referência opcional a um procedimento cirúrgico. Valida integridade
    /// (soma das formas == total efetivo).
    /// </summary>
    public static Orcamento CriarCompleto(
        long estabelecimentoId,
        long pacienteId,
        DateOnly validade,
        string? observacoes,
        Guid criadoPorUsuarioId,
        TipoOrcamento tipo,
        long? procedimentoCirurgicoId,
        ConfigPagamentoOrcamento? configuracao,
        decimal descontoBruto,
        decimal jurosBrutos,
        IEnumerable<ItemPayload> itens,
        IEnumerable<EquipePayload> equipe,
        IEnumerable<ImplantePayload> implantes,
        IEnumerable<FormaPagamentoPayload> formasPagamento,
        IEnumerable<CirurgiaPayload>? cirurgias = null,
        InternacaoPayload? internacao = null,
        AnestesiaPayload? anestesia = null)
    {
        ValidarBasico(estabelecimentoId, pacienteId, validade);
        if (descontoBruto < 0)
            throw new BusinessException("Desconto não pode ser negativo.");
        if (jurosBrutos < 0)
            throw new BusinessException("Juros não podem ser negativos.");

        var itensList = itens?.ToList() ?? new();
        var equipeList = equipe?.ToList() ?? new();
        var implantesList = implantes?.ToList() ?? new();
        var formasList = formasPagamento?.ToList() ?? new();
        var cirurgiasList = cirurgias?.ToList() ?? new();

        // Cirúrgico exige equipe, itens, implantes ou cirurgias (não pode ser vazio total).
        if (itensList.Count == 0 && equipeList.Count == 0 && implantesList.Count == 0 && cirurgiasList.Count == 0)
            throw new BusinessException("O orçamento deve ter ao menos um item, implante, cirurgia ou comissão de equipe.");

        var orc = new Orcamento
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Numero = string.Empty,
            Status = OrcamentoStatus.Pendente,
            Validade = validade,
            Observacoes = observacoes?.Trim(),
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow,
            Tipo = tipo,
            ProcedimentoCirurgicoId = procedimentoCirurgicoId,
            Configuracao = configuracao
        };

        foreach (var item in itensList)
            orc.Itens.Add(ItemOrcamento.Criar(0, item.Descricao, item.Quantidade, item.ValorUnitario, item.DescontoPercent));

        var ordemEq = 0;
        foreach (var e in equipeList)
            orc.Equipe.Add(OrcamentoEquipe.Criar(0, e.ProfissionalUsuarioId, e.Papel, e.Valor, ordemEq++));

        foreach (var imp in implantesList)
            orc.Implantes.Add(OrcamentoImplante.Criar(0, imp.ItemInventarioId, imp.Descricao, imp.Quantidade, imp.CustoUnitario));
        orc.CustoImplantesTotal = orc.Implantes.Sum(i => i.CustoTotal);

        var ordemFp = 0;
        foreach (var f in formasList)
            orc.FormasPagamento.Add(OrcamentoFormaPagamento.Criar(
                0, f.FormaPagamentoId, f.Valor, f.Parcelas,
                f.AcrescimoPercentual, f.EntradaPercentual, f.Observacao, ordemFp++));

        var ordemCir = 0;
        foreach (var c in cirurgiasList)
            orc.Cirurgias.Add(OrcamentoCirurgia.Criar(
                0, c.ProcedimentoCirurgicoId, c.Descricao, c.Quantidade,
                c.DuracaoMinutos, c.ValorTotal, ordemCir++));

        if (internacao is not null)
            orc.Internacao = OrcamentoInternacao.Criar(0, internacao.Tipo, internacao.Dias, internacao.ValorDiaria);

        if (anestesia is not null)
            orc.Anestesia = OrcamentoAnestesia.Criar(0, anestesia.Tipo, anestesia.Valor, anestesia.Observacao);

        orc.ValidarIntegridade(descontoBruto, jurosBrutos);
        return orc;
    }

    private static void ValidarBasico(long estabelecimentoId, long pacienteId, DateOnly validade)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Validade não pode ser uma data passada.");
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

    /// <summary>
    /// Substitui o aggregate inteiro (itens + equipe + implantes + formas + config +
    /// cirurgias + internação + anestesia). Mantém invariantes do <c>Pendente</c> e
    /// revalida integridade ao final.
    /// </summary>
    public void AtualizarCompleto(
        DateOnly validade,
        string? observacoes,
        TipoOrcamento tipo,
        long? procedimentoCirurgicoId,
        ConfigPagamentoOrcamento? configuracao,
        decimal descontoBruto,
        decimal jurosBrutos,
        IEnumerable<ItemPayload> itens,
        IEnumerable<EquipePayload> equipe,
        IEnumerable<ImplantePayload> implantes,
        IEnumerable<FormaPagamentoPayload> formasPagamento,
        IEnumerable<CirurgiaPayload>? cirurgias = null,
        InternacaoPayload? internacao = null,
        AnestesiaPayload? anestesia = null)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        if (validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Validade não pode ser uma data passada.");
        if (descontoBruto < 0)
            throw new BusinessException("Desconto não pode ser negativo.");
        if (jurosBrutos < 0)
            throw new BusinessException("Juros não podem ser negativos.");

        var itensList = itens?.ToList() ?? new();
        var equipeList = equipe?.ToList() ?? new();
        var implantesList = implantes?.ToList() ?? new();
        var formasList = formasPagamento?.ToList() ?? new();
        var cirurgiasList = cirurgias?.ToList() ?? new();

        if (itensList.Count == 0 && equipeList.Count == 0 && implantesList.Count == 0 && cirurgiasList.Count == 0)
            throw new BusinessException("O orçamento deve ter ao menos um item, implante, cirurgia ou comissão de equipe.");

        Validade = validade;
        Observacoes = observacoes?.Trim();
        Tipo = tipo;
        ProcedimentoCirurgicoId = procedimentoCirurgicoId;
        Configuracao = configuracao;

        Itens.Clear();
        foreach (var item in itensList)
            Itens.Add(ItemOrcamento.Criar(Id, item.Descricao, item.Quantidade, item.ValorUnitario, item.DescontoPercent));

        Equipe.Clear();
        var ordemEq = 0;
        foreach (var e in equipeList)
            Equipe.Add(OrcamentoEquipe.Criar(Id, e.ProfissionalUsuarioId, e.Papel, e.Valor, ordemEq++));

        Implantes.Clear();
        foreach (var imp in implantesList)
            Implantes.Add(OrcamentoImplante.Criar(Id, imp.ItemInventarioId, imp.Descricao, imp.Quantidade, imp.CustoUnitario));
        CustoImplantesTotal = Implantes.Sum(i => i.CustoTotal);

        FormasPagamento.Clear();
        var ordemFp = 0;
        foreach (var f in formasList)
            FormasPagamento.Add(OrcamentoFormaPagamento.Criar(
                Id, f.FormaPagamentoId, f.Valor, f.Parcelas,
                f.AcrescimoPercentual, f.EntradaPercentual, f.Observacao, ordemFp++));

        Cirurgias.Clear();
        var ordemCir = 0;
        foreach (var c in cirurgiasList)
            Cirurgias.Add(OrcamentoCirurgia.Criar(
                Id, c.ProcedimentoCirurgicoId, c.Descricao, c.Quantidade,
                c.DuracaoMinutos, c.ValorTotal, ordemCir++));

        Internacao = internacao is null
            ? null
            : OrcamentoInternacao.Criar(Id, internacao.Tipo, internacao.Dias, internacao.ValorDiaria);

        Anestesia = anestesia is null
            ? null
            : OrcamentoAnestesia.Criar(Id, anestesia.Tipo, anestesia.Valor, anestesia.Observacao);

        AtualizadoEm = DateTime.UtcNow;
        ValidarIntegridade(descontoBruto, jurosBrutos);
    }

    public void AdicionarMembroEquipe(Guid profissionalId, string papel, decimal valor)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");

        var ordem = Equipe.Count == 0 ? 0 : Equipe.Max(m => m.Ordem) + 1;
        Equipe.Add(OrcamentoEquipe.Criar(Id, profissionalId, papel, valor, ordem));
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverMembroEquipe(long membroId)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        var membro = Equipe.FirstOrDefault(m => m.Id == membroId)
            ?? throw new BusinessException("Membro da equipe não encontrado.");
        Equipe.Remove(membro);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AdicionarImplante(long? itemInventarioId, string descricao, decimal quantidade, decimal custoUnitario)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        Implantes.Add(OrcamentoImplante.Criar(Id, itemInventarioId, descricao, quantidade, custoUnitario));
        CustoImplantesTotal = Implantes.Sum(i => i.CustoTotal);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverImplante(long implanteId)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        var imp = Implantes.FirstOrDefault(i => i.Id == implanteId)
            ?? throw new BusinessException("Implante não encontrado.");
        Implantes.Remove(imp);
        CustoImplantesTotal = Implantes.Sum(i => i.CustoTotal);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AdicionarFormaPagamento(
        long formaPagamentoId,
        decimal valor,
        int parcelas,
        decimal acrescimoPercentual,
        decimal entradaPercentual,
        string? observacao)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        var ordem = FormasPagamento.Count == 0 ? 0 : FormasPagamento.Max(f => f.Ordem) + 1;
        FormasPagamento.Add(OrcamentoFormaPagamento.Criar(
            Id, formaPagamentoId, valor, parcelas, acrescimoPercentual, entradaPercentual, observacao, ordem));
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverFormaPagamento(long formaId)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        var f = FormasPagamento.FirstOrDefault(x => x.Id == formaId)
            ?? throw new BusinessException("Forma de pagamento não encontrada.");
        FormasPagamento.Remove(f);
        AtualizadoEm = DateTime.UtcNow;
    }

    // ----- Item 6 — métodos de manipulação de cirurgias / internação / anestesia. -----

    public void AdicionarCirurgia(
        long? procedimentoCirurgicoId,
        string? descricao,
        int quantidade,
        int? duracaoMinutos,
        decimal valorTotal)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        var ordem = Cirurgias.Count == 0 ? 0 : Cirurgias.Max(c => c.Ordem) + 1;
        Cirurgias.Add(OrcamentoCirurgia.Criar(
            Id, procedimentoCirurgicoId, descricao, quantidade, duracaoMinutos, valorTotal, ordem));
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverCirurgia(long cirurgiaId)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        var c = Cirurgias.FirstOrDefault(x => x.Id == cirurgiaId)
            ?? throw new BusinessException("Cirurgia não encontrada.");
        Cirurgias.Remove(c);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Define a internação (1:1) — substitui se já existe.</summary>
    public void DefinirInternacao(TipoInternacao tipo, int dias, decimal valorDiaria)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        Internacao = OrcamentoInternacao.Criar(Id, tipo, dias, valorDiaria);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverInternacao()
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        Internacao = null;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Define a anestesia (1:1) — substitui se já existe.</summary>
    public void DefinirAnestesia(TipoAnestesia tipo, decimal valor, string? observacao)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        Anestesia = OrcamentoAnestesia.Criar(Id, tipo, valor, observacao);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverAnestesia()
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        Anestesia = null;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AssociarProcedimentoCirurgico(long procedimentoId)
    {
        if (Status != OrcamentoStatus.Pendente)
            throw new BusinessException("Apenas orçamentos pendentes podem ser editados.");
        if (procedimentoId <= 0)
            throw new BusinessException("Procedimento cirúrgico inválido.");
        ProcedimentoCirurgicoId = procedimentoId;
        Tipo = TipoOrcamento.Cirurgico;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalcula o total de implantes (snapshot acumulado em <see cref="CustoImplantesTotal"/>).
    /// Útil quando handlers manipulam <c>Implantes</c> manualmente. Os demais totais
    /// (cirurgias/internação/anestesia) são calculados on-demand em <see cref="Total"/>.
    /// </summary>
    public void RecalcularTotais()
    {
        CustoImplantesTotal = Implantes.Sum(i => i.CustoTotal);
    }

    /// <summary>
    /// Total efetivo: soma bruta − desconto + juros. Esse é o valor que precisa ser coberto
    /// pelas <see cref="FormasPagamento"/>.
    /// </summary>
    public decimal CalcularTotalEfetivo(decimal descontoBruto, decimal jurosBrutos)
        => Math.Round(Total - descontoBruto + jurosBrutos, 2);

    /// <summary>
    /// Garante que a soma das formas de pagamento bate com o total efetivo (com tolerância
    /// de R$ 0,01 para arredondamento). Se não bater, lança <see cref="BusinessException"/>
    /// com a diferença explícita — facilita debug no frontend.
    ///
    /// Quando não há formas de pagamento informadas, a verificação é pulada (orçamento sem
    /// forma é válido em fase de cotação).
    /// </summary>
    public void ValidarIntegridade(decimal descontoBruto, decimal jurosBrutos)
    {
        if (FormasPagamento.Count == 0)
            return;

        var totalEfetivo = CalcularTotalEfetivo(descontoBruto, jurosBrutos);
        var somaFormas = Math.Round(FormasPagamento.Sum(f => f.Valor), 2);
        if (Math.Abs(somaFormas - totalEfetivo) > 0.01m)
        {
            throw new BusinessException(
                $"Soma das formas de pagamento ({somaFormas:N2}) não confere com o total ({totalEfetivo:N2}).");
        }
    }

    public record ItemPayload(string Descricao, decimal Quantidade, decimal ValorUnitario, decimal DescontoPercent);
    public record EquipePayload(Guid ProfissionalUsuarioId, string Papel, decimal Valor);
    public record ImplantePayload(long? ItemInventarioId, string Descricao, decimal Quantidade, decimal CustoUnitario);
    public record FormaPagamentoPayload(
        long FormaPagamentoId,
        decimal Valor,
        int Parcelas,
        decimal AcrescimoPercentual,
        decimal EntradaPercentual,
        string? Observacao);
    public record CirurgiaPayload(
        long? ProcedimentoCirurgicoId,
        string? Descricao,
        int Quantidade,
        int? DuracaoMinutos,
        decimal ValorTotal);
    public record InternacaoPayload(TipoInternacao Tipo, int Dias, decimal ValorDiaria);
    public record AnestesiaPayload(TipoAnestesia Tipo, decimal Valor, string? Observacao);
}
