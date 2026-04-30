namespace Imedto.Backend.Contracts.Lgpd.Queries;

/// <summary>
/// Exportação de dados do titular (Art. 18 LGPD).
///
/// Decisão de escopo V1: inclui apenas dados de conta (usuário, profissional, vínculos,
/// notificações e consentimentos LGPD). Dados clínicos profundos (prontuário, evoluções,
/// receitas, procedimentos) são omitidos nesta versão — sua geração exige job assíncrono
/// que produz um arquivo e envia link de download. TODO: implementar no item 4.3-V2.
/// </summary>
public class MeusDadosLgpdDto
{
    // Dados de conta
    public Guid UsuarioId { get; init; }
    public string Email { get; init; }
    public string NomeCompleto { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime? UltimoAcessoEm { get; init; }

    // Profissional vinculado (null se não for profissional)
    public ProfissionalResumidoDto Profissional { get; init; }

    // Vínculos com estabelecimentos
    public IEnumerable<VinculoResumidoDto> Vinculos { get; init; } = [];

    // Notificações recebidas (não inclui conteúdo clínico)
    public IEnumerable<NotificacaoResumidaDto> Notificacoes { get; init; } = [];

    // Histórico de consentimentos LGPD
    public IEnumerable<ConsentimentoDto> Consentimentos { get; init; } = [];
}

public class ProfissionalResumidoDto
{
    public long Id { get; init; }
    public string NomeCompleto { get; init; }
    public string Crm { get; init; }
}

public class VinculoResumidoDto
{
    public long EstabelecimentoId { get; init; }
    public string NomeEstabelecimento { get; init; }
    public string Status { get; init; }
    public DateTime VinculadoEm { get; init; }
}

public class NotificacaoResumidaDto
{
    public long Id { get; init; }
    public string Titulo { get; init; }
    public bool Lida { get; init; }
    public DateTime CriadaEm { get; init; }
}

public class ConsentimentoDto
{
    public string Tipo { get; init; }
    public string Versao { get; init; }
    public DateTime AceitoEm { get; init; }
}
