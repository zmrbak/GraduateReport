using System;

namespace GraduateReport.Models
{
    public class Graduates
    {
        public Userinfo? userInfo { get; set; }
        public int registrationCount { get; set; }
        public DateTime? registrationEarliest { get; set; }
        public int borrowCount { get; set; }
        public Borrowearliest? borrowEarliest { get; set; }
    }
}
