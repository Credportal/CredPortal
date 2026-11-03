using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("Garantias")]
    public class GarantiaModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O tipo de garantia é obrigatório.")]
        [Display(Name = "Tipo de Garantia")]
        public string Tipo { get; set; } // Penhor, Hipoteca, Outras

        [Required(ErrorMessage = "A descrição da garantia é obrigatória.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "O valor de aquisição é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor de Aquisição (MT)")]
        public decimal ValorAquisicao { get; set; }

        [Required(ErrorMessage = "O valor atual é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Valor Atual (MT)")]
        public decimal ValorActual { get; set; }

        [Display(Name = "Código CRR")]
        public long? Crr { get; set; }

        [Required(ErrorMessage = "A entidade é obrigatória.")]
        [Display(Name = "Entidade / Cliente")]
        public int EntidadeId { get; set; }

        [ForeignKey("EntidadeId")]
        public virtual EntidadeModel Entidade { get; set; }

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