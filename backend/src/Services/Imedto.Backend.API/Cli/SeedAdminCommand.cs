using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.API.Cli;

/// <summary>
/// Comando CLI para criar o primeiro admin em produção (CA46).
///
/// Uso: dotnet run --project backend/src/Services/Imedto.Backend.API -- seed-admin --email &lt;email&gt;
///
/// Comportamento:
/// - Gera senha aleatória 20 chars (política completa).
/// - Insere em imedto_admins com force_password_reset = true.
/// - Insere audit CRIAR_ADMIN com motivo = "Bootstrap CLI".
/// - Imprime senha no stdout UMA ÚNICA VEZ.
///
/// Idempotência: falha se e-mail já existe (UNIQUE constraint).
/// </summary>
public static class SeedAdminCommand
{
    public static async Task ExecutarAsync(string[] args)
    {
        // Lê e-mail do argumento --email
        var emailIdx = Array.IndexOf(args, "--email");
        if (emailIdx < 0 || emailIdx + 1 >= args.Length)
        {
            Console.Error.WriteLine("Uso: seed-admin --email <email>");
            Environment.Exit(1);
            return;
        }

        var email = args[emailIdx + 1].Trim().ToLowerInvariant();
        if (!email.Contains('@') || email.Length > 254)
        {
            Console.Error.WriteLine("E-mail inválido.");
            Environment.Exit(1);
            return;
        }

        // Configura DI mínimo — apenas o necessário para o command.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();

        // Verifica se e-mail já existe.
        var existente = await db.ImedtoAdmins.AnyAsync(a => a.Email == email);
        if (existente)
        {
            Console.Error.WriteLine($"Erro: já existe um admin com e-mail '{email}'.");
            Environment.Exit(1);
            return;
        }

        // Gera senha temporária forte.
        var senha = AdminSenhaPolicy.GerarSenhaTemporaria();
        var hash = hasher.Hash(senha);

        var admin = ImedtoAdmin.Criar(
            email: email,
            nome: "Administrador",
            senhaHash: hash,
            forcePasswordReset: true,
            criadoPorAdminId: null);

        db.ImedtoAdmins.Add(admin);

        // Insere audit.
        var auditLog = ImedtoAdminAuditLog.Registrar(
            acao: AcoesAuditAdmin.CriarAdmin,
            adminId: admin.Id,
            recursoTipo: "admin",
            recursoId: admin.Id.ToString(),
            motivo: "Bootstrap CLI");

        db.ImedtoAdminAuditLogs.Add(auditLog);
        await db.SaveChangesAsync();

        Console.WriteLine($"Admin criado com sucesso.");
        Console.WriteLine($"E-mail: {email}");
        Console.WriteLine($"Senha temporária: {senha}");
        Console.WriteLine("ATENÇÃO: Salve esta senha agora — ela não será exibida novamente.");
        Console.WriteLine("Faça login e altere a senha imediatamente.");
    }
}
