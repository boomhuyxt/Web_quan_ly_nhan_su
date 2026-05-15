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

        // CÁC BẢNG DÀNH CHO CHAT
        public DbSet<TinNhan> TinNhan { get; set; }
        public DbSet<NhomChat> NhomChat { get; set; }
        public DbSet<ThanhVienNhom> ThanhVienNhom { get; set; }
        public DbSet<TinNhanNhom> TinNhanNhom { get; set; }

        // BẢNG LƯU TRỮ KIẾN THỨC AI
        public DbSet<DanhMucKienThuc> DanhMucKienThuc { get; set; }

        public DbSet<LichCongTac> LichCongTacs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // BẮT BUỘC PHẢI CÓ DÒNG NÀY ĐỂ POSTGRESQL HỖ TRỢ VECTOR
            // =========================================================
            modelBuilder.HasPostgresExtension("vector");

            // Cấu hình tên bảng thủ công
            modelBuilder.Entity<NhanVien>().ToTable("NhanVien");
            modelBuilder.Entity<PhongBan>().ToTable("PhongBan");
            modelBuilder.Entity<VaiTro>().ToTable("VaiTro");
            modelBuilder.Entity<ChamCong>().ToTable("ChamCong");
            modelBuilder.Entity<NghiPhep>().ToTable("NghiPhep");
            modelBuilder.Entity<Luong>().ToTable("Luong");
            modelBuilder.Entity<LuuTruFile>().ToTable("LuuTruFile");
            modelBuilder.Entity<NhanVienVaiTro>().ToTable("NhanVienVaiTro");

            // Cấu hình bảng AI
            modelBuilder.Entity<DanhMucKienThuc>().ToTable("DanhMucKienThuc");

            // Cấu hình các bảng Chat
            modelBuilder.Entity<TinNhan>().ToTable("TinNhan");
            modelBuilder.Entity<NhomChat>().ToTable("NhomChat");
            modelBuilder.Entity<ThanhVienNhom>().ToTable("ThanhVienNhom");
            modelBuilder.Entity<TinNhanNhom>().ToTable("TinNhanNhom");

            // Cấu hình Quan hệ Many-to-Many trung gian (Nhân Viên - Vai Trò)
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

            // --- NGĂN CHẶN LỖI MULTIPLE CASCADE PATHS CỦA POSTGRESQL ---
            modelBuilder.Entity<TinNhan>()
                .HasOne(t => t.NguoiGui)
                .WithMany(n => n.TinNhanDaGui)
                .HasForeignKey(t => t.NguoiGuiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TinNhan>()
                .HasOne(t => t.NguoiNhan)
                .WithMany(n => n.TinNhanDaNhan)
                .HasForeignKey(t => t.NguoiNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TinNhanNhom>()
                .HasOne(t => t.NguoiGui)
                .WithMany()
                .HasForeignKey(t => t.NguoiGuiId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}