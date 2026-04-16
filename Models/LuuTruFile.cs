using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Web_quan_ly_nhan_su.Models
{
        public class LuuTruFile
        {
            [Key]
            public int Id { get; set; }
            [Required]
            public string TenFile { get; set; }
            public string? LoaiFile { get; set; }
            public long? KichThuoc { get; set; }
            [Required]
            public string DuongDan { get; set; } // Link URL từ Supabase
            public DateTime NgayUpload { get; set; } = DateTime.Now;
            public int? MaNhanVien { get; set; }

            [ForeignKey("MaNhanVien")]
            public virtual NhanVien? NhanVien { get; set; }
        }
    
}
