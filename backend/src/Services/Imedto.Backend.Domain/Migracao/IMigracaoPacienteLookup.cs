namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Lookup de leitura otimizado para resolução de vínculo paciente-migração (Onda 2 — CA14).
/// Usado pelo CarregarOnda2JobHandler para localizar o paciente já migrado por CPF ou
/// documento internacional sem carregar o aggregate completo.
///
/// Dapper puro — sem EF — para não inflar o DbContext com lookup cross-aggregate.
/// </summary>
public interface IMigracaoPacienteLookup
{
    /// <summary>Retorna (id, prontuario_id) do paciente cujo CPF bate no tenant. Null se não existir.</summary>
    Task<PacienteMigracaoInfo?> ObterPorCpfOuNulo(string cpf, long estabelecimentoId, CancellationToken ct = default);

    /// <summary>Retorna (id, prontuario_id) do paciente cujo documento internacional bate no tenant.</summary>
    Task<PacienteMigracaoInfo?> ObterPorDocumentoInternacionalOuNulo(string doc, long estabelecimentoId, CancellationToken ct = default);

    /// <summary>Retorna o id do primeiro modelo de prontuário ativo visível pelo tenant (padrão sistema ou próprio).</summary>
    Task<long?> ObterIdModeloPadraoProntuarioOuNulo(long estabelecimentoId, CancellationToken ct = default);
}

/// <summary>
/// Projeção mínima necessária para vincular prontuário durante a migração (CA14, CA21).
/// Sem campos PII — apenas IDs.
/// </summary>
public record PacienteMigracaoInfo(long PacienteId, long? ProntuarioId);
