using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace web_sport_gear.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Products()
        {
            return View();
        }
        public ActionResult Cart()
        {
             return View();
        }
        public ActionResult Checkout()
        {
             return View();
        }
        public ActionResult AoTheThao()
        {
            return View();
        }
        public ActionResult GiayDaBong()
        {
            return View();
        }

        public ActionResult PhuKien()
        {
            return View();
        }
        public ActionResult Details(int id)
        {
            ViewBag.ProductId = id;
            return View();
        }
    }
}