using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using System; // Thêm thư viện này

namespace Web_quan_ly_nhan_su
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // BƯỚC QUAN TRỌNG NHẤT: Tắt IPv6, ép ứng dụng chỉ dùng IPv4 để kết nối đến Supabase
            AppContext.SetSwitch("System.Net.DisableIPv6", true);

            var builder = WebApplication.CreateBuilder(args);

            // 1. Lấy chuỗi kết nối từ appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 2. Đăng ký AppDbContext sử dụng PostgreSQL (Npgsql) VÀ Bật tính năng tự động thử lại (RetryOnFailure)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }));

            // 3. CẤU HÌNH BẮT BUỘC CHO ĐĂNG NHẬP (Cookie Authentication)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

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
            app.UseStaticFiles();

            app.UseRouting();

            // Kích hoạt Middleware Xác thực (Phải đặt trước UseAuthorization)
            app.UseAuthentication();
            app.UseAuthorization();

            // Cấu hình Route mặc định trỏ về trang Login của AccountController
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}