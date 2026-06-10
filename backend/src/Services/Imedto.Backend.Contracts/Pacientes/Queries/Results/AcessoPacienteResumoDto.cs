namespace Imedto.Backend.Contracts.Pacientes.Queries.Results;

/// <summary>
/// Resumo de uma linha do relatório de acessos LGPD (Art. 9º/18).
/// Minimização: sem usuario_id cru, ip_origem, prontuario_id ou conteúdo clínico.
/// O rótulo leigo do "o quê" é montado no backend (coluna acao) — fonte única.
/// </summary>
public class AcessoPacienteResumoDto
{
    /// <summary>Nome do usuário que acessou, ou "Usuário removido" se não resolver.</summary>
    public string Quem { get; set; } = string.Empty;

    /// <summary>Data/hora do acesso (UTC).</summary>
    public DateTime Quando { get; set; }

    /// <summary>"Cadastro/dados do paciente" ou "Prontuário".</summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>Descrição leiga da ação (ex.: "Consultou o prontuário").</summary>
    public string Acao { get; set; } = string.Empty;
}

/// <summary>
/// Página paginada do relatório de acessos do paciente.
/// </summary>
public class PaginaAcessosDto
{
    public IEnumerable<AcessoPacienteResumoDto> Itens { get; set; } = Enumerable.Empty<AcessoPacienteResumoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
