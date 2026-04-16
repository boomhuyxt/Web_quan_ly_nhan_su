using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{

        [Table("ChamCong")]
        public class ChamCong
        {
            [Key]
            public int MaChamCong { get; set; }

            [Required]
            public int MaNhanVien { get; set; }

            [Required]
            public DateTime NgayLamViec { get; set; }

            public TimeSpan? GioVao { get; set; }

            public TimeSpan? GioRa { get; set; }

            [ForeignKey("MaNhanVien")]
            public virtual NhanVien? NhanVien { get; set; }
        }
    
}
