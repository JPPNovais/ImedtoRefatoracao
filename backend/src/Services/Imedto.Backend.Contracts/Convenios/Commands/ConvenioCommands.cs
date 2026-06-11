using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Convenios.Commands;

public class CriarConvenioCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? RegistroAns { get; set; }
}

public class AtualizarConvenioCommand : ICommand
{
    public long ConvenioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? RegistroAns { get; set; }
    public bool Ativo { get; set; }
}

public class ExcluirConvenioCommand : ICommand
{
    public long ConvenioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}

public class AdicionarPlanoConvenioCommand : ICommand
{
    public long ConvenioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class AtualizarPlanoConvenioCommand : ICommand
{
    public long ConvenioId { get; set; }
    public long PlanoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class InativarPlanoConvenioCommand : ICommand
{
    public long ConvenioId { get; set; }
    public long PlanoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
