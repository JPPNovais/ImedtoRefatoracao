namespace Imedto.Backend.Domain.Automacoes;

public interface IEmailService
{
    Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default);
}
