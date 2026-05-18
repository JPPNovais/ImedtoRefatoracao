using Imedto.Backend.Domain.PedidosExame;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.PedidosExame;

internal static class PedidoExameParsers
{
    public static TipoPedidoExame ParseTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new BusinessException("Tipo do pedido de exame é obrigatório.");

        return tipo.Trim() switch
        {
            "Laboratorial" => TipoPedidoExame.Laboratorial,
            "Imagem"       => TipoPedidoExame.Imagem,
            "Misto"        => TipoPedidoExame.Misto,
            _ => throw new BusinessException("Tipo de pedido de exame inválido."),
        };
    }
}
