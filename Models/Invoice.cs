using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using WebBiaProject.Models;
namespace WebBiaProject.Models;
[Table("Invoice")]
public partial class Invoice
{
    [Key]
    public int Id { get; set; }

    public int? CustomerId { get; set; } // Nullable

    public int BranchId { get; set; }

    public int TableId { get; set; } // Thêm TableId

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal FinalAmount { get; set; }

    public int PaymentMethodId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? Status { get; set; }

    [StringLength(100)]
    public string? CustomerName { get; set; } // Thêm

    [StringLength(20)]
    public string? CustomerPhone { get; set; } // Thêm

    [ForeignKey("BranchId")]
    [InverseProperty("Invoices")]
    public virtual Branch Branch { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("Invoices")]
    public virtual Customer? Customer { get; set; } // Nullable

    [ForeignKey("TableId")]
    [InverseProperty("Invoices")]
    public virtual BilliardTable Table { get; set; } = null!;

    [InverseProperty("Invoice")]
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    [ForeignKey("PaymentMethodId")]
    [InverseProperty("Invoices")]
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    [InverseProperty("Invoice")]
    public virtual ICollection<ServiceProductDetail> ServiceProductDetails { get; set; } = new List<ServiceProductDetail>();
}