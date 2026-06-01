using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.AssinaturaDigital.Commands;

/// <summary>
/// Dispara assinatura digital para uma receita. Apenas o médico prescritor pode disparar.
/// Retorna 202 Accepted — o status muda via webhook de callback do provedor.
/// </summary>
public class DispararAssinaturaCommand : ICommand
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Usuário autenticado que dispara — deve ser o prescritor da receita.</summary>
    public Guid CallerUsuarioId { get; set; }
}
