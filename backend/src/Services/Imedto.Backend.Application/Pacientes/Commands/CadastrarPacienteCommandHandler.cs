using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Text;

namespace Imedto.Backend.Application.Pacientes.Commands;

public class CadastrarPacienteCommandHandler : ICommandHandler<CadastrarPacienteCommand>
{
    private readonly IPacienteRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly IAssinaturaService _assinaturaService;

    public CadastrarPacienteCommandHandler(
        IPacienteRepository repository,
        IEventBus eventBus,
        IAssinaturaService assinaturaService)
    {
        _repository = repository;
        _eventBus = eventBus;
        _assinaturaService = assinaturaService;
    }

    public async Task Handle(CadastrarPacienteCommand command)
    {
        if (await _assinaturaService.LimiteAtingidoAsync(command.EstabelecimentoId, "pacientes"))
            throw new BusinessException("Plano não permite mais pacientes. Faça upgrade.");

        var cpfDigitos = TextSanitizer.DigitosOuNulo(command.Cpf);
        if (cpfDigitos != null &&
            await _repository.ExisteCpfNoEstabelecimento(cpfDigitos, command.EstabelecimentoId, ignorarPacienteId: 0))
        {
            throw new BusinessException("Já existe um paciente com este CPF neste estabelecimento.");
        }

        var docInternacional = TextSanitizer.TrimOuNulo(command.DocumentoInternacional);
        if (docInternacional != null &&
            await _repository.ExisteDocumentoInternacionalNoEstabelecimento(docInternacional, command.EstabelecimentoId, ignorarPacienteId: 0))
        {
            throw new BusinessException("Já existe um paciente com este documento neste estabelecimento.");
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
            command.Observacoes,
            command.DocumentoInternacional);

        await _repository.Salvar(paciente);
        paciente.MarcarComoCadastrado();

        foreach (var evt in paciente.DomainEvents)
            await _eventBus.Publish(evt);

        paciente.ClearDomainEvents();
    }
}
