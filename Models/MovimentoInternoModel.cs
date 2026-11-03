using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class MovimentoInternoModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; }

        // Substituição do campo Valor por Crédito e Débito
        [Range(0, double.MaxValue, ErrorMessage = "O valor do crédito deve ser positivo.")]
        public decimal Credito { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O valor do débito deve ser positivo.")]
        public decimal Debito { get; set; }

        public int? ContaOrigemId { get; set; }
        public ContaModel ContaOrigem { get; set; }

        public int? ContaDestinoId { get; set; }
        public ContaModel ContaDestino { get; set; }

        // Tipo de Operação: "Depósito" ou "Transferência"
        public string TipoOperacao { get; set; }

        public int UsuarioId { get; set; }
        public int? AgenciaId { get; set; }
        [ForeignKey("AgenciaId")]
        public virtual AgenciaModel Agencia { get; set; }
        public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.Now;
    }
}