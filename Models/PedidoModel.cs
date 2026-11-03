using Analise.Enuns;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class PedidoModel
    {
        // --- CHAVE PRIMÁRIA ---
        [Key]
        public int Id { get; set; }

        // --- DADOS FINANCEIROS DO PEDIDO ---
        [Column(TypeName = "bigint")] // Ou deixar que o EF mapeie automaticamente para bigint
        public long? CRR { get; set; }

        [Required(ErrorMessage = "Informe a entidade associada.")]
        public int EntidadeId { get; set; }

        [ForeignKey("EntidadeId")]
        public virtual EntidadeModel Entidade { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "O valor da solicitação é obrigatório.")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "O prazo é obrigatório.")]
        public int Prazo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "A taxa é obrigatória.")]
        public decimal Taxa { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Prestacao { get; set; }

        // --- DADOS DE DECISÃO / APROVAÇÃO (Preenchidos futuramente) ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ValorDecisao { get; set; }

        public int? PrazoDecisao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TaxaDecisao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrestacaoDecisao { get; set; }

        // --- GESTÃO E DESTINO ---
        public int? GestorId { get; set; }

        [ForeignKey("GestorId")]
        public virtual UsuarioModel Gestor { get; set; }

        [StringLength(320, ErrorMessage = "O campo destino deve ter no máximo 320 caracteres.")]
        public string Destino { get; set; }

        public int EstadoId { get; set; }

        public int AgenciaId { get; set; }

        // --- AUDITORIA ---
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public int? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }

        // --- PROPRIEDADES NÃO MAPEADAS (AUXILIARES PARA VIEWS) ---
        [NotMapped]
        public List<SelectListItem> ListaEntidades { get; set; }

        [NotMapped]
        public List<SelectListItem> ListaUsuarios { get; set; }

        [NotMapped]
        public List<SelectListItem> ListaGestores { get; set; }
               
        /// <summary>
        /// Valor total das garantias atuais registradas para a Entidade Principal (Cliente).
        /// Preenchido pelo controlador antes de enviar para a View.
        /// </summary>
        [NotMapped]
        public decimal TotalGarantiasCliente { get; set; }

        /// <summary>
        /// Valor total das garantias atuais registradas para todos os Intervenientes associados.
        /// Preenchido pelo controlador antes de enviar para a View.
        /// </summary>
        [NotMapped]
        public decimal TotalGarantiasIntervenientes { get; set; }

        /// <summary>
        /// Somatório das garantias do Cliente e dos Intervenientes.
        /// </summary>
        [NotMapped]
        public decimal SomaTotalGarantias => TotalGarantiasCliente + TotalGarantiasIntervenientes;

        /// <summary>
        /// Calcula o Limite Máximo de Aprovação garantindo que as garantias totais cobrem 150% do valor.
        /// Fórmula: TotalGarantias / 1.5
        /// </summary>
        [NotMapped]
        public decimal LimiteMaximoAprovacao => SomaTotalGarantias > 0 ? SomaTotalGarantias / 1.5m : 0;

        // --- DADOS DE DESEMBOLSO E EXECUÇÃO ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorDesembolsado { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CapitalPago { get; set; } = 0;

        /// <summary>
        /// Capital Vigente calculado em tempo de execução: Valor Desembolsado - Capital Pago.
        /// </summary>
        [NotMapped]
        public decimal CapitalVigente => ValorDesembolsado - CapitalPago;

        public DateTimeOffset? DataDesembolso { get; set; }
    }
}