namespace Imedto.Backend.Domain.Estabelecimentos;

/// <summary>
/// Data específica em que o estabelecimento não opera (ex: feriado, recesso).
/// Persistido como item de array JSONB em <c>estabelecimentos.datas_bloqueadas</c>.
/// </summary>
public record DataBloqueada(Guid Id, DateOnly Data, string Descricao);
