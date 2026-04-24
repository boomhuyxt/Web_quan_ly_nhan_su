using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;

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

            // ==========================================
            // 4. THÊM CẤU HÌNH DỊCH VỤ SESSION TẠI ĐÂY
            // ==========================================
            builder.Services.AddDistributedMemoryCache(); // Lưu session vào RAM
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian sống của Session
                options.Cookie.HttpOnly = true; // Bảo mật Cookie Session
                options.Cookie.IsEssential = true;
            });
            // ==========================================

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

            // ==========================================
            // 5. KÍCH HOẠT MIDDLEWARE SESSION TẠI ĐÂY
            // Phải nằm sau UseRouting() và trước UseAuthentication()
            // ==========================================
            app.UseSession();
            // ==========================================

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