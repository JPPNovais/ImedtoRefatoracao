using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Termos;

/// <summary>
/// Resolve as variáveis <c>{{...}}</c> server-side no momento da emissão. Lista fechada
/// definida em <see cref="VariaveisDisponiveis"/>. Variáveis não reconhecidas são
/// deixadas no HTML original — transparente para o tenant.
///
/// Lê dados via Dapper (leitura, sem rastrear no DbContext). Filtra por estabelecimento
/// sempre. Quando algum dado está ausente, usa o fallback documentado em
/// <see cref="Fallbacks"/> — placeholders visuais ("___________", "__/__/____").
///
/// Singleton-safe: stateless após o ctor.
/// </summary>
public sealed class TermoResolverDeVariaveis : ITermoResolverDeVariaveis
{
    private readonly string _connStr;
    private static readonly Regex Placeholder = new(@"\{\{\s*([a-z_.]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly CultureInfo PtBr = new("pt-BR");

    public TermoResolverDeVariaveis(AppReadConnectionString conn) => _connStr = conn.Value;

    public IReadOnlyList<VariavelDisponivel> VariaveisDisponiveis { get; } = new List<VariavelDisponivel>
    {
        new("paciente.nome", "Nome do paciente", "paciente"),
        new("paciente.cpf", "CPF (000.000.000-00)", "paciente"),
        new("paciente.documento_internacional", "Documento internacional", "paciente"),
        new("paciente.data_nascimento", "Data de nascimento (dd/mm/aaaa)", "paciente"),
        new("paciente.idade", "Idade calculada", "paciente"),
        new("paciente.telefone", "Telefone (00) 00000-0000", "paciente"),
        new("paciente.email", "E-mail", "paciente"),
        new("paciente.endereco", "Endereço", "paciente"),
        new("paciente.genero", "Gênero", "paciente"),
        new("estabelecimento.nome", "Nome fantasia", "estabelecimento"),
        new("estabelecimento.razao_social", "Razão social", "estabelecimento"),
        new("estabelecimento.cnpj", "CNPJ (XX.XXX.XXX/XXXX-XX)", "estabelecimento"),
        new("estabelecimento.endereco", "Endereço", "estabelecimento"),
        new("estabelecimento.telefone", "Telefone", "estabelecimento"),
        new("profissional.nome", "Nome do emissor", "profissional"),
        new("profissional.conselho_completo", "Conselho-UF Nº", "profissional"),
        new("profissional.especialidade", "Especialidade", "profissional"),
        new("data_atual", "Data atual por extenso (pt-BR)", "data"),
        new("data_atual_curta", "Data atual (dd/mm/aaaa)", "data"),
        new("cidade_atual", "Cidade do estabelecimento", "data"),
    }.AsReadOnly();

    private static class Fallbacks
    {
        public const string Linha = "___________";
        public const string Cpf = "___.___.___-__";
        public const string Data = "__/__/____";
        public const string Vazio = "";
    }

    public async Task<string> ResolverAsync(string conteudoHtml, ContextoDeVariaveis contexto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(conteudoHtml)) return conteudoHtml ?? string.Empty;
        if (!conteudoHtml.Contains("{{")) return conteudoHtml; // shortcut sem placeholders

        var dados = await CarregarDadosAsync(contexto, ct);
        var valores = MontarDicionarioDeValores(dados);

        return Placeholder.Replace(conteudoHtml, m =>
        {
            var chave = m.Groups[1].Value.Trim().ToLowerInvariant();
            return valores.TryGetValue(chave, out var v) ? v : m.Value; // mantém original se desconhecida
        });
    }

    // ─── Carga de dados via Dapper ────────────────────────────────────────────

