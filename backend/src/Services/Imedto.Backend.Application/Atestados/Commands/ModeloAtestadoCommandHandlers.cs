using Imedto.Backend.Contracts.Atestados.Commands;
using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Atestados.Commands;

public class CriarModeloAtestadoCommandHandler : ICommandHandler<CriarModeloAtestadoCommand>
{
    private readonly IModeloAtestadoRepository _repo;

    public CriarModeloAtestadoCommandHandler(IModeloAtestadoRepository repo) => _repo = repo;

    public async Task Handle(CriarModeloAtestadoCommand cmd)
    {
        var tipo = AtestadoParsers.ParseTipo(cmd.Tipo);
        var modelo = ModeloAtestado.Criar(
            cmd.EstabelecimentoId, cmd.ProfissionalUsuarioId, cmd.Nome, tipo, cmd.Conteudo);
        await _repo.Salvar(modelo);
        cmd.ModeloIdCriado = modelo.Id;
    }
}

public class AtualizarModeloAtestadoCommandHandler : ICommandHandler<AtualizarModeloAtestadoCommand>
{
    private readonly IModeloAtestadoRepository _repo;

    public AtualizarModeloAtestadoCommandHandler(IModeloAtestadoRepository repo) => _repo = repo;

    public async Task Handle(AtualizarModeloAtestadoCommand cmd)
    {
        // Defense-in-depth tenant + LGPD: mensagem genérica (não revela existência cross-tenant).
        var modelo = await _repo.ObterPorIdOuNulo(cmd.ModeloId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Modelo de atestado não encontrado.");

        var tipo = AtestadoParsers.ParseTipo(cmd.Tipo);
        modelo.Atualizar(cmd.Nome, tipo, cmd.Conteudo);
        await _repo.Salvar(modelo);
    }
}

public class ExcluirModeloAtestadoCommandHandler : ICommandHandler<ExcluirModeloAtestadoCommand>
{
    private readonly IModeloAtestadoRepository _repo;

    public ExcluirModeloAtestadoCommandHandler(IModeloAtestadoRepository repo) => _repo = repo;

    public async Task Handle(ExcluirModeloAtestadoCommand cmd)
    {
        var modelo = await _repo.ObterPorIdOuNulo(cmd.ModeloId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Modelo de atestado não encontrado.");
        await _repo.Excluir(modelo);
    }
}
