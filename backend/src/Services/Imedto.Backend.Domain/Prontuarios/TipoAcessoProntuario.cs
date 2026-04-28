namespace Imedto.Backend.Domain.Prontuarios;

public enum TipoAcessoProntuario
{
    /// <summary>GET / consulta (incluindo timeline).</summary>
    Leitura,

    /// <summary>POST / registro de evolução, início, etc.</summary>
    Escrita
}
