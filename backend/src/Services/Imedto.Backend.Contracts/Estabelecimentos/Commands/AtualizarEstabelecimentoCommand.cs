using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Estabelecimentos.Commands;

public class AtualizarEstabelecimentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string NomeFantasia { get; set; }
    public string RazaoSocial { get; set; }
    public string Cnpj { get; set; }
    public string Telefone { get; set; }
    public string Endereco { get; set; }
}
