using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace VIncentApplication.Models
{
    public class Comment
    {
        /// <summary>
        /// 文章編號(自動編號)
        /// </summary>
        [Required]
        public int CommentID { get; set; }

        /// <summary>
        /// 文章內容
        /// </summary>
        [Required]
        [MaxLength(250)]
        public string CommentContent { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Required]
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime? UpdateTime { get; set; }
        /// <summary>
        /// 留言者
        /// </summary>
        public string UserID { get; set; }
        /// <summary>
        /// 文章編號
        /// </summary>
        [Required]
        public int ArtID { get; set; }
    }
}