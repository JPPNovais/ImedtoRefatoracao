namespace Imedto.Backend.Domain.Vinculos;

public interface IVinculoRepository
{
    Task<VinculoProfissionalEstabelecimento> ObterPorId(long id);
    Task<VinculoProfissionalEstabelecimento> ObterVinculoAtivoOuPendente(Guid profissionalUsuarioId, long estabelecimentoId);

    /// <summary>
    /// Retorna QUALQUER vínculo (ativo, convidado ou inativo) entre profissional × estabelecimento,
    /// ou null se não existir. Usado pelo fluxo de re-convite — vínculos inativos podem ser reativados
    /// em vez de gerar novo registro (evita duplicidade e preserva histórico).
    /// </summary>
    Task<VinculoProfissionalEstabelecimento?> ObterPorProfissionalEEstabelecimentoOuNulo(
        Guid profissionalUsuarioId, long estabelecimentoId);

    /// <summary>
    /// Regra unificada de "este usuário pode atuar como profissional neste estabelecimento":
    /// possui vínculo não-inativo OU é o dono do estabelecimento.
    /// Esta é a fonte da verdade — usar nos handlers de Criar/Atualizar agendamento e em qualquer
    /// outra operação que dependa do conceito de "atuante". A query de listagem
    /// (<c>VinculoQueryRepository.ListarProfissionaisDoEstabelecimento</c>) deve refletir esta mesma regra.
    /// </summary>
    Task<bool> PodeAtuarComoProfissional(Guid usuarioId, long estabelecimentoId);

    Task Salvar(VinculoProfissionalEstabelecimento vinculo);
}
