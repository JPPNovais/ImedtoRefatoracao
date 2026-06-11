using Imedto.Backend.Contracts.PacienteConvenios.Commands;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.PacienteConvenios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.PacienteConvenios.Commands;

public class CriarPacienteConvenioCommandHandler : ICommandHandler<CriarPacienteConvenioCommand>
{
    private readonly IPacienteConvenioRepository _repo;
    private readonly IConvenioRepository _convenioRepo;
    private readonly IPacienteRepository _pacienteRepo;

    public CriarPacienteConvenioCommandHandler(
        IPacienteConvenioRepository repo,
        IConvenioRepository convenioRepo,
        IPacienteRepository pacienteRepo)
    {
        _repo = repo;
        _convenioRepo = convenioRepo;
        _pacienteRepo = pacienteRepo;
    }

    public async Task Handle(CriarPacienteConvenioCommand cmd)
    {
        // R5: valida paciente do tenant
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        // R5: valida convênio do mesmo estabelecimento (404 genérico para tenant alheio)
        var convenio = await _convenioRepo.ObterPorIdOuNulo(cmd.ConvenioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Convênio não encontrado.");

        // R5: plano deve pertencer ao convênio (CA138)
        if (cmd.PlanoId.HasValue && cmd.PlanoId.Value > 0)
        {
            var plano = convenio.Planos.FirstOrDefault(p => p.Id == cmd.PlanoId.Value);
            if (plano is null)
                throw new BusinessException("Plano não encontrado para o convênio informado.");
        }

        var carteirinha = PacienteConvenio.Criar(
            cmd.PacienteId,
            cmd.EstabelecimentoId,
            cmd.ConvenioId,
            cmd.PlanoId,
            cmd.NumeroCarteirinha,
            cmd.Validade);

        await _repo.Salvar(carteirinha);
    }
}

public class AtualizarPacienteConvenioCommandHandler : ICommandHandler<AtualizarPacienteConvenioCommand>
{
    private readonly IPacienteConvenioRepository _repo;
    private readonly IConvenioRepository _convenioRepo;

    public AtualizarPacienteConvenioCommandHandler(
        IPacienteConvenioRepository repo,
        IConvenioRepository convenioRepo)
    {
        _repo = repo;
        _convenioRepo = convenioRepo;
    }

    public async Task Handle(AtualizarPacienteConvenioCommand cmd)
    {
        var carteirinha = await _repo.ObterPorIdOuNulo(cmd.CarteirinhaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Carteirinha não encontrada.");

        // Verifica que a carteirinha pertence ao paciente do tenant
        if (carteirinha.PacienteId != cmd.PacienteId)
            throw new BusinessException("Carteirinha não encontrada.");

        // R5: valida convênio do mesmo estabelecimento
        var convenio = await _convenioRepo.ObterPorIdOuNulo(cmd.ConvenioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Convênio não encontrado.");

        // R5: plano deve pertencer ao convênio (CA138)
        if (cmd.PlanoId.HasValue && cmd.PlanoId.Value > 0)
        {
            var plano = convenio.Planos.FirstOrDefault(p => p.Id == cmd.PlanoId.Value);
            if (plano is null)
                throw new BusinessException("Plano não encontrado para o convênio informado.");
        }

        carteirinha.Atualizar(cmd.ConvenioId, cmd.PlanoId, cmd.NumeroCarteirinha, cmd.Validade, cmd.Ativo);
        await _repo.Salvar(carteirinha);
    }
}

public class ExcluirPacienteConvenioCommandHandler : ICommandHandler<ExcluirPacienteConvenioCommand>
{
    private readonly IPacienteConvenioRepository _repo;

    public ExcluirPacienteConvenioCommandHandler(IPacienteConvenioRepository repo) => _repo = repo;

    public async Task Handle(ExcluirPacienteConvenioCommand cmd)
    {
        var carteirinha = await _repo.ObterPorIdOuNulo(cmd.CarteirinhaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Carteirinha não encontrada.");

        if (carteirinha.PacienteId != cmd.PacienteId)
            throw new BusinessException("Carteirinha não encontrada.");

        await _repo.Excluir(carteirinha);
    }
}
