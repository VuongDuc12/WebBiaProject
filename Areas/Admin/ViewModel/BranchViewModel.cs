using System.ComponentModel.DataAnnotations;

namespace WebBiaProject.Areas.Admin.ViewModel
{
    public class BranchViewModel
    {
        [Required(ErrorMessage = "Branch name is required.")]
        [StringLength(100, ErrorMessage = "Branch name cannot exceed 100 characters.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string Address { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Phone number must be between 8 and 15 digits.")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = null!;
    }
}