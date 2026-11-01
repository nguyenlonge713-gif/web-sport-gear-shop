namespace web_sport_gear.Models
{
    public class OrderDetail
    {
        public int OrderDetailID { get; set; }

        public int OrderID { get; set; }

        public int ProductID { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
        // Thành tiền của từng sản phẩm
        public decimal SubTotal
        {
            get { return Quantity * UnitPrice; }
        }
    }
}