using Analise.Enuns;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("Despesas")]
    public class DespesaModel
    {
        // --- CHAVE PRIMÁRIA ---
        [Key]
        public int Id { get; set; }

        // --- DADOS DA DESPESA ---
        [Required(ErrorMessage = "A descrição da despesa é obrigatória.")]
        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        public string Descricao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "O valor da despesa é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Valor { get; set; }

        // --- CONTA DA DESPESA ---
        [Required(ErrorMessage = "A conta é obrigatória.")]
        public int ContaId { get; set; }

        [ForeignKey("ContaId")]
        [ValidateNever] // Ignora a validação deste objeto de navegação no ModelState
        public virtual ContaModel Conta { get; set; }

        // --- AUDITORIA ---
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public int? UsuarioCadastroId { get; set; }

        [ForeignKey("UsuarioCadastroId")]
        [ValidateNever] // Ignora a validação deste objeto de navegação no ModelState
        public virtual UsuarioModel UsuarioCadastro { get; set; }

        // --- PROPRIEDADES NÃO MAPEADAS (AUXILIARES PARA VIEWS / DROPDOWNS) ---
        [NotMapped]
        [ValidateNever] // Ignora a validação da lista auxiliar
        public List<SelectListItem> ListaContas { get; set; }
    }
}