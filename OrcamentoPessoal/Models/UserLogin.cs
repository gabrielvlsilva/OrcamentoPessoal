using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OrcamentoPessoal.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Informe o seu CPF")]
        [Display(Name = "CPF")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a sua senha")]
        [StringLength(1024, MinimumLength = 1, ErrorMessage = "A senha deve ter no máximo 1024 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        /// <summary>
        /// Retorna apenas os dígitos do CPF, removendo pontuações
        /// </summary>
        public string ObterCpfLimpo()
        {
            if (string.IsNullOrWhiteSpace(Usuario))
                return string.Empty;

            return Regex.Replace(Usuario, @"[^\d]", "");
        }
    }
}
