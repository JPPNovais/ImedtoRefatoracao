using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Marca o procedimento indicado de uma pendência como realizado (F4/briefing 2026-06-10_013).
/// Atomicidade: Cobrança + baixa de estoque + conclusão da pendência na mesma UoW.
/// Idempotência dupla: domínio (pendência já Concluída → no-op) + banco (índice UNIQUE parcial em cobrancas.evolucao_id).
/// </summary>
public class MarcarProcedimentoRealizadoCommand : ICommand
{
    public long PendenciaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioId { get; set; }
    /// <summary>Id da cobrança gerada — preenchido pelo handler após commit.</summary>
    public long CobrancaIdGerada { get; set; }
}
