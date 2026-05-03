using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Profissionais.Commands;

public class AtualizarProfissionalCommandHandler : ICommandHandler<AtualizarProfissionalCommand>
{
    private readonly IProfissionalRepository _repository;

    public AtualizarProfissionalCommandHandler(IProfissionalRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AtualizarProfissionalCommand command)
    {
        // Defesa minima: nunca aceitar Guid.Empty.
        if (command.UsuarioId == Guid.Empty)
            throw new BusinessException("Usuário não identificado.");

        var prof = await _repository.ObterPorIdOuNulo(command.UsuarioId)
            ?? throw new BusinessException("Cadastro profissional não encontrado.");

        var conselho = (command.Conselho ?? "").Trim().ToUpperInvariant();
        var uf = (command.Uf ?? "").Trim().ToUpperInvariant();
        var numero = (command.NumeroRegistro ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(conselho) && !string.IsNullOrWhiteSpace(uf) && !string.IsNullOrWhiteSpace(numero)
            && await _repository.ExisteConselhoRegistro(conselho, uf, numero, command.UsuarioId))
        {
            throw new BusinessException("Já existe outro profissional com este número de registro neste conselho/UF.");
        }

        prof.Atualizar(
            command.Conselho,
            command.Uf,
            command.NumeroRegistro,
            command.Especialidade,
            command.Bio);

        await _repository.Salvar(prof);
    }
}
