namespace Imedto.Backend.Domain.Migracao;

/// <summary>Status de cada linha do arquivo na pipeline de carga.</summary>
public enum StatusMigracaoRegistro
{
    Pendente,
    ImportadoCriado,
    ImportadoAtualizado,
    Rejeitado,
    Pulado
}
