using System;
using System.ComponentModel.DataAnnotations;

namespace OrcamentoPessoal.Models
{
    public class Usuario
    {
        public uint Id { get; set; }

        [Required(ErrorMessage = "Informe o Nome do Usuário")]
        [Display(Name = "Nome Completo")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "CPF")]
        [Required(ErrorMessage = "Informe o CPF")]
        public string CPF { get; set; } = string.Empty;

        [Display(Name = "E-mail")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Telefone / Celular")]
        public string Telefone { get; set; } = string.Empty;

        [Display(Name = "Perfil")]
        public string Perfil { get; set; } = "Administrador";

        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public string AvatarUrl { get; set; } = string.Empty;

        public string PrimeiroNome
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Nome)) return string.IsNullOrWhiteSpace(CPF) ? "Conta" : CPF;
                var partes = Nome.Trim().Split(' ');
                return partes[0];
            }
        }

        public string Iniciais
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Nome)) return "OP";
                var partes = Nome.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 1) return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpper();
                return $"{partes[0][0]}{partes[partes.Length - 1][0]}".ToUpper();
            }
        }
    }
}
