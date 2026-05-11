using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{
    public class ThanhVienNhom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaNhom { get; set; }

        [Required]
        public int MaNhanVien { get; set; }

        // QUAN HỆ: Mỗi dòng này thuộc về 1 Nhóm cụ thể
        [ForeignKey("MaNhom")]
        public virtual NhomChat? NhomChat { get; set; }

        // QUAN HỆ: Mỗi dòng này tương ứng với 1 Nhân Viên
        [ForeignKey("MaNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
    }
}