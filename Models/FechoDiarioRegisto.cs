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

        public DateTime DataReferencia { get; set; }
        public DateTime DataFecho { get; set; } = DateTime.Now;
        public string UsuarioFecho { get; set; }
        public bool Fechado { get; set; } = true;

        // Totais consolidados do dia
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

        [Column(TypeName = "decimal(18,2)")]
        public decimal CarteiraVigenteTotalVolume { get; set; }
        public int CarteiraVigenteTotalNr { get; set; }

        // Relações de detalhe
        public virtual ICollection<FechoDiarioConta> SaldosContas { get; set; }
        public virtual ICollection<FechoDiarioGestor> ResumoGestores { get; set; }
    }
}