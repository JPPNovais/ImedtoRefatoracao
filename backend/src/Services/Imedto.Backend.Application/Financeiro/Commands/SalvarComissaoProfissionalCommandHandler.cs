using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

/// <summary>
/// Cria ou atualiza a configuração de comissão de um profissional (R16/CA177).
/// RBAC: apenas o Dono pode editar (EhDono verificado no comando).
/// </summary>
public class SalvarComissaoProfissionalCommandHandler : ICommandHandler<SalvarComissaoProfissionalCommand>
{
    private readonly IConfigComissaoProfissionalRepository _repo;

    public SalvarComissaoProfissionalCommandHandler(IConfigComissaoProfissionalRepository repo)
        => _repo = repo;

    public async Task Handle(SalvarComissaoProfissionalCommand command)
    {
        if (!command.EhDono)
            throw new BusinessException("Apenas o Dono pode configurar comissões.");

        // Consulta
        if (command.PercentualConsulta.HasValue)
        {
            var config = await _repo.ObterOuNulo(
                command.EstabelecimentoId, command.ProfissionalUsuarioId, TipoComissao.Consulta);

            if (config is null)
            {
                config = ConfigComissaoProfissional.Criar(
                    command.EstabelecimentoId, command.ProfissionalUsuarioId,
                    TipoComissao.Consulta, command.PercentualConsulta.Value);
            }
            else
            {
                config.Atualizar(command.PercentualConsulta.Value);
            }
            await _repo.Salvar(config);
        }

        // Procedimento
        if (command.PercentualProcedimento.HasValue)
        {
            var config = await _repo.ObterOuNulo(
                command.EstabelecimentoId, command.ProfissionalUsuarioId, TipoComissao.Procedimento);

            if (config is null)
            {
                config = ConfigComissaoProfissional.Criar(
                    command.EstabelecimentoId, command.ProfissionalUsuarioId,
                    TipoComissao.Procedimento, command.PercentualProcedimento.Value);
            }
            else
            {
                config.Atualizar(command.PercentualProcedimento.Value);
            }
            await _repo.Salvar(config);
        }
    }
}
