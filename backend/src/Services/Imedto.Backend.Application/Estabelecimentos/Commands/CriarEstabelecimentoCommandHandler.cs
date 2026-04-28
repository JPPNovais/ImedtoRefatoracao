using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

public class CriarEstabelecimentoCommandHandler : ICommandHandler<CriarEstabelecimentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEventBus _eventBus;

    public CriarEstabelecimentoCommandHandler(
        IEstabelecimentoRepository repository,
        IUsuarioRepository usuarioRepository,
        IEventBus eventBus)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _eventBus = eventBus;
    }

    public async Task Handle(CriarEstabelecimentoCommand command)
    {
        var usuario = await _usuarioRepository.ObterPorId(command.DonoUsuarioId);

        // Regra: CPF do criador é obrigatório — só quem completou o onboarding pode criar.
        if (string.IsNullOrWhiteSpace(usuario.Cpf))
            throw new BusinessException("Complete o onboarding (nome e CPF) antes de criar um estabelecimento.");

        // Regra: cada usuário pode ser dono de apenas 1 estabelecimento.
        if (await _repository.UsuarioJaEhDono(command.DonoUsuarioId))
            throw new BusinessException("Você já é dono de um estabelecimento. Cada usuário pode ter apenas um.");

        var cnpjDigitos = new string((command.Cnpj ?? "").Where(char.IsDigit).ToArray());
        if (cnpjDigitos.Length > 0 && await _repository.ExisteCnpj(cnpjDigitos, ignorarEstabelecimentoId: 0))
            throw new BusinessException("CNPJ já cadastrado em outro estabelecimento.");

        var estab = Estabelecimento.Criar(
            command.DonoUsuarioId,
            command.NomeFantasia,
            command.RazaoSocial,
            command.Cnpj,
            command.Telefone,
            command.Endereco);

        await _repository.Salvar(estab);   // Popula Id auto-gerado (SaveChanges interno)
        estab.MarcarComoCriado();          // Anexa o domain event com o Id correto

        foreach (var domainEvent in estab.DomainEvents)
            await _eventBus.Publish(domainEvent);

        estab.ClearDomainEvents();
    }
}
