using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class AdicionarAnexoCommandHandler : ICommandHandler<AdicionarAnexoCommand>
{
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAnexoRepository _anexoRepo;
    private readonly IAnexoStorageService _storage;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public AdicionarAnexoCommandHandler(
        IProntuarioRepository prontuarioRepo,
        IProntuarioAnexoRepository anexoRepo,
        IAnexoStorageService storage,
        IPacienteRepository pacienteRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _prontuarioRepo = prontuarioRepo;
        _anexoRepo = anexoRepo;
        _storage = storage;
        _pacienteRepo = pacienteRepo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(AdicionarAnexoCommand command)
    {
        var paciente = await _pacienteRepo.ObterPorId(command.PacienteId);
        if (paciente.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Paciente não pertence a este estabelecimento.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente ainda não tem prontuário.");

        if (command.TamanhoBytes <= 0 || command.TamanhoBytes > 25 * 1024 * 1024)
            throw new BusinessException("Tamanho do anexo inválido (máx. 25 MB).");

        // Path isolado por tenant/paciente/uuid para evitar colisão e simplificar auditoria.
        var nomeSanitizado = SanitizarNome(command.NomeOriginal);
        var path = $"est_{command.EstabelecimentoId}/paciente_{command.PacienteId}/{Guid.NewGuid():N}_{nomeSanitizado}";

        // Upload primeiro (fora da transação do DbContext — é uma API externa).
        await _storage.UploadAsync(path, command.Conteudo, command.MimeType);

        // Registra metadata.
        var anexo = ProntuarioAnexo.Registrar(
            prontuario.Id,
            command.EstabelecimentoId,
            command.EvolucaoId,
            path,
            command.NomeOriginal,
            command.MimeType,
            command.TamanhoBytes,
            command.AutorUsuarioId);

        await _anexoRepo.Salvar(anexo);

        command.AnexoIdCriado = anexo.Id;
        command.StoragePath = path;

        // Audit LGPD: anexar um documento a prontuário é escrita sensível.
        await _acessoLog.RegistrarAsync(prontuario.Id, command.AutorUsuarioId, command.EstabelecimentoId, TipoAcessoProntuario.Escrita);
    }

    private static string SanitizarNome(string nome)
    {
        var seguro = string.Join("", nome.Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_'));
        if (string.IsNullOrWhiteSpace(seguro)) return "arquivo";
        return seguro.Length > 80 ? seguro[^80..] : seguro;
    }
}
