using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Sentinel.Panel
{
    public class WebPanel
    {
        private WebApplication _app;

        public WebPanel(SentinelCore core)
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton<WebPanel>(this);
            builder.Services.AddSingleton<SentinelCore>(core);

            _app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!_app.Environment.IsDevelopment())
            {
                //_app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                _app.UseHsts();
            }
            _app.UseExceptionHandler("/Error/500");
            _app.UseStatusCodePagesWithRedirects("/Error/{0}?message={1}&handler=ErrorHandler");

            _app.UseHttpsRedirection();
            _app.UseStaticFiles();
            _app.UseRouting();
            _app.UseAuthorization();

            _app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }

        public void Start()
        {
            Console.WriteLine("Starting Panel");
            _app.Run();
        }

    }
}
