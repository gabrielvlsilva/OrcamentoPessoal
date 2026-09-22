using System.Web.Mvc;
using OrcamentoPessoal.Database;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //teste
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(UserLogin login)
        {
            if (!ModelState.IsValid)
                return View(login); // Retorna validações do lado do cliente

            //Usuario usuario = HomeDB.ValidaLogin(login, out string errorMsg);
            bool valido = HomeDB.ValidaLogin(login, out string errorMsg);

            if (!string.IsNullOrEmpty(errorMsg) || valido == false)
            {
                ViewBag.Message = "CPF ou Senha inválidos!";
                return View(login);
            }

            // Se usuário não está ativo
            //if (!usuario.Ativo)
            //{
            //    ViewBag.Message = "Usuário desativado!";
            //    return View(login);
            //}

            // Criar cookie/session do usuário
            //Session["ORCPES"] = usuario;

            // Redirecionar para a página inicial
            return RedirectToAction("Inicio");
        }
    }
}