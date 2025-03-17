using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBiaProject.Models;

[Table("PaymentMethod")]
[Index("TransactionCode", Name = "UQ__PaymentM__D85E702652C33011", IsUnique = true)]
public partial class PaymentMethod
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string MethodType { get; set; } = null!;

    [StringLength(100)]
    public string TransactionCode { get; set; } = null!;

    [StringLength(255)]
    public string? PaymentDetails { get; set; }

    [StringLength(100)]
    public string ReceiverAccount { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? PaymentDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column("QRCodeData")]
    public string? QrcodeData { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    [InverseProperty("PaymentMethod")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
