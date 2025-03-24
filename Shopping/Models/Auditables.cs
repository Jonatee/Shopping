namespace Shopping.Models
{
    public class Auditables
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedOn { get; set; } = DateTime.Now;

    }
}
