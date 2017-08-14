using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class MigrateTableHistory
    {
        [Key]
        public int Revision { get; set; }

        public string MigrateName { get; set; }

        public string Model { get; set; }


        public string ProductVersion { get; set; }


        public MigrateTableHistory() { }

        public MigrateTableHistory(string migrateName,string model)
        {
            MigrateName = migrateName;
            Model = model;
            ProductVersion = ProductInfo.GetVersion();
        }

    }

    public class DbHistory : DbContext
    {
        public DbHistory(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var typeBuilder = builder.Entity<MigrateTableHistory>()
               .ToTable("EFMigrateTableHistory");
            typeBuilder.HasKey(h => h.Revision);
            typeBuilder.Property(h => h.Model).IsRequired();
            typeBuilder.Property(h => h.MigrateName).IsRequired();
            typeBuilder.Property(h => h.ProductVersion).IsRequired();
            base.OnModelCreating(builder);
        }

        public DbSet<MigrateTableHistory> History { get; set; }

    }

}
