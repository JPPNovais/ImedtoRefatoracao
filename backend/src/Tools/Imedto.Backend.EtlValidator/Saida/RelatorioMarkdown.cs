using System;
using System.IO;
using System.Text;

namespace Imedto.Backend.EtlValidator.Saida;

public static class RelatorioMarkdown
{
    public static string Gerar(RelatorioCompleto r, string diretorioSaida)
    {
        Directory.CreateDirectory(diretorioSaida);
        var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var caminho = Path.Combine(diretorioSaida, $"ETL_RELATORIO_{ts}.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# Relatório de paridade ETL — {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Sumário
        int contOk = 0, contAviso = 0, contErro = 0;
        foreach (var c in r.Contagens)
        {
            switch (c.Severidade)
            {
                case Severidade.Ok: contOk++; break;
                case Severidade.Aviso: contAviso++; break;
                case Severidade.Erro: contErro++; break;
            }
        }
        int intOk = 0, intErro = 0;
        foreach (var i in r.Integridades)
        {
            if (i.Severidade == Severidade.Ok) intOk++; else intErro++;
        }
        int smokeOk = 0, smokeErro = 0;
        foreach (var s in r.Smokes)
        {
            if (s.Sucesso) smokeOk++; else smokeErro++;
        }

        sb.AppendLine("## Sumário executivo");
        sb.AppendLine();
        sb.AppendLine($"- **Contagens:** {contOk} ok / {contAviso} avisos / {contErro} erros (total {r.Contagens.Count})");
        sb.AppendLine($"- **Integridade:** {intOk} ok / {intErro} erros (total {r.Integridades.Count})");
        sb.AppendLine($"- **Smoke tests:** {smokeOk} passaram / {smokeErro} falharam (total {r.Smokes.Count})");
        sb.AppendLine($"- **Resultado geral:** {(r.ToleranciaExcedida ? "TOLERÂNCIA EXCEDIDA" : "OK")}");
        sb.AppendLine();

        if (r.Contagens.Count > 0)
        {
            sb.AppendLine("## Contagens (legado vs novo)");
            sb.AppendLine();
            sb.AppendLine("| Tabela legado | Tabela novo | Legado | Novo | Diff | Status | Mensagem |");
            sb.AppendLine("|---|---|---:|---:|---:|---|---|");
            foreach (var c in r.Contagens)
            {
                sb.AppendLine($"| `{c.TabelaLegado}` | `{c.TabelaNovo}` | {c.ContagemLegado} | {c.ContagemNovo} | {c.DiffPercentual:P2} | {Marca(c.Severidade)} | {Esc(c.Mensagem)} |");
            }
            sb.AppendLine();
        }

        if (r.Integridades.Count > 0)
        {
            sb.AppendLine("## Integridade referencial");
            sb.AppendLine();
            sb.AppendLine("| Verificação | Órfãos encontrados | Status |");
            sb.AppendLine("|---|---:|---|");
            foreach (var i in r.Integridades)
            {
                sb.AppendLine($"| {Esc(i.Descricao)} | {i.Quantidade} | {Marca(i.Severidade)} |");
            }
            sb.AppendLine();
        }

        if (r.Smokes.Count > 0)
        {
            sb.AppendLine("## Smoke tests");
            sb.AppendLine();
            sb.AppendLine("| Usuário (hash) | Status | Etapa | Mensagem |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var s in r.Smokes)
            {
                var status = s.Sucesso ? "OK" : "FALHA";
                sb.AppendLine($"| `{s.IdentificadorAnonimizado}` | {status} | {Esc(s.Etapa)} | {Esc(s.Mensagem)} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("> Relatório gerado por `Imedto.Backend.EtlValidator`. Não contém PII de usuário (emails são hashed).");

        File.WriteAllText(caminho, sb.ToString());
        return caminho;
    }

    private static string Marca(Severidade s) => s switch
    {
        Severidade.Ok => "OK",
        Severidade.Aviso => "AVISO",
        _ => "ERRO",
    };

    private static string Esc(string s) =>
        string.IsNullOrEmpty(s) ? string.Empty : s.Replace("|", "\\|").Replace("\n", " ");
}
