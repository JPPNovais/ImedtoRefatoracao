namespace Imedto.Backend.Contracts.Admin.Configs.Queries.Results;

/// <summary>DTO de uma configuração global retornado para o admin.</summary>
public class ConfigAdminDto
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Secao { get; set; }
    public string? Descricao { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
    public Guid? AtualizadoPorAdminId { get; set; }
}

/// <summary>Seção de configurações agrupadas por secao.</summary>
public record SecaoConfigsDto(
    string Secao,
    IReadOnlyList<ConfigAdminDto> Configs);
