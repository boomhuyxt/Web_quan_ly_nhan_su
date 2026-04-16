using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Web_quan_ly_nhan_su.Context;

namespace ql_nhanSW.Context
{
    public class DesignDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // Dán chuỗi kết nối Supabase vào đây
            optionsBuilder.UseNpgsql("Host=db.xjmojrlupvxbpnhsftsv.supabase.co;Database=postgres;Username=postgres;Password=QlNhansw@123;Ssl Mode=Require;Trust Server Certificate=true");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}