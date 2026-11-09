using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Globalization;
using System.Reflection;

namespace OrcamentoPessoal.Database
{
    public class ConexaoBD
    {
        private const string NomeConnectionString = "OrcamentoPessoal";

        public static T ConsultaObjeto<T>(string sql, IDictionary<string, object> parametros, out string errorMsg)
            where T : class, new()
        {
            errorMsg = string.Empty;
            var resultados = ExecutarConsulta<T>(sql, parametros, true, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg) || resultados == null || resultados.Count == 0)
            {
                return null;
            }

            return resultados[0];
        }

        public static List<T> Consulta<T>(string sql, IDictionary<string, object> parametros, out string errorMsg)
            where T : class, new()
        {
            return ExecutarConsulta<T>(sql, parametros, false, out errorMsg);
        }

        private static List<T> ExecutarConsulta<T>(string sql, IDictionary<string, object> parametros, bool apenasUmRegistro, out string errorMsg)
            where T : class, new()
        {
            errorMsg = string.Empty;
            var resultados = new List<T>();

            if (string.IsNullOrWhiteSpace(sql))
            {
                errorMsg = "O comando SQL não foi informado.";
                return null;
            }

            var propriedades = ObterPropriedadesMapeaveis<T>();

            if (propriedades.Count == 0)
            {
                errorMsg = "O tipo informado não possui propriedades públicas graváveis para o mapeamento.";
                return null;
            }

            DbConnection conexao;

            if (!AbrirConexao(out conexao, out errorMsg))
            {
                return null;
            }

            try
            {
                using (conexao)
                using (var comando = conexao.CreateCommand())
                {
                    comando.CommandText = sql;
                    AdicionarParametros(comando, parametros);

                    using (var leitor = comando.ExecuteReader())
                    {
                        while (leitor.Read())
                        {
                            resultados.Add(MapearRegistro<T>(leitor, propriedades));

                            if (apenasUmRegistro)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = "Não foi possível executar a consulta: " + ex.Message;
                return null;
            }

            return resultados;
        }

        public static void ExecutarComando(string sql, IDictionary<string, object> parametros, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (string.IsNullOrWhiteSpace(sql))
            {
                errorMsg = "O comando SQL não foi informado.";
                return;
            }

            DbConnection conexao;

            if (!AbrirConexao(out conexao, out errorMsg))
            {
                return;
            }

            try
            {
                using (conexao)
                using (var comando = conexao.CreateCommand())
                {
                    comando.CommandText = sql;
                    AdicionarParametros(comando, parametros);
                    comando.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                errorMsg = "Não foi possível executar o comando: " + ex.Message;
            }
        }

        public static void ExecutarComandosEmTransacao(IList<ComandoSqlBD> comandos, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (comandos == null || comandos.Count == 0)
            {
                errorMsg = "Nenhum comando foi informado para a transação.";
                return;
            }

            foreach (var item in comandos)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Sql))
                {
                    errorMsg = "A transação contém um comando SQL inválido.";
                    return;
                }
            }

            DbConnection conexao;
            if (!AbrirConexao(out conexao, out errorMsg))
            {
                return;
            }

            using (conexao)
            using (var transacao = conexao.BeginTransaction())
            {
                try
                {
                    foreach (var item in comandos)
                    {
                        using (var comando = conexao.CreateCommand())
                        {
                            comando.Transaction = transacao;
                            comando.CommandText = item.Sql;
                            AdicionarParametros(comando, item.Parametros);
                            comando.ExecuteNonQuery();
                        }
                    }

                    transacao.Commit();
                }
                catch (Exception ex)
                {
                    try
                    {
                        transacao.Rollback();
                    }
                    catch (Exception)
                    {
                    }

                    errorMsg = "Não foi possível concluir a transação no banco de dados: " + ex.Message;
                }
            }
        }

        private static Dictionary<string, PropertyInfo> ObterPropriedadesMapeaveis<T>()
        {
            var propriedades = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var propriedade in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propriedade.CanWrite && propriedade.GetSetMethod() != null)
                {
                    propriedades[propriedade.Name] = propriedade;
                }
            }

