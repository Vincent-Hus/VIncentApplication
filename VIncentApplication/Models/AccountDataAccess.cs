using Dapper;
using Konscious.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace VIncentApplication.Models
{
    public class AccountDataAccess
    {
        /// <summary>
        /// 判斷帳號是否重複
        /// </summary>
        /// <param name="UserID"></param>
        /// <returns>bool</returns>
        private bool HaveSameAccount(string UserID)
        {
            string strSQL = " Select 1 from [ApplicationUser] Where [UserID] = @UserID";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                int result = conn.Query<int>(strSQL, new
                {
                    UserID = UserID
                }).SingleOrDefault();
                return result > 0;
            }
        }
        /// <summary>
        /// 註冊帳號
        /// </summary>
        /// <param name="register"></param>
        /// <returns>型別:string 內容:結果訊息</returns>
        public string RegisterAccount(Register register)
        {
            if (this.HaveSameAccount(register.UserID)) 
            { 
                return "使用者重複"; 
            }

            byte[]  salt = this.CreateSalt();
            string saltStr = Convert.ToBase64String(salt);
            string hashPassword = this.HashPassword(register.Password, salt);

            try
            {
                InsertUserDB(register, hashPassword, saltStr);
                return "註冊成功";
            }
            catch (Exception ex)
            {
                Util util = new Util();
                util.DeBug(ex.Message);
                return ex.Message;
            }
        }
        /// <summary>
        /// 判斷是否登入成功
        /// </summary>
        /// <param name="login"></param>
        /// <returns>bool</returns>
        public bool IsLoginSuccess(Login login)
        {

            if (HaveSameAccount(login.UserID))
            {
                try
                {
                    byte[] salt = GetSalt(login.UserID);
                    string hashPassword = HashPassword(login.Password, salt);

                    return HasUserInDB(login, hashPassword);
                }
                catch (Exception ex)
                {
                    Util util = new Util();
                    util.DeBug(ex.Message);
                    return false;
                }
            }
            return false;

        }

        /// <summary>
        /// 取得該帳號的鹽
        /// </summary>
        /// <param name="UserID"></param>
        /// <returns>byte[]</returns>
        private byte[] GetSalt(string UserID)
        {
            string strSQL = "Select [Salt] from [ApplicationUser] Where [UserID] = @UserID";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                
                Login result = conn.QuerySingle<Login>(strSQL, new
                {
                    UserID = UserID
                });
                return Convert.FromBase64String(result.Salt);
            }

        }
        // Argon2 加密
        //產生 Salt 功能
        private byte[] CreateSalt()
        {
            var buffer = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buffer);
            return buffer;
        }
        // Hash 處理加鹽的密碼功能
        private string HashPassword(string password, byte[] salt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

            //底下這些數字會影響運算時間，而且驗證時要用一樣的值
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8; // 4 核心就設成 8
            argon2.Iterations = 4; // 迭代運算次數
            argon2.MemorySize = 1024 * 1024; // 1 GB

            return Convert.ToBase64String(argon2.GetBytes(16));
        }
        private void InsertUserDB(Register register ,string hashPassword ,string saltStr)
        {
            string strSQL = "Insert into [ApplicationUser] ";
            strSQL += " ([UserID],[Password],[Salt],[Email],[CreateTime]) ";
            strSQL += " values ";
            strSQL += " (@UserID,@Password,@Salt,@Email,@CreateTime) ";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {
                conn.Execute(strSQL, new
                {
                    UserID = register.UserID,
                    Password = hashPassword,
                    Salt = saltStr,
                    Email = register.Email,
                    CreateTime = DateTime.Now
                });
            }
            
        }
        private bool HasUserInDB(Login login, string hashPassword)
        {
            string strSQL = "Select 1 From [ApplicationUser] Where UserID = @UserID and Password = @Password ";
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString))
            {

                int result = conn.Query<int>(strSQL, new
                {
                    UserID = login.UserID,
                    Password = hashPassword
                }).SingleOrDefault();
                return result > 0;
            }
        } 
    }
}