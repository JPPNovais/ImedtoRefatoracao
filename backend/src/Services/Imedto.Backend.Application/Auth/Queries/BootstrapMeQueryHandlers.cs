using Imedto.Backend.Contracts.Auth.Queries;
using Imedto.Backend.Contracts.Auth.Queries.Results;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Auth.Queries;

/// <summary>
/// Hidrata o estado de auth da SPA com uma única chamada — executa as projeções
/// de leitura em paralelo (Task.WhenAll) para minimizar a latência total.
///
/// Calcula <c>DeveConfigurar2fa</c> (R10/CA14/CA15): true quando o usuário é Dono
/// de pelo menos um estabelecimento com <c>exigir_2fa_dono = true</c> e não tem
/// 2FA ativo. Verificação falha-fechada: sem 2FA ativo → flag pode ser true.
/// </summary>
public class BootstrapMeQueryHandlers : IRequestHandler<BootstrapMeQuery, BootstrapMeDto>
{
    private readonly UsuarioQueryRepository _usuarioRepo;
    private readonly ProfissionalQueryRepository _profissionalRepo;
    private readonly EstabelecimentoQueryRepository _estabelecimentoRepo;
    private readonly IUsuario2faRepository _usuario2faRepo;

    public BootstrapMeQueryHandlers(
        UsuarioQueryRepository usuarioRepo,
        ProfissionalQueryRepository profissionalRepo,
        EstabelecimentoQueryRepository estabelecimentoRepo,
        IUsuario2faRepository usuario2faRepo)
    {
        _usuarioRepo = usuarioRepo;
        _profissionalRepo = profissionalRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
        _usuario2faRepo = usuario2faRepo;
    }

    public async Task<BootstrapMeDto> Handle(BootstrapMeQuery query)
    {
        var usuarioTask        = _usuarioRepo.ObterMeParaBootstrap(query.UsuarioId);
        var profissionalTask   = _profissionalRepo.ObterPorUsuario(query.UsuarioId);
        var estabelecimentosTask = _estabelecimentoRepo.ListarPorUsuario(query.UsuarioId);
        var estado2faTask      = _usuario2faRepo.ObterPorUsuarioId(query.UsuarioId);

        await Task.WhenAll(usuarioTask, profissionalTask, estabelecimentosTask, estado2faTask);

        var usuario = usuarioTask.Result;
        if (usuario is null)
            throw new BusinessException("Registro local do usuário não encontrado.");

        var estabelecimentos = estabelecimentosTask.Result.ToList();
        var tem2faAtivo = estado2faTask.Result is { Ativo: true };

        // Dono de qualquer estabelecimento que exige 2FA mas não tem 2FA ativo → forçar configuração
        var deveConfigurar2fa = !tem2faAtivo
            && estabelecimentos.Any(e => e.PapelDoUsuario == "Dono" && e.ExigirDono2fa);

        return new BootstrapMeDto(
            usuario,
            profissionalTask.Result,
            estabelecimentos,
            deveConfigurar2fa);
    }
}
