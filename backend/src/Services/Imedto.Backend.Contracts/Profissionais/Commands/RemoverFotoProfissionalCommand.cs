using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Profissionais.Commands;

/// <summary>
/// Remove a foto do profissional autenticado — apaga o blob no storage e zera
/// <c>FotoUrl</c> no aggregate. O caller (controller) sempre preenche
/// <see cref="UsuarioId"/> com o usuário do <c>ICurrentUser</c>, nunca de
/// parâmetro externo. Idempotente: se já não havia foto, não falha.
/// </summary>
public class RemoverFotoProfissionalCommand : ICommand
{
    public Guid UsuarioId { get; set; }
}
