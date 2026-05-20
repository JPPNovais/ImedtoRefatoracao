using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

public sealed class AlterarAtivoModeloTermoCommandHandler : ICommandHandler<AlterarAtivoModeloTermoCommand>
{
    private readonly ITermoModeloRepository _repo;
    private readonly ITermoAuditLogger _audit;

    public AlterarAtivoModeloTermoCommandHandler(ITermoModeloRepository repo, ITermoAuditLogger audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(AlterarAtivoModeloTermoCommand cmd)
    {
        var modelo = await _repo.ObterPorIdDoEstabelecimentoOuNulo(cmd.ModeloId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");

        modelo.AlterarAtivo(cmd.Ativo);
        await _repo.Salvar(modelo);

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            cmd.Ativo ? "modelo-ativado" : "modelo-desativado",
            "TermoModelo", modelo.Id);
    }
}
