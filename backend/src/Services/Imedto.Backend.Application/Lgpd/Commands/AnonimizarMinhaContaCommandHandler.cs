using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Contracts.Lgpd.Commands;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Application.Lgpd.Commands;

/// <summary>
/// Direito ao esquecimento (Art. 18 LGPD).
///
/// Fluxo:
/// 1. Anonimiza paciente(s) vinculados ao e-mail do usuário no mesmo estabelecimento.
/// 2. Anonimiza os dados PII do aggregate Usuario.
///
/// O e-mail no Supabase Auth permanece até o controller chamar revoke de sessão —
/// não podemos deletar do Auth sem o service role (isso é responsabilidade do AuthService).
/// </summary>
public class AnonimizarMinhaContaCommandHandler : ICommandHandler<AnonimizarMinhaContaCommand>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IAnonimizacaoService _anonimizacao;
    private readonly AppDbContext _db;

    public AnonimizarMinhaContaCommandHandler(
        IUsuarioRepository usuarios,
        IAnonimizacaoService anonimizacao,
        AppDbContext db)
    {
        _usuarios = usuarios;
        _anonimizacao = anonimizacao;
        _db = db;
    }

    public async Task Handle(AnonimizarMinhaContaCommand command)
    {
        // Defesa minima: nunca aceitar Guid.Empty (vetor obvio de bypass).
        if (command.UsuarioId == Guid.Empty)
            throw new BusinessException("Usuário não identificado.");

        var usuario = await _usuarios.ObterPorIdOuNulo(command.UsuarioId)
            ?? throw new BusinessException("Usuário não encontrado.");

        // Anonimiza pacientes cujo e-mail corresponde ao do usuário.
        // Não loga o e-mail — apenas opera sobre os ids.
        var pacientesVinculados = await _db.Pacientes
            .Where(p => p.Email == usuario.Email && p.AnonimizadoEm == null)
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var pacienteId in pacientesVinculados)
        {
            await _anonimizacao.AnonimizarPaciente(
                pacienteId,
                MotivoAnonimizacao.DireitoEsquecimento,
                command.UsuarioId);
        }

        // Anonimiza o aggregate de usuário.
        usuario.Anonimizar();
        await _usuarios.Salvar(usuario);
    }
}
