
using System;

namespace web_sport_gear.Models
{
    public class AdminUserViewModel
    {
        public int UserID { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Role { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
