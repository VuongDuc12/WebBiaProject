using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBiaProject.ViewModel
{
    public class BookingViewModel
    {
       
        public int CustomerId { get; set; }

        public int TableId { get; set; }

        public int BranchId { get; set; }

        
        public DateTime StartTime { get; set; }


        public string Status { get; set; } = null!;

    
    }
}