            return propriedades;
        }

        private static T MapearRegistro<T>(DbDataReader leitor, Dictionary<string, PropertyInfo> propriedades)
            where T : class, new()
        {
            var resultado = new T();

            for (int indice = 0; indice < leitor.FieldCount; indice++)
            {
                PropertyInfo propriedade;

                if (!propriedades.TryGetValue(leitor.GetName(indice), out propriedade))
                {
                    continue;
                }

                object valor = leitor.GetValue(indice);
                var tipoPropriedade = Nullable.GetUnderlyingType(propriedade.PropertyType) ?? propriedade.PropertyType;

                if (valor == DBNull.Value)
                {
                    if (propriedade.PropertyType.IsValueType && Nullable.GetUnderlyingType(propriedade.PropertyType) == null)
                    {
                        throw new InvalidOperationException("A coluna '" + leitor.GetName(indice) + "' retornou NULL para uma propriedade não anulável.");
                    }

                    propriedade.SetValue(resultado, null, null);
                    continue;
                }

                if (tipoPropriedade.IsEnum)
                {
                    valor = valor is string
                        ? Enum.Parse(tipoPropriedade, (string)valor, true)
                        : Enum.ToObject(tipoPropriedade, valor);
                }
                else if (!tipoPropriedade.IsInstanceOfType(valor))
                {
                    valor = Convert.ChangeType(valor, tipoPropriedade, CultureInfo.InvariantCulture);
                }

                propriedade.SetValue(resultado, valor, null);
            }

            return resultado;
        }

        private static void AdicionarParametros(DbCommand comando, IDictionary<string, object> parametros)
        {
            if (parametros == null)
            {
                return;
            }

            var nomesUtilizados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in parametros)
            {
                string nome = item.Key == null ? string.Empty : item.Key.Trim();

                if (string.IsNullOrWhiteSpace(nome))
                {
                    throw new ArgumentException("Os parâmetros SQL precisam ter um nome válido.");
                }

                if (!nome.StartsWith("@", StringComparison.Ordinal))
                {
                    nome = "@" + nome;
                }

                if (!nomesUtilizados.Add(nome))
                {
                    throw new ArgumentException("O parâmetro '" + nome + "' foi informado mais de uma vez.");
                }

                var parametro = comando.CreateParameter();
                parametro.ParameterName = nome;
                parametro.Value = item.Value ?? DBNull.Value;
                comando.Parameters.Add(parametro);
            }
        }

        public static bool AbrirConexao(out DbConnection conexao, out string errorMsg)
        {
            conexao = null;
            errorMsg = string.Empty;

            try
            {
                var configuracao = ConfigurationManager.ConnectionStrings[NomeConnectionString];

                if (configuracao == null || string.IsNullOrWhiteSpace(configuracao.ConnectionString))
                {
                    errorMsg = "A connection string '" + NomeConnectionString + "' não foi configurada.";
                    return false;
                }

                var provider = DbProviderFactories.GetFactory(configuracao.ProviderName);
                conexao = provider.CreateConnection();

                if (conexao == null)
                {
                    errorMsg = "Não foi possível criar a conexão com o provider configurado.";
                    return false;
                }

                conexao.ConnectionString = configuracao.ConnectionString;
                conexao.Open();

                return true;
            }
            catch (Exception ex)
            {
                if (conexao != null)
                {
                    conexao.Dispose();
                    conexao = null;
                }

                errorMsg = "Não foi possível abrir a conexão com o banco de dados: " + ex.Message;
                return false;
            }
        }

        public static bool FecharConexao(DbConnection conexao, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (conexao == null)
            {
                return true;
            }

            try
            {
                conexao.Close();
                conexao.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                conexao.Dispose();
                errorMsg = "Não foi possível fechar a conexão com o banco de dados: " + ex.Message;
                return false;
            }
        }
    }

    public class ComandoSqlBD
    {
        public string Sql { get; set; }
        public IDictionary<string, object> Parametros { get; set; }
    }
}
