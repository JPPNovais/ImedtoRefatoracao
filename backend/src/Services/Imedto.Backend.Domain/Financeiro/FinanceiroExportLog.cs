using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Log de auditoria LGPD append-only para exports do extrato financeiro (CA10 — briefing 2026-06-11_002).
/// Registra quem exportou, de qual estabelecimento, o período e a quantidade de linhas.
/// Sem PII: sem nomes de pacientes, CPF, telefone nem qualquer dado de paciente.
/// Gravado via Dapper (best-effort) em <see cref="Infrastructure.Database.Repositories.ConsolidacaoFinanceiraQueryRepository"/>.
/// </summary>
public class FinanceiroExportLog : Entity<long>
{
    /// <summary>ID do usuário que acionou o export. Sem FK declarada (log imutável).</summary>
    public virtual Guid UsuarioId { get; protected set; }

    /// <summary>Estabelecimento ao qual o export pertence.</summary>
    public virtual long EstabelecimentoId { get; protected set; }

    /// <summary>Ação auditada. Valor fixo: 'ExportarExtrato'.</summary>
    public virtual string Acao { get; protected set; }

    /// <summary>Início do período exportado (sem fuso — data-only convertida para timestamptz).</summary>
    public virtual DateTime PeriodoInicio { get; protected set; }

    /// <summary>Fim do período exportado.</summary>
    public virtual DateTime PeriodoFim { get; protected set; }

    /// <summary>Total de linhas retornadas no export. Sem dados individuais de paciente.</summary>
    public virtual int TotalLinhas { get; protected set; }

    /// <summary>Momento em que o export ocorreu (UTC).</summary>
    public virtual DateTime OcorridoEm { get; protected set; }

    protected FinanceiroExportLog() { }
}
