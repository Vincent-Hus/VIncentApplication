using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using static VIncentApplication.Models.Util;

namespace VIncentApplication.Models
{
    public class ArtDataAccess
    {
        /// <summary>
        /// 取得文章資料列表
        /// </summary>
        public IEnumerable<Art> GetArtList(string KeyWord)
        {

            if (string.IsNullOrEmpty(KeyWord))
            {
                KeyWord = "ArtList";
            }

            var radis = RedisConnectorHelper.Connection.GetDatabase();
            if (radis.StringGet(KeyWord).IsNullOrEmpty)
            {
                IEnumerable<Art> Art = SelectArtListDB(KeyWord);
                radis.StringSet(KeyWord, JsonConvert.SerializeObject(Art), TimeSpan.FromSeconds(60));
                return Art;
            }
            else
            {
                return JsonConvert.DeserializeObject<IEnumerable<Art>>(radis.StringGet(KeyWord));
            };
        }
        /// <summary>
        /// 取得文章單筆資料
        /// </summary>
        public Art GetArt(int ArtID)
        {
            string strArtID = $"GetArt_{ArtID}";
            var radis = RedisConnectorHelper.Connection.GetDatabase();

            if (radis.StringGet(strArtID).IsNullOrEmpty)
            {
                Art art = SelectArtDb(ArtID);
                radis.StringSet(strArtID, JsonConvert.SerializeObject(art), TimeSpan.FromSeconds(60));
                return art;
               
            }
            else
            {
                return JsonConvert.DeserializeObject<Art>(radis.StringGet(strArtID));
            }
        }
        /// <summary>
        /// 取得文章點閱數
        /// </summary>
        public long GetClicksNumber(int ArtID)
        {
            string clicksKey = $"ArtClicks_{ArtID}"; //時間內點擊數 key值
            string TimeKey = $"ArtTime_{ArtID}";     //最後寫入資料庫時間 key值
            long clickNumbr;                         //時間內點擊數
            long DbClickNumber;                      //DB點擊數
            DateTime LastUpdateTime;                 //最後寫入資料庫時間
            var redis = RedisConnectorHelper.Connection.GetDatabase();

            //時間內點擊數 有值:將值寫入REDIS 無值:帶昨日時間(讓判斷進入DB取資料),寫入REDIS
            if (redis.StringGet(TimeKey).HasValue)
            {
                LastUpdateTime = Convert.ToDateTime(redis.StringGet(TimeKey));
            }
            else
            {
                LastUpdateTime = DateTime.Now.AddDays(-1);
                redis.StringSet(TimeKey, LastUpdateTime.ToString(), TimeSpan.FromSeconds(60));
            }

            //時間內若有點閱寫入REDIS ,時間內點擊數 +1後取值 ,超時將Redis暫存寫入資料庫並更新最後寫入時間 ,時間內點擊數 +1後取值
            if (LastUpdateTime.AddSeconds(10) >= DateTime.Now)
            {
                clickNumbr = redis.StringIncrement(clicksKey);
            }
            else
            {
                UpdateClicksNumber(ArtID, clicksKey);
                redis.StringSet(TimeKey, DateTime.Now.ToString());
                clickNumbr = redis.StringIncrement(clicksKey);
            }
            //取DB點閱數
            DbClickNumber = GetDbClickNumber(ArtID);


            return clickNumbr + DbClickNumber;
        }
        /// <summary>
        /// 取DB點閱數寫入REDIS，回傳Reids內快取
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns></returns>
        private long GetDbClickNumber(int ArtID)
        {

            string DbClicksKey = $"DbArtClicks_{ArtID}";//DB點擊數 KET值
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            if (redis.StringGet(DbClicksKey).IsNullOrEmpty)
            {
                long ClicksNumber = SelectArtClicksNumberDB(ArtID);
                 redis.StringSet(DbClicksKey, ClicksNumber);
                 return ClicksNumber;
            }
            else
            {
                return Convert.ToInt64(redis.StringGet(DbClicksKey));
            }
        }

        /// <summary>
        /// 更新點閱數進資料庫歸零redis
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns></returns>
        private bool UpdateClicksNumber(int ArtID, string clicksKey)
        {
            string DbClicksKey = $"DbArtClicks_{ArtID}";//DB點擊數 KET值
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            long ClicksNumber = Convert.ToInt64(redis.StringGet(clicksKey));//取redis時間內點擊數
            long DbClickNumber = GetDbClickNumber(ArtID);  //取DB點擊數
            if (redis.StringSet(clicksKey, 0))//歸零Redis時間內點擊數
            {
                try
                {
                    UpdateArtClicksNumberDB(ArtID, ClicksNumber, DbClickNumber); //時間內點擊數+DB點擊數 更新至DB
                    redis.StringSet(DbClicksKey, ClicksNumber + DbClickNumber);    //更新DB點閱數後將Redis的DB點閱數更新
                    return true;
                    
                }
                catch (Exception ex)
                {
                    Util util = new Util();
                    util.DeBug(ex.Message);
                    return false;
                }
            };
            return false;

        }

