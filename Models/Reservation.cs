using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBiaProject.Models;

[Table("Reservation")]
public partial class Reservation
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int TableId { get; set; }

    public int BranchId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime StartTime { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("Reservations")]
    public virtual Branch Branch { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("Reservations")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("TableId")]
    [InverseProperty("Reservations")]
    public virtual BilliardTable Table { get; set; } = null!;
}
