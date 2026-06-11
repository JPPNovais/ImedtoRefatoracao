using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.PacienteConvenios;

/// <summary>
/// Carteirinha de convênio de um paciente em um estabelecimento.
/// Aggregate root próprio (paciente pode ter N convênios em clínicas diferentes).
/// numero_carteirinha é dado pessoal (R15/LGPD) — DTO minimizado, sem PII em log.
/// </summary>
public class PacienteConvenio : Entity
{
    public virtual long PacienteId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long ConvenioId { get; protected set; }
    public virtual long? PlanoId { get; protected set; }
    /// <summary>PII — dado pessoal do paciente (R15/LGPD).</summary>
    public virtual string NumeroCarteirinha { get; protected set; } = string.Empty;
    public virtual DateOnly? Validade { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected PacienteConvenio() { }

    public static PacienteConvenio Criar(
        long pacienteId,
        long estabelecimentoId,
        long convenioId,
        long? planoId,
        string numeroCarteirinha,
        DateOnly? validade)
    {
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (convenioId <= 0)
            throw new BusinessException("Convênio é obrigatório.");
        if (string.IsNullOrWhiteSpace(numeroCarteirinha))
            throw new BusinessException("Número da carteirinha é obrigatório.");

        return new PacienteConvenio
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            ConvenioId = convenioId,
            PlanoId = planoId > 0 ? planoId : null,
            NumeroCarteirinha = numeroCarteirinha.Trim(),
            Validade = validade,
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        };
    }

    public virtual void Atualizar(
        long convenioId,
        long? planoId,
        string numeroCarteirinha,
        DateOnly? validade,
        bool ativo)
    {
        if (convenioId <= 0)
            throw new BusinessException("Convênio é obrigatório.");
        if (string.IsNullOrWhiteSpace(numeroCarteirinha))
            throw new BusinessException("Número da carteirinha é obrigatório.");

        ConvenioId = convenioId;
        PlanoId = planoId > 0 ? planoId : null;
        NumeroCarteirinha = numeroCarteirinha.Trim();
        Validade = validade;
        Ativo = ativo;
        AtualizadoEm = DateTime.UtcNow;
    }
}
