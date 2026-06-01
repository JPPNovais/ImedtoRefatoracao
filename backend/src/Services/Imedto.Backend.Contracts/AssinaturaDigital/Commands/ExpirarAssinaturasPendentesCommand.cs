using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.AssinaturaDigital.Commands;

/// <summary>
/// Marca como AssinaturaExpirada todas as receitas em AssinaturaPendente
/// cuja <c>assinatura_solicitada_em</c> ultrapassou <see cref="LimiteMinutos"/> minutos.
/// Invocado pelo job <c>expirar-assinaturas-pendentes</c> (1×/hora).
/// </summary>
public class ExpirarAssinaturasPendentesCommand : ICommand
{
    /// <summary>Default: 30 minutos (configurável via appsettings AssinaturaDigital:ExpiracaoPendenteMinutos).</summary>
    public int LimiteMinutos { get; set; } = 30;
}
