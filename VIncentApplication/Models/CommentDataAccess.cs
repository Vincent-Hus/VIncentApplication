using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using static VIncentApplication.Models.Util;

namespace VIncentApplication.Models
{
    public class CommentDataAccess
    {
        /// <summary>
        /// 取得該文章留言資料列表
        /// </summary>
        /// <param ArtID="ArtID">文章代號</param>
        /// <returns></returns>
        public IEnumerable<Comment> GetCommentList(int artID)
        {
            string redisKey = $"GetCommentList_{artID}";
            var redis = RedisConnectorHelper.Connection.GetDatabase();


            if (redis.StringGet(redisKey).IsNullOrEmpty)
            {
                IEnumerable<Comment> CommentDB = this.SelectCommentListDB(artID);
                redis.StringSet(redisKey, JsonConvert.SerializeObject(CommentDB), TimeSpan.FromSeconds(60));
                return CommentDB;
            }
            else
            {
                return JsonConvert.DeserializeObject<IEnumerable<Comment>>(redis.StringGet(redisKey));
            }

        }

        /// <summary>
        /// 新增留言
        /// </summary>

        public string CreateComment(Comment comment)
        {
            try
            {
                InsertCommentDB(comment);
                return "新增成功";
            }
            catch (Exception ex)
            {
                Util util = new Util();
                util.DeBug(ex.Message);
                return "新增失敗";
            }
        }

        /// <summary>
        /// 刪除留言
        /// </summary>

        public string DeleteComment(int commentID)
        {
            Comment comment = this.GetComment(commentID);
            Util util = new Util();
            if (util.IsCorrectUser(comment.UserID))
            {
                return "錯誤使用者";
            }

            try
            {
                this.DeleteCommentDB(commentID);
                return "刪除成功";
            }
            catch (Exception ex)
            {
                util.DeBug(ex.Message);
                return "刪除失敗";
            }

        }
        /// <summary>
        /// 修改留言
        /// </summary>

        public string UpdateComment(Comment comment)
        {
            Util util = new Util();
            if (util.IsCorrectUser(comment.UserID))
            {
                return "錯誤使用者";
            }

            try
            {
                UpdateCommentDB(comment);
                return "修改成功";
            }
            catch (Exception ex)
            {
                util.DeBug(ex.Message);
                return "修改失敗";
            }

        }




        /// <summary>
        /// DB取單筆留言內容
        /// </summary>
        /// <param name="commentID"></param>
        /// <returns></returns>
        private Comment GetComment(int commentID)
        {
            string strSQL = @" select [CommentID],[CommentContent] ,[CreateTime],[UserID],[UpdateTime] from [Comment] ";
            strSQL += " where [CommentID] = @CommentID ";
            strSQL += " and [VisibleStatus] = 1";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                Comment result = conn.Query<Comment>(strSQL, new
                {
                    CommentID = commentID
                }).SingleOrDefault();
                return result;
            }
        }
        /// <summary>
        /// DB取連言列表
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns>IEnumerable<Comment></returns>
        private IEnumerable<Comment> SelectCommentListDB(int ArtID)
        {
            string strSQL = @" select [CommentID],[CommentContent] ,[CreateTime],[UserID],[UpdateTime] from [Comment] ";
            strSQL += " where [ArtID] = @ArtID ";
            strSQL += " and [VisibleStatus] = 1";
            strSQL += " Order by [CreateTime] asc";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                var result = conn.Query<Comment>(strSQL,
                    new
                    {
                        ArtID = ArtID
                    });
                //redis.StringSet(redisKey, JsonConvert.SerializeObject(result), TimeSpan.FromSeconds(60));
                return result;
            }
        }
        /// <summary>
        /// 新增DB留言
        /// </summary>
        /// <param name="comment"></param>
        /// <returns>string 狀態</returns>
        private void InsertCommentDB(Comment comment)
        {
            string strSQL = @" Insert into [Comment] ([ArtID],[CommentContent],[CreateTime],[UserID]) values (@ArtID, @CommentContent, @CreateTime,@UserID ) ";

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                conn.Execute(strSQL, new
                {
                    ArtID = comment.ArtID,
                    CommentContent = comment.CommentContent,
                    CreateTime = DateTime.Now,
                    UserID = HttpContext.Current.Session["UserID"]
                });

            }
        }
        /// <summary>
        /// 刪除DB留言
        /// </summary>
        /// <param name="commentID"></param>
        private void DeleteCommentDB(int commentID)
        {
            string strSQL = @" Update [Comment] Set [VisibleStatus] = 0 where [CommentID] = @CommentID ";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                conn.Execute(strSQL, new
                {
                    CommentID = commentID
                });
            }
        }
        /// <summary>
        /// 修改DB留言
        /// </summary>
        /// <param name="comment"></param>
        private void UpdateCommentDB(Comment comment)
        {
            string strSQL = @" Update [Comment] Set [CommentContent] = @CommentContent , [UpdateTime] = @UpdateTime where [CommentID] = @CommentID ";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                conn.Execute(strSQL, new
                {
                    CommentID = comment.CommentID,
                    CommentContent = comment.CommentContent,
                    UpdateTime = DateTime.Now
                });
            }
        }
    }
}