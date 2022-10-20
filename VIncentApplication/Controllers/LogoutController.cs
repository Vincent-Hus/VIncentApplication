using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace VIncentApplication.Controllers
{
    public class LogoutController : Controller
    {
        public ActionResult Logout()
        {
            Session["UserID"] = null;
            return RedirectToAction("Index","Art");
        }
    }
}