namespace Imedto.Backend.Domain.Vinculos;

public enum VinculoStatus
{
    /// <summary>Convite enviado, aguardando o profissional aceitar.</summary>
    Convidado,

    /// <summary>Vínculo aceito e ativo — profissional atua no estabelecimento.</summary>
    Ativo,

    /// <summary>Vínculo encerrado pelo dono ou pelo próprio profissional.</summary>
    Inativo
}
