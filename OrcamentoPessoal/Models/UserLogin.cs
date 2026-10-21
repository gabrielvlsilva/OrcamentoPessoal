using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OrcamentoPessoal.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Informe o seu CPF ou Usuário")]
        [Display(Name = "CPF ou Usuário")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a sua senha")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Display(Name = "Lembrar meu acesso")]
        public bool LembrarMe { get; set; } = false;

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
