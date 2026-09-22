using System.ComponentModel.DataAnnotations;

namespace OrcamentoPessoal.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Digite seu CPF", AllowEmptyStrings = false)]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Digite sua Senha", AllowEmptyStrings = false)]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;
    }
}