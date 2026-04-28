using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

public class AlterarFotoEstabelecimentoCommandHandler : ICommandHandler<AlterarFotoEstabelecimentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;
    private readonly IFotoStorageService _storage;

    public AlterarFotoEstabelecimentoCommandHandler(
        IEstabelecimentoRepository repository,
        IFotoStorageService storage)
    {
        _repository = repository;
        _storage = storage;
    }

    public async Task Handle(AlterarFotoEstabelecimentoCommand command)
    {
        var estab = await _repository.ObterPorId(command.EstabelecimentoId);

        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode alterar a foto.");

        var ext = string.IsNullOrWhiteSpace(command.Extensao) ? "jpg" : command.Extensao.Trim().TrimStart('.').ToLowerInvariant();
        var path = $"estabelecimentos/{command.EstabelecimentoId}.{ext}";

        var url = await _storage.UploadFotoAsync(path, command.Conteudo, command.MimeType);

        estab.AlterarFoto(url);
        await _repository.Salvar(estab);
    }
}
