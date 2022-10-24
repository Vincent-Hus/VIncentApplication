using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using VIncentApplication.Models;

namespace VIncentApplication.Controllers
{
    public class CommentController : Controller
    {
        [HttpPost]
        public ActionResult Create(Comment comment)
        {
            if (ModelState.IsValid)
            {
                CommentDataAccess da = new CommentDataAccess();
                string message = da.CreateComment(comment);
                return Json(message);
            }
            else
            {

                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
        }
        [HttpPost]
        public ActionResult Delete(Comment comment)
        {
            CommentDataAccess da = new CommentDataAccess();
            Util util = new Util();
            if (!util.IsCorrectUser(comment.UserID))
            {
                string message = da.DeleteComment(comment.CommentID);
                return Json(message);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }
        [HttpPost]
        public ActionResult Edit(Comment comment)
        {
            if (ModelState.IsValid)
            {
                CommentDataAccess da = new CommentDataAccess();
                string message = da.UpdateComment(comment);
                return Json(message);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
    }
}