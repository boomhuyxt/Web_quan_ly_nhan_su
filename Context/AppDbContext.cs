using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Models;

namespace Web_quan_ly_nhan_su.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<NhanVien> NhanVien { get; set; }
        public DbSet<PhongBan> PhongBan { get; set; } // Đã bỏ 's' để đồng bộ
        public DbSet<ChamCong> ChamCong { get; set; }  // Đã bỏ 's'
        public DbSet<NghiPhep> NghiPhep { get; set; }  // Đã bỏ 's'
        public DbSet<VaiTro> VaiTro { get; set; }      // Đã bỏ 's'
        public DbSet<NhanVienVaiTro> NhanVienVaiTro { get; set; }
        public DbSet<Luong> Luong { get; set; }        // Đã bỏ 's'
        public DbSet<LuuTruFile> LuuTruFile { get; set; } // Đã bỏ 's'

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình tên bảng thủ công
            modelBuilder.Entity<NhanVien>().ToTable("NhanVien");
            modelBuilder.Entity<PhongBan>().ToTable("PhongBan");
            modelBuilder.Entity<VaiTro>().ToTable("VaiTro");
            modelBuilder.Entity<ChamCong>().ToTable("ChamCong");
            modelBuilder.Entity<NghiPhep>().ToTable("NghiPhep");
            modelBuilder.Entity<Luong>().ToTable("Luong");
            modelBuilder.Entity<LuuTruFile>().ToTable("LuuTruFile");
            modelBuilder.Entity<NhanVienVaiTro>().ToTable("NhanVienVaiTro");

            // Cấu hình Quan hệ Many-to-Many trung gian
            modelBuilder.Entity<NhanVienVaiTro>()
                .HasKey(x => new { x.MaNhanVien, x.MaVaiTro });

            modelBuilder.Entity<NhanVienVaiTro>()
                .HasOne(x => x.NhanVien)
                .WithMany(x => x.NhanVienVaiTro) // Lỗi CS1061 biến mất nếu NhanVien.cs có prop này
                .HasForeignKey(x => x.MaNhanVien);

            modelBuilder.Entity<NhanVienVaiTro>()
                .HasOne(x => x.VaiTro)
                .WithMany(x => x.NhanVienVaiTro) // Lỗi CS1061 biến mất nếu VaiTro.cs có prop này
                .HasForeignKey(x => x.MaVaiTro);
        }
    }
}