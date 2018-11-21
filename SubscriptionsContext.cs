using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Skraper3
{
    public partial class SubscriptionsContext : DbContext
    {
        public SubscriptionsContext()
        {
        }

        public SubscriptionsContext(DbContextOptions<SubscriptionsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Subscriptions> Subscriptions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subscriptions>(entity =>
            {
                entity.HasKey(e => new { e.Url, e.Email });

                entity.Property(e => e.Url).HasColumnName("URL");

                entity.Property(e => e.Xpath).HasColumnName("XPath");
            });
        }
    }
}
