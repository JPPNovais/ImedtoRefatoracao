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
    /// Lookup sem filtro de tenant — uso restrito a operacoes cross-tenant
    /// legitimas. Hoje so usado pelo <c>AnonimizacaoService</c> (job de
    /// retencao + handler "AnonimizarMinhaConta" do titular). Qualquer outro
    /// caller deve preferir a sobrecarga com <c>estabelecimentoId</c>
    /// (defense-in-depth LGPD).
    /// </summary>
    [Obsolete("Use ObterPorIdOuNulo(long, long) para garantir filtro de tenant. Esta sobrecarga so eh permitida em AnonimizacaoService (operacao cross-tenant legitima).")]
    Task<Paciente?> ObterPorIdOuNulo(long id);

    Task<bool> ExisteCpfNoEstabelecimento(string cpf, long estabelecimentoId, long ignorarPacienteId);
    Task Salvar(Paciente paciente);
}
