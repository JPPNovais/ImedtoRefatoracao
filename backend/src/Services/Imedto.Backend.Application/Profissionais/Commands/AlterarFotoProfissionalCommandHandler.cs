using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Profissionais.Commands;

public class AlterarFotoProfissionalCommandHandler : ICommandHandler<AlterarFotoProfissionalCommand>
{
    private readonly IProfissionalRepository _repository;
    private readonly IFotoStorageService _storage;

    public AlterarFotoProfissionalCommandHandler(
        IProfissionalRepository repository,
        IFotoStorageService storage)
    {
        _repository = repository;
        _storage = storage;
    }

    public async Task Handle(AlterarFotoProfissionalCommand command)
    {
        // Defesa minima: nunca aceitar Guid.Empty.
        if (command.UsuarioId == Guid.Empty)
            throw new BusinessException("Usuário não identificado.");

        var prof = await _repository.ObterPorIdOuNulo(command.UsuarioId)
            ?? throw new BusinessException("Cadastre seu perfil profissional antes de adicionar foto.");

        var ext = string.IsNullOrWhiteSpace(command.Extensao) ? "jpg" : command.Extensao.Trim().TrimStart('.').ToLowerInvariant();
        var path = $"profissionais/{command.UsuarioId}.{ext}";

        var url = await _storage.UploadFotoAsync(path, command.Conteudo, command.MimeType);

        prof.AlterarFoto(url);
        await _repository.Salvar(prof);
    }
}
