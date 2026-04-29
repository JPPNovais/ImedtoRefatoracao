namespace Imedto.Backend.Domain.Assinaturas;

/// <summary>
/// Status de ciclo de vida da assinatura. Persistido como string no Postgres
/// (legibilidade direta na tabela). <see cref="Trial"/> e <see cref="Ativa"/>
/// são os únicos status que liberam features no <c>IAssinaturaService</c>.
/// </summary>
public enum StatusAssinatura
{
    /// <summary>Período de avaliação. Libera features do plano "Trial" até <c>ExpiraEm</c>.</summary>
    Trial,

    /// <summary>Assinatura paga e em dia. Libera features conforme o plano vigente.</summary>
    Ativa,

    /// <summary>Suspensa por inadimplência ou ação administrativa — bloqueia features.</summary>
    Suspensa,

    /// <summary>Cancelada pelo dono — bloqueia features. Estado terminal de cancelamento voluntário.</summary>
    Cancelada,

    /// <summary>Trial encerrou sem conversão. Estado terminal até nova assinatura/upgrade.</summary>
    Expirada
}
