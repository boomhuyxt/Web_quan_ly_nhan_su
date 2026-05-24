using Microsoft.EntityFrameworkCore;
using Web_quan_ly_nhan_su.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using Pgvector.EntityFrameworkCore;
using System;
using Npgsql;
// Khai báo thư mục chứa ChatHub
using Web_quan_ly_nhan_su.Hubs;

namespace Web_quan_ly_nhan_su
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // BƯỚC QUAN TRỌNG: Tắt IPv6 để tránh lỗi kết nối đến Supabase (ép dùng IPv4)
            AppContext.SetSwitch("System.Net.DisableIPv6", true);

            var builder = WebApplication.CreateBuilder(args);

            // BƯỚC QUAN TRỌNG: Tắt IPv6 để tránh lỗi kết nối đến Supabase (ép dùng IPv4)
            AppContext.SetSwitch("System.Net.DisableIPv6", true);

            // 👉 THÊM DÒNG NÀY ĐỂ TẮT KIỂM TRA MÚI GIỜ KHẮT KHE CỦA POSTGRESQL 👈
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


            // =========================================================================
            // 1. Cấu hình Database (PostgreSQL) - ĐÃ THÊM USEVECTOR() ĐỂ SỬA LỖI AI
            // =========================================================================
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Khởi tạo DataSource và báo cho Npgsql biết hệ thống có xài Vector
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector(); // Bắt buộc phải có dòng này
            var dataSource = dataSourceBuilder.Build();

            // Đăng ký DbContext với DataSource đã kích hoạt Vector
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(dataSource, sqlOptions =>
                {
                    sqlOptions.UseVector(); // Kích hoạt Vector trong EF Core
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }));
            // Đăng ký HttpClient vào hệ thống Dependency Injection
            builder.Services.AddHttpClient();
            // 2. Cấu hình Xác thực bằng Cookie (Cookie Authentication)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn khi bị chặn quyền ADMIN
                    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Duy trì đăng nhập 7 ngày
                });

            // 3. Cấu hình Session 
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // 4. Đăng ký dịch vụ MVC
            builder.Services.AddControllersWithViews();

            // 5. Kích hoạt dịch vụ SignalR trên máy chủ để chạy Chat Real-time
            builder.Services.AddSignalR();

            // Đăng ký dịch vụ HttpClient cho toàn bộ hệ thống
            builder.Services.AddHttpClient();

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

            // 6. Kích hoạt Session (Phải nằm sau UseRouting và trước UseAuthentication)
            app.UseSession();

            // 7. Kích hoạt Xác thực và Phân quyền (Thứ tự Authentication trước Authorization là bắt buộc)
            app.UseAuthentication();
            app.UseAuthorization();

            // 8. Mở đường dẫn kết nối "/chathub" cho Frontend gọi tới
            app.MapHub<ChatHub>("/chathub");

            // 9. Cấu hình Route (Trỏ mặc định về trang Login của Account)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}