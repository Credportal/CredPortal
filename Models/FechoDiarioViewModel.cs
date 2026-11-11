using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class FechoDiarioViewModel
    {
        public DateTime DataReferencia { get; set; }
        public bool DiaFechado { get; set; }

        // Totais Globais para os Cards de Resumo Executivo
        public decimal TotalDespesasDiaVolume { get; set; }
        public int TotalDespesasDiaNr { get; set; }

        public decimal TotalDesembolsosVolume { get; set; }
        public int TotalDesembolsosNumero { get; set; }

        public decimal TotalReembolsosVolume { get; set; }
        public int TotalReembolsosNumero { get; set; }

        public int PedidosPendentesNumero { get; set; }
        public int PedidosAprovadosNumero { get; set; }

        // Totais de Carteira Vigente Global
        public decimal CarteiraVigenteTotalVolume { get; set; }
        public int CarteiraVigenteTotalNr { get; set; }

        // Listas detalhadas para as tabelas
        public List<FechoDiarioContaViewModel> SaldosContas { get; set; } = new();
        public List<FechoDiarioGestorViewModel> ResumoGestores { get; set; } = new();
        public List<FechoDiarioGestorViewModel> CarteiraMoraGestores { get; set; } = new();
    }

    // Classes auxiliares para projeção na View
    public class FechoDiarioContaViewModel
    {
        public string NomeConta { get; set; }
        public decimal SaldoAbertura { get; set; }
        public decimal CreditosDia { get; set; }
        public decimal DebitosDia { get; set; }
        public decimal SaldoFecho { get; set; }
    }

    public class FechoDiarioGestorViewModel
    {
        public string GestorNome { get; set; }
        public decimal DesembolsoVolume { get; set; }
        public int DesembolsoNr { get; set; }
        public decimal ReembolsoVolume { get; set; }
        public int ReembolsoNr { get; set; }
        public decimal PedidosPendentesVolume { get; set; }
        public int PedidosPendentesNr { get; set; }
        public decimal PedidosAprovadosVolume { get; set; }
        public int PedidosAprovadosNr { get; set; }
        public decimal JurosRecebidosVolume { get; set; }
        public int JurosRecebidosNr { get; set; }

        public decimal VigenteVolume { get; set; }
        public int VigenteNr { get; set; }

        public decimal MoraMenor7Volume { get; set; }
        public int MoraMenor7Nr { get; set; }
        public decimal MoraMenor15Volume { get; set; }
        public int MoraMenor15Nr { get; set; }
        public decimal MoraMenor30Volume { get; set; }
        public int MoraMenor30Nr { get; set; }
        public decimal MoraMenor60Volume { get; set; }
        public int MoraMenor60Nr { get; set; }
        public decimal MoraMenor90Volume { get; set; }
        public int MoraMenor90Nr { get; set; }
        public decimal MoraMaior90Volume { get; set; }
        public int MoraMaior90Nr { get; set; }

        // Volume Total em Mora (soma de todas as faixas)
        [NotMapped]
        public decimal MoraTotalVolume => MoraMenor7Volume + MoraMenor15Volume + MoraMenor30Volume +
                                          MoraMenor60Volume + MoraMenor90Volume + MoraMaior90Volume;

        // Percentagem de Mora sobre a Carteira Vigente
        [NotMapped]
        public decimal PercentagemMora => VigenteVolume > 0m ? (MoraTotalVolume / VigenteVolume) * 100m : 0m;
    }
}