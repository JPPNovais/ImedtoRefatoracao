using Microsoft.Extensions.Options;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Storage;
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
    private readonly StorageOptions _storageOptions;

    public AdicionarAnexoCommandHandler(
        IProntuarioRepository prontuarioRepo,
        IProntuarioAnexoRepository anexoRepo,
        IAnexoStorageService storage,
        IPacienteRepository pacienteRepo,
        IProntuarioAcessoLogService acessoLog,
        IOptions<StorageOptions> storageOptions)
    {
        _prontuarioRepo = prontuarioRepo;
        _anexoRepo = anexoRepo;
        _storage = storage;
        _pacienteRepo = pacienteRepo;
        _acessoLog = acessoLog;
        _storageOptions = storageOptions.Value;
    }

    public async Task Handle(AdicionarAnexoCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        // Mensagem padronizada (nao vaza existencia cross-tenant).
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente ainda não tem prontuário.");

        // Validação de tamanho (defense-in-depth; o storage também valida ao subir).
        var limiteBytes = (long)_storageOptions.TamanhoMaxMb * 1024L * 1024L;
        if (command.TamanhoBytes <= 0 || command.TamanhoBytes > limiteBytes)
            throw new BusinessException($"Tamanho do anexo inválido (máx. {_storageOptions.TamanhoMaxMb} MB).");

        // Validação de MIME — falhar cedo, antes de gastar I/O com o upload.
        if (string.IsNullOrWhiteSpace(command.MimeType) ||
            !_storageOptions.MimeTypesPermitidos.Contains(command.MimeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException("Tipo de arquivo não permitido.");
        }

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
        if (string.IsNullOrWhiteSpace(nome)) return "arquivo";

        // 1. Path.GetFileName remove qualquer componente de diretório — descarta '..',
        //    '/foo/bar.pdf' e 'C:\evil\bar.pdf' direto, deixando só o nome do arquivo.
        //    GetFileName é seguro mesmo com input malicioso (não toca o filesystem).
        var apenasNome = Path.GetFileName(nome.Replace('\\', '/'));

        // 2. Whitelist de caracteres — letras, dígitos, ponto, hífen, underscore.
        var seguro = string.Join("", apenasNome.Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_'));

        // 3. Bloqueia nomes que sobraram só com pontos ('.', '..', '...').
        if (string.IsNullOrWhiteSpace(seguro) || seguro.All(c => c == '.'))
            return "arquivo";

        return seguro.Length > 80 ? seguro[^80..] : seguro;
    }
}
