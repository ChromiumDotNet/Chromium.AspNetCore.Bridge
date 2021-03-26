using CefSharp.AspNetCore.Mvc.Example.Wpf.Models;
using Microsoft.AspNetCore.Mvc;

namespace CefSharp.AspNetCore.Mvc.Example.Wpf.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var model = new HomeViewModel
            {
                Text = "Welcome",
                Method = "GET"
            };
            return View("Index", model);
        }

        [HttpPost]
        public ActionResult Index(HomeBindingModel inputModel)
        {
            var model = new HomeViewModel
            {
                Text = string.Format("Input from Form Post: {0} - {1}", inputModel.Input1, inputModel.Input2),
                Method = "POST"
            };
            return View("Index", model);
        }
    }
}
