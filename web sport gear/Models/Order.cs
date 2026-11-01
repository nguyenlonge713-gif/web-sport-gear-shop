using System;
using System.Collections.Generic;

namespace web_sport_gear.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        public int UserID { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string ShippingAddress { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string OrderStatus { get; set; }

        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }
    }
}