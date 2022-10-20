using StackExchange.Redis;
using System;
using System.Collections.Generic;
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
            return HttpContext.Current.Session["UserID"].ToString() == UserID;
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
        
    }
}