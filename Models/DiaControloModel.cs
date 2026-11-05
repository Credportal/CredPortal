using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    [Table("DiasControlo")] // Ajuste o nome da tabela se necessário para coincidir com a sua BD
    public class DiaControloModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DataReferencia { get; set; }

        [Required]
        public bool EstaAberto { get; set; } = true;

        public DateTime? DataAbertura { get; set; }
        public int? UsuarioAberturaId { get; set; }

        public DateTime? DataFecho { get; set; }
        public int? UsuarioFechoId { get; set; }
    }
}