using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Imedto.Backend.EtlValidator.Mapeamento;
using Npgsql;

namespace Imedto.Backend.EtlValidator.Validacoes;

/// <summary>
/// Conta linhas em cada par (legado, novo) e calcula diff percentual,
/// classificando segundo a tolerância configurada.
/// </summary>
public sealed class ContagemValidacao : IValidacao
{
    private readonly Opcoes _opcoes;

    public ContagemValidacao(Opcoes opcoes) => _opcoes = opcoes;

    public string Nome => "Contagem lado-a-lado";

    public async Task ExecutarAsync(RelatorioCompleto relatorio, CancellationToken ct)
    {
        await using var connLegado = new NpgsqlConnection(_opcoes.ConexaoLegado);
        await using var connNovo = new NpgsqlConnection(_opcoes.ConexaoNovo);
        await connLegado.OpenAsync(ct);
        await connNovo.OpenAsync(ct);

        foreach (var par in TabelasLegadoNovo.Pares)
        {
            ct.ThrowIfCancellationRequested();
            long contLegado = 0;
            long contNovo = 0;
            string mensagem = string.Empty;
            Severidade sev = Severidade.Ok;

            try
            {
                contLegado = await ContarSeguro(connLegado, par.Legado, ct);

                if (par.Descartada || string.IsNullOrEmpty(par.Novo))
                {
                    // Descartadas: não há contagem-alvo no destino. Reportamos só o legado.
                    relatorio.Contagens.Add(new ResultadoContagem(
                        par.Legado, "(descartada)", contLegado, 0, 0,
                        Severidade.Ok, $"Descartada conscientemente. {par.Observacao}"));
                    continue;
                }

                contNovo = await ContarSeguro(connNovo, par.Novo, ct);

                double diff = contLegado == 0
                    ? (contNovo == 0 ? 0d : 1d)
                    : Math.Abs((double)(contLegado - contNovo)) / contLegado;

                if (contLegado == contNovo)
                {
                    sev = Severidade.Ok;
                    mensagem = "Igual.";
                }
                else if (diff <= _opcoes.ToleranciaContagem)
                {
                    sev = Severidade.Aviso;
                    mensagem = $"Dentro da tolerância ({diff:P2} ≤ {_opcoes.ToleranciaContagem:P2}).";
                }
                else
                {
                    sev = Severidade.Erro;
                    mensagem = $"Diff {diff:P2} excede tolerância {_opcoes.ToleranciaContagem:P2}.";
                }

                relatorio.Contagens.Add(new ResultadoContagem(
                    par.Legado, par.Novo, contLegado, contNovo, diff, sev, mensagem));
            }
            catch (Exception ex)
            {
                relatorio.Contagens.Add(new ResultadoContagem(
                    par.Legado, par.Novo ?? "?", contLegado, contNovo, 0,
                    Severidade.Erro, $"Erro: {ex.GetType().Name} — {ex.Message}"));
            }
        }
    }

    private static async Task<long> ContarSeguro(NpgsqlConnection conn, string tabela, CancellationToken ct)
    {
        if (!EhIdentificadorSeguro(tabela))
        {
            throw new ArgumentException($"Nome de tabela inválido: {tabela}");
        }

        // Confirma existência antes — tabelas dropadas no legado retornam "tabela ausente" (0 esperado).
        var existe = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=@t)",
            new { t = tabela });

        if (!existe) return 0;

        var sql = $"SELECT COUNT(*) FROM public.\"{tabela}\"";
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, cancellationToken: ct));
    }

    private static bool EhIdentificadorSeguro(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var c in s)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
        }
        return true;
    }
}
