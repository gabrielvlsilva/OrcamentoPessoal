using System.Web.Mvc;
using OrcamentoPessoal.Database;
using OrcamentoPessoal.Models;

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
            return View("Index", HomeDB.ObterDadosDashboard(Session["UsuarioLogado"] as Usuario));
        }

        public ActionResult ImportadorIA()
        {
            ViewBag.ActiveMenu = "ImportadorIA";
            return View("Index", HomeDB.ObterDadosDashboard(Session["UsuarioLogado"] as Usuario));
        }

        public ActionResult Categorias()
        {
            ViewBag.ActiveMenu = "Categorias";
            return View("Index", HomeDB.ObterDadosDashboard(Session["UsuarioLogado"] as Usuario));
        }

        public ActionResult Relatorios()
        {
            ViewBag.ActiveMenu = "Relatorios";
            return View("Index", HomeDB.ObterDadosDashboard(Session["UsuarioLogado"] as Usuario));
        }
    }
}
