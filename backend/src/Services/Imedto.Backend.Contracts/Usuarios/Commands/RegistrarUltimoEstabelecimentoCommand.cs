using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Usuarios.Commands;

/// <summary>
/// Persiste o último estabelecimento acessado pelo usuário autenticado.
/// O UsuarioId é extraído do JWT pelo handler — o corpo apenas informa qual
/// estabelecimento deve ser registrado como o último.
/// </summary>
public class RegistrarUltimoEstabelecimentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
}
