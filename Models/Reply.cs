//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Plagiarism.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Reply
    {
        public int Id { get; set; }
        public string ReplayText { get; set; }
        public Nullable<System.DateTime> ReplyDate { get; set; }
        public int MessageId { get; set; }
        public Nullable<int> UserId { get; set; }
    
        public virtual Account Account { get; set; }
        public virtual Message Message { get; set; }
    }
}
