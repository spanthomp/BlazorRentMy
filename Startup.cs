using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RentMyApi.Configuration;
using RentMyApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentMyApi
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
            //need to define jwt token class added in config folder<JwtCofig>
            //use configuration built in method.getsection to specify which section we want to get which is jwtconfig
            services.Configure<JwtConfig>(Configuration.GetSection("JwtConfig"));
            //the line above reads configuration we have directly from app settings and then maps it
            //once youve added service it will go through app settings and look for same settings found here - then automatically links
            //this means you can now read secret anywhere within the application

            //calls the database middleware that calls apidb context defined earlier in dbcontext class
            //and then define the options and tell asp.net that sqlite is being used
            //define connection string using config services
            services.AddDbContext<ApiDbContext>(options =>
            options.UseSqlite(
                Configuration.GetConnectionString("DefaultConnection")
                ));

            //add authentication here
            services.AddAuthentication(options =>
            {
                //this utilises jwtbearerdefaults
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                //next define the default scheme incase first one fails
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                //third one will be default challenge scheme which 
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            })
            //then add jwt token configuration
            .AddJwtBearer(jwt =>
            {//define option for jwt key that will be used - that key will be the secret used
                var key = Encoding.ASCII.GetBytes(Configuration["JwtConfig:Secret"]); //within the configuration you put jwtconfig : secret to reference

                //next you add the settings that you want for your jwt token
                jwt.SaveToken = true;
                jwt.TokenValidationParameters = new TokenValidationParameters //here you define the different configuration that your token will have
                {
                    ValidateIssuerSigningKey = true, //this validates third part of jwt token using secret and verify we have generated this token
                    //next part defining signing key which does all encryption
                    IssuerSigningKey = new SymmetricSecurityKey(key), //pass the key defined earlier
                    ValidateIssuer = false, 
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = false //dontwant token to expire whilst testing
                }; 
            });

            //identity configuration
            //identity user <> will be built in for us, then define the options
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApiDbContext>();//pass api db context earlier


            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "RentMyApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RentMyApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            //added this to add authentication built above
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
