
using System;
using System.Collections.Generic;

namespace web_sport_gear.Models
{
    public class CustomerOrderHistoryViewModel
    {
        public int OrderID { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ShippingAddress { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<CustomerOrderItemViewModel> Items { get; set; }

        public CustomerOrderHistoryViewModel()
        {
            Items = new List<CustomerOrderItemViewModel>();
        }
    }

    public class CustomerOrderItemViewModel
    {
        public int OrderDetailID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal SubTotal
        {
            get { return Quantity * UnitPrice; }
        }
    }
}
