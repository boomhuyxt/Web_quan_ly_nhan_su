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
        public DbSet<PhongBan> PhongBan { get; set; }
        public DbSet<ChamCong> ChamCong { get; set; }
        public DbSet<NghiPhep> NghiPhep { get; set; }
        public DbSet<VaiTro> VaiTro { get; set; }
        public DbSet<NhanVienVaiTro> NhanVienVaiTro { get; set; }
        public DbSet<Luong> Luong { get; set; }
        public DbSet<LuuTruFile> LuuTruFile { get; set; }

        // Thêm DbSet cho bảng TinNhan
        public DbSet<TinNhan> TinNhan { get; set; }

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

            // Cấu hình bảng TinNhan
            modelBuilder.Entity<TinNhan>().ToTable("TinNhan");

            // Cấu hình Quan hệ Many-to-Many trung gian
            modelBuilder.Entity<NhanVienVaiTro>()
                .HasKey(x => new { x.MaNhanVien, x.MaVaiTro });

            modelBuilder.Entity<NhanVienVaiTro>()
                .HasOne(x => x.NhanVien)
                .WithMany(x => x.NhanVienVaiTro)
                .HasForeignKey(x => x.MaNhanVien);

            modelBuilder.Entity<NhanVienVaiTro>()
                .HasOne(x => x.VaiTro)
                .WithMany(x => x.NhanVienVaiTro)
                .HasForeignKey(x => x.MaVaiTro);
        }
    }
}