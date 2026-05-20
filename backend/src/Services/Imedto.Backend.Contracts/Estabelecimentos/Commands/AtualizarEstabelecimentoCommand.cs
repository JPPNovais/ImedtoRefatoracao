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
    /// <summary>Cidade onde o estabelecimento opera. Opcional. Usado em {{cidade_atual}} dos termos.</summary>
    public string Cidade { get; set; }
    /// <summary>UF (2 letras). Opcional. Validação no Aggregate.</summary>
    public string Estado { get; set; }
}
