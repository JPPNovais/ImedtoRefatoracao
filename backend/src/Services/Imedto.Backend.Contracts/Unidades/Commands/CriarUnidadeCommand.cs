using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Unidades.Commands;

public class CriarUnidadeCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string Nome { get; set; }
    public bool IsPrincipal { get; set; }
    public string Cep { get; set; }
    public string Logradouro { get; set; }
    public string Numero { get; set; }
    public string Complemento { get; set; }
    public string Bairro { get; set; }
    public string Cidade { get; set; }
    public string Estado { get; set; }
    public string Telefone { get; set; }
}
