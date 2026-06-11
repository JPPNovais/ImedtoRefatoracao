namespace Imedto.Backend.Domain.Admin;

public interface IImedtoConfigTrialRepository
{
    Task<ImedtoConfigTrial?> ObterAsync(CancellationToken ct = default);
    void Adicionar(ImedtoConfigTrial config);
    void Atualizar(ImedtoConfigTrial config);
}
