namespace Imedto.Backend.Domain.Prontuarios;

public interface IProntuarioVariavelPoolRepository
{
    Task<ProntuarioVariavelPool> ObterPorId(long id);
    Task<ProntuarioVariavelPool> ObterPorIdOuNulo(long id);
    Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, TipoVariavelPool tipo, string nome, long ignorarId);
    Task Salvar(ProntuarioVariavelPool item);
    Task Excluir(ProntuarioVariavelPool item);
}
