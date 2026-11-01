using System;

namespace web_sport_gear.Models
{
    public class OrderListViewModel
    {
        public int OrderID { get; set; }

        public int UserID { get; set; }

        public string CustomerName { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string ShippingAddress { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string OrderStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}