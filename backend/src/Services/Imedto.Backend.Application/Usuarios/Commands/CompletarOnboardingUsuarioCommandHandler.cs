using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Usuarios.Commands;

public class CompletarOnboardingUsuarioCommandHandler : ICommandHandler<CompletarOnboardingUsuarioCommand>
{
    private readonly IUsuarioRepository _repository;

    public CompletarOnboardingUsuarioCommandHandler(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CompletarOnboardingUsuarioCommand command)
    {
        // Defesa minima: nunca aceitar Guid.Empty (vetor obvio de bypass).
        if (command.UsuarioId == Guid.Empty)
            throw new BusinessException("Usuário não identificado.");

        var usuario = await _repository.ObterPorIdOuNulo(command.UsuarioId)
            ?? throw new BusinessException("Usuário não encontrado.");

        var cpfDigitos = new string((command.Cpf ?? "").Where(char.IsDigit).ToArray());
        if (await _repository.ExisteCpf(cpfDigitos, command.UsuarioId))
            throw new BusinessException("CPF já cadastrado em outra conta.");

        usuario.CompletarOnboarding(command.NomeCompleto, command.Cpf, command.Telefone);
        await _repository.Salvar(usuario);
    }
}
