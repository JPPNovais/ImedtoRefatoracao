using System.Text.Json;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Automacoes;

/// <summary>
/// Executor V1 da DSL de ações. Formato fechado:
/// <code>
/// [
///   { "tipo": "enviar-notificacao",
///     "parametros": {
///       "usuarioId": "&lt;guid|campo do payload&gt;",
///       "titulo": "Texto",
///       "mensagem": "Texto",
///       "categoria": "Automacao",
///       "linkAcao": "/agenda?id=123"
///     }
///   },
///   { "tipo": "enviar-email", "parametros": { ... } }
/// ]
/// </code>
///
/// V1 suporta <c>enviar-notificacao</c> (delega para <see cref="INotificacaoService"/>)
/// e <c>enviar-email</c> (item 4.7 — delega para <see cref="IEmailService"/>; em dev sem
/// API key, cai no <c>NoOpEmailService</c>).
/// Outras ações lançam <see cref="BusinessException"/> — falha controlada que vira retry.
/// </summary>
public class ExecutorAcao : IExecutorAcao
{
    private readonly INotificacaoService _notificacoes;
    private readonly IEmailService _email;
    private readonly ILogger<ExecutorAcao> _logger;

    public ExecutorAcao(
        INotificacaoService notificacoes,
        IEmailService email,
        ILogger<ExecutorAcao> logger)
    {
        _notificacoes = notificacoes;
        _email = email;
        _logger = logger;
    }

    public async Task ExecutarAsync(string acoesJson, JsonDocument payload, long estabelecimentoId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(acoesJson))
            throw new BusinessException("Regra sem ações configuradas.");

        using var doc = JsonDocument.Parse(acoesJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new BusinessException("acoes_json deve ser um array.");

        foreach (var acao in doc.RootElement.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            await ExecutarAcaoAsync(acao, payload, estabelecimentoId, ct);
        }
    }

    private async Task ExecutarAcaoAsync(JsonElement acao, JsonDocument payload, long estabelecimentoId, CancellationToken ct)
    {
        if (acao.ValueKind != JsonValueKind.Object)
            throw new BusinessException("Cada ação deve ser um objeto.");
        if (!acao.TryGetProperty("tipo", out var tipoEl) || tipoEl.ValueKind != JsonValueKind.String)
            throw new BusinessException("Ação sem 'tipo'.");

        var tipo = tipoEl.GetString()!;
        var parametros = acao.TryGetProperty("parametros", out var pEl) ? pEl : default;

        switch (tipo)
        {
            case "enviar-notificacao":
                await EnviarNotificacaoAsync(parametros, payload, estabelecimentoId, ct);
                break;

            case "enviar-email":
                await EnviarEmailAsync(parametros, ct);
                break;

            default:
                throw new BusinessException($"Ação de tipo '{tipo}' não suportada na V1.");
        }
    }

    private async Task EnviarNotificacaoAsync(JsonElement parametros, JsonDocument payload, long estabelecimentoId, CancellationToken ct)
    {
        if (parametros.ValueKind != JsonValueKind.Object)
            throw new BusinessException("Ação 'enviar-notificacao' requer objeto 'parametros'.");

        var usuarioIdRaw = LerString(parametros, "usuarioId")
            ?? throw new BusinessException("Ação 'enviar-notificacao' requer 'usuarioId'.");

        // Permite "usuarioId": "$.profissionalUsuarioId" para puxar do payload do evento.
        var usuarioId = ResolverUsuarioId(usuarioIdRaw, payload)
            ?? throw new BusinessException($"Não foi possível resolver 'usuarioId' = '{usuarioIdRaw}' a partir do payload.");

        var titulo = LerString(parametros, "titulo")
            ?? throw new BusinessException("Ação 'enviar-notificacao' requer 'titulo'.");
        var mensagem = LerString(parametros, "mensagem")
            ?? throw new BusinessException("Ação 'enviar-notificacao' requer 'mensagem'.");

        // Categoria default = Automacao (ações disparadas por regras configuradas).
        var categoriaStr = LerString(parametros, "categoria") ?? "Automacao";
        if (!Enum.TryParse<CategoriaNotificacao>(categoriaStr, true, out var categoria))
            categoria = CategoriaNotificacao.Automacao;

        var linkAcao = LerString(parametros, "linkAcao");

        await _notificacoes.EnviarAsync(
            usuarioId: usuarioId,
            estabelecimentoId: estabelecimentoId,
            titulo: titulo,
            mensagem: mensagem,
            categoria: categoria,
            linkAcao: linkAcao,
            ct: ct);
    }

    private async Task EnviarEmailAsync(JsonElement parametros, CancellationToken ct)
    {
        if (parametros.ValueKind != JsonValueKind.Object)
            throw new BusinessException("Ação 'enviar-email' requer objeto 'parametros'.");

        var para = LerString(parametros, "para")
            ?? throw new BusinessException("Ação 'enviar-email' requer 'para'.");
        var assunto = LerString(parametros, "assunto")
            ?? throw new BusinessException("Ação 'enviar-email' requer 'assunto'.");
        var corpoHtml = LerString(parametros, "corpoHtml")
            ?? throw new BusinessException("Ação 'enviar-email' requer 'corpoHtml'.");
        var corpoTexto = LerString(parametros, "corpoTexto");

        // IEmailService já trata retry e logs LGPD-safe internamente.
        // Falha permanente NÃO bloqueia o handler (logado pelo serviço).
        await _email.EnviarAsync(para, assunto, corpoHtml, corpoTexto, ct);
    }

    private static Guid? ResolverUsuarioId(string raw, JsonDocument payload)
    {
        // GUID literal.
        if (Guid.TryParse(raw, out var direto)) return direto;

        // Referência ao payload: "$.profissionalUsuarioId" ou apenas "profissionalUsuarioId".
        var campo = raw.StartsWith("$.") ? raw.Substring(2) : raw;

        foreach (var prop in payload.RootElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, campo, StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.String
                && Guid.TryParse(prop.Value.GetString(), out var puxado))
            {
                return puxado;
            }
        }
        return null;
    }

    private static string? LerString(JsonElement obj, string nome)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(nome, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }
}
