using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using static VIncentApplication.Models.Util;
using System.Linq;

namespace VIncentApplication.Models
{
    //TODO: dapper簡化
    //TODO: REDIS資料修改後刪除key值
    
    public class ArtDataAccess
    {
        private readonly Util _util = new Util();
        /// <summary>
        /// 取得文章資料列表
        /// </summary>
        public IEnumerable<Art> GetArtList(string keyWord)
        {

            if (string.IsNullOrEmpty(keyWord))
            {
                keyWord = "ArtList";
            }

            var redis = RedisConnectorHelper.Connection.GetDatabase();
            
            if (redis.StringGet(keyWord).IsNullOrEmpty)
            {
                IEnumerable<Art> art = SelectArtListDB(keyWord);
                redis.StringSet(keyWord, JsonConvert.SerializeObject(art), TimeSpan.FromSeconds(60));
                   
                return art;
            }
            else
            {
                return JsonConvert.DeserializeObject<IEnumerable<Art>>(redis.StringGet(keyWord));
            };
        }
        /// <summary>
        /// 取得文章單筆資料
        /// </summary>
        public Art GetArt(int artId)
        {
            string strartid = $"GetArt_{artId}";
            var radis = RedisConnectorHelper.Connection.GetDatabase();
            if (radis.StringGet(strartid).IsNullOrEmpty)
            {
                Art art = SelectArtDb(artId);
                radis.StringSet(strartid, JsonConvert.SerializeObject(art), TimeSpan.FromSeconds(60));
                
                return art;
               
            }
            else
            {
                return JsonConvert.DeserializeObject<Art>(radis.StringGet(strartid));
            }
        }
        /// <summary>
        /// 取得文章點閱數
        /// </summary>
        public long GetClicksNumber(int artId)
        {
            string clickskey = $"ArtClicks_{artId}"; //時間內點擊數 key值
            string timekey = $"ArtTime_{artId}";     //最後寫入資料庫時間 key值
            long clicknumbr;                         //時間內點擊數
            long dbclicknumber;                      //DB點擊數
            DateTime lastupdatetime;                 //最後寫入資料庫時間
            var redis = RedisConnectorHelper.Connection.GetDatabase();

            //時間內點擊數 有值:將值寫入REDIS 無值:帶昨日時間(讓判斷進入DB取資料),寫入REDIS
            if (redis.StringGet(timekey).HasValue)
            {
                lastupdatetime = Convert.ToDateTime(redis.StringGet(timekey));
            }
            else
            {
                lastupdatetime = DateTime.Now.AddDays(-1);
                redis.StringSet(timekey, lastupdatetime.ToString(), TimeSpan.FromSeconds(60));
            }

            //時間內若有點閱寫入REDIS ,時間內點擊數 +1後取值 ,超時將Redis暫存寫入資料庫並更新最後寫入時間 ,時間內點擊數 +1後取值
            if (lastupdatetime.AddSeconds(10) >= DateTime.Now)
            {
                clicknumbr = redis.StringIncrement(clickskey);
            }
            else
            {
                UpdateClicksNumber(artId, clickskey);
                redis.StringSet(timekey, DateTime.Now.ToString());
                clicknumbr = redis.StringIncrement(clickskey);
            }
            //取DB點閱數
            dbclicknumber = GetDbClickNumber(artId);


            return clicknumbr + dbclicknumber;
        }
        /// <summary>
        /// 取DB點閱數寫入REDIS，回傳Reids內快取
        /// </summary>
        /// <param name="artId"></param>
        /// <returns></returns>
        private long GetDbClickNumber(int artId)
        {

            string dbclicksnumber = $"DbArtClicks_{artId}";//DB點擊數 KET值
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            if (redis.StringGet(dbclicksnumber).IsNullOrEmpty)
            {
                long clicksnumber = SelectArtClicksNumberDB(artId);
                 redis.StringSet(dbclicksnumber, clicksnumber);
                 return clicksnumber;
            }
            else
            {
                return Convert.ToInt64(redis.StringGet(dbclicksnumber));
            }
        }

