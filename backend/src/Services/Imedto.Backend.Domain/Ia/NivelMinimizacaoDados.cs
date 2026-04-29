namespace Imedto.Backend.Domain.Ia;

/// <summary>
/// Nível de minimização de dados aplicado ao conteúdo enviado para a IA por estabelecimento.
///
/// LGPD: <see cref="Standard"/> aplica as regras-base de sanitização PII (CPF/telefone/email/etc).
/// <see cref="Minimized"/> é uma marca para que o decorator aplique redação adicional
/// (ex: nome próprio, datas) — por hora, apenas registrada para auditoria; reforço efetivo
/// fica para a Fase 3 quando vamos diferenciar o pipeline de sanitização.
/// </summary>
public enum NivelMinimizacaoDados
{
    Standard = 0,
    Minimized = 1
}
