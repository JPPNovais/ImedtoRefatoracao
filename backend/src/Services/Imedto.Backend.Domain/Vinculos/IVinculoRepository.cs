namespace Imedto.Backend.Domain.Vinculos;

public interface IVinculoRepository
{
    Task<VinculoProfissionalEstabelecimento> ObterPorId(long id);
    Task<VinculoProfissionalEstabelecimento> ObterVinculoAtivoOuPendente(Guid profissionalUsuarioId, long estabelecimentoId);

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
