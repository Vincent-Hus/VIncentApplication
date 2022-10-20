using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace VIncentApplication.Models
{
    public class Art
    {

        /// <summary>
        /// 文章編號(自動編號)
        /// </summary>
        [Required]
        public int ArtID { get; set; }

        /// <summary>
        /// 文章內容
        /// </summary>
        [Required]
        [Display(Name = "文章內容")]
        [MaxLength(2000)]
        public string ArtContent { get; set; }

        /// <summary>
        /// 文章標題
        /// </summary>
        [Required]
        [Display(Name ="文章標題")]
        [MaxLength(50)]
        public string Title { get; set; }


        /// <summary>
        /// 建立時間
        /// </summary>
        [Required]
        [Display(Name ="發表時間")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "最後編輯時間")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 作者
        /// </summary>
        [Required]
        [Display(Name ="作者")]
        public string UserID { get; set; }
        /// <summary>
        /// 作者
        /// </summary>
        [Display(Name = "點閱數")]
        public long ClicksNumber { get; set; }

        /// <summary>
        /// 點讚數
        /// </summary>
        [Display(Name = "點讚數")]
        public int LikeNumber { get; set; }

        /// <summary>
        /// 使用者是否喜歡
        /// </summary>
        public bool Like { get; set; }
        /// <summary>
        /// 多筆留言資料
        /// </summary>

        public List<Comment> Comment { get; set; } 
    }
    public class ArtView
    {
        /// <summary>
        /// 關鍵字
        /// </summary>
        public string KeyWord { get; set; }

        /// <summary>
        /// 多筆文章資料
        /// </summary>
        public List<Art> Arts { get; set; }
    }

}