        /// <summary>
        /// 更新點閱數進資料庫歸零redis
        /// </summary>
        /// <param name="artId"></param>
        /// <returns></returns>
        private bool UpdateClicksNumber(int artId, string clicksKey)
        {
            string dbclickskey = $"DbArtClicks_{artId}";//DB點擊數 KET值
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            long clicksnumber = Convert.ToInt64(redis.StringGet(clicksKey));//取redis時間內點擊數
            long dbclicknumber = GetDbClickNumber(artId);  //取DB點擊數
            if (redis.StringSet(clicksKey, 0))//歸零Redis時間內點擊數
            {
                try
                {
                    UpdateArtClicksNumberDB(artId, clicksnumber, dbclicknumber); //時間內點擊數+DB點擊數 更新至DB
                    redis.StringSet(dbclickskey, clicksnumber + dbclicknumber);    //更新DB點閱數後將Redis的DB點閱數更新
                    return true;
                    
                }
                catch (Exception ex)
                {
                    _util.DeBug(ex.Message);
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
            Art artDb = this.GetArt(art.ArtID);
            if (!_util.IsCorrectUser(artDb.UserID))
            {
                return "錯誤使用者";
            }

            try
            {
                string strartid = $"GetArt_{art.ArtID}";
                var redis = RedisConnectorHelper.Connection.GetDatabase();
                UpdateArtDB(art);
                redis.KeyDelete(strartid);
                return "修改完成";
            }
            catch (Exception ex)
            {
                _util.DeBug(ex.Message);
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
                string strartid = $"ArtList";
                var redis = RedisConnectorHelper.Connection.GetDatabase();
                InsertArtDb(art);
                redis.KeyDelete(strartid);
                return "新增完成";
            }
            catch (Exception ex)
            {
                _util.DeBug(ex.Message);
                return "新增失敗";
            }
        }

        /// <summary>
        /// 刪除文章資料
        /// </summary>

        public string DeleteArt(int artId)
        {
            Art art = this.GetArt(artId);
            if (!_util.IsCorrectUser(art.UserID))
            {
                return "錯誤使用者";
            }
            try
            {
                string strartid = $"ArtList";
                var redis = RedisConnectorHelper.Connection.GetDatabase();
                DeleteArtDB(artId);
                redis.KeyDelete(strartid);
                return "刪除完成";
            }
            catch (Exception ex)
            {
                _util.DeBug(ex.Message);
                return "刪除失敗";
            }

        }

        /// <summary>
        /// 取DB文章列表
        /// </summary>
        /// <param name="keyWord"></param>
        /// <returns> IEnumerable<Art></returns>
        private IEnumerable<Art> SelectArtListDB(string keyWord)
        {
            string strsql = @" select [ArtID],[Title], [ArtContent] ,[CreateTime],[UserID] from [Art] ";
            strsql += " Where VisibleStatus = 1";

            if (keyWord != "ArtList") //關鍵字
            {
                strsql += " And [Title] like '%'+@Title+'%' ";
            }
            strsql += "Order by [CreateTime] asc";
            using (var conn = _util.GetSqlConnection())
            {
                var result = conn.Query<Art>(strsql,
                    new
                    {
                        Title = keyWord
                    });
                return result;
            }
        }
        /// <summary>
        /// 取DB單筆文章
        /// </summary>
        /// <param name="artId"></param>
        /// <returns>Art</returns>
        private Art SelectArtDb(int artId)
        {
            string strsql = @" Select [ArtID],[Title], [ArtContent] ,[CreateTime],[UserID],[UpdateTime],(select count(*) from [ArtLike] l where l.[ArtID] = a.[ArtID]) from [Art] a ";
            strsql += " Where [VisibleStatus] = 1 ";
            strsql += " And [ArtID] = @ArtID ";

            using (var conn = _util.GetSqlConnection())
            {
                Art result = conn.QuerySingleOrDefault<Art>(strsql,new Art { ArtID = artId});
                return result;
            }
        }
        /// <summary>
        /// 取DB點閱數
        /// </summary>
        /// <param name="artId"></param>
        /// <returns></returns>
        private long SelectArtClicksNumberDB(int artId)
        {
            string strsql = @" Select [ClicksNumber] from [Art] ";
            strsql += " Where [VisibleStatus] = 1 ";
            strsql += " And [ArtID] = @ArtID ";

            using (var conn = _util.GetSqlConnection())
            {
                Art result = conn.QuerySingleOrDefault<Art>(strsql,
                    new
                    {
                        ArtID = artId
                    });

                return result.ClicksNumber;
            }
        }
        /// <summary>
        /// ClicksNumber + DbClickNumber 更新至DB
        /// </summary>
        /// <param name="artId"></param>
        /// <param name="clicksNumber"></param>
        /// <param name="dbClickNumber"></param>
        private void UpdateArtClicksNumberDB(int artId,long clicksNumber,long dbClickNumber)
        {
            string strSQL = @" Update [Art] set [ClicksNumber] =  @DBClicksNumber + @ClicksNumber ";
            strSQL += " Where [VisibleStatus] = 1 ";
            strSQL += " And [ArtID] = @ArtID ";
            
              using (var conn = _util.GetSqlConnection())
              {
                  var result = conn.Execute(strSQL,
                      new
                      {
                          ArtID = artId,
                          ClicksNumber = clicksNumber,
                          DBClicksNumber = dbClickNumber
                      });
              }
        }
        /// <summary>
        /// 更新文章DB
        /// </summary>
        /// <param name="art"></param>
        private void UpdateArtDB(Art art)
        {
            string strsql = @" Update [Art] set [Title] = @Title , [ArtContent] = @ArtContent , [UpdateTime] = @UpdateTime ";
            strsql += " Where ArtID = @ArtID";

            using (var conn = _util.GetSqlConnection())
            {
                art.UpdateTime = DateTime.Now;
                conn.Execute(strsql,art);
            }
            
        }
        /// <summary>
        /// 新增文章DB
        /// </summary>
        /// <param name="art"></param>
        private void InsertArtDb(Art art)
        {
            string strsql = @" Insert into [Art] ([Title],[ArtContent],[CreateTime],[UserID]) values (@Title , @ArtContent, @CreateTime,@UserID ) ";

            using (var conn = _util.GetSqlConnection())
            {
                art.UserID = HttpContext.Current.Session["UserID"].ToString();
                art.CreateTime = DateTime.Now;
                conn.Execute(strsql,art);
            }
        }



        /// <summary>
        /// 刪除文章DB
        /// </summary>
        /// <param name="artId"></param>
        private void DeleteArtDB(int artId)
        {
            string strsql = @" Update [Art] Set [VisibleStatus] = 0 where [ArtID] = @ArtID ";
            using (var conn = _util.GetSqlConnection())
            {
                conn.Execute(strsql, new
                {
                    ArtID = artId
                });
            }
        }
        /// <summary>
        /// session使用者是否喜歡此文章
        /// </summary>
        /// <param name="artId"></param>
        /// <returns></returns>
        public bool UserLikeThis(int artId)
        {
            string strSql = " Select 1 from [ArtLike] where [ArtID]=@ArtID and [UserID]=@UserID ";
            using(var conn = _util.GetSqlConnection())
            {
                int result = conn.QuerySingleOrDefault<int>(strSql, new
                {
                    ArtID = artId,
                    UserID = HttpContext.Current.Session["UserID"]
                });
                return result > 0;
            }
        }
        /// <summary>
        /// 取得點讚數有快取拿快取 沒快取拿DB
        /// </summary>
        /// <param name="artId"></param>
        /// <returns></returns>
        public int GetArtLikeNumber(int artId)
        {
            string artlikekey = $"ArtLike_{artId}";
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            if (redis.StringGet(artlikekey).HasValue)
            {
                return Convert.ToInt32(redis.StringGet(artlikekey));
            }
            else
            {
                int dblikes = SelectArtLikeNumberDB(artId);
                redis.StringSet(artlikekey, dblikes, TimeSpan.FromSeconds(60));
                return dblikes;
            }

        }
        /// <summary>
        /// 抓DB文章點讚數
        /// </summary>
        /// <param name="artId"></param>
        /// <returns></returns>
        private int SelectArtLikeNumberDB(int artId)
        {
            string strsql = " Select Count(*) as LikeNumber from [ArtLike] where [ArtID]=@ArtID ";
            using (var conn = _util.GetSqlConnection())
            {
                Art result = conn.QuerySingle<Art>(strsql, new
                {
                    ArtID = artId,
                    UserID = HttpContext.Current.Session["UserID"]
                });
                return result.LikeNumber;
            }
        }
        /// <summary>
        /// 已點讚刪除讚紀錄 沒點讚新增讚紀錄
        /// </summary>
        /// <param name="artId"></param>
        public void LikeClick(int artId)
        {
            Art artDb = GetArt(artId);
            var redis = RedisConnectorHelper.Connection.GetDatabase();
            string artlikekey = $"ArtLike_{artId}";
            try
            {
                if (UserLikeThis(artId))
                {
                    DeleteArtLikeDB(artDb);
                    redis.StringDecrement(artlikekey);
                }
                else
                {
                    InsertArtLikeDB(artDb);
                    redis.StringIncrement(artlikekey);
                }
            }
            catch (Exception ex)
            {

                _util.DeBug(ex.Message);
            }

        }
        /// <summary>
        /// 新增點讚數至DB
        /// </summary>
        /// <param name="artId"></param>
        private void InsertArtLikeDB(Art art)
        {
            string strsql = " Insert into [ArtLike] ([UserID],[ArtID],[CreateTime]) values (@UserID,@ArtID,@CreateTime) ";
            using (var conn = _util.GetSqlConnection())
            {
                art.UserID = HttpContext.Current.Session["UserID"].ToString();
                conn.Execute(strsql, art);
            }
        }
        /// <summary>
        /// 刪除DB點讚數
        /// </summary>
        /// <param name="artId"></param>
        private void DeleteArtLikeDB(Art art)
        {
            string strsql = "Delete [ArtLike]  Where [ArtID] = @ArtID and [UserID] = @UserID ";
            try
            {
                using (var conn = _util.GetSqlConnection())
                {
                    art.UserID = HttpContext.Current.Session["UserID"].ToString();
                    conn.Execute(strsql, art);
                }
            }
            catch (Exception ex)
            {
                _util.DeBug(ex.Message);
            }

        }
    }
}