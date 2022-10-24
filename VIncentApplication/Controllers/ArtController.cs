using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using VIncentApplication.Models;
using static VIncentApplication.Models.Util;

namespace VIncentApplication.Controllers
{
    //TODO: 表單後臺驗證
    public class ArtController : Controller
    {
        public ActionResult Edit(int artid)
        {

            ArtDataAccess da = new ArtDataAccess();
            Art result = da.GetArt(artid);
            return View(result);
        }
        [HttpPost]
        public  ActionResult Edit(Art art) 
        {
            if (ModelState.IsValid)
            {
                ArtDataAccess da = new ArtDataAccess();
                string message = da.UpdateArt(art);
                TempData["UpdateResult"] = message;

                return RedirectToAction("Edit", art);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        public ActionResult Index()
        {
            ArtDataAccess da = new ArtDataAccess();
            ArtView result = new ArtView();
            result.Arts = da.GetArtList("").ToList();
            return View(result);
        }

        [HttpPost]
        public ActionResult Index(ArtView postData)
        {
            if (ModelState.IsValid)
            {
                ArtDataAccess da = new ArtDataAccess();
                postData.Arts = da.GetArtList(postData.KeyWord).ToList();
                return View(postData);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        public ActionResult Details(int? artId)
        {
            
            if (artId.HasValue)
            {
                int hasartid = Convert.ToInt32(artId);
                ArtDataAccess artdata = new ArtDataAccess();
                CommentDataAccess commentdata = new CommentDataAccess();
                Art res = artdata.GetArt(hasartid);
                res.Comment = commentdata.GetCommentList(hasartid).ToList();
                res.ClicksNumber = artdata.GetClicksNumber(hasartid);
                res.Like = artdata.UserLikeThis(hasartid);
                res.LikeNumber = artdata.GetArtLikeNumber(hasartid);
                return View(res);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
        }

        [HttpPost]
        public ActionResult Like(int artId)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(HttpContext.Session["UserID"].ToString()))
                {
                    return RedirectToAction("Login", "Login");
                }
                else
                {
                    ArtDataAccess artdata = new ArtDataAccess();
                    artdata.LikeClick(artId);
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [HttpPost]
        public ActionResult Delete(int artId)
        {
            if (ModelState.IsValid)
            {
                ArtDataAccess da = new ArtDataAccess();
                da.DeleteArt(artId);
                return RedirectToAction("Index", "Art");
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }


        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(Art art)
        {
            if (ModelState.IsValid)
            {
                ArtDataAccess da = new ArtDataAccess();
                string message = da.CreateArt(art);
                TempData["CreateResult"] = message;
                return RedirectToAction("Index");
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
    }
}