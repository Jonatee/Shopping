namespace Shopping.Models
{
    public class Product : Auditables
    {
        public  string Name { get; set; }
        public decimal  Price { get; set; }
        public decimal Discount { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set;}

    }
}
