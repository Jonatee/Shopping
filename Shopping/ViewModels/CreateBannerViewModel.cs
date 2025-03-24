using System.ComponentModel.DataAnnotations;

namespace Shopping.ViewModels
{
    public class CreateBannerViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = default!;

        [Required(ErrorMessage = "Subtitle is required.")]
        public string SubTitle { get; set; } = default!;

        [Required(ErrorMessage = "Image is required.")]
        public IFormFile ImageUrl { get; set; }

        [Url(ErrorMessage = "Invalid URL format.")]
        public string Link { get; set; }
    }
}
