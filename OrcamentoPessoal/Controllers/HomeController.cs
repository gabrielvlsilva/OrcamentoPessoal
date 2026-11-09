using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using Newtonsoft.Json.Linq;
using OrcamentoPessoal.Database;
using OrcamentoPessoal.Models;
using OrcamentoPessoal.Services;

namespace OrcamentoPessoal.Controllers
{
    public class HomeController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var action = filterContext.ActionDescriptor.ActionName;

            if (action != "Login" && action != "Logout" && Session["UsuarioLogado"] == null)
            {
                filterContext.Result = RedirectToAction("Login");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // GET: / ou /Home/Index (Dashboard principal com menu lateral responsivo)
        public ActionResult Index()
        {
            var usuario = Session["UsuarioLogado"] as Usuario;
            var dashboardData = ObterDashboardAtual(usuario);
            ViewBag.UploadSucesso = TempData["UploadSucesso"];
            ViewBag.UploadErro = TempData["UploadErro"];
            return View(dashboardData);
        }

        public ActionResult About()
        {
            return View();
        }

        // POST: /Home/ImportarArquivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ImportarArquivo(HttpPostedFileBase arquivo)
        {
            var usuario = Session["UsuarioLogado"] as Usuario;
            var dashboard = ObterDashboardAtual(usuario);

            if (arquivo == null || arquivo.ContentLength <= 0)
            {
                ViewBag.UploadErro = "Selecione um arquivo válido para enviar.";
                return View("Index", dashboard);
            }

            if (arquivo.ContentLength > 10 * 1024 * 1024)
            {
                ViewBag.UploadErro = "O arquivo excede o limite de 10 MB.";
                return View("Index", dashboard);
            }

            string nomeArquivo = Path.GetFileName(arquivo.FileName ?? string.Empty);
            string extensao = Path.GetExtension(nomeArquivo).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(nomeArquivo) || nomeArquivo.Length > 255 ||
                (extensao != ".pdf" && extensao != ".csv" && extensao != ".xlsx"))
            {
                ViewBag.UploadErro = "Envie um arquivo PDF, CSV ou XLSX com um nome válido.";
                return View("Index", dashboard);
            }

            if (!ValidarAssinaturaArquivo(arquivo, extensao))
            {
                ViewBag.UploadErro = "A extensão do arquivo não corresponde ao seu conteúdo.";
                return View("Index", dashboard);
            }

            if (usuario == null || usuario.Id == 0)
            {
                return RedirectToAction("Login");
            }

            byte[] conteudoArquivo = new byte[arquivo.ContentLength];
            int totalLido = 0;
            try
            {
                while (totalLido < conteudoArquivo.Length)
                {
                    int bytesLidos = arquivo.InputStream.Read(conteudoArquivo, totalLido, conteudoArquivo.Length - totalLido);
                    if (bytesLidos == 0)
                    {
                        Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
                        ViewBag.UploadErro = "Não foi possível ler o arquivo completo em memória.";
                        return View("Index", dashboard);
                    }

                    totalLido += bytesLidos;
                }
            }
            catch (Exception)
            {
                Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
                ViewBag.UploadErro = "Não foi possível ler o arquivo em memória.";
                return View("Index", dashboard);
            }

            string errorMsg;
            string identificador = GerarIdentificadorArquivo(usuario.Id, conteudoArquivo);

            try
            {
                if (DocumentoDB.JaFoiImportado(usuario.Id, identificador, out errorMsg))
                {
                    Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
                    TempData["UploadSucesso"] = "Este arquivo já foi importado anteriormente. Nenhum lançamento duplicado foi criado.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception)
            {
                Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
                ViewBag.UploadErro = "Não foi possível verificar se este arquivo já foi importado.";
                return View("Index", dashboard);
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
                ViewBag.UploadErro = "Não foi possível verificar se este arquivo já foi importado.";
                return View("Index", dashboard);
            }

            try
            {
                DocumentoDB.Registrar(usuario.Id, nomeArquivo, extensao, arquivo.ContentLength, identificador, out errorMsg);
            }
            catch (Exception)
            {
                Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
                ViewBag.UploadErro = "Não foi possível registrar os metadados do arquivo.";
                return View("Index", dashboard);
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
                ViewBag.UploadErro = "Não foi possível registrar o arquivo no banco de dados.";
                return View("Index", dashboard);
            }

            bool analiseConcluida = false;
            try
            {
                var resultado = await new ServicoAnaliseArquivos().AnalisarAsync(conteudoArquivo, extensao);
                var analise = CriarPreviaAnalise(nomeArquivo, resultado);
                analiseConcluida = true;
                TransacaoDB.RegistrarImportacao(usuario.Id, identificador, analise.Transacoes, out errorMsg);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    DocumentoDB.AtualizarStatus(usuario.Id, identificador, "Falha na importação", out errorMsg);
                    TempData["UploadErro"] = "A análise foi concluída, mas os lançamentos não puderam ser salvos. Confira se a migração 1.2.0.0 foi aplicada e tente novamente.";
                    return RedirectToAction("Index");
                }

                TempData["UploadSucesso"] = analise.Transacoes.Count == 0
                    ? "Arquivo analisado. Nenhum lançamento foi identificado para importar."
                    : string.Format("Arquivo analisado. {0} lançamento(s) foram salvos.", analise.Transacoes.Count);

                if (analise.ResultadoTruncado)
                {
                    TempData["UploadSucesso"] += " A leitura foi limitada ao volume máximo configurado.";
                }

                return RedirectToAction("Index");
            }
            catch (ServicoAnaliseArquivosException erro)
            {
                DocumentoDB.AtualizarStatus(usuario.Id, identificador, "Falha na análise", out errorMsg);
                TempData["UploadErro"] = "A análise não foi concluída: " + erro.Message;
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                DocumentoDB.AtualizarStatus(usuario.Id, identificador, analiseConcluida ? "Falha na importação" : "Falha na análise", out errorMsg);
                TempData["UploadErro"] = analiseConcluida
                    ? "O arquivo foi analisado, mas não foi possível concluir sua importação. Verifique a conexão com o banco de dados."
                    : "A análise não foi concluída. Confira se o conteúdo é válido e se os serviços estão configurados.";
                return RedirectToAction("Index");
            }
            finally
            {
                Array.Clear(conteudoArquivo, 0, conteudoArquivo.Length);
            }
        }

        // GET: /Home/Inicio (Compatibilidade com redirecionamentos anteriores)
        public ActionResult Inicio()
        {
            return RedirectToAction("Index");
        }

        // GET: /Home/Login
        public ActionResult Login()
        {
            // Se já estiver logado, pode redirecionar para a home
            if (Session["UsuarioLogado"] != null)
            {
                return RedirectToAction("Index");
            }

            return View(new UserLogin());
        }

        // POST: /Home/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login)
        {
            if (!ModelState.IsValid)
            {
                return View(login);
            }

            bool valido = UsuarioDB.Autenticar(login, out Usuario usuario, out string errorMsg);

            if (!valido || usuario == null)
            {
                ViewBag.Message = string.IsNullOrEmpty(errorMsg) ? "CPF ou senha inválidos." : errorMsg;
                return View(login);
            }

            Session["UsuarioLogado"] = usuario;

            return RedirectToAction("Index");
        }

        // POST: /Home/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        // Seções auxiliares do menu lateral (preparadas para os próximos desenvolvimentos do aluno)
        public ActionResult Transacoes()
        {
            ViewBag.ActiveMenu = "Transacoes";
            ViewBag.PageTitle = "Revisar lançamentos importados";
            ViewBag.TransacaoSucesso = TempData["TransacaoSucesso"];
            ViewBag.TransacaoErro = TempData["TransacaoErro"];

            var usuario = Session["UsuarioLogado"] as Usuario;
            string errorMsg;
            var transacoes = TransacaoDB.ObterImportadas(usuario.Id, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                ViewBag.TransacaoErro = "Não foi possível carregar os lançamentos importados.";
            }

            return View(new RevisaoTransacoesViewModel
            {
                Transacoes = transacoes ?? new System.Collections.Generic.List<Transacao>(),
                Categorias = CatalogoCategorias.ObterCategorias()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AtualizarTransacaoImportada(EditarTransacaoImportadaViewModel edicao)
        {
            decimal valor;
            if (!ModelState.IsValid || !TryParseValor(edicao?.Valor, out valor) || valor <= 0 || valor > 9999999999999.99m)
            {
                TempData["TransacaoErro"] = "Confira a descrição, o valor e a data. Use vírgula para separar os centavos.";
                return RedirectToAction("Transacoes");
            }

            var usuario = Session["UsuarioLogado"] as Usuario;
            string errorMsg;
            TransacaoDB.AtualizarImportada(usuario.Id, edicao, valor, out errorMsg);

            TempData[string.IsNullOrEmpty(errorMsg) ? "TransacaoSucesso" : "TransacaoErro"] = string.IsNullOrEmpty(errorMsg)
                ? "Lançamento atualizado com sucesso."
                : "Não foi possível atualizar o lançamento. Confira os dados e tente novamente.";

            return RedirectToAction("Transacoes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExcluirTransacaoImportada(int id)
        {
            var usuario = Session["UsuarioLogado"] as Usuario;
            string errorMsg;
            TransacaoDB.ExcluirImportada(usuario.Id, id, out errorMsg);

            TempData[string.IsNullOrEmpty(errorMsg) ? "TransacaoSucesso" : "TransacaoErro"] = string.IsNullOrEmpty(errorMsg)
                ? "Lançamento excluído com sucesso."
                : "Não foi possível excluir o lançamento. Atualize a página e tente novamente.";

            return RedirectToAction("Transacoes");
        }

        public ActionResult ImportadorIA()
        {
            return Redirect(Url.Action("Index", "Home") + "#formImportarArquivo");
        }

        public ActionResult Categorias()
        {
            ViewBag.ActiveMenu = "Categorias";
            return View("Index", ObterDashboardAtual(Session["UsuarioLogado"] as Usuario));
        }

        public ActionResult Relatorios()
        {
            ViewBag.ActiveMenu = "Relatorios";
            ViewBag.PageTitle = "Relatórios & Planilhas";
            ViewBag.RelatorioErro = TempData["RelatorioErro"];

            var usuario = Session["UsuarioLogado"] as Usuario;
            DateTime hoje = DateTime.Today;
            int mes = hoje.Month;
            int ano = hoje.Year;
            bool periodoInformado = Request.QueryString["mes"] != null || Request.QueryString["ano"] != null;
            bool periodoValido = !periodoInformado ||
                (int.TryParse(Request.QueryString["mes"], out mes) && int.TryParse(Request.QueryString["ano"], out ano)
                    && mes >= 1 && mes <= 12 && ano >= 2000 && ano <= 2100);

            if (!periodoValido)
            {
                mes = hoje.Month;
                ano = hoje.Year;
            }

            string errorMsg;
            var relatorio = RelatorioDB.Obter(usuario.Id, ano, mes, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                relatorio.Erro = "Não foi possível carregar os dados do relatório. Confira a conexão com o banco de dados.";
            }
            else if (!periodoValido)
            {
                relatorio.Erro = "O período informado é inválido. Exibindo o mês atual.";
            }

            return View(relatorio);
        }

        public ActionResult ExportarRelatorio(int? mes, int? ano)
        {
            var usuario = Session["UsuarioLogado"] as Usuario;
            DateTime hoje = DateTime.Today;
            int mesReferencia = mes.HasValue && mes.Value >= 1 && mes.Value <= 12 ? mes.Value : hoje.Month;
            int anoReferencia = ano.HasValue && ano.Value >= 2000 && ano.Value <= 2100 ? ano.Value : hoje.Year;
            DateTime inicio = new DateTime(anoReferencia, mesReferencia, 1);
            string errorMsg;
            var lancamentos = RelatorioDB.ObterLancamentos(usuario.Id, inicio, inicio.AddMonths(1), null, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg) || lancamentos == null)
            {
                TempData["RelatorioErro"] = "Não foi possível exportar os dados do período. Tente novamente.";
                return RedirectToAction("Relatorios", new { mes = mesReferencia, ano = anoReferencia });
            }

            var conteudo = new StringBuilder();
            conteudo.AppendLine("Data;Tipo;Descrição;Categoria;Forma de pagamento;Valor;Arquivo de origem");

            foreach (var lancamento in lancamentos)
            {
                conteudo.Append(CampoCsv(lancamento.DataTransacao.HasValue ? lancamento.DataTransacao.Value.ToString("yyyy-MM-dd") : string.Empty)).Append(';')
                    .Append(CampoCsv(lancamento.Tipo.ToString())).Append(';')
                    .Append(CampoCsv(lancamento.Descricao)).Append(';')
                    .Append(CampoCsv(lancamento.CategoriaNome)).Append(';')
                    .Append(CampoCsv(lancamento.FormaPagamento)).Append(';')
                    .Append(CampoCsv(lancamento.Valor.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR")))).Append(';')
                    .Append(CampoCsv(lancamento.NomeArquivoOrigem)).AppendLine();
            }

            byte[] corpo = Encoding.UTF8.GetBytes(conteudo.ToString());
            byte[] preambulo = Encoding.UTF8.GetPreamble();
            byte[] arquivo = new byte[preambulo.Length + corpo.Length];
            Buffer.BlockCopy(preambulo, 0, arquivo, 0, preambulo.Length);
            Buffer.BlockCopy(corpo, 0, arquivo, preambulo.Length, corpo.Length);

            return File(arquivo, "text/csv", string.Format("relatorio-{0}-{1:00}.csv", anoReferencia, mesReferencia));
        }

        private DashboardViewModel ObterDashboardAtual(Usuario usuario)
        {
            string errorMsg;
            var dashboard = HomeDB.ObterDadosDashboard(usuario, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                ViewBag.DashboardErro = "Não foi possível carregar os dados atuais do banco de dados.";
            }

            return dashboard;
        }

        private static bool TryParseValor(string valorInformado, out decimal valor)
        {
            valor = 0;
            if (string.IsNullOrWhiteSpace(valorInformado))
            {
                return false;
            }

            string valorNormalizado = valorInformado.Trim();
            bool usaFormatoBrasileiro = valorNormalizado.LastIndexOf(',') > valorNormalizado.LastIndexOf('.');
            var cultura = usaFormatoBrasileiro
                ? CultureInfo.GetCultureInfo("pt-BR")
                : CultureInfo.InvariantCulture;

            return decimal.TryParse(valorNormalizado, NumberStyles.Number, cultura, out valor);
        }

        private static string CampoCsv(string valor)
        {
            valor = valor ?? string.Empty;
            string inicio = valor.TrimStart(' ', '\t', '\r', '\n');
            if (inicio.Length > 0 && "=+-@".IndexOf(inicio[0]) >= 0)
            {
                valor = "'" + valor;
            }

            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

        private static string GerarIdentificadorArquivo(uint usuarioId, byte[] conteudoArquivo)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashConteudo = sha256.ComputeHash(conteudoArquivo);
                byte[] identificadorBase = Encoding.UTF8.GetBytes(
                    usuarioId.ToString(CultureInfo.InvariantCulture) + ":" + Convert.ToBase64String(hashConteudo));
                byte[] hashIdentificador = sha256.ComputeHash(identificadorBase);
                return BitConverter.ToString(hashIdentificador, 0, 16).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static bool ValidarAssinaturaArquivo(HttpPostedFileBase arquivo, string extensao)
        {
            var fluxo = arquivo.InputStream;

            if (fluxo == null || !fluxo.CanRead || !fluxo.CanSeek)
            {
                return false;
            }

            long posicaoOriginal = fluxo.Position;
            var cabecalho = new byte[Math.Min(1024, arquivo.ContentLength)];
            int bytesLidos = fluxo.Read(cabecalho, 0, cabecalho.Length);
            fluxo.Position = posicaoOriginal;

            if (extensao == ".pdf")
            {
                string inicio = System.Text.Encoding.ASCII.GetString(cabecalho, 0, bytesLidos);
                return inicio.IndexOf("%PDF-", StringComparison.Ordinal) >= 0;
            }

            if (extensao == ".xlsx")
            {
                return bytesLidos >= 4 && cabecalho[0] == 0x50 && cabecalho[1] == 0x4B;
            }

            return extensao == ".csv" && bytesLidos > 0 && Array.IndexOf(cabecalho, (byte)0, 0, bytesLidos) < 0;
        }

        private static AnaliseArquivoViewModel CriarPreviaAnalise(string nomeArquivo, JObject resultado)
        {
            string formato = resultado["format"] == null ? string.Empty : resultado["format"].ToObject<string>();
            bool temTextoExtraivel = resultado["hasExtractableText"] == null || resultado["hasExtractableText"].ToObject<bool>();
            bool truncado = resultado["truncated"] != null && resultado["truncated"].ToObject<bool>();
            var transacoes = resultado["transactions"]?.ToObject<System.Collections.Generic.List<TransacaoSugeridaViewModel>>()
                ?? new System.Collections.Generic.List<TransacaoSugeridaViewModel>();
            var categorias = CatalogoCategorias.ObterCategorias();

            foreach (var transacao in transacoes)
            {
                var categoria = categorias.FirstOrDefault(item => item.Codigo == transacao.CategoriaCodigo);
                if (categoria == null || categoria.Tipo != transacao.Tipo || transacao.Valor <= 0 ||
                    transacao.Confianca < 0 || transacao.Confianca > 1 || string.IsNullOrWhiteSpace(transacao.Descricao))
                {
                    throw new InvalidOperationException("O serviço de análise retornou um lançamento inválido.");
                }

                transacao.CategoriaNome = categoria.Nome;
            }

            return new AnaliseArquivoViewModel
            {
                NomeArquivo = nomeArquivo,
                Formato = formato,
                Transacoes = transacoes,
                Avisos = resultado["warnings"]?.ToObject<System.Collections.Generic.List<string>>() ?? new System.Collections.Generic.List<string>(),
                ResultadoTruncado = truncado,
                PDFSemTextoExtraivel = formato == "PDF" && !temTextoExtraivel
            };
        }
    }
}
