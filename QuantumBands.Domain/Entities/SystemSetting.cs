using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantumBands.Domain.Entities;

[Index("SettingKey", Name = "UQ__SystemSe__01E719ADD25DB536", IsUnique = true)]
public partial class SystemSetting
{
    [Key]
    [Column("SettingID")]
    public int SettingId { get; set; }

    [StringLength(100)]
    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    [StringLength(50)]
    public string SettingDataType { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsEditableByAdmin { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    [Column("UpdatedByUserID")]
    public int? UpdatedByUserId { get; set; }

    [ForeignKey("UpdatedByUserId")]
    [InverseProperty("SystemSettings")]
    public virtual User? UpdatedByUser { get; set; }
}
