namespace Imedto.Backend.Domain.Prontuarios;

public enum TipoAcessoProntuario
{
    /// <summary>GET / consulta (incluindo timeline).</summary>
    Leitura,

    /// <summary>POST / registro de evolução, início, etc.</summary>
    Escrita,

    /// <summary>Exportação (PDF) — histórico completo ou evolução individual.</summary>
    Exportacao
}
