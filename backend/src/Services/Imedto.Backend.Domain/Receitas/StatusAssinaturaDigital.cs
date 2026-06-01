namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Estado da assinatura digital da receita.
/// Máquina de estados: NaoAssinada → AssinaturaPendente → AssinadaIcp | FalhaAssinatura | AssinaturaExpirada.
/// Re-disparo permitido de FalhaAssinatura e AssinaturaExpirada → AssinaturaPendente.
/// AssinadaIcp é estado terminal (imutável).
///
/// <list type="bullet">
///   <item><see cref="NaoAssinada"/>: default. Receita gerada apenas com a
///   identificação do profissional emissor (nome, conselho, especialidade). Não
///   tem validade jurídica como receita digital — deve ser impressa e assinada
///   manualmente quando exigido (CFM 2.299/2021 + Lei 14.063/2020).</item>
///   <item><see cref="AssinaturaPendente"/>: assinatura foi disparada no provedor
///   ICP-Brasil (BirdID/VIDaaS). Aguardando confirmação do médico no app móvel.</item>
///   <item><see cref="AssinadaIcp"/>: assinada com certificado ICP-Brasil (A1/A3
///   ou nuvem) — validade jurídica plena, dispensa impressão. Estado terminal.</item>
///   <item><see cref="FalhaAssinatura"/>: médico recusou o PUSH ou o provedor
///   retornou erro. Pode ser re-disparado.</item>
///   <item><see cref="AssinaturaExpirada"/>: job periódico marcou como expirada
///   após 30 minutos sem resposta do médico. Pode ser re-disparado.</item>
///   <item><see cref="AssinadaMemed"/>: reservado para integração futura Memed.</item>
/// </list>
/// </summary>
public enum StatusAssinaturaDigital
{
    NaoAssinada = 0,
    AssinadaIcp = 1,
    AssinadaMemed = 2,
    AssinaturaPendente = 3,
    FalhaAssinatura = 4,
    AssinaturaExpirada = 5
}
