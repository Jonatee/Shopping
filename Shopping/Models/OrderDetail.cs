namespace Shopping.Models
{
    
      public partial class OrderDetail : Auditables
      {

            public string ProductTitle { get; set; } = null!;

            public decimal ProductPrice { get; set; }

            public int Count { get; set; }

            public Guid OrderId { get; set; }

            public Guid ProductId { get; set; }
      }
}
