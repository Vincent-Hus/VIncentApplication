using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace VIncentApplication.Models
{
    public class Util
    {
        /// <summary>
        /// 確認是否為登入中的使用者
        /// </summary>
        public bool IsCorrectUser(string UserID)
        {
            if (string.IsNullOrWhiteSpace(UserID))
            {
                return false;
            }
            else
            {
                return HttpContext.Current.Session["UserID"].ToString() == UserID;
            }

        }

        public void DeBug(string Message)
        {
            if (Environment.UserInteractive)
            {
                System.Diagnostics.Debug.WriteLine(Message);
            }
        }

        /// <summary>
        /// redis快取
        /// </summary>
        public class RedisConnectorHelper
        {
            static RedisConnectorHelper()
            {
                RedisConnectorHelper._connection = new Lazy<ConnectionMultiplexer>(() =>
                {
                    return ConnectionMultiplexer.Connect("localhost");
                });
            }

            private static Lazy<ConnectionMultiplexer> _connection;

            public static ConnectionMultiplexer Connection
            {
                get
                {
                    return _connection.Value;
                }
            }

        }
        /// <summary>
        /// 資料庫連線
        /// </summary>
        /// <returns></returns>
        public SqlConnection GetSqlConnection()
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString);
            return conn;
        }

        
    }
}