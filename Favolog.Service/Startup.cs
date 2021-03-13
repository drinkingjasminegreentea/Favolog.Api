using ElmahCore.Mvc;
using ElmahCore.Sql;
using Favolog.Service.AuthorizationPolicies;
using Favolog.Service.Repository;
using Favolog.Service.ServiceClients;
using Favolog.Service.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
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
            //Setup Elmah logging
            services.AddElmah<SqlErrorLog>(options =>
            {
                options.ConnectionString = Configuration.GetConnectionString("ElmahDatabase");
            });

            services.AddDbContext<FavologDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("FavologDatabase"));                
            });

            services.AddCors(options =>
            {
                options.AddPolicy(name: "DefaultPolicy",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:3000",
                                            "https://favologservice.azurewebsites.net")
                        .SetIsOriginAllowed((host) => true)
                         .AllowAnyMethod()
                         .AllowAnyHeader();
                    });
            });

            services.AddMicrosoftIdentityWebApiAuthentication(Configuration, "AzureAdB2C");

            services.AddControllers(options =>
                options.EnableEndpointRouting = false)
                .AddNewtonsoftJson(o => o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("access",
                        policy => policy.Requirements.Add(new ScopesRequirement("access")));
            });

            services.Configure<AppSettings>(Configuration.GetSection(
                                        AppSettings.Section));

            services.AddScoped<IFavologRepository, FavologRepository>();
            services.AddScoped<IBlobStorageService, BlobStorageService>();
            services.AddHttpClient<IOpenGraphGenerator, OpenGraphGenerator>();            
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
            app.UseCors("DefaultPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });            

        }
    }
}
