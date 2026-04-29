using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Ia;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Ia;

/// <summary>
/// Decorator de <see cref="IIaService"/> que adiciona, antes de chamar a IA real:
///   1. Rate limit por usuário (janela de 1 min, configurável em <c>Ia:LimitePorMinuto</c>).
///   2. Cache de outputs por hash SHA256 do prompt (TTL em <c>Ia:CacheTtlHoras</c>).
///   3. Audit log com hashes de prompt/resposta (NUNCA conteúdo cru — LGPD).
///
/// O streaming preserva o comportamento original: cada chunk recebido do inner é
/// imediatamente entregue ao caller via <c>yield return</c>, e em paralelo
/// acumulado num <see cref="StringBuilder"/> para gerar hash + cache no final.
/// </summary>
public class RateLimitedIaService : IIaService
{
    private readonly IIaService _inner;
    private readonly IAiAuditRepository _audit;
    private readonly IAiCacheRepository _cache;
    private readonly IAiRateLimitRepository _rate;
    private readonly IVinculoRepository _vinculos;
    private readonly IHttpContextAccessor _http;
    private readonly IOptions<IaOptions> _opts;
    private readonly string _modelo;

    public RateLimitedIaService(
        IIaService inner,
        IAiAuditRepository audit,
        IAiCacheRepository cache,
        IAiRateLimitRepository rate,
        IVinculoRepository vinculos,
        IHttpContextAccessor http,
        IOptions<IaOptions> opts,
        IConfiguration config)
    {
        _inner = inner;
        _audit = audit;
        _cache = cache;
        _rate  = rate;
        _vinculos = vinculos;
        _http  = http;
        _opts  = opts;
        _modelo = config["Ia:Modelo"] ?? "claude-haiku-4-5-20251001";
    }

    public async IAsyncEnumerable<string> SugerirSecaoProntuarioAsync(
        SugestaoSecaoProntuarioRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (usuarioId, estabelecimentoId) = ResolverContexto();

        // TODO Fase 3: substituir por validação de permissão fina ModeloPermissao.AssistenteClinico.
        // Trava mínima: só pode chamar IA quem tem vínculo ativo no estabelecimento OU é o dono.
        // Reusa a regra unificada do IVinculoRepository (mesma usada nos handlers de agendamento)
        // — fonte da verdade única para "este usuário pode atuar neste estabelecimento".
        var podeAtuar = await _vinculos.PodeAtuarComoProfissional(usuarioId, estabelecimentoId);
        if (!podeAtuar)
            throw new BusinessException("Você não tem permissão para usar o assistente de IA neste estabelecimento.");

        // 1) Rate limit
        var permitido = await _rate.RegistrarTentativaAsync(
            usuarioId, _opts.Value.LimitePorMinuto, ct);
        if (!permitido)
            throw new BusinessException("Limite de uso da IA atingido. Aguarde 1 minuto.");

        // 2) Sanitização PII (LGPD): redige CPF/CNPJ/telefone/email/CEP/RG do conteúdo
        // que vai para a IA. Estratégia string-level sobre o JSON serializado — qualquer
        // campo novo que venha a entrar no request é coberto por padrão. Campos que precisam
        // de PII de propósito NÃO devem ir para a IA — minimização semântica é responsabilidade
        // de quem monta o request.
        var requestSanitizado = SanitizarRequest(request);

        // 3) Cache (hash determinístico sobre o conteúdo JÁ sanitizado — assim cache e
        // audit operam exclusivamente sobre o conteúdo sem PII).
        var promptHash = HashSha256(JsonSerializer.Serialize(requestSanitizado));
        var cached = await _cache.ObterAsync(promptHash, ct);
        if (cached is not null)
        {
            await _audit.RegistrarAsync(AiAuditLog.Criar(
                usuarioId:         usuarioId,
                estabelecimentoId: estabelecimentoId,
                promptHash:        promptHash,
                responseHash:      HashSha256(cached),
                modelo:            _modelo,
                endpoint:          "sugestao-secao-cache",
                duracaoMs:         0,
                sucesso:           true,
                erroMensagem:      null), ct);

            yield return cached;
            yield break;
        }

        // 4) Chama o inner com o request sanitizado — streaming chunk-a-chunk
        await foreach (var chunk in StreamComAuditEcache(requestSanitizado, usuarioId, estabelecimentoId, promptHash, ct))
            yield return chunk;
    }

