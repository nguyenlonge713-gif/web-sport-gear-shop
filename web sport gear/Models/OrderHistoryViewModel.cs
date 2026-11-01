using System;

namespace web_sport_gear.Models
{
    public class OrderHistoryViewModel
    {
        public int OrderID { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}