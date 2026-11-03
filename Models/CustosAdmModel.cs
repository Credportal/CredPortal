using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class CustosAdmModel
    {
        [Key]
        public int Id { get; set; }

        // Pedido associado
        public int? PedidoId { get; set; }
        [ForeignKey("PedidoId")]
        public virtual PedidoModel Pedido { get; set; }

        // Entidade responsável / pagadora
        [Required(ErrorMessage = "A entidade é obrigatória.")]
        public int EntidadeId { get; set; }
        [ForeignKey("EntidadeId")]
        public virtual EntidadeModel Entidade { get; set; }

        // Código CRR
        [Column(TypeName = "bigint")]
        public long? CRR { get; set; }

        // Valor da comissão administrativa
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser superior a zero.")]
        public decimal Valor { get; set; }

        // Conta bancária associada
        [Required(ErrorMessage = "A conta bancária é obrigatória.")]
        public int ContaId { get; set; }
        [ForeignKey("ContaId")]
        public virtual ContaModel Conta { get; set; }

        // Auditoria e metadados (Seguindo o padrão de AgenciaId)
        public int? AgenciaId { get; set; }
        // Se tiver a classe AgenciaModel criada, pode descomentar a linha abaixo:
        // [ForeignKey("AgenciaId")]
        // public virtual AgenciaModel Agencia { get; set; }

        public int? UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }

        public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.Now;

        // Colunas auxiliares / não mapeadas para views
        [NotMapped]
        public string NomeEntidade { get; set; }
    }
}