using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("FechosDiariosContas")]
    public class FechoDiarioConta
    {
        [Key]
        public int Id { get; set; }

        public int FechoDiarioRegistoId { get; set; }
        [ForeignKey("FechoDiarioRegistoId")]
        public virtual FechoDiarioRegisto FechoRegisto { get; set; }

        public string Banco { get; set; }
        public string Conta { get; set; }
        public string Titular { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoAbertura { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Creditos { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debitos { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoFecho { get; set; }
    }
}