﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface IMigrateTable
    {
        void Migrate<T>(string MigrateName = null, string tableName = null) where T : class;

        void Migrate(string MigrateName,Action<ModelBuilder> OnModelCreating);
    }
}
