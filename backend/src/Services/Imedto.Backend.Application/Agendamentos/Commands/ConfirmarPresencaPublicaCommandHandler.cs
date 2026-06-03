using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

/// <summary>
/// Confirma presença do paciente via link público anônimo (Fase 2, CA18/CA20).
///
/// Fluxo:
/// 1. Carrega agendamento por token (sem filtro de tenant — token é segredo).
/// 2. Token inexistente/expirado/agendamento cancelado → BusinessException com mensagem genérica → 410.
/// 3. Idempotência: já Confirmado → não altera estado, devolve JaConfirmado (200).
/// 4. Agendado com token válido → chama ConfirmarPorLinkPublico → Confirmado.
/// 5. Grava acesso "confirmou_presenca" no log (R16, CA22).
/// </summary>
public sealed class ConfirmarPresencaPublicaCommandHandler
    : ICommandHandler<ConfirmarPresencaPublicaCommand>
{
    public const string MensagemLinkInvalido = Agendamento.MensagemLinkInvalido;

    private readonly IAgendamentoRepository _agendamentoRepo;

    public ConfirmarPresencaPublicaCommandHandler(IAgendamentoRepository agendamentoRepo)
        => _agendamentoRepo = agendamentoRepo;

    public async Task Handle(ConfirmarPresencaPublicaCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Token))
            throw new BusinessException(MensagemLinkInvalido);

        var agendamento = await _agendamentoRepo.ObterPorTokenOuNulo(cmd.Token);
        if (agendamento is null)
            throw new BusinessException(MensagemLinkInvalido);

        // CA20: idempotência — já Confirmado retorna 200 sem alterar estado.
        if (agendamento.Status == AgendamentoStatus.Confirmado)
        {
            cmd.Resultado = ResultadoConfirmacaoPresenca.JaConfirmado;
            await GravarLog(agendamento, cmd, "tentativa_idempotente");
            return;
        }

        // CA19: cancelado, concluído ou token inválido/expirado → 410 genérico.
        if (agendamento.Status != AgendamentoStatus.Agendado
            || string.IsNullOrWhiteSpace(agendamento.TokenConfirmacao)
            || agendamento.TokenConfirmacaoExpiraEm is null
            || agendamento.TokenConfirmacaoExpiraEm < DateTime.UtcNow)
        {
            await GravarLog(agendamento, cmd, "tentativa_invalida");
            throw new BusinessException(MensagemLinkInvalido);
        }

        // CA18: token válido + Agendado → Confirmado.
        agendamento.ConfirmarPorLinkPublico(cmd.IpOrigem, cmd.UserAgent);
        await _agendamentoRepo.Salvar(agendamento);
        await GravarLog(agendamento, cmd, "confirmou_presenca");

        cmd.Resultado = ResultadoConfirmacaoPresenca.ConfirmadoAgora;
    }

    private Task GravarLog(Agendamento agendamento, ConfirmarPresencaPublicaCommand cmd, string acao)
    {
        var log = AgendamentoConfirmacaoAcessoLog.Registrar(
            agendamento.Id, agendamento.EstabelecimentoId, cmd.IpOrigem, cmd.UserAgent, acao);
        return _agendamentoRepo.SalvarAcessoLog(log);
    }
}
