using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{

        [Table("PhongBan")]
        public class PhongBan
        {
            [Key]
            public int MaPhongBan { get; set; }

            [Required]
            [MaxLength(255)]
            public string TenPhongBan { get; set; }

            // Danh sách nhân viên thuộc phòng ban này
            public virtual ICollection<NhanVien>? NhanViens { get; set; }
        }
    
}
