using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Conta apenas o total de evoluções do prontuário de um paciente.
/// Usado para badges/contadores na UI sem precisar trafegar a timeline inteira
/// (LGPD: minimização — não expõe dado clínico, apenas o número agregado).
/// Retorna 0 se o paciente ainda não tem prontuário iniciado.
/// </summary>
public class ContarEvolucoesProntuarioPacienteQuery : IQuery<ContagemEvolucoesDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public record ContagemEvolucoesDto(int Total);
