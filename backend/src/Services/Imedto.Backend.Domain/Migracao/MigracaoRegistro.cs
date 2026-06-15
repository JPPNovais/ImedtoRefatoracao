using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// POCO de mapeamento EF para registro individual de migração (uma linha do arquivo de origem).
/// Aggregate root completo será construído pelo developer.
/// </summary>
public class MigracaoRegistro : Entity
{
    public virtual long MigracaoJobId { get; protected set; }

    /// <summary>Redundante — necessário para queries diretas multi-tenant sem JOIN no job.</summary>
    public virtual long EstabelecimentoId { get; protected set; }

    /// <summary>
    /// Tipo de entidade: paciente, agendamento, item_estoque, fornecedor_estoque,
    /// categoria_estoque, fabricante_estoque, local_estoque, produto_orcamento, procedimento_orcamento, etc.
    /// </summary>
    public virtual string Entidade { get; protected set; } = string.Empty;

    /// <summary>Linha original do arquivo de origem em JSON. PII do tenant — não logar.</summary>
    public virtual string PayloadBruto { get; protected set; } = "{}";

    /// <summary>
    /// Status: pendente, importado_criado, importado_atualizado, rejeitado, pulado
    /// </summary>
    public virtual string Status { get; protected set; } = "pendente";

    /// <summary>
    /// Motivo de rejeição sem PII — ex.: "identificador ausente", "CPF ausente", "data inválida".
    /// </summary>
    public virtual string? MotivoRejeicao { get; protected set; }

    /// <summary>PK da entidade de domínio criada/atualizada. Usado pelo undo (desfazer).</summary>
    public virtual long? EntidadeAlvoId { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }

    protected MigracaoRegistro() { }

    public static MigracaoRegistro Criar(long jobId, long estabelecimentoId, string entidade, string payloadBruto)
    {
        if (jobId <= 0) throw new BusinessException("Job é obrigatório.");
        if (estabelecimentoId <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(entidade)) throw new BusinessException("Entidade é obrigatória.");
        return new MigracaoRegistro
        {
            MigracaoJobId = jobId,
            EstabelecimentoId = estabelecimentoId,
            Entidade = entidade.Trim(),
            PayloadBruto = string.IsNullOrWhiteSpace(payloadBruto) ? "{}" : payloadBruto,
            Status = "pendente",
            CriadoEm = DateTime.UtcNow,
        };
    }

    public virtual void MarcarImportadoCriado(long entidadeAlvoId)
    {
        EntidadeAlvoId = entidadeAlvoId;
        Status = "importado_criado";
    }

    public virtual void MarcarImportadoAtualizado(long entidadeAlvoId)
    {
        EntidadeAlvoId = entidadeAlvoId;
        Status = "importado_atualizado";
    }

    public virtual void MarcarRejeitado(string motivoSemPii)
    {
        MotivoRejeicao = motivoSemPii;
        Status = "rejeitado";
    }

    public virtual void MarcarPulado(string motivoSemPii)
    {
        MotivoRejeicao = motivoSemPii;
        Status = "pulado";
    }
}
