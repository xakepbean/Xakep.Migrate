using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sample1.Models;

namespace sample1.Data
{
    public class TestDbContext : DbContext
    {
        /// <summary>
     /// Connection string<br/>
     /// 连接字符串<br/>
     /// </summary>
        private string ConnectionString { get; set; }

        public TestDbContext(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public DbSet<TestTable> DClass { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(ConnectionString);
        }

        /// <summary>
        /// 用于指定新的表名
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            //modelBuilder.Entity<TestTable>().ToTable("TestTable1");

            base.OnModelCreating(modelBuilder);
        }
    }

    
}