    private async Task<DadosResolver> CarregarDadosAsync(ContextoDeVariaveis ctx, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        // Carrega paciente + estabelecimento numa só ida (queries simples, paralelas).
        var paciente = await conn.QuerySingleOrDefaultAsync<PacienteResolver>(
            new CommandDefinition("""
                SELECT  id              AS Id,
                        nome_completo   AS Nome,
                        cpf             AS Cpf,
                        documento_internacional AS DocumentoInternacional,
                        data_nascimento AS DataNascimento,
                        telefone        AS Telefone,
                        email           AS Email,
                        endereco        AS Endereco,
                        genero          AS Genero
                FROM    public.pacientes
                WHERE   id = @PacienteId
                  AND   estabelecimento_id = @EstabelecimentoId
                  AND   deletado_em IS NULL
                """, new { ctx.PacienteId, ctx.EstabelecimentoId }, cancellationToken: ct));

        var estab = await conn.QuerySingleOrDefaultAsync<EstabelecimentoResolver>(
            new CommandDefinition("""
                SELECT  id              AS Id,
                        nome_fantasia   AS NomeFantasia,
                        razao_social    AS RazaoSocial,
                        cnpj            AS Cnpj,
                        telefone        AS Telefone,
                        endereco        AS Endereco,
                        cidade          AS Cidade,
                        estado          AS Estado
                FROM    public.estabelecimentos
                WHERE   id = @EstabelecimentoId
                """, new { ctx.EstabelecimentoId }, cancellationToken: ct));

        ProfissionalResolver prof = null;
        if (ctx.ProfissionalUsuarioId is { } pid && pid != Guid.Empty)
        {
            // Defense-in-depth multi-tenant: só carrega o profissional se ele tiver
            // vínculo Ativo no estabelecimento atual. Sem isso, profissional que
            // atua em outro estab poderia ter seus dados (conselho/UF/registro)
            // puxados num termo de tenant diferente.
            prof = await conn.QuerySingleOrDefaultAsync<ProfissionalResolver>(
                new CommandDefinition("""
                    SELECT  u.nome_completo  AS Nome,
                            p.conselho       AS Conselho,
                            p.uf             AS Uf,
                            p.numero_registro AS NumeroRegistro,
                            COALESCE(v.especialidade_convidada, p.especialidade) AS Especialidade
                    FROM    public.profissionais p
                    INNER JOIN public.usuarios u
                            ON u.id = p.usuario_id
                    INNER JOIN public.vinculo_profissional_estabelecimento v
                            ON v.profissional_usuario_id = p.usuario_id
                           AND v.estabelecimento_id = @EstabelecimentoId
                           AND v.status = 'Ativo'
                    WHERE   p.usuario_id = @UsuarioId
                      AND   p.deletado_em IS NULL
                    """, new { UsuarioId = pid, ctx.EstabelecimentoId }, cancellationToken: ct));
        }

        return new DadosResolver(paciente, estab, prof);
    }

    // ─── Construção do dicionário (variável → string formatada) ───────────────

