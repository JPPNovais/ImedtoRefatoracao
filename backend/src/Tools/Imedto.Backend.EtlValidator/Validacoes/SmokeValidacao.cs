using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Imedto.Backend.EtlValidator.Validacoes;

/// <summary>
/// Smoke test funcional: autentica usuários reais via API BFF e dispara queries
/// representativas, validando 200/204 e shape do payload — sem logar PII.
/// </summary>
public sealed class SmokeValidacao : IValidacao
{
    private readonly Opcoes _opcoes;

    public SmokeValidacao(Opcoes opcoes) => _opcoes = opcoes;

    public string Nome => "Smoke test funcional";

    public async Task ExecutarAsync(RelatorioCompleto relatorio, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.ArquivoSmokeUsers))
        {
            relatorio.Smokes.Add(new ResultadoSmoke(
                "-", false, "config",
                "Arquivo --smoke-users não informado — smoke test ignorado."));
            return;
        }

        if (!File.Exists(_opcoes.ArquivoSmokeUsers))
        {
            relatorio.Smokes.Add(new ResultadoSmoke(
                "-", false, "config",
                $"Arquivo de smoke users não encontrado: {_opcoes.ArquivoSmokeUsers}"));
            return;
        }

        List<UsuarioSmoke> usuarios;
        try
        {
            await using var fs = File.OpenRead(_opcoes.ArquivoSmokeUsers);
            usuarios = await JsonSerializer.DeserializeAsync<List<UsuarioSmoke>>(
                fs, JsonOpts, ct);
        }
        catch (Exception ex)
        {
            relatorio.Smokes.Add(new ResultadoSmoke(
                "-", false, "parse",
                $"Falha ao ler smoke-users: {ex.GetType().Name}"));
            return;
        }

        if (usuarios is null || usuarios.Count == 0)
        {
            relatorio.Smokes.Add(new ResultadoSmoke(
                "-", false, "config", "Lista de smoke users vazia."));
            return;
        }

        foreach (var u in usuarios)
        {
            ct.ThrowIfCancellationRequested();
            var id = HashAnonimo(u.Email ?? "?");
            await ExecutarUsuario(u, id, relatorio, ct);
        }
    }

    private async Task ExecutarUsuario(
        UsuarioSmoke u, string id, RelatorioCompleto relatorio, CancellationToken ct)
    {
        var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer() };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(_opcoes.ApiBaseUrl) };

        // 1. Login
        try
        {
            var resp = await http.PostAsJsonAsync("/api/auth/login",
                new { email = u.Email, senha = u.Senha }, JsonOpts, ct);
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                relatorio.Smokes.Add(new ResultadoSmoke(id, false, "login",
                    $"HTTP {(int)resp.StatusCode}"));
                return;
            }
        }
        catch (Exception ex)
        {
            relatorio.Smokes.Add(new ResultadoSmoke(id, false, "login",
                $"Exceção: {ex.GetType().Name}"));
            return;
        }

        // 2. /me
        try
        {
            var resp = await http.GetAsync("/api/auth/me", ct);
            if (!resp.IsSuccessStatusCode)
            {
                relatorio.Smokes.Add(new ResultadoSmoke(id, false, "me",
                    $"HTTP {(int)resp.StatusCode}"));
                return;
            }
            var body = await resp.Content.ReadFromJsonAsync<MeResponse>(JsonOpts, ct);
            if (body is null || string.IsNullOrEmpty(body.Id))
            {
                relatorio.Smokes.Add(new ResultadoSmoke(id, false, "me",
                    "Payload sem id."));
                return;
            }
        }
        catch (Exception ex)
        {
            relatorio.Smokes.Add(new ResultadoSmoke(id, false, "me",
                $"Exceção: {ex.GetType().Name}"));
            return;
        }

        // 3. Pacientes
        try
        {
            var resp = await http.GetAsync("/api/pacientes?pagina=1&tamanho=10", ct);
            if (!resp.IsSuccessStatusCode)
            {
                relatorio.Smokes.Add(new ResultadoSmoke(id, false, "pacientes",
                    $"HTTP {(int)resp.StatusCode}"));
                return;
            }
        }
        catch (Exception ex)
        {
            relatorio.Smokes.Add(new ResultadoSmoke(id, false, "pacientes",
                $"Exceção: {ex.GetType().Name}"));
            return;
        }

        // 4. Agendamentos
        try
        {
            var inicio = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var resp = await http.GetAsync($"/api/agendamentos?dataInicio={inicio}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                relatorio.Smokes.Add(new ResultadoSmoke(id, false, "agendamentos",
                    $"HTTP {(int)resp.StatusCode}"));
                return;
            }
        }
        catch (Exception ex)
        {
            relatorio.Smokes.Add(new ResultadoSmoke(id, false, "agendamentos",
                $"Exceção: {ex.GetType().Name}"));
            return;
        }

        // 5. Logout
        try
        {
            var resp = await http.PostAsync("/api/auth/logout", content: null, ct);
            if (resp.StatusCode != HttpStatusCode.NoContent
                && resp.StatusCode != HttpStatusCode.OK)
            {
                relatorio.Smokes.Add(new ResultadoSmoke(id, false, "logout",
                    $"HTTP {(int)resp.StatusCode}"));
                return;
            }
        }
        catch (Exception ex)
        {
            relatorio.Smokes.Add(new ResultadoSmoke(id, false, "logout",
                $"Exceção: {ex.GetType().Name}"));
            return;
        }

        relatorio.Smokes.Add(new ResultadoSmoke(id, true, "ok", "Todas as etapas passaram."));
    }

    /// <summary>Hash de 8 chars do email para identificação sem expor PII no relatório.</summary>
    private static string HashAnonimo(string entrada)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(entrada));
        var sb = new StringBuilder(16);
        for (int i = 0; i < 4; i++) sb.Append(bytes[i].ToString("x2"));
        return $"u#{sb}";
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class UsuarioSmoke
    {
        public string Email { get; set; }
        public string Senha { get; set; }
        [JsonPropertyName("estab_esperado")]
        public long? EstabEsperado { get; set; }
    }

    private sealed class MeResponse
    {
        public string Id { get; set; }
    }
}
