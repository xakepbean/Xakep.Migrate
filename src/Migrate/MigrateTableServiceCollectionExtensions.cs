﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;

namespace Xakep.Migrate
{
    public static class MigrateTableServiceCollectionExtensions
    {
        public static IServiceCollection AddMigrateTable(this IServiceCollection services,Action<DbContextOptionsBuilder> optionBuilder)
        {
            DbContextOptionsBuilder dbBuilder = new DbContextOptionsBuilder();
            optionBuilder(dbBuilder);

            services.AddOptions();
            services.Configure<MigrateTableOptions>(opt => opt.DbOptions = dbBuilder.Options);

            services.TryAddSingleton<IMigrateTable, MigrateTable>();

            return services;
        }

        public static IServiceCollection AddMigrateTable(this IServiceCollection services,Action<DbContextOptionsBuilder> optionBuilder, string MigrateName, Action<ModelBuilder> OnModelCreating)
        {
            if (string.IsNullOrWhiteSpace(MigrateName) || OnModelCreating == null)
                throw new Exception("not null");

            DbContextOptionsBuilder dbBuilder = new DbContextOptionsBuilder();
            optionBuilder(dbBuilder);

            var MigrateTable = new MigrateTable(new MigrateTableOptions() { DbOptions = dbBuilder.Options });

            MigrateTable.Migrate(MigrateName,OnModelCreating);

            return AddMigrateTable(services, optionBuilder);
        }
        
        public static IServiceCollection AddMigrateTable<T>(this IServiceCollection services,Action<DbContextOptionsBuilder> optionBuilder, string MigrateName = null, string tableName = null) where T : class
        {
            DbContextOptionsBuilder dbBuilder = new DbContextOptionsBuilder();
            optionBuilder(dbBuilder);

            var MigrateTable = new MigrateTable(new MigrateTableOptions() { DbOptions = dbBuilder.Options });

            MigrateTable.Migrate<T>(MigrateName,tableName);

            return AddMigrateTable(services, optionBuilder);
        }
    }
}
