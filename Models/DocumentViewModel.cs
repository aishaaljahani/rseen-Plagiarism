

using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace Plagiarism.Models
{
    public class DocumentViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; }
        public Nullable<int> UploaderId { get; set; }
        [Required]
        [Display(Name = "File")]
        public HttpPostedFileBase FileUpload { get; set; }

    }
}