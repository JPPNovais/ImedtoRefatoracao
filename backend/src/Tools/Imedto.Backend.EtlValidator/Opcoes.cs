namespace Imedto.Backend.EtlValidator;

public enum ModoExecucao { Counts, Integrity, Smoke, Full }

public sealed class Opcoes
{
    public string ConexaoLegado { get; set; }
    public string ConexaoNovo { get; set; }
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public string ArquivoSmokeUsers { get; set; }
    public ModoExecucao Modo { get; set; } = ModoExecucao.Full;

    /// <summary>Tolerância em fração (0,005 = 0,5%) para diff de contagem.</summary>
    public double ToleranciaContagem { get; set; } = 0.005;

    public string DiretorioRelatorios { get; set; } = "Docs";

    public static Opcoes ParseArgs(string[] args)
    {
        var o = new Opcoes
        {
            ConexaoLegado = System.Environment.GetEnvironmentVariable("PG_LEGADO"),
            ConexaoNovo = System.Environment.GetEnvironmentVariable("PG_NOVO"),
            ApiBaseUrl = System.Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000",
        };

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string proxima() => i + 1 < args.Length ? args[++i] : null;
            switch (a)
            {
                case "--legado-conn": o.ConexaoLegado = proxima(); break;
                case "--novo-conn": o.ConexaoNovo = proxima(); break;
                case "--api-base-url": o.ApiBaseUrl = proxima(); break;
                case "--smoke-users": o.ArquivoSmokeUsers = proxima(); break;
                case "--modo":
                    var v = proxima();
                    if (!System.Enum.TryParse<ModoExecucao>(v, ignoreCase: true, out var modo))
                    {
                        throw new System.ArgumentException(
                            $"--modo inválido: '{v}'. Use counts | integrity | smoke | full.");
                    }
                    o.Modo = modo;
                    break;
                case "--tolerancia":
                    var t = proxima();
                    if (!double.TryParse(t, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var tol))
                    {
                        throw new System.ArgumentException($"--tolerancia inválida: '{t}'.");
                    }
                    o.ToleranciaContagem = tol;
                    break;
                case "--out-dir": o.DiretorioRelatorios = proxima(); break;
                case "--help":
                case "-h":
                    ImprimirAjuda();
                    System.Environment.Exit(0);
                    break;
            }
        }

        return o;
    }

    public static void ImprimirAjuda()
    {
        System.Console.WriteLine("""
            Validador de paridade pós-ETL Imedto

            Uso:
              dotnet run -- --modo {counts|integrity|smoke|full} [opções]

            Conexões (obrigatório quando aplicável):
              --legado-conn "Host=...;Username=...;Password=...;Database=postgres"
              --novo-conn   "Host=...;Username=...;Password=...;Database=postgres"
              (também lidas de PG_LEGADO / PG_NOVO no ambiente)

            Smoke test:
              --api-base-url http://localhost:5000   (default; também API_BASE_URL)
              --smoke-users  smoke-users.json
                Formato:
                [{ "email": "...", "senha": "...", "estab_esperado": 123 }]

            Outras:
              --tolerancia 0.005    diff aceitável em counts (default 0,5%)
              --out-dir Docs        diretório do relatório markdown
              --help                esta ajuda
            """);
    }
}
