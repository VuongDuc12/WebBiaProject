using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBiaProject.Models;

[Table("InvoiceDetail")]
public partial class InvoiceDetail
{
    [Key]
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int PlayTimeMinutes { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalPlayPrice { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? TimeInput { get; set; } // Thêm thời gian bắt đầu

    [Column(TypeName = "datetime")]
    public DateTime? TimeOutput { get; set; } // Thêm thời gian kết thúc

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    [ForeignKey("InvoiceId")]
    [InverseProperty("InvoiceDetails")]
    public virtual Invoice Invoice { get; set; } = null!;
}