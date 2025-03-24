namespace Shopping.Models
{
    public partial class Coupon : Auditables
    {
        public string Code { get; set; } = null!;

        public decimal Discount { get; set; }
    }
}
