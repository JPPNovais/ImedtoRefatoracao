namespace Imedto.Backend.Domain.Lgpd;

public enum MotivoAnonimizacao
{
    /// <summary>Titular exerceu o direito ao esquecimento (Art. 18 LGPD).</summary>
    DireitoEsquecimento,

    /// <summary>Prazo de retenção legal vencido (CFM 1.821/07 — 20 anos).</summary>
    RetencaoVencida,

    /// <summary>Conta inativada voluntariamente pelo titular.</summary>
    InativacaoVoluntaria
}
