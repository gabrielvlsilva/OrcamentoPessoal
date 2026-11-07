using System.Collections.Generic;
using OrcamentoPessoal.Models;
using OrcamentoPessoal.Security;

namespace OrcamentoPessoal.Database
{
    public class UsuarioDB
    {
        public static bool Autenticar(UserLogin login, out Usuario usuario, out string errorMsg)
        {
            usuario = null;
            errorMsg = string.Empty;

            if (login == null || string.IsNullOrWhiteSpace(login.Usuario) || string.IsNullOrEmpty(login.Senha))
            {
                errorMsg = "CPF ou senha inválidos.";
                return false;
            }

            string cpf = login.ObterCpfLimpo();

            if (cpf.Length != 11)
            {
                errorMsg = "CPF ou senha inválidos.";
                return false;
            }

            string cpfFormatado = cpf.Substring(0, 3) + "." + cpf.Substring(3, 3) + "." +
                cpf.Substring(6, 3) + "-" + cpf.Substring(9, 2);
            var parametros = new Dictionary<string, object>
            {
                { "CPF", cpf },
                { "CPFFormatado", cpfFormatado }
            };

            const string sql = @"
                SELECT USR_AIC AS Id, USR_CFJ AS CPF, USR_PSW AS SenhaHash
                FROM usr
                WHERE USR_CFJ IN (@CPF, @CPFFormatado)
                ORDER BY USR_AIC
                LIMIT 2";

            string erroConsulta;
            var usuarios = ConexaoBD.Consulta<UsuarioAutenticacao>(sql, parametros, out erroConsulta);

            if (!string.IsNullOrEmpty(erroConsulta) || usuarios == null)
            {
                errorMsg = "Não foi possível validar suas credenciais no momento. Tente novamente.";
                return false;
            }

            if (usuarios.Count != 1 || !PasswordHasher.VerificarSenha(login.Senha, usuarios[0].SenhaHash))
            {
                errorMsg = "CPF ou senha inválidos.";
                return false;
            }

            usuario = new Usuario
            {
                Id = usuarios[0].Id,
                CPF = usuarios[0].CPF,
                Nome = "Usuário",
                Perfil = "Usuário",
                Ativo = true
            };

            return true;
        }
    }
}
