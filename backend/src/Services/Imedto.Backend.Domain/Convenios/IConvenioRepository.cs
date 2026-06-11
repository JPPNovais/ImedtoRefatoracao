namespace Imedto.Backend.Domain.Convenios;

public interface IConvenioRepository
{
    Task<Convenio?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<bool> TemCarteirinhasOuCobrancas(long convenioId);
    Task Salvar(Convenio convenio);
    Task Excluir(Convenio convenio);
}
