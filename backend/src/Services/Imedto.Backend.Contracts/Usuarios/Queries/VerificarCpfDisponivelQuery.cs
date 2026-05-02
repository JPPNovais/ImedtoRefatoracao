using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Usuarios.Queries;

/// <summary>
/// Verifica se um CPF está disponível para o usuário corrente
/// (válido sintaticamente E não cadastrado em outra conta).
/// </summary>
public class VerificarCpfDisponivelQuery : IQuery<VerificarCpfDisponivelResult>
{
    public Guid UsuarioId { get; set; }
    public string Cpf { get; set; } = "";
}

public record VerificarCpfDisponivelResult(
    bool Valido,
    bool Disponivel,
    string? Motivo);
