using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Services
{
    public class ServicoAnaliseArquivos
    {
        private static readonly HttpClient ClienteHttp = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(90)
        };

        public async Task<JObject> AnalisarAsync(byte[] conteudoArquivo, string extensao)
        {
            string url = ConfigurationManager.AppSettings["ServicoAnaliseArquivosUrl"];

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("O serviço de análise não está configurado.");
            }

            if (conteudoArquivo == null || conteudoArquivo.Length == 0)
            {
                throw new InvalidDataException("O conteúdo do arquivo não foi informado.");
            }

            using (var requisicao = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requisicao.Headers.TryAddWithoutValidation("X-File-Extension", extensao);
                string categorias = JsonConvert.SerializeObject(CatalogoCategorias.ObterCategorias().Select(categoria => new
                {
                    code = categoria.Codigo,
                    name = categoria.Nome,
                    type = categoria.Tipo
                }));
                requisicao.Headers.TryAddWithoutValidation("X-Category-Catalog", Convert.ToBase64String(Encoding.UTF8.GetBytes(categorias)));
                requisicao.Content = new ByteArrayContent(conteudoArquivo);
                requisicao.Content.Headers.ContentLength = conteudoArquivo.Length;

                using (var resposta = await ClienteHttp.SendAsync(requisicao))
                {
                    if (!resposta.IsSuccessStatusCode)
                    {
                        string conteudoErro = await resposta.Content.ReadAsStringAsync();
                        string mensagemErro = ObterMensagemErro(conteudoErro);
                        throw new ServicoAnaliseArquivosException(mensagemErro ?? "O serviço de análise não conseguiu processar o arquivo.");
                    }

                    string conteudo = await resposta.Content.ReadAsStringAsync();

                    try
                    {
                        return JObject.Parse(conteudo);
                    }
                    catch (JsonReaderException)
                    {
                        throw new InvalidOperationException("O serviço de análise retornou uma resposta inválida.");
                    }
                }
            }
        }

        private static string ObterMensagemErro(string conteudo)
        {
            try
            {
                var erro = JObject.Parse(conteudo);
                string detalhe = erro["detail"]?.ToObject<string>();

                if (!string.IsNullOrWhiteSpace(detalhe))
                {
                    detalhe = detalhe.Replace('\r', ' ').Replace('\n', ' ').Trim();
                    return detalhe.Length > 1000 ? detalhe.Substring(0, 1000) : detalhe;
                }
            }
            catch (JsonReaderException)
            {
            }

            return null;
        }
    }

    public class ServicoAnaliseArquivosException : Exception
    {
        public ServicoAnaliseArquivosException(string message) : base(message)
        {
        }
    }
}
