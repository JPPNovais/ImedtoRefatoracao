namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Lookup de leitura otimizado para resolução de profissional durante a carga de agendamentos históricos.
///
/// Usado pelo <see cref="Imedto.Backend.Application.Migracao.Jobs.CarregarOnda1JobHandler"/>
/// para localizar o UsuarioId do profissional pelo nome — os payloads canônicos contêm
/// <c>profissional_nome</c> (texto), não o Guid interno do sistema.
///
/// Dapper puro — sem EF.
/// </summary>
public interface IMigracaoAgendamentoLookup
{
    /// <summary>
    /// Retorna o UsuarioId do profissional cujo nome completo bate (case-insensitive) no tenant.
    /// Retorna null se não encontrado ou se houver ambiguidade (mais de 1 profissional com o mesmo nome).
    /// </summary>
    Task<Guid?> ObterProfissionalIdPorNomeOuNulo(string nome, long estabelecimentoId, CancellationToken ct = default);
}
