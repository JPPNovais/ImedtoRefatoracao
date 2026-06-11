using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.PacienteConvenios.Commands;

public class CriarPacienteConvenioCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public long ConvenioId { get; set; }
    public long? PlanoId { get; set; }
    public string NumeroCarteirinha { get; set; } = string.Empty;
    public DateOnly? Validade { get; set; }
}

public class AtualizarPacienteConvenioCommand : ICommand
{
    public long CarteirinhaId { get; set; }
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public long ConvenioId { get; set; }
    public long? PlanoId { get; set; }
    public string NumeroCarteirinha { get; set; } = string.Empty;
    public DateOnly? Validade { get; set; }
    public bool Ativo { get; set; }
}

public class ExcluirPacienteConvenioCommand : ICommand
{
    public long CarteirinhaId { get; set; }
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
