using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{
    public class Luong
    {
        [Key]
        public int MaLuong { get; set; }

        public int MaNhanVien { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal Thuong { get; set; }
        public decimal KhauTru { get; set; }

        // 👉 CÁC CỘT MỚI THÊM CHO BẢO HIỂM VÀ TĂNG CA
        public decimal BaoHiemXaHoi { get; set; }     // 8%
        public decimal BaoHiemYTe { get; set; }       // 1.5%
        public decimal BaoHiemThatNghiep { get; set; } // 1.0%
        public decimal TienTangCa { get; set; }       // Tiền tăng ca đã nhân hệ số 1.2 (tăng 20%)

        public decimal TongLuong { get; set; }

        [ForeignKey("MaNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
    }
}