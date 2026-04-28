using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Commands;

public class AtualizarPacienteCommandHandler : ICommandHandler<AtualizarPacienteCommand>
{
    private readonly IPacienteRepository _repository;

    public AtualizarPacienteCommandHandler(IPacienteRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AtualizarPacienteCommand command)
    {
        var paciente = await _repository.ObterPorId(command.PacienteId);

        // Isolamento multi-tenant — o paciente precisa pertencer ao tenant da request.
        if (paciente.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Paciente não pertence a este estabelecimento.");

        var cpfDigitos = new string((command.Cpf ?? "").Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(cpfDigitos) &&
            await _repository.ExisteCpfNoEstabelecimento(cpfDigitos, command.EstabelecimentoId, ignorarPacienteId: command.PacienteId))
        {
            throw new BusinessException("Já existe outro paciente com este CPF neste estabelecimento.");
        }

        if (!Enum.TryParse<GeneroPaciente>(command.Genero, ignoreCase: true, out var genero))
            genero = GeneroPaciente.NaoInformado;

        paciente.AtualizarDados(
            command.NomeCompleto,
            command.Cpf,
            command.DataNascimento,
            genero,
            command.Telefone,
            command.Email,
            command.Endereco,
            command.Observacoes);

        await _repository.Salvar(paciente);
    }
}
