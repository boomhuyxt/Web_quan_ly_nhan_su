using Pgvector; // Gọi thư viện Vector
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// Có thể xóa dòng using System.Numerics; đi

namespace Web_quan_ly_nhan_su.Models
{
    [Table("DanhMucKienThuc")]
    public class DanhMucKienThuc
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public string TieuDe { get; set; }

        public string NoiDung { get; set; }

        [MaxLength(100)]
        public string LoaiTaiLieu { get; set; } // Hợp đồng, Nội quy, Quy trình...

        // Cột lưu trữ vector ngữ nghĩa (Gemini embedding trả về 768 chiều)
        [Column(TypeName = "vector(768)")]

        // SỬA LỖI Ở ĐÂY: Thêm Pgvector. vào trước chữ Vector
        public Pgvector.Vector VectoNoiDung { get; set; }
    }
}