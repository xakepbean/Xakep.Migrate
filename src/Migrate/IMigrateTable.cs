using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xakep.Migrate
{
    public interface IMigrateTable
    {
        void Migrate<T>(bool ObjectNameLowerCase = false) where T : class;

        void Migrate<T>(string MigrateName,bool ObjectNameLowerCase = false) where T : class;

        void Migrate<T>(string MigrateName, string tableName, bool ObjectNameLowerCase = false) where T : class;

        void Migrate(string MigrateName, Action<ModelBuilder> OnModelCreating, bool ObjectNameLowerCase = false);
    }
}
