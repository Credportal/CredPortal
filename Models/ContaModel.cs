using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class ContaModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do banco é obrigatório.")]
        [StringLength(100)]
        public string Banco { get; set; }

        [Required(ErrorMessage = "O número da conta é obrigatório.")]
        [StringLength(50)]
        public string Conta { get; set; }

        [Required(ErrorMessage = "O NIB é obrigatório.")]
        [StringLength(34)]
        public string NIB { get; set; }

        [Required(ErrorMessage = "O titular da conta é obrigatório.")]
        [StringLength(100)]
        public string Titular { get; set; }

        // --- AUDITORIA ---
        public int? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }

        // Tratamento da data idêntico ao EntidadeModel
        public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.Now;
        // --- NOVOS CAMPOS PARA CONTROLO DE DEPÓSITOS E SALDO ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal Credito { get; set; } = 0; // Total depositado / entradas

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debito { get; set; } = 0;  // Total levantado / saídas / aplicações

        [NotMapped] // Ou configurado no Fluent API como computed column
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo => Credito - Debito; // Calcula automaticamente em tempo de execução
    }
}