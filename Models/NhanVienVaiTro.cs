using System.ComponentModel.DataAnnotations.Schema;

namespace Web_quan_ly_nhan_su.Models
{



        public class NhanVienVaiTro
        {
            public int MaNhanVien { get; set; }
            public int MaVaiTro { get; set; }

            [ForeignKey("MaNhanVien")]
            public virtual NhanVien NhanVien { get; set; }

            [ForeignKey("MaVaiTro")]
            public virtual VaiTro VaiTro { get; set; }
        }
    
}
