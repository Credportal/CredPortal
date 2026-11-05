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

        [Column(TypeName = "decimal(18,2)")]
        public decimal DesembolsoMontante { get; set; }
        public int QtdNovos { get; set; }
        public int QtdRep { get; set; }

        public int VigenteQtd { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal VigenteCapital { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReembolsoCapital { get; set; }
        public int ReembolsoQtd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraHojeCapital { get; set; }

        public int MoraTotalQtd { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraTotalCapital { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MoraTotalPercentual { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Dias7Capital { get; set; }
        public int Dias7Qtd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Dias15Capital { get; set; }
        public int Dias15Qtd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Dias30Capital { get; set; }
        public int Dias30Qtd { get; set; }
    }
}