namespace Imedto.Backend.Domain.Pacientes;

public interface IPacienteRepository
{
    /// <summary>
    /// Carrega o paciente filtrando por <paramref name="estabelecimentoId"/> (defense-in-depth
    /// LGPD: nao confia que o handler vai checar depois). Retorna null se inexistente
    /// ou de outro tenant — em ambos os casos o handler deve responder "nao encontrado"
    /// para nao vazar existencia.
    /// </summary>
    Task<Paciente?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Existe so para chamadores que ainda nao tem o tenant em contexto (caso raro).
    /// Prefira a sobrecarga com estabelecimentoId.
    /// </summary>
    [Obsolete("Use ObterPorId(long, long) para garantir filtro de tenant — defense-in-depth LGPD. Esta sobrecarga sera removida assim que todos os modulos forem migrados (Fases 3-5).")]
    Task<Paciente> ObterPorId(long id);

    [Obsolete("Use ObterPorIdOuNulo(long, long) para garantir filtro de tenant — defense-in-depth LGPD. Esta sobrecarga sera removida assim que todos os modulos forem migrados (Fases 3-5).")]
    Task<Paciente> ObterPorIdOuNulo(long id);

    Task<bool> ExisteCpfNoEstabelecimento(string cpf, long estabelecimentoId, long ignorarPacienteId);
    Task Salvar(Paciente paciente);
}
