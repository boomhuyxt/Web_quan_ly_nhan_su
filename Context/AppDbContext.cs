
using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<PhongBan> PhongBans { get; set; }
        public DbSet<ChamCong> ChamCongs { get; set; }
        public DbSet<NghiPhep> NghiPheps { get; set; }
        public DbSet<VaiTro> VaiTros { get; set; }
        public DbSet<NhanVienVaiTro> NhanVienVaiTros { get; set; }
        public DbSet<Luong> Luongs { get; set; }
        public DbSet<LuuTruFile> LuuTruFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Khóa chính phức hợp cho bảng phân quyền mới
            modelBuilder.Entity<NhanVienVaiTro>()
                .HasKey(nv => new { nv.MaNhanVien, nv.MaVaiTro });

            modelBuilder.Entity<NhanVien>().ToTable("NhanVien");
            modelBuilder.Entity<VaiTro>().ToTable("VaiTro");
            modelBuilder.Entity<PhongBan>().ToTable("PhongBan");
        }
    }

}
