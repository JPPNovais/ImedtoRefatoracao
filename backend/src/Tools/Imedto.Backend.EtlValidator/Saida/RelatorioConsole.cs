using System;

namespace Imedto.Backend.EtlValidator.Saida;

public static class RelatorioConsole
{
    public static void Imprimir(RelatorioCompleto r)
    {
        if (r.ErroFatal)
        {
            Cor(ConsoleColor.Red, $"ERRO FATAL: {r.ErroFatalMensagem}");
            return;
        }

        if (r.Contagens.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("== Contagens (legado vs novo) ==");
            Console.WriteLine($"{"Tabela legado",-45} {"Tabela novo",-40} {"Legado",10} {"Novo",10} {"Diff",8}  Status");
            foreach (var c in r.Contagens)
            {
                var cor = c.Severidade switch
                {
                    Severidade.Ok => ConsoleColor.Green,
                    Severidade.Aviso => ConsoleColor.Yellow,
                    _ => ConsoleColor.Red,
                };
                Cor(cor,
                    $"{Truncar(c.TabelaLegado, 45),-45} {Truncar(c.TabelaNovo, 40),-40} " +
                    $"{c.ContagemLegado,10} {c.ContagemNovo,10} {c.DiffPercentual,8:P2}  {c.Mensagem}");
            }
        }

        if (r.Integridades.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("== Integridade referencial (esperado: 0 órfãos) ==");
            foreach (var i in r.Integridades)
            {
                var cor = i.Severidade == Severidade.Ok ? ConsoleColor.Green : ConsoleColor.Red;
                Cor(cor, $"  [{i.Quantidade,5}] {i.Descricao}");
            }
        }

        if (r.Smokes.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("== Smoke tests ==");
            foreach (var s in r.Smokes)
            {
                var cor = s.Sucesso ? ConsoleColor.Green : ConsoleColor.Red;
                var marca = s.Sucesso ? "OK" : "FALHA";
                Cor(cor, $"  [{marca,-5}] {s.IdentificadorAnonimizado}  etapa={s.Etapa}  {s.Mensagem}");
            }
        }

        Console.WriteLine();
        if (r.ToleranciaExcedida)
            Cor(ConsoleColor.Red, "Resultado: tolerância excedida — investigar antes do cutover.");
        else
            Cor(ConsoleColor.Green, "Resultado: dentro da tolerância — paridade aceitável.");
    }

    private static void Cor(ConsoleColor cor, string msg)
    {
        var anterior = Console.ForegroundColor;
        Console.ForegroundColor = cor;
        Console.WriteLine(msg);
        Console.ForegroundColor = anterior;
    }

    private static string Truncar(string s, int n)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= n ? s : string.Concat(s.AsSpan(0, n - 1), "…"));
}
