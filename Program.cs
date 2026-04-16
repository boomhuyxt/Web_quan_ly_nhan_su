using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Context;

namespace Web_quan_ly_nhan_su
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Lấy chuỗi kết nối từ appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 2. Đăng ký AppDbContext sử dụng PostgreSQL (Npgsql)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Cấu hình để sử dụng các file tĩnh trong wwwroot (CSS, JS, Images)
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // Cấu hình Route mặc định trỏ về trang Login của AccountController
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}