    private static Dictionary<string, string> MontarDicionarioDeValores(DadosResolver d)
    {
        var p = d.Paciente;
        var e = d.Estabelecimento;
        var pr = d.Profissional;
        var agora = DateTime.UtcNow;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["paciente.nome"] = string.IsNullOrWhiteSpace(p?.Nome) ? Fallbacks.Linha : p.Nome,
            ["paciente.cpf"] = FormatarCpf(p?.Cpf),
            ["paciente.documento_internacional"] = p?.DocumentoInternacional ?? Fallbacks.Vazio,
            ["paciente.data_nascimento"] = FormatarData(p?.DataNascimento),
            ["paciente.idade"] = CalcularIdade(p?.DataNascimento),
            ["paciente.telefone"] = FormatarTelefone(p?.Telefone),
            ["paciente.email"] = p?.Email ?? Fallbacks.Vazio,
            ["paciente.endereco"] = p?.Endereco ?? Fallbacks.Linha,
            ["paciente.genero"] = FormatarGenero(p?.Genero),
            ["estabelecimento.nome"] = e?.NomeFantasia ?? Fallbacks.Linha,
            ["estabelecimento.razao_social"] = e?.RazaoSocial ?? Fallbacks.Vazio,
            ["estabelecimento.cnpj"] = FormatarCnpj(e?.Cnpj),
            ["estabelecimento.endereco"] = e?.Endereco ?? Fallbacks.Vazio,
            ["estabelecimento.telefone"] = FormatarTelefone(e?.Telefone),
            ["profissional.nome"] = pr?.Nome ?? Fallbacks.Linha,
            ["profissional.conselho_completo"] = FormatarConselho(pr),
            ["profissional.especialidade"] = pr?.Especialidade ?? Fallbacks.Vazio,
            ["data_atual"] = FormatarDataExtenso(agora),
            ["data_atual_curta"] = agora.ToString("dd/MM/yyyy", PtBr),
            ["cidade_atual"] = e?.Cidade ?? Fallbacks.Linha,
        };
    }

    // ─── Formatters ──────────────────────────────────────────────────────────

    private static string FormatarCpf(string cpf)
    {
        var d = SomenteDigitos(cpf);
        if (d.Length != 11) return Fallbacks.Cpf;
        return $"{d.Substring(0, 3)}.{d.Substring(3, 3)}.{d.Substring(6, 3)}-{d.Substring(9, 2)}";
    }

    private static string FormatarCnpj(string cnpj)
    {
        var d = SomenteDigitos(cnpj);
        if (d.Length != 14) return Fallbacks.Vazio;
        return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}";
    }

    private static string FormatarTelefone(string tel)
    {
        var d = SomenteDigitos(tel);
        return d.Length switch
        {
            11 => $"({d.Substring(0, 2)}) {d.Substring(2, 5)}-{d.Substring(7, 4)}",
            10 => $"({d.Substring(0, 2)}) {d.Substring(2, 4)}-{d.Substring(6, 4)}",
            _ => string.IsNullOrEmpty(d) ? Fallbacks.Vazio : tel ?? Fallbacks.Vazio,
        };
    }

    private static string FormatarData(DateTime? data) =>
        data is null ? Fallbacks.Data : data.Value.ToString("dd/MM/yyyy", PtBr);

    private static string FormatarDataExtenso(DateTime dt)
    {
        // pt-BR: "19 de maio de 2026" — mês minúsculo.
        var mes = PtBr.DateTimeFormat.MonthNames[dt.Month - 1].ToLower(PtBr);
        return $"{dt.Day} de {mes} de {dt.Year}";
    }

    private static string CalcularIdade(DateTime? nascimento)
    {
        if (nascimento is null) return Fallbacks.Vazio;
        var hoje = DateTime.UtcNow.Date;
        var anos = hoje.Year - nascimento.Value.Year;
        if (nascimento.Value.Date > hoje.AddYears(-anos)) anos--;
        if (anos < 0) return Fallbacks.Vazio;
        return $"{anos} {(anos == 1 ? "ano" : "anos")}";
    }

    private static string FormatarGenero(string genero) => genero switch
    {
        "Feminino" => "Feminino",
        "Masculino" => "Masculino",
        "Outro" => "Outro",
        _ => Fallbacks.Vazio,
    };

    private static string FormatarConselho(ProfissionalResolver pr)
    {
        if (pr is null) return Fallbacks.Vazio;
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(pr.Conselho)) sb.Append(pr.Conselho);
        if (!string.IsNullOrWhiteSpace(pr.Uf)) sb.Append('-').Append(pr.Uf);
        if (!string.IsNullOrWhiteSpace(pr.NumeroRegistro))
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(pr.NumeroRegistro);
        }
        return sb.Length > 0 ? sb.ToString() : Fallbacks.Vazio;
    }

    private static string SomenteDigitos(string s) =>
        string.IsNullOrEmpty(s) ? string.Empty : new string(s.Where(char.IsDigit).ToArray());

    // ─── Tipos internos pra leitura ──────────────────────────────────────────

    private sealed record DadosResolver(PacienteResolver Paciente, EstabelecimentoResolver Estabelecimento, ProfissionalResolver Profissional);

    private sealed class PacienteResolver
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string DocumentoInternacional { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
        public string Endereco { get; set; }
        public string Genero { get; set; }
    }

    private sealed class EstabelecimentoResolver
    {
        public long Id { get; set; }
        public string NomeFantasia { get; set; }
        public string RazaoSocial { get; set; }
        public string Cnpj { get; set; }
        public string Telefone { get; set; }
        public string Endereco { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
    }

    private sealed class ProfissionalResolver
    {
        public string Nome { get; set; }
        public string Conselho { get; set; }
        public string Uf { get; set; }
        public string NumeroRegistro { get; set; }
        public string Especialidade { get; set; }
    }
}
