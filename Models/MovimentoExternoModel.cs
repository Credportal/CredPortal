using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class MovimentoExternoModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O valor do crédito deve ser positivo.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Credito { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O valor do débito deve ser positivo.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Debito { get; set; }

        // Conta associada à operação
        [Required(ErrorMessage = "A conta é obrigatória.")]
        public int ContaId { get; set; }
        public ContaModel Conta { get; set; }

        // Adicionado: Associação com a Entidade
        public int? EntidadeId { get; set; }
        public EntidadeModel Entidade { get; set; }

        // Adicionado: Associação com o Pedido
        public long? Crr { get; set; }
        public PedidoModel Pedido { get; set; }

        // Tipo de Operação (ex: "Levantamento", "Pagamento", "Taxa")
        public string TipoOperacao { get; set; }

        public int UsuarioId { get; set; }
        public int? AgenciaId { get; set; }
        [ForeignKey("AgenciaId")]
        public virtual AgenciaModel Agencia { get; set; }
        public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.Now;
    }
}