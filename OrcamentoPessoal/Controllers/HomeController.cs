using System;
using System.Web.Mvc;
using OrcamentoPessoal.Database;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Controllers
{
    public class HomeController : Controller
    {
        // GET: / ou /Home/Index (Dashboard principal com menu lateral responsivo)
        public ActionResult Index()
        {
            var usuario = Session["UsuarioLogado"] as Usuario;
            var dashboardData = HomeDB.ObterDadosDashboard(usuario);
            return View(dashboardData);
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

            var model = new UserLogin
            {
                // Sugere credenciais mock para facilitar a validação inicial do usuário
                Usuario = "123.456.789-09"
            };

            return View(model);
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

            bool valido = HomeDB.ValidaLogin(login, out string errorMsg, out Usuario usuario);

            if (!valido || usuario == null)
            {
                ViewBag.Message = string.IsNullOrEmpty(errorMsg) ? "CPF ou Senha inválidos!" : errorMsg;
                return View(login);
            }

            if (!usuario.Ativo)
            {
                ViewBag.Message = "Este usuário encontra-se desativado.";
                return View(login);
            }

            // Armazena sessão do usuário mockado
            Session["UsuarioLogado"] = usuario;

            return RedirectToAction("Index");
        }

        // GET: /Home/Logout
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
            var transacoes = HomeDB.ObterTransacoesMock();
            return View("Index", HomeDB.ObterDadosDashboard());
        }

        public ActionResult ImportadorIA()
        {
            ViewBag.ActiveMenu = "ImportadorIA";
            return View("Index", HomeDB.ObterDadosDashboard());
        }

        public ActionResult Categorias()
        {
            ViewBag.ActiveMenu = "Categorias";
            return View("Index", HomeDB.ObterDadosDashboard());
        }

        public ActionResult Relatorios()
        {
            ViewBag.ActiveMenu = "Relatorios";
            return View("Index", HomeDB.ObterDadosDashboard());
        }
    }
}