    /// <summary>
    /// Serializa o request, aplica regexes de PII string-level e reidrata. Caso a deserialização
    /// falhe (cenário improvável — JSON gerado por nós), volta a um request mínimo apenas com o
    /// título da seção sanitizado para garantir que o conteúdo cru nunca chegue ao Anthropic.
    /// </summary>
    private static SugestaoSecaoProntuarioRequest SanitizarRequest(SugestaoSecaoProntuarioRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var sanitizado = PiiSanitizer.Sanitize(json);

        try
        {
            return JsonSerializer.Deserialize<SugestaoSecaoProntuarioRequest>(sanitizado)
                ?? new SugestaoSecaoProntuarioRequest();
        }
        catch (JsonException)
        {
            // Fail-secure: nunca propagar conteúdo cru se a redação corromper o JSON.
            return new SugestaoSecaoProntuarioRequest
            {
                SecaoAlvoTitulo = PiiSanitizer.Sanitize(request.SecaoAlvoTitulo),
                SecoesContexto = new Dictionary<string, string>()
            };
        }
    }

    private async IAsyncEnumerable<string> StreamComAuditEcache(
        SugestaoSecaoProntuarioRequest request,
        Guid usuarioId,
        long estabelecimentoId,
        string promptHash,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var sb = new StringBuilder();
        var inicio = DateTime.UtcNow;
        Exception? erro = null;
        var enumerator = _inner.SugerirSecaoProntuarioAsync(request, ct).GetAsyncEnumerator(ct);

        try
        {
            while (true)
            {
                bool temProximo;
                try
                {
                    temProximo = await enumerator.MoveNextAsync();
                }
                catch (Exception ex)
                {
                    erro = ex;
                    break;
                }

                if (!temProximo) break;

                var chunk = enumerator.Current;
                sb.Append(chunk);
                yield return chunk;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();

            var resposta = sb.ToString();
            var duracao = (int)(DateTime.UtcNow - inicio).TotalMilliseconds;

            // Audit registra sempre — sucesso, falha ou cancelamento.
            // CancellationToken.None: não queremos abortar o registro se o caller cancelou.
            try
            {
                await _audit.RegistrarAsync(AiAuditLog.Criar(
                    usuarioId:         usuarioId,
                    estabelecimentoId: estabelecimentoId,
                    promptHash:        promptHash,
                    responseHash:      erro is null && resposta.Length > 0 ? HashSha256(resposta) : null,
                    modelo:            _modelo,
                    endpoint:          "sugestao-secao",
                    duracaoMs:         duracao,
                    sucesso:           erro is null,
                    erroMensagem:      erro?.Message), CancellationToken.None);
            }
            catch
            {
                // Audit não pode quebrar o fluxo do usuário — engole erros de gravação aqui.
            }

            if (erro is null && !string.IsNullOrWhiteSpace(resposta))
            {
                try
                {
                    await _cache.SalvarAsync(
                        promptHash,
                        estabelecimentoId,
                        "sugestao-secao",
                        resposta,
                        DateTime.UtcNow.AddHours(_opts.Value.CacheTtlHoras),
                        ct: CancellationToken.None);
                }
                catch
                {
                    // Cache é otimização — falha não afeta o caller.
                }
            }

            if (erro is not null)
                throw erro;
        }
    }

    private (Guid usuarioId, long estabelecimentoId) ResolverContexto()
    {
        var ctx = _http.HttpContext
            ?? throw new BusinessException("Requisição inválida: contexto HTTP ausente.");

        var subClaim = ctx.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var usuarioId))
            throw new BusinessException("Usuário não autenticado.");

        var estabHeader = ctx.Request.Headers["X-Estabelecimento-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(estabHeader) || !long.TryParse(estabHeader, out var estabelecimentoId))
            throw new BusinessException("Estabelecimento não informado.");

        return (usuarioId, estabelecimentoId);
    }

    private static string HashSha256(string entrada)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(entrada));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
