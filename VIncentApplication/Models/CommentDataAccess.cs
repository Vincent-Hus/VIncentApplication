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
        private readonly Util _util = new Util();
        /// <summary>
        /// 取得該文章留言資料列表
        /// </summary>
        /// <param ArtID="ArtID">文章代號</param>
        /// <returns></returns>
        public IEnumerable<Comment> GetCommentList(int artId)
        {
            string rediskey = $"GetCommentList_{artId}";
            var redis = RedisConnectorHelper.Connection.GetDatabase();


            if (redis.StringGet(rediskey).IsNullOrEmpty)
            {
                IEnumerable<Comment> commentdb = SelectCommentListDB(artId);
                redis.StringSet(rediskey, JsonConvert.SerializeObject(commentdb), TimeSpan.FromSeconds(60));
                return commentdb;
            }
            else
            {
                return JsonConvert.DeserializeObject<IEnumerable<Comment>>(redis.StringGet(rediskey));
            }

        }

        /// <summary>
        /// 新增留言
        /// </summary>

        public string CreateComment(Comment comment)
        {
            try
            {
                string rediskey = $"GetCommentList_{comment.ArtID}";
                var redis = RedisConnectorHelper.Connection.GetDatabase();
                InsertCommentDB(comment);
                redis.KeyDelete(rediskey);
                return "新增成功";
            }
            catch (Exception ex)
            {
                _util.DeBug(ex.Message);
                return "新增失敗";
            }
        }

        /// <summary>
        /// 刪除留言
        /// </summary>

        public string DeleteComment(int commentId)
        {

            Comment comment = GetComment(commentId);
            if (!_util.IsCorrectUser(comment.UserID))
            {
                return "錯誤使用者";
            }

            try
            {
                string rediskey = $"GetCommentList_{comment.ArtID}";
                var redis = RedisConnectorHelper.Connection.GetDatabase();
                DeleteCommentDB(commentId);
                redis.KeyDelete(rediskey);
                return "刪除成功";
            }
            catch (Exception ex)
            {
                _util.DeBug(ex.Message);
                return "刪除失敗";
            }

        }
        /// <summary>
        /// 修改留言
        /// </summary>

        public string UpdateComment(Comment comment)
        {
            Comment commentdb = GetComment(comment.CommentID);
            if (!_util.IsCorrectUser(commentdb.UserID))
            {
                return "錯誤使用者";
            }

            try
            {
                string rediskey = $"GetCommentList_{commentdb.ArtID}";
                var redis = RedisConnectorHelper.Connection.GetDatabase();
                UpdateCommentDB(comment);
                redis.KeyDelete(rediskey);
                return "修改成功";
            }
            catch (Exception ex)
            {
                _util.DeBug(ex.Message);
                return "修改失敗";
            }

        }

        /// <summary>
        /// DB取單筆留言內容
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        private Comment GetComment(int commentId)
        {
            string strsql = @" select [CommentID],[CommentContent] ,[CreateTime],[UserID],[UpdateTime],[ArtID] from [Comment] ";
            strsql += " where [CommentID] = @CommentID ";
            strsql += " and [VisibleStatus] = 1";
            using (var conn = _util.GetSqlConnection())
            {
                Comment result = conn.Query<Comment>(strsql, new
                {
                    CommentID = commentId
                }).SingleOrDefault();
                return result;
            }
        }
        /// <summary>
        /// DB取連言列表
        /// </summary>
        /// <param name="artId"></param>
        /// <returns>IEnumerable<Comment></returns>
        private IEnumerable<Comment> SelectCommentListDB(int artId)
        {
            string strsql = @" select [CommentID],[CommentContent] ,[CreateTime],[UserID],[UpdateTime] from [Comment] ";
            strsql += " where [ArtID] = @ArtID ";
            strsql += " and [VisibleStatus] = 1";
            strsql += " Order by [CreateTime] asc";
            using (var conn = _util.GetSqlConnection())
            {
                var result = conn.Query<Comment>(strsql,
                    new
                    {
                        ArtID = artId
                    });
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
            string strsql = @" Insert into [Comment] ([ArtID],[CommentContent],[CreateTime],[UserID]) values (@ArtID, @CommentContent, @CreateTime,@UserID ) ";

            using (var conn = _util.GetSqlConnection())
            {
                comment.UserID = HttpContext.Current.Session["UserID"].ToString();
                comment.CreateTime = DateTime.Now;
                conn.Execute(strsql, comment);

            }
        }
        /// <summary>
        /// 刪除DB留言
        /// </summary>
        /// <param name="commentId"></param>
        private void DeleteCommentDB(int commentId)
        {
            string strsql = @" Update [Comment] Set [VisibleStatus] = 0 where [CommentID] = @CommentID ";
            using (var conn = _util.GetSqlConnection())
            {
                conn.Execute(strsql, new
                {
                    CommentID = commentId
                });
            }
        }
        /// <summary>
        /// 修改DB留言
        /// </summary>
        /// <param name="comment"></param>
        private void UpdateCommentDB(Comment comment)
        {
            string strsql = @" Update [Comment] Set [CommentContent] = @CommentContent , [UpdateTime] = @UpdateTime where [CommentID] = @CommentID ";
            using (var conn = _util.GetSqlConnection())
            {
                comment.UpdateTime = DateTime.Now;
                conn.Execute(strsql,comment);

                //conn.Execute(strsql, new Comment
                //{
                //    CommentID = comment.CommentID,
                //    CommentContent = comment.CommentContent,
                //    UpdateTime = DateTime.Now
                //});
            }
        }
    }
}