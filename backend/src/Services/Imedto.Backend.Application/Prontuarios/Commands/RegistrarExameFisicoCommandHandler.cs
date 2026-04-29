using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Registra um novo exame físico vinculado a uma evolução existente.
/// Validações: evolução existe, pertence a um prontuário do estabelecimento, não está deletada.
/// O <see cref="DadosGeraisJson"/> é armazenado como string (jsonb no banco) — o backend
/// não validará a forma do JSON; é o aggregate de UI quem garante a estrutura, mas
/// fazemos parse mínimo para garantir que é JSON válido (defesa contra payload corrompido).
/// </summary>
public class RegistrarExameFisicoCommandHandler : ICommandHandler<RegistrarExameFisicoCommand>
{
    private readonly AppDbContext _context;
    private readonly IExameFisicoRepository _exameRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public RegistrarExameFisicoCommandHandler(
        AppDbContext context,
        IExameFisicoRepository exameRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _context = context;
        _exameRepo = exameRepo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(RegistrarExameFisicoCommand command)
    {
        if (command.EvolucaoId <= 0)
            throw new BusinessException("Evolução é obrigatória.");

        // Carrega evolução + prontuário em uma query — validamos que pertence ao tenant.
        var dados = await (
            from e in _context.ProntuarioEvolucoes.AsNoTracking()
            join p in _context.Prontuarios.AsNoTracking() on e.ProntuarioId equals p.Id
            where e.Id == command.EvolucaoId
            select new
            {
                e.Id,
                e.ProntuarioId,
                e.DeletadoEm,
                p.PacienteId,
                p.EstabelecimentoId,
                ProntuarioDeletado = p.DeletadoEm
            }).FirstOrDefaultAsync()
            ?? throw new BusinessException("Evolução não encontrada.");

        if (dados.DeletadoEm is not null)
            throw new BusinessException("Evolução está deletada.");
        if (dados.ProntuarioDeletado is not null)
            throw new BusinessException("Prontuário está deletado.");
        if (dados.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Evolução não pertence a este estabelecimento.");

        // Validação leve do JSON — evita armazenar lixo.
        if (!string.IsNullOrWhiteSpace(command.DadosGeraisJson))
            ValidarJsonOuLancar(command.DadosGeraisJson, "Dados gerais");

        var regioesInput = (command.Regioes ?? Array.Empty<RegiaoExameFisicoInput>())
            .Select(MapearRegiao)
            .ToList();

        var exame = ExameFisico.Registrar(
            evolucaoId: dados.Id,
            prontuarioId: dados.ProntuarioId,
            pacienteId: dados.PacienteId,
            estabelecimentoId: dados.EstabelecimentoId,
            realizadoPorUsuarioId: command.AutorUsuarioId,
            realizadoEm: command.RealizadoEm,
            dadosGeraisJson: command.DadosGeraisJson,
            observacoesGerais: command.ObservacoesGerais,
            regioes: regioesInput);

        await _exameRepo.Salvar(exame);
        command.ExameFisicoIdCriado = exame.Id;

        // Audit LGPD: registro de exame físico é escrita sensível (dados clínicos).
        await _acessoLog.RegistrarAsync(
            dados.ProntuarioId, command.AutorUsuarioId, command.EstabelecimentoId, TipoAcessoProntuario.Escrita);
    }

    internal static ExameFisico.RegiaoInput MapearRegiao(RegiaoExameFisicoInput r)
    {
        var lateralidade = ParsearLateralidade(r.Lateralidade);
        var severidade = ParsearSeveridade(r.Severidade);
        return new ExameFisico.RegiaoInput(
            Codigo: r.Codigo,
            PaiCodigo: r.PaiCodigo,
            Lateralidade: lateralidade,
            Achados: r.Achados,
            Severidade: severidade,
            Ordem: r.Ordem);
    }

    internal static Lateralidade ParsearLateralidade(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return Lateralidade.NaoAplicavel;
        if (Enum.TryParse<Lateralidade>(valor, ignoreCase: true, out var resultado))
            return resultado;
        throw new BusinessException($"Lateralidade '{valor}' inválida.");
    }

    internal static SeveridadeExame? ParsearSeveridade(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (Enum.TryParse<SeveridadeExame>(valor, ignoreCase: true, out var resultado))
            return resultado;
        throw new BusinessException($"Severidade '{valor}' inválida.");
    }

    internal static void ValidarJsonOuLancar(string json, string campo)
    {
        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessException($"{campo}: JSON inválido.");
        }
    }
}
