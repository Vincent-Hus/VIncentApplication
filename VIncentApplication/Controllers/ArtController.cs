using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using VIncentApplication.Models;
using static VIncentApplication.Models.Util;

namespace VIncentApplication.Controllers
{
    public class ArtController : Controller
    {
        public ActionResult Edit(int ArtID)
        {
            ArtDataAccess da = new ArtDataAccess();
            Art result = da.GetArt(ArtID);
            return View(result);
        }
        [HttpPost]
        public  ActionResult Edit(Art art) 
        {
            ArtDataAccess da = new ArtDataAccess();
            string message = da.UpdateArt(art);
            TempData["UpdateResult"] = message;
            
            return RedirectToAction("Edit",art);
        }

        public ActionResult Index()
        {
            ArtDataAccess da = new ArtDataAccess();
            ArtView result = new ArtView();
            result.Arts = da.GetArtList("").ToList();
            return View(result);
        }
        [HttpPost]
        public ActionResult Index(ArtView PostData)
        {
            ArtDataAccess da = new ArtDataAccess();
            PostData.Arts = da.GetArtList(PostData.KeyWord).ToList();
            return View(PostData);
        }

        public ActionResult Details(int? ArtID)
        {
            if (ArtID.HasValue)
            {
                int HasArtID = Convert.ToInt32(ArtID);
                ArtDataAccess ArtData = new ArtDataAccess();
                CommentDataAccess commentData = new CommentDataAccess();
                Art res = ArtData.GetArt(HasArtID);
                res.Comment = commentData.GetCommentList(HasArtID).ToList();
                res.ClicksNumber = ArtData.GetClicksNumber(HasArtID);
                res.Like = ArtData.UserLikeThis(HasArtID);
                res.LikeNumber = ArtData.GetArtLikeNumber(HasArtID);
                return View(res);
            }
            return RedirectToAction("Index", "Art");
        }

        [HttpPost]
        public ActionResult Like(int ArtID)
        {
            if (string.IsNullOrEmpty(HttpContext.Session["UserID"].ToString()))
            {
                return RedirectToAction("Login", "Login");
            }
            else
            {
                ArtDataAccess ArtData = new ArtDataAccess();
                ArtData.LikeClick(ArtID);
                return RedirectToAction("Details", "Art");
            }
        }

        [HttpPost]
        public ActionResult Delete(int ArtID)
        {
             ArtDataAccess da = new ArtDataAccess();
             da.DeleteArt(ArtID);
            return RedirectToAction("Index","Art");
            
        }


        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(Art art)
        {
            
            ArtDataAccess da = new ArtDataAccess();
            string message = da.CreateArt(art);
            TempData["CreateResult"] = message;
            return RedirectToAction("Index");
        }
    }
}