        /// <summary>
        /// 修改文章資料
        /// </summary>

        public string UpdateArt(Art art)
        {
            Util util = new Util();
            if (util.IsCorrectUser(art.UserID))
            {
                return "錯誤使用者";
            }

            try
            {
                UpdateArtDB(art);
                return "修改完成";
            }
            catch (Exception ex)
            {
                util.DeBug(ex.Message);
                return ex.Message;
            }
        }

        /// <summary>
        /// 新增文章資料列表
        /// </summary>

        public string CreateArt(Art art)
        {
            try
            {
                InsertArtDb(art);
                return "新增完成";
            }
            catch (Exception ex)
            {
                Util util = new Util();
                util.DeBug(ex.Message);
                return "新增失敗";
            }
        }

        /// <summary>
        /// 刪除文章資料
        /// </summary>

        public string DeleteArt(int ArtID)
        {
            Art art = this.GetArt(ArtID);
            Util util = new Util();
            if (util.IsCorrectUser(art.UserID))
            {
                return "錯誤使用者";
            }
            try
            {
                DeleteArtDB(ArtID);
                return "刪除完成";
            }
            catch (Exception ex)
            {
                util.DeBug(ex.Message);
                return "刪除失敗";
            }

        }

        /// <summary>
        /// 取DB文章列表
        /// </summary>
        /// <param name="KeyWord"></param>
        /// <returns> IEnumerable<Art></returns>
        private IEnumerable<Art> SelectArtListDB(string KeyWord)
        {
            string strSQL = @" select [ArtID],[Title], [ArtContent] ,[CreateTime],[UserID] from [Art] ";
            strSQL += " Where VisibleStatus = 1";

            if (KeyWord != "ArtList") //關鍵字
            {
                strSQL += " And [Title] like '%'+@Title+'%' ";
            }
            strSQL += "Order by [CreateTime] asc";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                var result = conn.Query<Art>(strSQL,
                    new
                    {
                        Title = KeyWord
                    });
                return result;
            }
        }
        /// <summary>
        /// 取DB單筆文章
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns>Art</returns>
        private Art SelectArtDb(int ArtID)
        {
            string strSQL = @" Select [ArtID],[Title], [ArtContent] ,[CreateTime],[UserID],[UpdateTime] from [Art] ";
            strSQL += " Where [VisibleStatus] = 1 ";
            strSQL += " And [ArtID] = @ArtID ";

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                Art result = conn.QuerySingleOrDefault<Art>(strSQL,
                    new
                    {
                        ArtID = ArtID
                    });
                return result;
            }
        }
        /// <summary>
        /// 取DB點閱數
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns></returns>
        private long SelectArtClicksNumberDB(int ArtID)
        {
            string strSQL = @" Select [ClicksNumber] from [Art] ";
            strSQL += " Where [VisibleStatus] = 1 ";
            strSQL += " And [ArtID] = @ArtID ";

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                Art result = conn.QuerySingleOrDefault<Art>(strSQL,
                    new
                    {
                        ArtID = ArtID
                    });

                return result.ClicksNumber;
            }
        }
        /// <summary>
        /// ClicksNumber + DbClickNumber 更新至DB
        /// </summary>
        /// <param name="ArtID"></param>
        /// <param name="ClicksNumber"></param>
        /// <param name="DbClickNumber"></param>
        private void UpdateArtClicksNumberDB(int ArtID,long ClicksNumber,long DbClickNumber)
        {
            string strSQL = @" Update [Art] set [ClicksNumber] =  @DBClicksNumber + @ClicksNumber ";
            strSQL += " Where [VisibleStatus] = 1 ";
            strSQL += " And [ArtID] = @ArtID ";
            
              using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
              {
                  var result = conn.Execute(strSQL,
                      new
                      {
                          ArtID = ArtID,
                          ClicksNumber = ClicksNumber,
                          DBClicksNumber = DbClickNumber
                      });
              }
        }
        /// <summary>
        /// 更新文章DB
        /// </summary>
        /// <param name="art"></param>
        private void UpdateArtDB(Art art)
        {
            string strSQL = @" Update [Art] set [Title] = @Title , [ArtContent] = @ArtContent , [UpdateTime] = @UpdateTime ";
            strSQL += " Where ArtID = @ArtID";

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                conn.Execute(strSQL, new
                {
                    ArtID = art.ArtID,
                    Title = art.Title,
                    ArtContent = art.ArtContent,
                    UpdateTime = DateTime.Now
                });
            }
            
        }
        /// <summary>
        /// 新增文章DB
        /// </summary>
        /// <param name="art"></param>
        private void InsertArtDb(Art art)
        {
            string strSQL = @" Insert into [Art] ([Title],[ArtContent],[CreateTime],[UserID]) value (@Title , @ArtContent, @CreateTime,@UserID ) ";

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                conn.Execute(strSQL, new
                {
                    Title = art.Title,
                    ArtContent = art.ArtContent,
                    CreateTime = DateTime.Now,
                    UserID = HttpContext.Current.Session["UserID"]
                });
            }
        }
        /// <summary>
        /// 刪除文章DB
        /// </summary>
        /// <param name="ArtID"></param>
        private void DeleteArtDB(int ArtID)
        {
            string strSQL = @" Update [Art] Set [VisibleStatus] = 0 where [ArtID] = @ArtID ";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                conn.Execute(strSQL, new
                {
                    ArtID = ArtID
                });
            }
        }
        /// <summary>
        /// session使用者是否喜歡此文章
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns></returns>
        public bool UserLikeThis(int ArtID)
        {
            string strSql = " Select 1 from [ArtLike] where [ArtID]=@ArtID and [UserID]=@UserID ";
            using(SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                int resultCount = conn.QuerySingleOrDefault<int>(strSql, new
                {
                    ArtID = ArtID,
                    UserID = HttpContext.Current.Session["UserID"]
                });
                return resultCount > 0;
            }
        }
        /// <summary>
        /// 取得點讚數有快取拿快取 沒快取拿DB
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns></returns>
        public int GetArtLikeNumber(int ArtID)
        {
            string ArtLikeKey = $"ArtLike_{ArtID}";
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            if (redis.StringGet(ArtLikeKey).HasValue)
            {
                return Convert.ToInt32(redis.StringGet(ArtLikeKey));
            }
            else
            {
                int DBLikes = SelectArtLikeNumberDB(ArtID);
                redis.StringSet(ArtLikeKey, DBLikes, TimeSpan.FromSeconds(60));
                return DBLikes;
            }

        }
        /// <summary>
        /// 抓DB文章點讚數
        /// </summary>
        /// <param name="ArtID"></param>
        /// <returns></returns>
        private int SelectArtLikeNumberDB(int ArtID)
        {
            string strSql = " Select Count(*) as LikeNumber from [ArtLike] where [ArtID]=@ArtID ";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                Art result = conn.QuerySingle<Art>(strSql, new
                {
                    ArtID = ArtID,
                    UserID = HttpContext.Current.Session["UserID"]
                });
                return result.LikeNumber;
            }
        }
        /// <summary>
        /// 已點讚刪除讚紀錄 沒點讚新增讚紀錄
        /// </summary>
        /// <param name="ArtID"></param>
        public void LikeClick(int ArtID)
        {
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            string ArtLikeKey = $"ArtLike_{ArtID}";
            try
            {
                if (UserLikeThis(ArtID))
                {
                    DeleteArtLikeDB(ArtID);
                    redis.StringDecrement(ArtLikeKey);
                }
                else
                {
                    InsertArtLikeDB(ArtID);
                    redis.StringIncrement(ArtLikeKey);
                }
            }
            catch (Exception ex)
            {

                Util util = new Util();
                util.DeBug(ex.Message);
            }

        }
        /// <summary>
        /// 新增點讚數至DB
        /// </summary>
        /// <param name="ArtID"></param>
        private void InsertArtLikeDB(int ArtID)
        {
            string strSql = " Insert into [ArtLike] ([UserID],[ArtID],[CreateTime]) values (@UserID,@ArtID,@CreateTime) ";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                Art result = conn.QuerySingleOrDefault<Art>(strSql, new
                {
                    ArtID = ArtID,
                    UserID = HttpContext.Current.Session["UserID"],
                    CreateTime = DateTime.Now
                });
            }
        }
        /// <summary>
        /// 刪除DB點讚數
        /// </summary>
        /// <param name="ArtID"></param>
        private void DeleteArtLikeDB(int ArtID)
        {
            string strSql = "Delete [ArtLike]  Where [ArtID] = @ArtID and [UserID] = @UserID ";
            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
                {
                    Art result = conn.QuerySingle<Art>(strSql, new
                    {
                        ArtID = ArtID,
                        UserID = HttpContext.Current.Session["UserID"],
                        Updatetime = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                Util util = new Util();
                util.DeBug(ex.Message);
            }

        }
    }
}