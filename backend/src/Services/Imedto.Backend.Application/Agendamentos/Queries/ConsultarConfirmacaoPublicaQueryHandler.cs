using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Queries;

/// <summary>
/// Query pública de confirmação de agendamento via token (Fase 2, CA17/CA23).
///
/// NÃO filtra por tenant — o token (256 bits) é o único segredo (R20).
/// Grava acesso "visualizou_publico" no log de auditoria (R16, CA22).
///
/// LGPD: retorna apenas nome fantasia do estabelecimento, nome do profissional,
/// tipo de serviço e data/hora. Sem paciente_id, estabelecimento_id, nome, CPF
/// ou e-mail do paciente no payload de resposta.
/// </summary>
public sealed class ConsultarConfirmacaoPublicaQueryHandler
    : IRequestHandler<ConsultarConfirmacaoPublicaQuery, ConfirmacaoPublicaDto>
{
    public const string MensagemLinkInvalido = Agendamento.MensagemLinkInvalido;

    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IEstabelecimentoRepository _estabRepo;
    private readonly IUsuarioRepository _usuarioRepo;

    public ConsultarConfirmacaoPublicaQueryHandler(
        IAgendamentoRepository agendamentoRepo,
        IEstabelecimentoRepository estabRepo,
        IUsuarioRepository usuarioRepo)
    {
        _agendamentoRepo = agendamentoRepo;
        _estabRepo = estabRepo;
        _usuarioRepo = usuarioRepo;
    }

    public async Task<ConfirmacaoPublicaDto> Handle(ConsultarConfirmacaoPublicaQuery q)
    {
        if (string.IsNullOrWhiteSpace(q.Token))
            throw new BusinessException(MensagemLinkInvalido);

        var agendamento = await _agendamentoRepo.ObterPorTokenOuNulo(q.Token);
        if (agendamento is null)
            throw new BusinessException(MensagemLinkInvalido);

        // Auditar acesso mesmo antes de validar (R16, CA22).
        var acao = EhTokenValido(agendamento) ? "visualizou_publico" : "tentativa_invalida";
        var log = AgendamentoConfirmacaoAcessoLog.Registrar(
            agendamento.Id, agendamento.EstabelecimentoId, q.IpOrigem, q.UserAgent, acao);
        await _agendamentoRepo.SalvarAcessoLog(log);

        // CA19: token inválido/expirado/cancelado → 410 genérico.
        if (!EhTokenValido(agendamento))
            throw new BusinessException(MensagemLinkInvalido);

        var estab = await _estabRepo.ObterPorIdOuNulo(agendamento.EstabelecimentoId);
        var profissional = await _usuarioRepo.ObterPorIdOuNulo(agendamento.ProfissionalUsuarioId);

        return new ConfirmacaoPublicaDto
        {
            // CA17/CA23: sem paciente_id, estabelecimento_id, CPF, e-mail.
            EstabelecimentoNome = estab?.NomeFantasia ?? "Estabelecimento",
            ProfissionalNome = profissional?.NomeCompleto ?? "Profissional",
            TipoServico = agendamento.TipoServico,
            InicioPrevisto = agendamento.InicioPrevisto,
            FimPrevisto = agendamento.FimPrevisto,
            StatusAgendamento = agendamento.Status.ToString(),
        };
    }

    private static bool EhTokenValido(Agendamento a)
        => a.Status is AgendamentoStatus.Agendado or AgendamentoStatus.Confirmado
           && !string.IsNullOrWhiteSpace(a.TokenConfirmacao)
           && a.TokenConfirmacaoExpiraEm is not null
           && a.TokenConfirmacaoExpiraEm >= DateTime.UtcNow;
}
