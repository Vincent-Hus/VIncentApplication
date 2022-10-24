using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using VIncentApplication.Models;

namespace VIncentApplication.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(Login login)
        {
            if (ModelState.IsValid)
            {
                AccountDataAccess accountData = new AccountDataAccess();
                if (accountData.IsLoginSuccess(login))
                {
                    Session["UserID"] = login.UserID;
                    return RedirectToAction("Index", "Art");
                }
                else
                {
                    TempData["LoginResult"] = "帳號或密碼錯誤";
                    return RedirectToAction("Login", "Login");
                }
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            

        }

    }
}