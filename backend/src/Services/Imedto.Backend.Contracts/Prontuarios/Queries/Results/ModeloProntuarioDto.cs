using System.Text.Json;

namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

public class ModeloProntuarioDto
{
    public long Id { get; set; }
    // EstabelecimentoId removido (LGPD): UI distingue padrao-sistema via EhPadraoSistema.
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public JsonElement Estrutura { get; set; }
    public bool EhPadraoSistema { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
