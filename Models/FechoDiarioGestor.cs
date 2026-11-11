using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("FechosDiariosGestores")]
    public class FechoDiarioGestor
    {
        [Key]
        public int Id { get; set; }

        public int FechoDiarioRegistoId { get; set; }
        [ForeignKey("FechoDiarioRegistoId")]
        public virtual FechoDiarioRegisto FechoRegisto { get; set; }

        public string GestorNome { get; set; }

        // Operacionais do Dia
        [Column(TypeName = "decimal(18,2)")]
        public decimal DesembolsoVolume { get; set; }
        public int DesembolsoNr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReembolsoVolume { get; set; }
        public int ReembolsoNr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PedidosPendentesVolume { get; set; }
        public int PedidosPendentesNr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PedidosAprovadosVolume { get; set; }
        public int PedidosAprovadosNr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal JurosRecebidosVolume { get; set; }
        public int JurosRecebidosNr { get; set; }

        // Carteira Vigente
        [Column(TypeName = "decimal(18,2)")]
        public decimal VigenteVolume { get; set; }
        public int VigenteNr { get; set; }

        // Carteira em Mora por Faixas de Atraso
        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraMenor7Volume { get; set; }
        public int MoraMenor7Nr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraMenor15Volume { get; set; }
        public int MoraMenor15Nr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraMenor30Volume { get; set; }
        public int MoraMenor30Nr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraMenor60Volume { get; set; }
        public int MoraMenor60Nr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraMenor90Volume { get; set; }
        public int MoraMenor90Nr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraMaior90Volume { get; set; }
        public int MoraMaior90Nr { get; set; }
    }
}