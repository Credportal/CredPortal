using Analise.Models;
using System.Collections.Generic;

namespace Analise.Models
{
    public class PedidoViewModel
    {
        public List<PedidoModel> ListaPedidos { get; set; }
        public PedidoModel Pedido { get; set; }
    }
}