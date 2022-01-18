using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentMyApi.Models;
using Microsoft.AspNetCore.Identity.UI;

namespace RentMyApi.Data
{
    //inherit from dbcontext class created by ef
    public class ApiDbContext : IdentityDbContext //changed from DbContext to identitydbcontext
    {
        //then need to add models 
        public virtual DbSet<ItemData> Items { get; set; }

        //then need to initialise api dbcontext
        public ApiDbContext(DbContextOptions<ApiDbContext> options) 
            : base(options) //need to send options to base class
        {
                
        }
    }
}
