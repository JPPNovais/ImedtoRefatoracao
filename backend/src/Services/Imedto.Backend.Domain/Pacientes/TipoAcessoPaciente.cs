namespace Imedto.Backend.Domain.Pacientes;

/// <summary>
/// Categorias de acesso a dados pessoais de paciente (LGPD Art. 5 II).
/// Mais granular que <c>TipoAcessoProntuario</c> porque paciente tem
/// operacoes especificas (Export Art. 18, Anonimizacao).
/// </summary>
public enum TipoAcessoPaciente
{
    /// <summary>GET de listagem ou detalhe.</summary>
    Leitura,

    /// <summary>POST/PUT — atualizacao de dados pessoais.</summary>
    Edicao,

    /// <summary>Soft-delete.</summary>
    Exclusao,

    /// <summary>LGPD Art. 18 — direito a portabilidade.</summary>
    Export,

    /// <summary>LGPD Art. 18 — direito ao esquecimento (anonimizacao irreversivel de PII).</summary>
    Anonimizacao,

    /// <summary>Acesso explícito a campo de PII sensível (CPF/telefone completo) via endpoint dedicado.</summary>
    RevelacaoDadosSensiveis
}
