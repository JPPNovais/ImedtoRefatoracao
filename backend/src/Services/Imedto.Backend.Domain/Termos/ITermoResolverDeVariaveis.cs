namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Substitui variáveis <c>{{paciente.nome}}</c>, <c>{{estabelecimento.nome}}</c>, etc.
/// no momento da emissão. Lista fechada de variáveis (ver implementação) — qualquer
/// placeholder não reconhecido é deixado como está (transparência).
///
/// Resolução server-side garante que o snapshot reflete o estado real no momento da
/// emissão e nunca expõe campos que o paciente/usuário não tem permissão de ver.
/// </summary>
public interface ITermoResolverDeVariaveis
{
    /// <summary>
    /// Substitui as variáveis no conteúdo bruto e retorna o HTML final.
    /// </summary>
    Task<string> ResolverAsync(string conteudoHtml, ContextoDeVariaveis contexto, CancellationToken ct = default);

    /// <summary>Lista de variáveis suportadas — para UI de descoberta.</summary>
    IReadOnlyList<VariavelDisponivel> VariaveisDisponiveis { get; }
}

/// <summary>
/// Contexto da resolução. Todos os campos são opcionais — quando ausente, a variável
/// resolve para o fallback configurado (ex: <c>___________</c> para nome, <c>__/__/____</c>
/// para datas).
/// </summary>
public sealed record ContextoDeVariaveis(
    long PacienteId,
    long EstabelecimentoId,
    Guid? ProfissionalUsuarioId);

/// <summary>Metadado de uma variável disponível (categoria → label → key).</summary>
public sealed record VariavelDisponivel(string Chave, string Rotulo, string Categoria);
