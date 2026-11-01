
namespace web_sport_gear.Models
{
    public class Product
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        public int CategoryID { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public string CategoryName { get; set; }
    }
}
