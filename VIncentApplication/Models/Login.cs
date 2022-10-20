using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace VIncentApplication.Models
{
    public class Login
    {
        /// <summary>
        /// 帳號
        /// </summary>
        [Required]
        [Display(Name ="帳號")]
        [MaxLength(18)]
        [MinLength(6)]
        public string UserID { get; set; }
        /// <summary>
        /// 密碼
        /// </summary>
        [Required]
        [Display(Name = "密碼")]
        [MaxLength(18)]
        [MinLength(6)]
        public string Password { get; set; }
        /// <summary>
        /// 鹽
        /// </summary>
        public string Salt { get; set; }
    }
}