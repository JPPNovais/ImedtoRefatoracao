using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Atualiza um exame físico existente. A coleção de regiões enviada substitui a anterior:
/// - regiões fora da lista são removidas;
/// - regiões novas são adicionadas;
/// - regiões existentes têm achados/severidade/lateralidade atualizados.
/// Mantém invariantes via aggregate (uniqueness, validações).
/// </summary>
public class AtualizarExameFisicoCommandHandler : ICommandHandler<AtualizarExameFisicoCommand>
{
    private readonly IExameFisicoRepository _exameRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public AtualizarExameFisicoCommandHandler(
        IExameFisicoRepository exameRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _exameRepo = exameRepo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(AtualizarExameFisicoCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var exame = await _exameRepo.ObterPorIdOuNulo(command.ExameFisicoId, command.EstabelecimentoId)
            ?? throw new BusinessException("Exame físico não encontrado.");
        if (exame.DeletadoEm is not null)
            throw new BusinessException("Exame físico está deletado.");

        if (!string.IsNullOrWhiteSpace(command.DadosGeraisJson))
            RegistrarExameFisicoCommandHandler.ValidarJsonOuLancar(command.DadosGeraisJson, "Dados gerais");

        exame.AtualizarDadosGerais(command.DadosGeraisJson, command.ObservacoesGerais);

        // Sincroniza regiões — remove o que sumiu, atualiza o que ficou, adiciona o que é novo.
        var input = (command.Regioes ?? Array.Empty<RegiaoExameFisicoInput>()).ToList();
        var enviadas = input
            .Select(r => r.Codigo)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 1) remove os que sumiram (snapshot da lista atual antes de iterar).
        var existentes = exame.Regioes.Select(r => r.RegiaoCodigo).ToList();
        foreach (var codigo in existentes.Where(c => !enviadas.Contains(c)))
            exame.RemoverRegiao(codigo);

        // 2) atualiza ou adiciona.
        var atuaisLookup = exame.Regioes.ToDictionary(r => r.RegiaoCodigo, StringComparer.OrdinalIgnoreCase);
        foreach (var r in input)
        {
            if (string.IsNullOrWhiteSpace(r.Codigo))
                throw new BusinessException("Código da região é obrigatório.");

            var lateralidade = RegistrarExameFisicoCommandHandler.ParsearLateralidade(r.Lateralidade);
            var severidade = RegistrarExameFisicoCommandHandler.ParsearSeveridade(r.Severidade);

            if (atuaisLookup.ContainsKey(r.Codigo))
            {
                exame.AtualizarRegiao(r.Codigo, r.Achados, severidade, lateralidade);
            }
            else
            {
                exame.AdicionarRegiao(RegistrarExameFisicoCommandHandler.MapearRegiao(r));
            }
        }

        await _exameRepo.Salvar(exame);

        // Audit LGPD: edição de exame físico é escrita sensível.
        await _acessoLog.RegistrarAsync(
            exame.ProntuarioId, command.AutorUsuarioId, command.EstabelecimentoId, TipoAcessoProntuario.Escrita);
    }
}
