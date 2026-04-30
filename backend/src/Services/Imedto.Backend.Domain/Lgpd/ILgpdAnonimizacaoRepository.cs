namespace Imedto.Backend.Domain.Lgpd;

public interface ILgpdAnonimizacaoRepository
{
    Task Salvar(LgpdAnonimizacao registro);
    Task<IEnumerable<LgpdAnonimizacao>> ListarPorRegistro(string tabela, long registroId);
}
