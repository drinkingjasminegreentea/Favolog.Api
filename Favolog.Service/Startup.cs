using ElmahCore.Mvc;
using ElmahCore.Sql;
using Favolog.Service.Repository;
using Favolog.Service.ServiceClients;
using Favolog.Service.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Favolog.Service
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
            services.AddDbContext<FavologDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("FavologDatabase"));                
            });

            // Add CORS policy
            services.AddCors(options => {
                options.AddPolicy("localhost", builder => builder
                 .WithOrigins("http://localhost:3000/")
                 .SetIsOriginAllowed((host) => true)
                 .AllowAnyMethod()
                 .AllowAnyHeader());
            });

            services.AddControllers(options =>
                options.EnableEndpointRouting = false)
                .AddNewtonsoftJson(o => o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.Configure<AppSettings>(Configuration.GetSection(
                                        AppSettings.Section));

            services.AddScoped<IFavologRepository, FavologRepository>();
            services.AddScoped<IBlobStorageService, BlobStorageService>();
            services.AddHttpClient<IOpenGraphGenerator, OpenGraphGenerator>();

            //Setup Elmah logging
            services.AddElmah<SqlErrorLog>(options =>
            {
                options.ConnectionString = Configuration.GetConnectionString("ElmahDatabase");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseElmah();
            app.UseRouting();

            app.UseCors("localhost");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });            

        }
    }
}
