using System;
using System.Collections.Generic;

namespace Analise.Models
{
    public class FechoDiarioViewModel
    {
        public DateTime DataReferencia { get; set; } = DateTime.Now;

        // --- 1. RESUMO FINANCEIRO DO DIA ---
        public int TotalDespesasNumero { get; set; }
        public decimal TotalDespesasVolume { get; set; }

        public int TotalDesembolsosNumero { get; set; }
        public decimal TotalDesembolsosVolume { get; set; }

        public int TotalReembolsosNumero { get; set; }
        public decimal TotalReembolsosVolume { get; set; }

        // --- 2. ESTADO DOS PEDIDOS ---
        public int PedidosPendentesNumero { get; set; }
        public int PedidosAprovadosNumero { get; set; }

        // --- 3. CARTEIRA GERAL ---
        public int CarteiraVigenteNumero { get; set; }
        public decimal CarteiraVigenteVolume { get; set; }

        public int CarteiraMoraNumero { get; set; }
        public decimal CarteiraMoraVolume { get; set; }

        // --- 4. SALDOS DE CONTAS (Abertura e Fecho) ---
        public List<ContaResumoSaldoDto> SaldosContas { get; set; } = new List<ContaResumoSaldoDto>();

        // --- 5. MAPA DE DESEMPENHO POR GESTOR (Baseado na imagem) ---
        public List<MapaDesempenhoLinhaDto> MapaDesempenho { get; set; } = new List<MapaDesempenhoLinhaDto>();
    }

    public class ContaResumoSaldoDto
    {
        public string Banco { get; set; }
        public string Conta { get; set; }
        public string Titular { get; set; }
        public decimal SaldoAbertura { get; set; }
        public decimal Creditos { get; set; } // Entradas no dia
        public decimal Debitos { get; set; }  // Saídas no dia (despesas, desembolsos)
        public decimal SaldoFecho { get; set; }
    }
    public class MapaDesempenhoLinhaDto
    {
        public string GestorNome { get; set; }

        // Desembolso no Mês
        public decimal DesembolsoMontante { get; set; }
        public int QtdNovos { get; set; }
        public int QtdRep { get; set; }

        // Vigente
        public int VigenteQtd { get; set; }
        public decimal VigenteCapital { get; set; }

        // Reembolso
        public decimal ReembolsoCapital { get; set; }
        public int ReembolsoQtd { get; set; }

        // Carteira em Mora Hoje
        public int MoraHojeQtd { get; set; }
        public decimal MoraHojeCapital { get; set; }

        // Carteira em Mora Total (Risco)
        public int MoraTotalQtd { get; set; }
        public decimal MoraTotalCapital { get; set; }
        public decimal MoraTotalPercentual => VigenteCapital > 0 ? (MoraTotalCapital / VigenteCapital) * 100 : 0;

        // Carteira em Mora em Dias (> 7, > 15, > 30 Dias)
        public decimal Dias7Capital { get; set; }
        public int Dias7Qtd { get; set; }

        public decimal Dias15Capital { get; set; }
        public int Dias15Qtd { get; set; }

        public decimal Dias30Capital { get; set; }
        public int Dias30Qtd { get; set; }
    }
}