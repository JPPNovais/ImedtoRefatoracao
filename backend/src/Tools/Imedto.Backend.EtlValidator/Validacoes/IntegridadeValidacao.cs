using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Imedto.Backend.EtlValidator.Mapeamento;
using Npgsql;

namespace Imedto.Backend.EtlValidator.Validacoes;

/// <summary>
/// Roda cada SELECT COUNT de integridade contra o banco novo. Esperado: 0.
/// Qualquer FK órfã é erro crítico (tolerância zero).
/// </summary>
public sealed class IntegridadeValidacao : IValidacao
{
    private readonly Opcoes _opcoes;

    public IntegridadeValidacao(Opcoes opcoes) => _opcoes = opcoes;

    public string Nome => "Integridade referencial";

    public async Task ExecutarAsync(RelatorioCompleto relatorio, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_opcoes.ConexaoNovo);
        await conn.OpenAsync(ct);

        foreach (var v in TabelasLegadoNovo.Integridade)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var qtd = await conn.ExecuteScalarAsync<long>(
                    new CommandDefinition(v.SqlContagem, cancellationToken: ct));
                var sev = qtd == 0 ? Severidade.Ok : Severidade.Erro;
                relatorio.Integridades.Add(new ResultadoIntegridade(v.Descricao, qtd, sev));
            }
            catch (Exception ex)
            {
                relatorio.Integridades.Add(new ResultadoIntegridade(
                    $"{v.Descricao} (erro: {ex.GetType().Name})", -1, Severidade.Erro));
            }
        }
    }
}
