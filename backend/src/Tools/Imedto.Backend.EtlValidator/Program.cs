using System;
using System.Threading;
using System.Threading.Tasks;
using Imedto.Backend.EtlValidator;
using Imedto.Backend.EtlValidator.Saida;
using Imedto.Backend.EtlValidator.Validacoes;

// Exit codes:
//   0  — tudo OK
//   1  — tolerância excedida (contagem/integridade/smoke)
//   2  — erro fatal (config inválida, conexão, exceção não-tratada)

try
{
    var opcoes = Opcoes.ParseArgs(args);

    if (PrecisaBanco(opcoes.Modo))
    {
        if (string.IsNullOrWhiteSpace(opcoes.ConexaoLegado)
            || string.IsNullOrWhiteSpace(opcoes.ConexaoNovo))
        {
            Console.Error.WriteLine(
                "ERRO: --legado-conn e --novo-conn (ou env vars PG_LEGADO/PG_NOVO) são obrigatórios para os modos counts/integrity/full.");
            Opcoes.ImprimirAjuda();
            return 2;
        }
    }

    var relatorio = new RelatorioCompleto();

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    Console.WriteLine($"Modo: {opcoes.Modo}");
    Console.WriteLine($"Tolerância de contagem: {opcoes.ToleranciaContagem:P2}");
    Console.WriteLine();

    if (opcoes.Modo is ModoExecucao.Counts or ModoExecucao.Full)
    {
        Console.WriteLine("→ Rodando contagens...");
        await new ContagemValidacao(opcoes).ExecutarAsync(relatorio, cts.Token);
    }

    if (opcoes.Modo is ModoExecucao.Integrity or ModoExecucao.Full)
    {
        Console.WriteLine("→ Rodando integridade referencial...");
        await new IntegridadeValidacao(opcoes).ExecutarAsync(relatorio, cts.Token);
    }

    if (opcoes.Modo is ModoExecucao.Smoke or ModoExecucao.Full)
    {
        Console.WriteLine("→ Rodando smoke tests...");
        await new SmokeValidacao(opcoes).ExecutarAsync(relatorio, cts.Token);
    }

    RelatorioConsole.Imprimir(relatorio);
    var caminho = RelatorioMarkdown.Gerar(relatorio, opcoes.DiretorioRelatorios);
    Console.WriteLine();
    Console.WriteLine($"Relatório markdown gerado em: {caminho}");

    return relatorio.ToleranciaExcedida ? 1 : 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Execução cancelada pelo usuário.");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERRO FATAL: {ex.GetType().Name} — {ex.Message}");
    return 2;
}

static bool PrecisaBanco(ModoExecucao m) =>
    m is ModoExecucao.Counts or ModoExecucao.Integrity or ModoExecucao.Full;
