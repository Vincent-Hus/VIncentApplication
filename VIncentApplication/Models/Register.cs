using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace VIncentApplication.Models
{
    public class Register
    {
        /// <summary>
        /// 帳號
        /// </summary>
        [Required]
        [Display(Name = "帳號")]
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
        /// <summary>
        ///信箱
        /// </summary>
        [Display(Name ="電子郵件")]
        public string Email { get; set; }
        public DateTime CreateTime { get; set; }
    }
}