using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBiaProject.Models;

[Table("BilliardTable")]
[Index("BranchId", "TableNumber", Name = "UQ_BilliardTable_Branch_TableNumber", IsUnique = true)]
public partial class BilliardTable
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    public int BranchId { get; set; }

    public int TableNumber { get; set; }

    public int StatusId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("BilliardTables")]
    public virtual Branch Branch { get; set; } = null!;

    [InverseProperty("Table")]
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    [ForeignKey("StatusId")]
    [InverseProperty("BilliardTables")]
    public virtual TableStatus Status { get; set; } = null!;
}
