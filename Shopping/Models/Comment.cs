namespace Shopping.Models
{
    public class Comment : Auditables
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string CommentText { get; set; }
        public Guid ProductId { get; set; }
    }
}
