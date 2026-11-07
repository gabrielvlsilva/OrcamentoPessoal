using System;
using System.Globalization;
using System.Security.Cryptography;

namespace OrcamentoPessoal.Security
{
    public static class PasswordHasher
    {
        private const string Algoritmo = "PBKDF2-SHA256";
        private const int Iteracoes = 600000;
        private const int TamanhoSalt = 16;
        private const int TamanhoHash = 32;
        private const int TamanhoMaximoSenha = 1024;

        public static string GerarHash(string senha)
        {
            if (string.IsNullOrEmpty(senha) || senha.Length > TamanhoMaximoSenha)
            {
                throw new ArgumentException("A senha deve conter entre 1 e 1024 caracteres.", "senha");
            }

            byte[] salt = new byte[TamanhoSalt];

            using (var gerador = RandomNumberGenerator.Create())
            {
                gerador.GetBytes(salt);
            }

            byte[] hash = null;

            try
            {
                hash = DerivarHash(senha, salt, Iteracoes);

                return Algoritmo + "$" + Iteracoes.ToString(CultureInfo.InvariantCulture) + "$" +
                    Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
            finally
            {
                Array.Clear(salt, 0, salt.Length);

                if (hash != null)
                {
                    Array.Clear(hash, 0, hash.Length);
                }
            }
        }

        public static bool VerificarSenha(string senha, string hashArmazenado)
        {
            if (string.IsNullOrEmpty(senha) || senha.Length > TamanhoMaximoSenha || string.IsNullOrWhiteSpace(hashArmazenado))
            {
                return false;
            }

            string[] partes = hashArmazenado.Split('$');

            if (partes.Length != 4 || partes[0] != Algoritmo)
            {
                return false;
            }

            int iteracoesArmazenadas;

            if (!int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out iteracoesArmazenadas) ||
                iteracoesArmazenadas != Iteracoes)
            {
                return false;
            }

            byte[] salt = null;
            byte[] hashEsperado = null;
            byte[] hashCalculado = null;

            try
            {
                salt = Convert.FromBase64String(partes[2]);
                hashEsperado = Convert.FromBase64String(partes[3]);

                if (salt.Length != TamanhoSalt || hashEsperado.Length != TamanhoHash)
                {
                    return false;
                }

                hashCalculado = DerivarHash(senha, salt, iteracoesArmazenadas);

                return ComparacaoConstante(hashEsperado, hashCalculado);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
            finally
            {
                if (salt != null)
                {
                    Array.Clear(salt, 0, salt.Length);
                }

                if (hashEsperado != null)
                {
                    Array.Clear(hashEsperado, 0, hashEsperado.Length);
                }

                if (hashCalculado != null)
                {
                    Array.Clear(hashCalculado, 0, hashCalculado.Length);
                }
            }
        }

        private static byte[] DerivarHash(string senha, byte[] salt, int iteracoes)
        {
#if NETFRAMEWORK
            using (var derivador = new Rfc2898DeriveBytes(senha, salt, iteracoes, HashAlgorithmName.SHA256))
            {
                return derivador.GetBytes(TamanhoHash);
            }
#else
            return Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
#endif
        }

        private static bool ComparacaoConstante(byte[] valorEsperado, byte[] valorCalculado)
        {
            if (valorEsperado == null || valorCalculado == null || valorEsperado.Length != valorCalculado.Length)
            {
                return false;
            }

            int diferenca = 0;

            for (int indice = 0; indice < valorEsperado.Length; indice++)
            {
                diferenca |= valorEsperado[indice] ^ valorCalculado[indice];
            }

            return diferenca == 0;
        }
    }
}
