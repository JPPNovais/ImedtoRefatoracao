using System.Text.RegularExpressions;
using Imedto.Backend.Domain.PedidosExame.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.PedidosExame;

/// <summary>
/// Aggregate root — pedido de exame emitido para um paciente. Lista de exames
/// (string) persistida como jsonb. MVP — sem modelos.
/// </summary>
public class PedidoExame : Entity, ISoftDeletable
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual TipoPedidoExame Tipo { get; protected set; }
    /// <summary>Lista de exames solicitados (texto livre). Persistido como jsonb.</summary>
    public virtual List<string> Exames { get; protected set; } = new();
    public virtual string IndicacaoClinica { get; protected set; } = string.Empty;
    public virtual string? Cid10 { get; protected set; }
    public virtual string? Observacoes { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    private static readonly Regex Cid10Regex = new(@"^[A-TV-Z]\d{2}(\.\d)?$", RegexOptions.Compiled);

    protected PedidoExame() { }

    public static PedidoExame Emitir(
        long estabelecimentoId,
        long pacienteId,
        Guid profissionalUsuarioId,
        TipoPedidoExame tipo,
        IEnumerable<string>? exames,
        string indicacaoClinica,
        string? cid10,
        string? observacoes)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional emissor é obrigatório.");
        if (string.IsNullOrWhiteSpace(indicacaoClinica))
            throw new BusinessException("Indicação clínica é obrigatória.");
        if (indicacaoClinica.Length > 2000)
            throw new BusinessException("Indicação clínica excede 2000 caracteres.");
        if (observacoes is not null && observacoes.Length > 2000)
            throw new BusinessException("Observações excedem 2000 caracteres.");

        var listaExames = (exames ?? Enumerable.Empty<string>())
            .Select(e => (e ?? string.Empty).Trim())
            .Where(e => e.Length > 0)
            .ToList();

        if (listaExames.Count == 0)
            throw new BusinessException("Informe ao menos um exame.");
        if (listaExames.Count > 50)
            throw new BusinessException("Limite de 50 exames por pedido.");

        var cidNormalizado = NormalizarCid(cid10);

        return new PedidoExame
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            Tipo = tipo,
            Exames = listaExames,
            IndicacaoClinica = indicacaoClinica.Trim(),
            Cid10 = cidNormalizado,
            Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim(),
            CriadoEm = DateTime.UtcNow,
        };
    }

    public virtual void MarcarComoEmitido()
    {
        if (Id == 0)
            throw new InvalidOperationException("Pedido de exame ainda não foi persistido — Id é 0.");
        AddDomainEvent(new PedidoExameEmitidoEvent(
            Id, PacienteId, EstabelecimentoId, ProfissionalUsuarioId, Tipo, Exames.Count));
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Pedido de exame já está deletado.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }

    private static string? NormalizarCid(string? cid)
    {
        if (string.IsNullOrWhiteSpace(cid)) return null;
        var cidUp = cid.Trim().ToUpperInvariant();
        if (!Cid10Regex.IsMatch(cidUp))
            throw new BusinessException("CID-10 inválido. Formato esperado: A00 ou A00.0.");
        return cidUp;
    }
}
