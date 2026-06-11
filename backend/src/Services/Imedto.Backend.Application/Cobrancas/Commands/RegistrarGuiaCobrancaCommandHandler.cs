using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cobrancas.Commands;

/// <summary>
/// Registra ou edita guia/autorização na cobrança convênio (F6/R10/R13).
/// RBAC verificado no controller: financeiro_paciente.registrar.
/// </summary>
public class RegistrarGuiaCobrancaCommandHandler : ICommandHandler<RegistrarGuiaCobrancaCommand>
{
    private readonly ICobrancaRepository _repo;

    public RegistrarGuiaCobrancaCommandHandler(ICobrancaRepository repo) => _repo = repo;

    public async Task Handle(RegistrarGuiaCobrancaCommand cmd)
    {
        // Multi-tenant: filtro por estabelecimentoId (CA148).
        var cobranca = await _repo.ObterPorIdOuNulo(cmd.CobrancaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Cobrança não encontrada.");

        // R10: guia só em convênio — domain lança 422 se Particular.
        cobranca.RegistrarGuia(cmd.GuiaNumero, cmd.GuiaSenha, cmd.GuiaAutorizadaEm);
        await _repo.Salvar(cobranca);
    }
}
