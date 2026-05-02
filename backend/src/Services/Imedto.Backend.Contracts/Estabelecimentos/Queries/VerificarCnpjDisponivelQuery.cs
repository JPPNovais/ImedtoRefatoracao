using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Estabelecimentos.Queries;

/// <summary>
/// Verifica se um CNPJ é válido (algoritmo padrão) e está disponível
/// (não cadastrado em outro estabelecimento). Usado no onboarding.
/// </summary>
public class VerificarCnpjDisponivelQuery : IQuery<VerificarCnpjDisponivelResult>
{
    public string Cnpj { get; set; } = "";
}

public record VerificarCnpjDisponivelResult(
    bool Valido,
    bool Disponivel,
    string? Motivo);
