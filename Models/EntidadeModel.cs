using Analise.Enuns;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class EntidadeModel
    {
        // --- CHAVE PRIMÁRIA ---
        [Key]
        public int Id { get; set; }

        // --- DADOS PESSOAIS E IDENTIFICAÇÃO ---
        [Required(ErrorMessage = "Digite o nome")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Digite o NUIT")]
        [StringLength(9, ErrorMessage = "O NUIT não pode exceder 9 caracteres")]
        public string Nuit { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Aniversario { get; set; } // Mapeado a partir de Aniversario / DataNascimento

        [StringLength(20)]
        public string Genero { get; set; }

        [StringLength(20)]
        public string EstadoCivil { get; set; }

        [StringLength(100)]
        public string Naturalidade { get; set; }

        [StringLength(20)]
        public string TipoCliente { get; set; }

        // --- DOCUMENTAÇÃO ---
        [StringLength(50)]
        public string Documento { get; set; } // Mapeado a partir de Documento / TipoDocumento

        [StringLength(30)]
        public string NrDocumento { get; set; }

        [StringLength(100)]
        public string LocalEmissao { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Emissao { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Validade { get; set; }

        // --- CONTATO E ENDEREÇO ---
        [Required(ErrorMessage = "Digite o contacto")]
        [StringLength(20)]
        public string Telefone { get; set; } // Mapeado a partir de Telefone / Contacto

        [StringLength(20)]
        public string Alternativo { get; set; }

        [Required(ErrorMessage = "Digite o email")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(50)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Digite o endereço")]
        [StringLength(255)]
        public string Endereco { get; set; }

        // --- DADOS PROFISSIONAIS ---
        [StringLength(20)]
        public string Profissao { get; set; }

        [StringLength(50)]
        public string Cargo { get; set; }

        [StringLength(100)]
        public string LocalTrabalho { get; set; }

        // --- FILIAÇÃO E CÔNJUGE ---
        [StringLength(100)]
        public string Pai { get; set; }

        [StringLength(100)]
        public string Mae { get; set; }

        [StringLength(100)]
        public string? Conjuge { get; set; }

        [StringLength(20)]
        public string? TelefoneConjuge { get; set; }

        // --- CHAVES ESTRANGEIRAS E AUDITORIA ---
        public int? AgenciaId { get; set; }

        public int? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }

        public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.Now;

        public bool Ativo { get; set; } = true;

        // --- PROPRIEDADES NÃO MAPEADAS (AUXILIARES PARA VIEWS) ---
        [NotMapped]
        public List<SelectListItem> ListaUsuarios { get; set; }
    }
}
