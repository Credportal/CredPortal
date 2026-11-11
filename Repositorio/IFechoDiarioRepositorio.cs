using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Analise.Models;

namespace Analise.Repositories // Ajuste o namespace conforme o seu projeto
{
    public interface IFechoDiarioRepositorio
    {
        Task<FechoDiarioRegisto> ObterPorDataAsync(DateTime data);
        Task<bool> ExisteFechoParaDataAsync(DateTime data);
        Task InserirFechoAsync(FechoDiarioRegisto fecho);

        // Métodos de consulta real para cálculo em tempo real (caso queira separar a lógica de negócio do controller)
        Task<List<ContaModel>> ObterContasAsync();
        Task<List<MovimentoExternoModel>> ObterMovimentosExternosPorDataAsync(DateTime data, string tipoOperacao);
        Task<List<AmortizacaoModel>> ObterAmortizacoesPagasPorDataAsync(DateTime data);
        Task<List<PedidoModel>> ObterPedidosAsync();
    }
}