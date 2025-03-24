using Shopping.Models;

namespace Shopping.ViewModels
{
    public class ProductCartViewModel
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? ImageName { get; set; }
        public int Count { get; set; }
        public decimal? Price { get; set; }

        public decimal? RowSumPrice { get; set; }
    }
}
