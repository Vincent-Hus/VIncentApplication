using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VIncentApplication.Models;

namespace VIncentApplication.Controllers
{
    public class CommentController : Controller
    {
        [HttpPost]
        public ActionResult Create(Comment comment)
        {
            CommentDataAccess da = new CommentDataAccess();
            string message = da.CreateComment(comment);
            return Json(message);
            
        }
        [HttpPost]
        public ActionResult Delete(int CommentID)
        {
            CommentDataAccess da = new CommentDataAccess();
            string message = da.DeleteComment(CommentID);
            return Json(message);
        }
        [HttpPost]
        public ActionResult Edit(Comment Comment)
        {
            CommentDataAccess da = new CommentDataAccess();
            string message = da.UpdateComment(Comment);
            return Json(message);
        }
    }
}