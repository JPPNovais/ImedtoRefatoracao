using Imedto.Backend.Contracts.Auth.Queries;
using Imedto.Backend.Contracts.Auth.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Auth.Queries;

/// <summary>
/// Hidrata o estado de auth da SPA com uma única chamada — executa as 3
/// projeções de leitura em paralelo (Task.WhenAll) para minimizar a latência
/// total no servidor além de reduzir round-trips no cliente.
/// </summary>
public class BootstrapMeQueryHandlers : IRequestHandler<BootstrapMeQuery, BootstrapMeDto>
{
    private readonly UsuarioQueryRepository _usuarioRepo;
    private readonly ProfissionalQueryRepository _profissionalRepo;
    private readonly EstabelecimentoQueryRepository _estabelecimentoRepo;

    public BootstrapMeQueryHandlers(
        UsuarioQueryRepository usuarioRepo,
        ProfissionalQueryRepository profissionalRepo,
        EstabelecimentoQueryRepository estabelecimentoRepo)
    {
        _usuarioRepo = usuarioRepo;
        _profissionalRepo = profissionalRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
    }

    public async Task<BootstrapMeDto> Handle(BootstrapMeQuery query)
    {
        var usuarioTask = _usuarioRepo.ObterMeParaBootstrap(query.UsuarioId);
        var profissionalTask = _profissionalRepo.ObterPorUsuario(query.UsuarioId);
        var estabelecimentosTask = _estabelecimentoRepo.ListarPorUsuario(query.UsuarioId);

        await Task.WhenAll(usuarioTask, profissionalTask, estabelecimentosTask);

        var usuario = usuarioTask.Result;
        if (usuario is null)
            throw new BusinessException("Registro local do usuário não encontrado.");

        return new BootstrapMeDto(
            usuario,
            profissionalTask.Result,
            estabelecimentosTask.Result);
    }
}
