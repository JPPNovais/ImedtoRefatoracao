namespace Imedto.Backend.Domain.Vinculos;

/// <summary>
/// Status do fluxo de solicitação inversa (profissional → estabelecimento).
/// Diferente de <see cref="VinculoStatus"/>, que é o estado do vínculo em si após aceito.
/// </summary>
public enum StatusSolicitacaoVinculo
{
    /// <summary>Solicitação criada pelo profissional, aguardando resposta do dono.</summary>
    Pendente,

    /// <summary>Aprovada pelo dono — gera o vínculo automaticamente via event handler.</summary>
    Aprovada,

    /// <summary>Recusada pelo dono — guarda <see cref="SolicitacaoVinculo.MotivoRecusa"/>.</summary>
    Recusada,

    /// <summary>Cancelada pelo próprio profissional antes da resposta.</summary>
    Cancelada
}
