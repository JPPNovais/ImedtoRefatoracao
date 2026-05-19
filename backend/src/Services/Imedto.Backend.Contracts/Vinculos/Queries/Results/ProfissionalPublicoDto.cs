namespace Imedto.Backend.Contracts.Vinculos.Queries.Results;

/// <summary>
/// DTO minimizado (LGPD) de profissional de um estabelecimento, exposto a todos
/// os membros do tenant (Dono, Profissional e Recepção). Usado em seletores —
/// agenda, prontuário, orçamento — onde a UX precisa só nome + especialidade.
///
/// NÃO contém: e-mail, modelo de permissão, datas de convite/aceite,
/// vínculoId interno. Esses campos vivem no <see cref="ProfissionalVinculadoDto"/>
/// e ficam atrás da permissão "equipe.ver" (= apenas Dono ou modelos que
/// concedam a área).
/// </summary>
public class ProfissionalPublicoDto
{
    public Guid UsuarioId { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string? Especialidade { get; set; }
    public string? Conselho { get; set; }
    /// <summary>"Ativo" ou "Dono" — Inativo nunca aparece aqui (já filtrado no SQL).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// URL presigned (S3) da foto do profissional, quando houver. PII baixo
    /// risco — visível para qualquer membro do tenant (mesma regra de
    /// <see cref="NomeCompleto"/> e <see cref="Especialidade"/>).
    /// </summary>
    public string? FotoUrl { get; set; }
}
