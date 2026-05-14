namespace Imedto.Backend.Domain.Vinculos;

public interface IVinculoRepository
{
    /// <summary>
    /// Carrega o vínculo SEM filtro de tenant — usado em fluxos onde o caller
    /// é o próprio profissional (Aceitar convite/Inativar pelo profissional)
    /// e o tenant da request não está disponível. Caller DEVE validar
    /// <c>vinculo.ProfissionalUsuarioId == solicitante</c>.
    /// Para fluxos do dono (com tenant ativo), usar <see cref="ObterPorIdNoEstabelecimentoOuNulo"/>.
    /// </summary>
    Task<VinculoProfissionalEstabelecimento?> ObterPorIdOuNulo(long id);

    /// <summary>
    /// Carrega o vínculo filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Usar em fluxos com tenant ativo (admin do dono).
    /// Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<VinculoProfissionalEstabelecimento?> ObterPorIdNoEstabelecimentoOuNulo(long id, long estabelecimentoId);

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

    /// <summary>
    /// Lista todos os vínculos do usuário em status <see cref="VinculoStatus.Convidado"/>.
    /// Usado pelo fluxo "Aceitar convite por link de e-mail" (POST /api/auth/aceitar-convite),
    /// que precisa ativar automaticamente o(s) vínculo(s) pendente(s) imediatamente após
    /// o convidado definir sua senha — sem essa etapa, o usuário entra na plataforma mas
    /// continua sem tenant ativo até clicar manualmente em "Aceitar" em /meus-convites.
    /// </summary>
    Task<IReadOnlyList<VinculoProfissionalEstabelecimento>> ListarPendentesPorUsuario(Guid profissionalUsuarioId);

    Task Salvar(VinculoProfissionalEstabelecimento vinculo);
}
