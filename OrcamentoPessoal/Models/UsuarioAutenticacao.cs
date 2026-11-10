namespace OrcamentoPessoal.Models
{
    public class UsuarioAutenticacao
    {
        public uint Id { get; set; }

        public string CPF { get; set; }

        public string Nome { get; set; }

        public string SenhaHash { get; set; }
    }
}
