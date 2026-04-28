using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Commands;

public class CadastrarPacienteCommandHandler : ICommandHandler<CadastrarPacienteCommand>
{
    private readonly IPacienteRepository _repository;
    private readonly IEventBus _eventBus;

    public CadastrarPacienteCommandHandler(IPacienteRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task Handle(CadastrarPacienteCommand command)
    {
        var cpfDigitos = new string((command.Cpf ?? "").Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(cpfDigitos) &&
            await _repository.ExisteCpfNoEstabelecimento(cpfDigitos, command.EstabelecimentoId, ignorarPacienteId: 0))
        {
            throw new BusinessException("Já existe um paciente com este CPF neste estabelecimento.");
        }

        if (!Enum.TryParse<GeneroPaciente>(command.Genero, ignoreCase: true, out var genero))
            genero = GeneroPaciente.NaoInformado;

        var paciente = Paciente.Cadastrar(
            command.EstabelecimentoId,
            command.NomeCompleto,
            command.Cpf,
            command.DataNascimento,
            genero,
            command.Telefone,
            command.Email,
            command.Endereco,
            command.Observacoes);

        await _repository.Salvar(paciente);
        paciente.MarcarComoCadastrado();

        foreach (var evt in paciente.DomainEvents)
            await _eventBus.Publish(evt);

        paciente.ClearDomainEvents();
    }
}
