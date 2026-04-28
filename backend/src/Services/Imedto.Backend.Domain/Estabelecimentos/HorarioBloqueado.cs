namespace Imedto.Backend.Domain.Estabelecimentos;

/// <summary>
/// Bloqueio diário recorrente (ex: pausa de almoço) dentro do horário de funcionamento.
/// Persistido como item de array JSONB em <c>estabelecimentos.horarios_bloqueados</c>.
/// </summary>
public record HorarioBloqueado(Guid Id, TimeOnly Inicio, TimeOnly Fim, string Descricao);
