namespace Imedto.Backend.Contracts.Agendamentos.Queries.Results;

/// <summary>
/// Resultado da consulta pública de confirmação de agendamento (Fase 2).
///
/// LGPD: contém apenas nome do estabelecimento, profissional, tipo de serviço e data/hora.
/// Sem paciente_id, estabelecimento_id, nome, CPF ou e-mail do paciente.
/// </summary>
public class ConfirmacaoPublicaDto
{
    public string EstabelecimentoNome { get; set; } = string.Empty;
    public string ProfissionalNome { get; set; } = string.Empty;
    public string TipoServico { get; set; } = string.Empty;
    public DateTime InicioPrevisto { get; set; }
    public DateTime FimPrevisto { get; set; }
    public string StatusAgendamento { get; set; } = string.Empty;
}
