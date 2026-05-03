using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class AdicionarListaEsperaCommandHandler : ICommandHandler<AdicionarListaEsperaCommand>
{
    private readonly IListaEsperaRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;

    public AdicionarListaEsperaCommandHandler(
        IListaEsperaRepository repo,
        IPacienteRepository pacienteRepo)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
    }

    public async Task Handle(AdicionarListaEsperaCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        if (!Enum.TryParse<ListaEsperaPrioridade>(cmd.Prioridade, ignoreCase: true, out var prioridade))
            throw new BusinessException($"Prioridade '{cmd.Prioridade}' inválida.");
        if (!Enum.TryParse<ListaEsperaPreferenciaPeriodo>(cmd.PreferenciaPeriodo, ignoreCase: true, out var periodo))
            throw new BusinessException($"Preferência de período '{cmd.PreferenciaPeriodo}' inválida.");

        var entity = ListaEsperaAgendamento.Criar(
            cmd.EstabelecimentoId,
            cmd.PacienteId,
            cmd.Motivo,
            cmd.ProfissionalPreferidoId,
            cmd.CriadoPorUsuarioId,
            prioridade,
            periodo);

        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class RemoverListaEsperaCommandHandler : ICommandHandler<RemoverListaEsperaCommand>
{
    private readonly IListaEsperaRepository _repo;
    public RemoverListaEsperaCommandHandler(IListaEsperaRepository repo) => _repo = repo;

    public async Task Handle(RemoverListaEsperaCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id)
            ?? throw new BusinessException("Entrada da lista de espera não encontrada.");
        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (entity.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Entrada da lista de espera não encontrada.");
        await _repo.Remover(entity);
    }
}
