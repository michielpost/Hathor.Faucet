using Hathor.Faucet.Database;
using Hathor.Faucet.Services;
using Hathor.Faucet.Services.Models;
using Hathor.Faucet.Web.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Recaptcha.Web.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hathor.Faucet.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");

            //Local testing
            services.AddDbContext<FaucetDbContext>(options =>
                options.UseSqlite(new SqliteConnection($"Filename={Path.Combine("App_Data", "localdev.sqlite")}")));

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddMemoryCache();
            services.AddControllersWithViews();

            services.AddScoped<FaucetService>();
            services.AddScoped<HathorService>();
            services.AddScoped<WalletTransactionService>();

            //Config
            services.Configure<HathorConfig>(Configuration.GetSection(nameof(HathorConfig)));
            services.Configure<FaucetConfig>(Configuration.GetSection(nameof(FaucetConfig)));

            RecaptchaConfigurationManager.SetConfiguration(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
