using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("FechosDiariosRegistos")]
    public class FechoDiarioRegisto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DataFecho { get; set; }

        public DateTime DataHoraProcessamento { get; set; } = DateTime.Now;

        public int UsuarioId { get; set; }

        // Resumo Executivo
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDespesasVolume { get; set; }
        public int TotalDespesasNumero { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDesembolsosVolume { get; set; }
        public int TotalDesembolsosNumero { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalReembolsosVolume { get; set; }
        public int TotalReembolsosNumero { get; set; }

        public int PedidosPendentesNumero { get; set; }
        public int PedidosAprovadosNumero { get; set; }

        public int CarteiraVigenteNumero { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CarteiraVigenteVolume { get; set; }

        // Relações dos Detalhes
        public virtual List<FechoDiarioConta> SaldosContas { get; set; } = new List<FechoDiarioConta>();
        public virtual List<FechoDiarioGestor> MapaDesempenho { get; set; } = new List<FechoDiarioGestor>();
    }
}