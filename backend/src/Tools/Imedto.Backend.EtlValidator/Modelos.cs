using System.Collections.Generic;

namespace Imedto.Backend.EtlValidator;

public enum Severidade { Ok, Aviso, Erro }

public sealed record ResultadoContagem(
    string TabelaLegado,
    string TabelaNovo,
    long ContagemLegado,
    long ContagemNovo,
    double DiffPercentual,
    Severidade Severidade,
    string Mensagem);

public sealed record ResultadoIntegridade(
    string Descricao,
    long Quantidade,
    Severidade Severidade);

public sealed record ResultadoSmoke(
    string IdentificadorAnonimizado,
    bool Sucesso,
    string Etapa,
    string Mensagem);

public sealed class RelatorioCompleto
{
    public List<ResultadoContagem> Contagens { get; } = new();
    public List<ResultadoIntegridade> Integridades { get; } = new();
    public List<ResultadoSmoke> Smokes { get; } = new();
    public bool ErroFatal { get; set; }
    public string ErroFatalMensagem { get; set; }

    public bool ToleranciaExcedida =>
        Contagens.Exists(c => c.Severidade == Severidade.Erro)
        || Integridades.Exists(i => i.Severidade == Severidade.Erro)
        || Smokes.Exists(s => !s.Sucesso);
}
