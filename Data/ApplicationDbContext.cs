using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Unanet_POC.Models.Domain;

namespace Unanet_POC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Feedback> Feedbacks { get; set; }
    }
}
