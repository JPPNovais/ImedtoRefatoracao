namespace Imedto.Backend.Contracts.Convenios.Queries.Results;

public record ConvenioListadoDto(
    long Id,
    string Nome,
    string? RegistroAns,
    bool Ativo,
    int TotalPlanos);

public record ConvenioDetalheDto(
    long Id,
    string Nome,
    string? RegistroAns,
    bool Ativo,
    IReadOnlyList<ConvenioPlanoDto> Planos);

public record ConvenioPlanoDto(
    long Id,
    string Nome,
    bool Ativo);

/// <summary>DTO minimalista para select do check-in e select de carteirinha.</summary>
public record ConvenioSelectDto(
    long Id,
    string Nome,
    IReadOnlyList<ConvenioPlanoDto> Planos);
