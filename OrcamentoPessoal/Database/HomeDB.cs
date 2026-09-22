using Microsoft.Ajax.Utilities;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Database
{
    public class HomeDB
    {
        public static bool ValidaLogin(UserLogin login, out string errorMsg)
        {
            errorMsg = string.Empty;

            Usuario bancoTeste = new Usuario
            {
                Nome = "Administrador",
                Ativo = true,
                CPF = "12345678909",
                SenhaUser = "senha123"
            };
            //string sqlCommand = (@"
            //    SELECT USR_NM, USR_ATV
            //    FROM usr
            //    WHERE USR_ATV = 1 
            //    AND USR_CPF = @USR_CPF 
            //    AND USR_SN = @USR_SN");
            //sqlCommand.Parameters.AddWithValue("@USR_CPF", login.Usuario);
            //sqlCommand.Parameters.AddWithValue("@USR_SN", login.Senha);

            if (login.Senha == bancoTeste.SenhaUser && login.Usuario == bancoTeste.CPF)
                return true;
            else
            {
                errorMsg = "Crendenciais incorretas!";
                return false;
            }

            //var row = DBMySQL.ConsultaObjeto(sqlCommand, out errorMsg);

            //if (row == null || !string.IsNullOrEmpty(errorMsg))
            //    return null;

        }
    }
}