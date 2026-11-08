using System.ComponentModel.DataAnnotations;

namespace WebAtividadeEntrevista.Models
{
    /// <summary>
    /// Classe de Modelo de Beneficiario
    /// </summary>
    public class BeneficiarioModel
    {
        public long Id { get; set; }

        /// <summary>
        /// CPF (999.999.999-99)
        /// </summary>
        [Required(ErrorMessage = "O CPF do beneficiário é obrigatório.")]
        public string CPF { get; set; }

        /// <summary>
        /// Nome
        /// </summary>
        [Required(ErrorMessage = "O nome do beneficiário é obrigatório.")]
        [MaxLength(50, ErrorMessage = "O nome do beneficiário deve ter no máximo 50 caracteres.")]
        public string Nome { get; set; }
    }
}
