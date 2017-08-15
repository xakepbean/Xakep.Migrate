using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Xakep.Migrate
{
    public static class MigrateTableServiceCollectionExtensions
    {
        public static IServiceCollection AddMigrateTable(this IServiceCollection services,Action<DbContextOptionsBuilder> optionBuilder)
        {
            var Opt = AddOptions(services, optionBuilder);
            return Opt.Services;
        }

        public static IServiceCollection AddMigrateTable(this IServiceCollection services,Action<DbContextOptionsBuilder> optionBuilder, string MigrateName, Action<ModelBuilder> OnModelCreating, bool ObjectNameLowerCase = false)
        {
            if (string.IsNullOrWhiteSpace(MigrateName) || OnModelCreating == null)
                throw new Exception("not null");

            var Opt = AddOptions(services, optionBuilder);
            services = Opt.Services;

            new MigrateTable(new MigrateTableOptions() {
                DbOptions = Opt.Options
            }).Migrate(MigrateName,OnModelCreating,ObjectNameLowerCase);

            return Opt.Services;
        }

        public static IServiceCollection AddMigrateTable<T>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionBuilder, bool ObjectNameLowerCase = false) where T : class
        => AddMigrateTable<T>(services, optionBuilder, null, ObjectNameLowerCase);

        public static IServiceCollection AddMigrateTable<T>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionBuilder, string MigrateName, bool ObjectNameLowerCase = false) where T : class
         => AddMigrateTable<T>(services, optionBuilder, null, null, ObjectNameLowerCase);

        public static IServiceCollection AddMigrateTable<T>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionBuilder, string MigrateName , string tableName, bool ObjectNameLowerCase = false) where T : class
        {
            var Opt = AddOptions(services, optionBuilder);
            services = Opt.Services;

            new MigrateTable(new MigrateTableOptions()
            {
                DbOptions = Opt.Options
            }).Migrate<T>(MigrateName, tableName, ObjectNameLowerCase);

            return Opt.Services;
        }

        private static (IServiceCollection Services, DbContextOptions Options) AddOptions(this IServiceCollection services, Action<DbContextOptionsBuilder> optionBuilder)
        {

            DbContextOptionsBuilder dbBuilder = new DbContextOptionsBuilder();
            optionBuilder(dbBuilder);

            services.AddOptions();
            services.Configure<MigrateTableOptions>(opt => opt.DbOptions = dbBuilder.Options);

            services.TryAddSingleton<IMigrateTable, MigrateTable>();
            return (services, dbBuilder.Options);
        }

    }
}
