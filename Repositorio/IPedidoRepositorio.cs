using Analise.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public interface IPedidoRepositorio
    {
        Task<List<PedidoModel>> BuscarTodosAsync();
        Task<PedidoModel> BuscarPorIdAsync(int id);
        Task<PedidoModel> AdicionarAsync(PedidoModel pedido);
        Task<PedidoModel> AtualizarAsync(PedidoModel pedido);
        Task<bool> ApagarAsync(int id);
        Task<int> ContarPedidosPorEntidadeAsync(int entidadeId);

        // --- Garantias do Pedido Principal ---
        Task<List<GarantiaModel>> BuscarGarantiasPorEntidadeECrrAsync(int entidadeId, long? crr);
        Task<GarantiaModel> AdicionarGarantiaAsync(GarantiaModel garantia);

        // --- Intervenientes do Pedido ---
        Task<List<IntervenienteModel>> BuscarIntervenientesPorPedidoAsync(int entidadeId, long? crr);
        Task<IntervenienteModel> AdicionarIntervenienteAsync(IntervenienteModel interveniente);
        Task<bool> RemoverIntervenienteAsync(int id);

        // --- Garantias do Interveniente ---
        Task<List<GarantiaModel>> BuscarGarantiasIntervenienteAsync(int entidadeIdInterveniente, long? crr);
        Task<GarantiaModel> AdicionarGarantiaIntervenienteAsync(GarantiaModel garantia);
        Task<bool> ApagarGarantiaAsync(int id);

        Task<List<GarantiaModel>> BuscarGarantiasClienteAsync(int entidadeId, long? crr);
        Task AtualizarDecisaoPedidoAsync(PedidoModel pedido);
    }
}