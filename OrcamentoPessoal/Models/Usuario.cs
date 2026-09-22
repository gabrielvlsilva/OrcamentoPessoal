using System.ComponentModel.DataAnnotations;

namespace OrcamentoPessoal.Models
{
    public class Usuario
    {
        [Required(ErrorMessage = "Informe o Nome do Usuário", AllowEmptyStrings = false)]
        public string Nome { get; set; }

        [Display(Name = "Login (CPF)")]
        [Required(ErrorMessage = "Informe um CPF válido", AllowEmptyStrings = false)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "O CPF deve ter 11 dígitos")]
        public string CPF { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Senha do Usuário")]
        [StringLength(int.MaxValue, MinimumLength = 6, ErrorMessage = "Mínimo de 6 caracteres")]
        public string SenhaUser { get; set; }

        [Compare("SenhaUser", ErrorMessage = "As senhas digitadas não estão iguais")]
        public string ConfirmaSenha { get; set; }

        public bool Ativo { get; set; } = true;
    }
}