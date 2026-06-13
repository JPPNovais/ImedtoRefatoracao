namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

public class ModeloDescricaoCirurgicaDto
{
    public long Id { get; set; }
    // EstabelecimentoId omitido (LGPD/minimização): UI distingue padrão-sistema via EhPadraoSistema.
    public string Titulo { get; set; }
    public string Corpo { get; set; }
    public bool Ativo { get; set; }
    public bool EhPadraoSistema { get; set; }
}
