using System.Globalization;
using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Aggregate root do orçamento (paridade com legado). Estrutura única — não há
/// distinção "simples vs cirúrgico"; cirurgias, equipe, implantes, local cirúrgico
/// e anestesia são collections opcionais. Descontos/acréscimos vão sempre por
/// <see cref="OrcamentoFormaPagamento"/> (não há mais jsonb opaco de configuração).
///
/// Nota 2026-05-18: o conceito de "Internação" (4 tipos: Apartamento/Enfermaria/UTI/Ambulatorial)
/// foi substituído pelo "Local Cirúrgico" (5 tipos: IntLocal/IntPeridural/IntGeral/SemInternacao/Ambulatorio)
/// vindo do legado. O valor é calculado a partir da <c>ConfiguracaoLocalCirurgia</c>
/// do estabelecimento × tempo total da cirurgia (snapshot persistido em <see cref="ValorLocal"/>).
/// </summary>
public class Orcamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual string Numero { get; protected set; } = string.Empty;
    /// <summary>Título livre do orçamento (paridade com legado, opcional).</summary>
    public virtual string? Titulo { get; protected set; }
    public virtual OrcamentoStatus Status { get; protected set; }
    public virtual DateOnly Validade { get; protected set; }
    public virtual string? Observacoes { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }
    public virtual long? ProcedimentoCirurgicoId { get; protected set; }
    /// <summary>
    /// Agendamento que originou este orçamento (opcional). Permite "Criar orçamento"
    /// a partir da ficha do agendamento e listar orçamentos vinculados ao agendamento.
    /// </summary>
    public virtual long? AgendamentoId { get; protected set; }
    public virtual decimal CustoImplantesTotal { get; protected set; }

    // ── Local cirúrgico (snapshot) ───────────────────────────────────────────
    /// <summary>Tipo do local cirúrgico escolhido. Null = "sem local definido".</summary>
    public virtual TipoLocalCirurgia? TipoLocal { get; protected set; }
    /// <summary>Tempo total da cirurgia em minutos, usado no cálculo do local.</summary>
    public virtual int? TempoLocalMinutos { get; protected set; }
    /// <summary>Valor calculado do local cirúrgico (snapshot — fonte da verdade da cotação).</summary>
    public virtual decimal ValorLocal { get; protected set; }

    public virtual List<ItemOrcamento> Itens { get; protected set; } = new();
    public virtual List<OrcamentoEquipe> Equipe { get; protected set; } = new();
    public virtual List<OrcamentoImplante> Implantes { get; protected set; } = new();
    public virtual List<OrcamentoFormaPagamento> FormasPagamento { get; protected set; } = new();
    public virtual List<OrcamentoCirurgia> Cirurgias { get; protected set; } = new();
    public virtual OrcamentoAnestesia? Anestesia { get; protected set; }

    /// <summary>
    /// Total bruto = soma de itens + total de implantes + comissões da equipe + cirurgias +
    /// valor do local cirúrgico + anestesia. Não aplica desconto/juros — descontos/acréscimos
    /// vivem em cada <see cref="OrcamentoFormaPagamento"/>.
    /// </summary>
    public decimal Total =>
        Itens.Sum(i => i.Subtotal)
        + Implantes.Sum(i => i.CustoTotal)
        + Equipe.Sum(e => e.Valor)
        + Cirurgias.Sum(c => c.ValorTotal)
        + ValorLocal
        + (Anestesia?.Valor ?? 0m);

    protected Orcamento() { }

    /// <summary>
    /// Fábrica única do orçamento. Todas as collections (cirurgias, equipe, implantes,
    /// formas de pagamento, local cirúrgico, anestesia) são opcionais. O orçamento nasce em
    /// <see cref="OrcamentoStatus.Rascunho"/> — só vira <c>Enviado</c> via
    /// <see cref="Enviar"/>, e só pode ser aprovado/recusado a partir de <c>Enviado</c>.
    /// </summary>
    public static Orcamento Criar(
        long estabelecimentoId,
        long pacienteId,
        DateOnly validade,
        string? observacoes,
        Guid criadoPorUsuarioId,
        long? procedimentoCirurgicoId,
        IEnumerable<ItemPayload>? itens = null,
        IEnumerable<EquipePayload>? equipe = null,
        IEnumerable<ImplantePayload>? implantes = null,
        IEnumerable<FormaPagamentoPayload>? formasPagamento = null,
        IEnumerable<CirurgiaPayload>? cirurgias = null,
        LocalCirurgiaPayload? local = null,
        AnestesiaPayload? anestesia = null,
        string? titulo = null,
        long? agendamentoId = null)
    {
        ValidarBasico(estabelecimentoId, pacienteId, validade);

        var itensList = itens?.ToList() ?? new();
        var equipeList = equipe?.ToList() ?? new();
        var implantesList = implantes?.ToList() ?? new();
        var formasList = formasPagamento?.ToList() ?? new();
        var cirurgiasList = cirurgias?.ToList() ?? new();

        if (itensList.Count == 0 && equipeList.Count == 0 && implantesList.Count == 0 && cirurgiasList.Count == 0)
            throw new BusinessException("O orçamento deve ter ao menos um item, implante, cirurgia ou comissão de equipe.");

        var orc = new Orcamento
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Numero = string.Empty,
            Titulo = TratarTitulo(titulo),
            Status = OrcamentoStatus.Rascunho,
            Validade = validade,
            Observacoes = observacoes?.Trim(),
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = DateTime.UtcNow,
            ProcedimentoCirurgicoId = procedimentoCirurgicoId,
            AgendamentoId = agendamentoId,
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

        if (local is not null)
            orc.DefinirLocalInterno(local.Tipo, local.TempoMinutos, local.ValorCalculado);

        if (anestesia is not null)
            orc.Anestesia = OrcamentoAnestesia.Criar(0, anestesia.Tipo, anestesia.Valor, anestesia.Observacao);

        orc.ValidarIntegridade();
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

    private static string? TratarTitulo(string? titulo)
    {
        var t = titulo?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        if (t.Length > 120) throw new BusinessException("Título do orçamento não pode passar de 120 caracteres.");
        return t;
    }

    public void DefinirNumero()
        => Numero = $"ORC-{CriadoEm:yyyyMM}-{Id:D4}";

    /// <summary>Transição Rascunho → Enviado. Único caminho para sair de Rascunho.</summary>
    public void Enviar()
    {
        if (Status != OrcamentoStatus.Rascunho)
            throw new BusinessException("Apenas orçamentos em rascunho podem ser enviados.");
        if (Validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Orçamento expirado não pode ser enviado.");
        Status = OrcamentoStatus.Enviado;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Transição Enviado → Aprovado.</summary>
    public void Aprovar()
    {
        if (Status != OrcamentoStatus.Enviado)
            throw new BusinessException("Apenas orçamentos enviados podem ser aprovados.");
        if (Validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Orçamento expirado não pode ser aprovado.");

        Status = OrcamentoStatus.Aprovado;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrcamentoAprovadoEvent(Id, EstabelecimentoId, PacienteId, Total));
    }

    /// <summary>Transição Enviado → Recusado.</summary>
    public void Recusar()
    {
        if (Status != OrcamentoStatus.Enviado)
            throw new BusinessException("Apenas orçamentos enviados podem ser recusados.");
        Status = OrcamentoStatus.Recusado;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancelamento manual. Pode partir de qualquer status não-terminal
    /// (<c>Rascunho</c>, <c>Enviado</c>, <c>Aprovado</c>). Recusado/Cancelado/Expirado
    /// já são terminais.
    /// </summary>
    public void Cancelar()
    {
        if (Status is OrcamentoStatus.Recusado or OrcamentoStatus.Cancelado or OrcamentoStatus.Expirado)
            throw new BusinessException("Orçamento já está em status terminal.");
        Status = OrcamentoStatus.Cancelado;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Marca como expirado se a validade venceu (job ou observador). Idempotente.</summary>
    public void Expirar()
    {
        if (Status is OrcamentoStatus.Aprovado or OrcamentoStatus.Recusado
                   or OrcamentoStatus.Cancelado or OrcamentoStatus.Expirado)
            return;
        Status = OrcamentoStatus.Expirado;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Substitui o aggregate inteiro (itens + equipe + implantes + formas + cirurgias +
    /// local cirúrgico + anestesia). Permitido apenas em <c>Rascunho</c> e <c>Enviado</c>.
    /// </summary>
    public void Atualizar(
        DateOnly validade,
        string? observacoes,
        long? procedimentoCirurgicoId,
        IEnumerable<ItemPayload>? itens = null,
        IEnumerable<EquipePayload>? equipe = null,
        IEnumerable<ImplantePayload>? implantes = null,
        IEnumerable<FormaPagamentoPayload>? formasPagamento = null,
        IEnumerable<CirurgiaPayload>? cirurgias = null,
        LocalCirurgiaPayload? local = null,
        AnestesiaPayload? anestesia = null,
        string? titulo = null,
        long? agendamentoId = null)
    {
        if (Status is not OrcamentoStatus.Rascunho and not OrcamentoStatus.Enviado)
            throw new BusinessException("Apenas orçamentos em rascunho ou enviados podem ser editados.");
        if (validade < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("Validade não pode ser uma data passada.");

        var itensList = itens?.ToList() ?? new();
        var equipeList = equipe?.ToList() ?? new();
        var implantesList = implantes?.ToList() ?? new();
        var formasList = formasPagamento?.ToList() ?? new();
        var cirurgiasList = cirurgias?.ToList() ?? new();

        if (itensList.Count == 0 && equipeList.Count == 0 && implantesList.Count == 0 && cirurgiasList.Count == 0)
            throw new BusinessException("O orçamento deve ter ao menos um item, implante, cirurgia ou comissão de equipe.");

        Validade = validade;
        Observacoes = observacoes?.Trim();
        Titulo = TratarTitulo(titulo);
        ProcedimentoCirurgicoId = procedimentoCirurgicoId;
        AgendamentoId = agendamentoId;

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

        if (local is null)
        {
            TipoLocal = null;
            TempoLocalMinutos = null;
            ValorLocal = 0m;
        }
        else
        {
            DefinirLocalInterno(local.Tipo, local.TempoMinutos, local.ValorCalculado);
        }

        Anestesia = anestesia is null
            ? null
            : OrcamentoAnestesia.Criar(Id, anestesia.Tipo, anestesia.Valor, anestesia.Observacao);

        AtualizadoEm = DateTime.UtcNow;
        ValidarIntegridade();
    }

    public void AdicionarMembroEquipe(Guid profissionalId, string papel, decimal valor)
    {
        GarantirEditavel();
        var ordem = Equipe.Count == 0 ? 0 : Equipe.Max(m => m.Ordem) + 1;
        Equipe.Add(OrcamentoEquipe.Criar(Id, profissionalId, papel, valor, ordem));
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverMembroEquipe(long membroId)
    {
        GarantirEditavel();
        var membro = Equipe.FirstOrDefault(m => m.Id == membroId)
            ?? throw new BusinessException("Membro da equipe não encontrado.");
        Equipe.Remove(membro);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AdicionarImplante(long? itemInventarioId, string descricao, decimal quantidade, decimal custoUnitario)
    {
        GarantirEditavel();
        Implantes.Add(OrcamentoImplante.Criar(Id, itemInventarioId, descricao, quantidade, custoUnitario));
        CustoImplantesTotal = Implantes.Sum(i => i.CustoTotal);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverImplante(long implanteId)
    {
        GarantirEditavel();
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
        GarantirEditavel();
        var ordem = FormasPagamento.Count == 0 ? 0 : FormasPagamento.Max(f => f.Ordem) + 1;
        FormasPagamento.Add(OrcamentoFormaPagamento.Criar(
            Id, formaPagamentoId, valor, parcelas, acrescimoPercentual, entradaPercentual, observacao, ordem));
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverFormaPagamento(long formaId)
    {
        GarantirEditavel();
        var f = FormasPagamento.FirstOrDefault(x => x.Id == formaId)
            ?? throw new BusinessException("Forma de pagamento não encontrada.");
        FormasPagamento.Remove(f);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AdicionarCirurgia(
        long? procedimentoCirurgicoId,
        string? descricao,
        int quantidade,
        int? duracaoMinutos,
        decimal valorTotal)
    {
        GarantirEditavel();
        var ordem = Cirurgias.Count == 0 ? 0 : Cirurgias.Max(c => c.Ordem) + 1;
        Cirurgias.Add(OrcamentoCirurgia.Criar(
            Id, procedimentoCirurgicoId, descricao, quantidade, duracaoMinutos, valorTotal, ordem));
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverCirurgia(long cirurgiaId)
    {
        GarantirEditavel();
        var c = Cirurgias.FirstOrDefault(x => x.Id == cirurgiaId)
            ?? throw new BusinessException("Cirurgia não encontrada.");
        Cirurgias.Remove(c);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Define/atualiza o local cirúrgico (snapshot calculado vem de fora).</summary>
    public void DefinirLocal(TipoLocalCirurgia tipo, int tempoMinutos, decimal valorCalculado)
    {
        GarantirEditavel();
        DefinirLocalInterno(tipo, tempoMinutos, valorCalculado);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverLocal()
    {
        GarantirEditavel();
        TipoLocal = null;
        TempoLocalMinutos = null;
        ValorLocal = 0m;
        AtualizadoEm = DateTime.UtcNow;
    }

    private void DefinirLocalInterno(TipoLocalCirurgia tipo, int tempoMinutos, decimal valorCalculado)
    {
        if (tempoMinutos < 0) throw new BusinessException("Tempo da cirurgia não pode ser negativo.");
        if (valorCalculado < 0) throw new BusinessException("Valor do local não pode ser negativo.");
        TipoLocal = tipo;
        TempoLocalMinutos = tempoMinutos;
        ValorLocal = Math.Round(valorCalculado, 2);
    }

    public void DefinirAnestesia(TipoAnestesia tipo, decimal valor, string? observacao)
    {
        GarantirEditavel();
        Anestesia = OrcamentoAnestesia.Criar(Id, tipo, valor, observacao);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverAnestesia()
    {
        GarantirEditavel();
        Anestesia = null;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AssociarProcedimentoCirurgico(long procedimentoId)
    {
        GarantirEditavel();
        if (procedimentoId <= 0)
            throw new BusinessException("Procedimento cirúrgico inválido.");
        ProcedimentoCirurgicoId = procedimentoId;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Vincula o procedimento cirúrgico criado pela conversão (orçamento aprovado →
    /// cirurgia). Diferente de <see cref="AssociarProcedimentoCirurgico"/>, este método
    /// exige <c>Status = Aprovado</c> e bloqueia conversão dupla. Não muda o status do
    /// orçamento — ele permanece <c>Aprovado</c>.
    /// </summary>
    public void RegistrarConversaoEmProcedimento(long procedimentoId)
    {
        if (Status != OrcamentoStatus.Aprovado)
            throw new BusinessException("Apenas orçamentos aprovados podem ser convertidos em cirurgia.");
        if (ProcedimentoCirurgicoId is not null && ProcedimentoCirurgicoId != 0)
            throw new BusinessException("Este orçamento já foi convertido em cirurgia.");
        if (procedimentoId <= 0)
            throw new BusinessException("Procedimento cirúrgico inválido.");
        ProcedimentoCirurgicoId = procedimentoId;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Recalcula o snapshot acumulado de implantes (os demais totais são on-demand).</summary>
    public void RecalcularTotais()
    {
        CustoImplantesTotal = Implantes.Sum(i => i.CustoTotal);
    }

    private void GarantirEditavel()
    {
        if (Status is not OrcamentoStatus.Rascunho and not OrcamentoStatus.Enviado)
            throw new BusinessException("Apenas orçamentos em rascunho ou enviados podem ser editados.");
    }

    /// <summary>
    /// Garante que a soma das formas de pagamento bate com o <see cref="Total"/> bruto
    /// (com tolerância de R$ 0,01 para arredondamento). Se não bater, lança
    /// <see cref="BusinessException"/> com a diferença explícita.
    ///
    /// Quando não há formas de pagamento informadas, a verificação é pulada (orçamento
    /// sem forma é válido em fase de cotação).
    /// </summary>
    public void ValidarIntegridade()
    {
        if (FormasPagamento.Count == 0)
            return;

        var total = Math.Round(Total, 2);
        var somaFormas = Math.Round(FormasPagamento.Sum(f => f.Valor), 2);
        if (Math.Abs(somaFormas - total) > 0.01m)
        {
            // Formatação em pt-BR explícita — não confiar em CurrentCulture (CI roda en-US).
            var ptBr = CultureInfo.GetCultureInfo("pt-BR");
            throw new BusinessException(
                $"Soma das formas de pagamento ({somaFormas.ToString("N2", ptBr)}) não confere com o total ({total.ToString("N2", ptBr)}).");
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
    /// <summary>
    /// Local cirúrgico do orçamento (substitui o antigo <c>InternacaoPayload</c>).
    /// O <c>ValorCalculado</c> é snapshot — quem calcula é o handler com a
    /// <c>ConfiguracaoLocalCirurgia</c> do estabelecimento.
    /// </summary>
    public record LocalCirurgiaPayload(TipoLocalCirurgia Tipo, int TempoMinutos, decimal ValorCalculado);
    public record AnestesiaPayload(TipoAnestesia Tipo, decimal Valor, string? Observacao);
}
