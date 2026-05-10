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
            // BƯỚC QUAN TRỌNG: Tắt IPv6 để tránh lỗi kết nối đến Supabase (ép dùng IPv4)
            AppContext.SetSwitch("System.Net.DisableIPv6", true);

            var builder = WebApplication.CreateBuilder(args);

            // 1. Cấu hình Database (PostgreSQL)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }));

            // 2. Cấu hình Xác thực bằng Cookie (Cookie Authentication)
            // Cho phép NhanVienController sử dụng Claims để lấy ID người dùng
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Duy trì đăng nhập 7 ngày
                });

            // 3. Cấu hình Session (Nếu bạn vẫn muốn dùng Session song song với Cookie)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // 4. Đăng ký dịch vụ MVC
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Cấu hình Pipeline (Middleware)
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 5. Kích hoạt Session (Phải nằm sau UseRouting và trước UseAuthentication)
            app.UseSession();

            // 6. Kích hoạt Xác thực và Phân quyền
            app.UseAuthentication();
            app.UseAuthorization();

            // 7. Cấu hình Route (Trỏ mặc định về Login)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}