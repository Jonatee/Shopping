namespace Shopping.ViewModels
{
    public class CreateProductViewModel
    {
        public string Name { get; set; } = default!;
        public int Price { get; set; }
        public int Discount { get; set; }
        public string Description { get; set; } = default!;
        public string Tags { get; set; } = default!;
        public IFormFile? ImageUrl { get; set; }
        public int Quantity { get; set; }

    }
}
