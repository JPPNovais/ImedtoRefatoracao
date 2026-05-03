namespace Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;

public class ModeloPermissaoDto
{
    public long Id { get; set; }
    // EstabelecimentoId removido (LGPD): vem da rota; ampliava IDOR.
    // AtualizadoEm removido: nao consumido pelo front (so estava na interface TS).
    public string Nome { get; set; } = string.Empty;
    public string TipoAcesso { get; set; } = string.Empty;
    public IReadOnlyList<string> Permissoes { get; set; } = Array.Empty<string>();
    public bool EhPadrao { get; set; }
    public DateTime CriadoEm { get; set; }
}
