using WebView2.AspNetCore.Mvc.Example.Wpf.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace WebView2.AspNetCore.Mvc.Example.Wpf
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
            
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("hi")))
            {
                HttpContext.Session.SetString("hi", "Hello world");
            }

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
            var sessionhi = HttpContext.Session.GetString("hi");
            return View("Index", model);
        }

        public IActionResult Privacy()
        {
            var sessionhi = HttpContext.Session.GetString("hi");
            return View();
        }

        public IActionResult RedirectToPrivacy()
        {
            return RedirectToAction(nameof(Privacy));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = context.Error; // Your exception

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
