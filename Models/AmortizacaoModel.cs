using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("Amortizacoes")]
    public class AmortizacaoModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }

        [ForeignKey("PedidoId")]
        public virtual PedidoModel Pedido { get; set; }

        public long? CRR { get; set; }

        [Required]
        public int NR { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Vencimento { get; set; }

        // Componentes da Prestação
        [Column(TypeName = "decimal(18,2)")]
        public decimal Prestacao { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Principal { get; set; } = 0; // Amortização do capital

        [Column(TypeName = "decimal(18,2)")]
        public decimal Juro { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoDevedor { get; set; } = 0; // Capital em dívida após a prestação

        // Taxa Diária de Multa por Atraso (Ex: 0.0002 = 0.02% ao dia)
        [Column(TypeName = "decimal(18,4)")]
        public decimal TaxaMultaDia { get; set; } = 0.0002m;

        // Histórico de Valores Pagos
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPago { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalPago { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal JuroPago { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MultaPago { get; set; } = 0;

        // Valores a Perdoar
        [Column(TypeName = "decimal(18,2)")]
        public decimal JuroAPerdoar { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MultaAPerdoar { get; set; } = 0;

        // --- PROPRIEDADES CALCULADAS DIGNAS DE LÓGICA DE NEGÓCIO ---

        // 1. Dias de atraso (calculado dinamicamente caso a data atual ultrapasse o vencimento)
        [NotMapped]
        public int DiasAtraso => DateTime.Today > Vencimento ? (DateTime.Today - Vencimento).Days : 0;

        // 2. Componentes por pagar da prestação (sem considerar multas)
        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalPorPagar => Math.Max(0, Principal - PrincipalPago);

        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        public decimal JuroPorPagar => Math.Max(0, Juro - JuroPago - JuroAPerdoar);

        // 3. Valor em falta da prestação sem incluir multas
        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorPrestacaoEmFaltaSemMultas => PrincipalPorPagar + JuroPorPagar;

        // 4. Multa calculada: Dias de atraso * Taxa * Valor em falta da prestação sem multas
        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Multa => DiasAtraso > 0 ? (DiasAtraso * TaxaMultaDia * ValorPrestacaoEmFaltaSemMultas) : 0m;

        // 5. Multa líquida restante por pagar
        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MultaPorPagar => Math.Max(0, Multa - MultaPago - MultaAPerdoar);

        // 6. Total acumulado por pagar
        [NotMapped]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPorPagar => PrincipalPorPagar + JuroPorPagar + MultaPorPagar;
        public DateTime? DataPagamento { get; set; }
    }
}