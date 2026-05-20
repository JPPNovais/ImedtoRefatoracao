using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

public sealed class ExcluirModeloTermoCommandHandler : ICommandHandler<ExcluirModeloTermoCommand>
{
    private readonly ITermoModeloRepository _repo;
    private readonly ITermoAuditLogger _audit;

    public ExcluirModeloTermoCommandHandler(ITermoModeloRepository repo, ITermoAuditLogger audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(ExcluirModeloTermoCommand cmd)
    {
        var modelo = await _repo.ObterPorIdDoEstabelecimentoOuNulo(cmd.ModeloId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");

        modelo.MarcarComoDeletado();
        await _repo.Salvar(modelo);

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "modelo-excluido", "TermoModelo", modelo.Id);
    }
}
