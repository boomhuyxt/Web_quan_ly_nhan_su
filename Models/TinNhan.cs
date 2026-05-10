using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{
    [Table("TinNhan")]
    public class TinNhan
    {
        [Key]
        public int MaTinNhan { get; set; }

        [Required]
        public int NguoiGuiId { get; set; }

        [Required]
        public int NguoiNhanId { get; set; }

        [Required]
        public string NoiDung { get; set; }

        public DateTime ThoiGianGui { get; set; } = DateTime.UtcNow;

        public bool DaDoc { get; set; } = false;
    }
}