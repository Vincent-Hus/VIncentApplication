using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VIncentApplication.Models;

namespace VIncentApplication.Controllers
{
    public class RegisterController: Controller
    {
        public ActionResult Register()
        {
            return View();
        }
        //註冊帳號
        [HttpPost]
        public ActionResult Create(Register register)
        {
            AccountDataAccess da = new AccountDataAccess();
            string result = da.RegisterAccount(register);
            TempData["RegisterResult"] = result;
            return RedirectToAction("Register", "Register");
        }
    }
}