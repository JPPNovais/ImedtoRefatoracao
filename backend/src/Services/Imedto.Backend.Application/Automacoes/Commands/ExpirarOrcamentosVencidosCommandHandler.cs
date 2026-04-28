using Dapper;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Npgsql;

namespace Imedto.Backend.Application.Automacoes.Commands;

public class ExpirarOrcamentosVencidosCommandHandler : ICommandHandler<ExpirarOrcamentosVencidosCommand>
{
    private readonly string _connStr;

    public ExpirarOrcamentosVencidosCommandHandler(AppReadConnectionString conn)
        => _connStr = conn.Value;

    public async Task Handle(ExpirarOrcamentosVencidosCommand command)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            """
            UPDATE orcamentos
            SET status = 'Expirado', atualizado_em = NOW()
            WHERE status = 'Pendente'
              AND validade < CURRENT_DATE
            """);
    }
}
