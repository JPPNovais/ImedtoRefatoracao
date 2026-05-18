using System.Text.RegularExpressions;
using Imedto.Backend.Domain.Atestados.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Atestados;

/// <summary>
/// Aggregate root — atestado emitido por um profissional para um paciente em um
/// estabelecimento. Append-only do ponto de vista clínico (soft delete preserva
/// histórico LGPD).
/// </summary>
public class Atestado : Entity, ISoftDeletable
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual TipoAtestado Tipo { get; protected set; }
    /// <summary>Obrigatório quando <see cref="Tipo"/> == <see cref="TipoAtestado.Afastamento"/>; null caso contrário.</summary>
    public virtual int? DiasAfastamento { get; protected set; }
    /// <summary>CID-10 (opcional). Formato A00 ou A00.0 (regex <c>^[A-TV-Z]\d{2}(\.\d)?$</c>).</summary>
    public virtual string? Cid10 { get; protected set; }
    public virtual string Conteudo { get; protected set; } = string.Empty;
    public virtual DateTime CriadoEm { get; protected set; }

    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    private static readonly Regex Cid10Regex = new(@"^[A-TV-Z]\d{2}(\.\d)?$", RegexOptions.Compiled);

    protected Atestado() { }

    public static Atestado Emitir(
        long estabelecimentoId,
        long pacienteId,
        Guid profissionalUsuarioId,
        TipoAtestado tipo,
        int? diasAfastamento,
        string? cid10,
        string conteudo)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional emissor é obrigatório.");
        if (string.IsNullOrWhiteSpace(conteudo))
            throw new BusinessException("Conteúdo do atestado é obrigatório.");
        if (conteudo.Length > 4000)
            throw new BusinessException("Conteúdo excede 4000 caracteres.");

        ValidarDias(tipo, diasAfastamento);
        var cidNormalizado = NormalizarCid(cid10);

        return new Atestado
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            Tipo = tipo,
            DiasAfastamento = tipo == TipoAtestado.Afastamento ? diasAfastamento : null,
            Cid10 = cidNormalizado,
            Conteudo = conteudo.Trim(),
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Anexa <see cref="AtestadoEmitidoEvent"/> ao aggregate. Chamar APÓS o
    /// primeiro <c>Salvar</c> — o evento carrega o Id, que só é resolvido pelo
    /// banco no insert.
    /// </summary>
    public virtual void MarcarComoEmitido()
    {
        if (Id == 0)
            throw new InvalidOperationException("Atestado ainda não foi persistido — Id é 0.");
        AddDomainEvent(new AtestadoEmitidoEvent(Id, PacienteId, EstabelecimentoId, ProfissionalUsuarioId, Tipo));
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Atestado já está deletado.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }

    private static void ValidarDias(TipoAtestado tipo, int? dias)
    {
        if (tipo == TipoAtestado.Afastamento)
        {
            if (dias is null || dias <= 0)
                throw new BusinessException("Atestado de afastamento exige número de dias maior que zero.");
            if (dias > 365)
                throw new BusinessException("Dias de afastamento excede o limite de 365.");
        }
        // Demais tipos: ignora dias mesmo se informado (limpamos no construtor).
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
