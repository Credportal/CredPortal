using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("Intervenientes")]
    public class IntervenienteModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "A entidade principal do pedido é obrigatória.")]
        [Display(Name = "Entidade ID (Pedido)")]
        public int EntidadeId { get; set; }

        [ForeignKey("EntidadeId")]
        public virtual EntidadeModel Entidade { get; set; }

        [Display(Name = "Código CRR")]
        public long? Crr { get; set; }

        [Required(ErrorMessage = "O interveniente é obrigatório.")]
        [Display(Name = "Interveniente")]
        public int IntervenienteID { get; set; }

        [ForeignKey("IntervenienteID")]
        public virtual EntidadeModel IntervenienteEntidade { get; set; }

        [NotMapped]
        [Display(Name = "NUIT do Interveniente")]
        public string NuitInterveniente => IntervenienteEntidade?.Nuit; // Obtido diretamente da entidade associada

        [Required(ErrorMessage = "O tipo de interveniente é obrigatório.")]
        [Display(Name = "Tipo de Interveniente")]
        public string Tipo { get; set; } // Co-Devedor, Avalista, etc.

        // --- CAMPOS APENAS PARA VISUALIZAÇÃO (SOMATÓRIO DE GARANTIAS) ---
        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Valor Aquisição (MT)")]
        public decimal ValorCompraGarantia { get; set; }

        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Valor Atual (MT)")]
        public decimal ValorActualGarantia { get; set; }

        // --- CAMPOS DE CONTROLO E AUDITORIA ---
        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        [Display(Name = "Utilizador ID")]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }

        [Display(Name = "Agência ID")]
        public int AgenciaId { get; set; }
    }
}