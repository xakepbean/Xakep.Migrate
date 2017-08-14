using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore.Metadata;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.Extensions.DependencyModel;
using System.Runtime.Loader;

namespace WebApplication1.Services
{
    public class MigrateTable : IMigrateTable
    {

        private const string ModelSnapshotFilePrefix = "EFModelTableSnapshot_";

        private const string ModelSnapshotNamespace = "MigrateTable.Migrations";

        private const string ModelSnapshotClassPrefix = "Migration_";
        private ISet<Assembly> LoadedAssemblies { get; set; }
        private HashSet<string> LoadedNamespaces { get; set; }

        private LoadContext AssemblyContext { get; set; }

        private MigrateTableOptions DbOptions { get; }

        private void Init()
        {
            LoadedNamespaces = new HashSet<string>();
            LoadedAssemblies = new HashSet<Assembly>();
            AssemblyContext = new LoadContext();
            foreach (var library in DependencyContext.Default.RuntimeLibraries)
            {
                foreach (var assemblyName in library.GetDefaultAssemblyNames(DependencyContext.Default))
                {
                    Load(assemblyName);
                }
            }
        }

        public MigrateTable(IOptions<MigrateTableOptions> mtOptions)
        {
            Init();
            DbOptions = mtOptions.Value;
        }
        public MigrateTable(MigrateTableOptions mtOptions)
        {
            Init();
            DbOptions = mtOptions;
        }

        public void Migrate(string MigrateName, Action<ModelBuilder> OnModelCreating)
        {
            EnsureCreated();
           
            using (var context = new DbContext(DbOptions.DbOptions))
            {
                var serviceProvider = ((IInfrastructure<IServiceProvider>)context).Instance;
                var databaseCreator = serviceProvider.GetService<IDatabaseCreator>();
                if (databaseCreator is IRelationalDatabaseCreator)
                {
                    var newModel = InitModel(serviceProvider, OnModelCreating, context);
                    var lastModel = newModel;
                    var DHContent = GetHistory();
                    var lastMigration = DHContent.History.Where(w=>w.MigrateName.Equals(MigrateName)).OrderByDescending(h => h.Revision).FirstOrDefault();
                    if (lastMigration != null)
                    {
                        DelTempFile();
                        var tempPath = Path.GetTempPath();
                        var assemblyName = ModelSnapshotFilePrefix + DateTime.UtcNow.Ticks;
                        var codePath = Path.Combine(tempPath, assemblyName + ".cs");
                        var assemblyPath = Path.Combine(tempPath, assemblyName + ".dll");
                        File.WriteAllText(codePath, lastMigration.Model);

                        Compile(new[] { codePath }, assemblyName, assemblyPath);
                      
                        var assembly = Assembly.LoadFile(assemblyPath);
                        var snapshot = (ModelSnapshot)Activator.CreateInstance(
                            assembly.GetTypes().First(t =>
                            typeof(ModelSnapshot).GetTypeInfo().IsAssignableFrom(t)));
                        lastModel = snapshot.Model;
                    }
                    else
                        lastModel = null;


                    var modelDiffer = serviceProvider.GetService<IMigrationsModelDiffer>();
                    var sqlGenerator = serviceProvider.GetService<IMigrationsSqlGenerator>();

                    var operations = modelDiffer.GetDifferences(lastModel, newModel);
                    if (operations.Count <= 0)
                    {
                        return;
                    }
                    var commands = sqlGenerator.Generate(operations, newModel);

                    var connection = serviceProvider.GetService<IRelationalConnection>();
                    var commandExecutor = serviceProvider.GetService<IMigrationCommandExecutor>();


                    var codeHelper = new CSharpHelper();
                    var generator = new CSharpMigrationsGenerator(
                        new MigrationsCodeGeneratorDependencies(),
                        new CSharpMigrationsGeneratorDependencies(codeHelper, 
                            new CSharpMigrationOperationGenerator(new CSharpMigrationOperationGeneratorDependencies(codeHelper)),
                            new CSharpSnapshotGenerator(new CSharpSnapshotGeneratorDependencies(codeHelper))
                            )
                        );
                   
                    
                    var modelSnapshot = generator.GenerateSnapshot(
                        ModelSnapshotNamespace, context.GetType(),
                        ModelSnapshotClassPrefix + DateTime.UtcNow.Ticks, context.Model);


                    var history = new MigrateTableHistory(MigrateName,modelSnapshot);
                    DHContent.History.Add(history);
                    DHContent.SaveChanges();
                    try
                    {
                        commandExecutor.ExecuteNonQuery(commands, connection);
                    }
                    catch
                    {
                        DHContent.Remove(history);
                        DHContent.SaveChanges();
                    }
                    finally
                    {
                        if (lastModel != null)
                            DelTempFile();
                    }
                }
            }
        }

        public void Migrate<T>(string MigrateName = null, string tableName = null) where T : class
        {
            var vType = typeof(T);
            tableName = tableName ?? vType.Name;
            MigrateName = MigrateName ?? ModelSnapshotFilePrefix + tableName;
            Migrate(MigrateName, m => m.Entity(vType).ToTable(tableName));
        }



        /// <summary>
        /// 删除临时文件
        /// </summary>
        private void DelTempFile()
        {
            var tempPath = Path.GetTempPath();
            foreach (var file in Directory.EnumerateFiles(tempPath, ModelSnapshotFilePrefix + "*").ToList())
            {
                try { File.Delete(file); } catch { }
            }
        }

        private DbHistory GetHistory()
        {
           var dbcon=  new DbHistory(DbOptions.DbOptions);
            try
            {
                dbcon.History.FirstOrDefault();
            }
            catch
            {
                var serviceProvider = ((IInfrastructure<IServiceProvider>)dbcon).Instance;
                var databaseCreator = serviceProvider.GetService<IDatabaseCreator>();
                if (databaseCreator is IRelationalDatabaseCreator)
                {
                    var modelDiffer = serviceProvider.GetService<IMigrationsModelDiffer>();
                    var sqlGenerator = serviceProvider.GetService<IMigrationsSqlGenerator>();

                    var operations = modelDiffer.GetDifferences(null, dbcon.Model);
                    if (operations.Count > 0)
                    {
                        var commands = sqlGenerator.Generate(operations, dbcon.Model);
                        var connection = serviceProvider.GetService<IRelationalConnection>();
                        var commandExecutor = serviceProvider.GetService<IMigrationCommandExecutor>();
                        var generator = serviceProvider.GetService<IMigrationsCodeGenerator>();
                        commandExecutor.ExecuteNonQuery(commands, connection);
                    }
                }
            }
            return dbcon;
        }

        private void Compile(IList<string> sourceFiles, string assemblyName, string assemblyPath)
        {
            var parseOptions = CSharpParseOptions.Default;
            parseOptions = parseOptions.WithPreprocessorSymbols("NETCORE");

            var syntaxTrees = sourceFiles
                .Select(path => CSharpSyntaxTree.ParseText(
                    File.ReadAllText(path), parseOptions, path, Encoding.UTF8))
                .ToList();

            LoadAssembliesFromUsings(syntaxTrees);
            var references = LoadedAssemblies.Select(assembly => assembly.Location)
                 .Select(path => MetadataReference.CreateFromFile(path))
                 .ToList();
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,    optimizationLevel: OptimizationLevel.Release);
            var withTopLevelBinderFlagsMethod = compilationOptions.GetType()
                .GetMethod("WithTopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            var binderFlagsType = withTopLevelBinderFlagsMethod.GetParameters()[0].ParameterType;
            compilationOptions = (CSharpCompilationOptions)withTopLevelBinderFlagsMethod.Invoke(
                compilationOptions,
            new object[] { binderFlagsType.GetField("IgnoreCorLibraryDuplicatedTypes").GetValue(binderFlagsType) });

            var compilation = CSharpCompilation.Create(assemblyName)
                .WithOptions(compilationOptions)
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTrees);
            var emitResult = compilation.Emit(assemblyPath, null);
            if (!emitResult.Success)
            {
                throw new Exception(string.Join("\r\n",
                    emitResult.Diagnostics.Where(d => d.WarningLevel == 0)));
            }
        }

		protected void LoadAssembliesFromUsings(IList<SyntaxTree> syntaxTrees)
        {
            foreach (var tree in syntaxTrees)
            {
                foreach (var usingSyntax in ((CompilationUnitSyntax)tree.GetRoot()).Usings)
                {
                    var name = usingSyntax.Name;
                    var names = new List<string>();
                    while (name != null)
                    {
                        if (name is QualifiedNameSyntax)
                        {
                            var qualifiedName = (QualifiedNameSyntax)name;
                            var identifierName = (IdentifierNameSyntax)qualifiedName.Right;
                            names.Add(identifierName.Identifier.Text);
                            name = qualifiedName.Left;
                        }
                        else if (name is IdentifierNameSyntax)
                        {
                            var identifierName = (IdentifierNameSyntax)name;
                            names.Add(identifierName.Identifier.Text);
                            name = null;
                        }
                    }
                    if (names.Contains("src"))
                    {
                        continue;
                    }
                    names.Reverse();
                    for (int c = 1; c <= names.Count; ++c)
                    {
                        var usingName = string.Join(".", names.Take(c));
                        if (LoadedNamespaces.Contains(usingName))
                        {
                            continue;
                        }
                        try
                        {
                           Load(usingName);
                        }
                        catch
                        {
                        }
                        LoadedNamespaces.Add(usingName);
                    }
                }
            }
        }

        private IModel InitModel(IServiceProvider serviceProvider, Action<ModelBuilder> OnModelCreating, DbContext context)
        {
            var Dependencies = serviceProvider.GetRequiredService<ModelSourceDependencies>();
            var conventionSetBuilder = serviceProvider.GetService<IConventionSetBuilder>();
            var validator = serviceProvider.GetRequiredService<IModelValidator>();

            var conventionSet = conventionSetBuilder.AddConventions(Dependencies.CoreConventionSetBuilder.CreateConventionSet());

            var modelBuilder = new ModelBuilder(conventionSet);

            var internalModelBuilder = ((IInfrastructure<InternalModelBuilder>)modelBuilder).Instance;

            internalModelBuilder.Metadata.SetProductVersion(ProductInfo.GetVersion());

            OnModelCreating(modelBuilder);

            Dependencies.ModelCustomizer.Customize(modelBuilder, context);

            internalModelBuilder.Validate();

            validator.Validate(modelBuilder.Model);
            return modelBuilder.Model;
        }

        /// <summary>
        /// 创建数据库，存在则不处理
        /// </summary>
        private void EnsureCreated()
        {
            //数据库不存在就创建
            using (var context = new DbContext(DbOptions.DbOptions))
            {
                context.Database.EnsureCreated();
            }
        }

        private Assembly Load(string name)
        {
            var assembly = AssemblyContext.LoadFromAssemblyName(new AssemblyName(name));
            return HandleLoadedAssembly(assembly);
        }

        /// <summary>
		/// Load assembly by name object<br/>
		/// 根据名称对象加载程序集<br/>
		/// </summary>
		private Assembly Load(AssemblyName assemblyName)
        {
            var assembly = AssemblyContext.LoadFromAssemblyName(assemblyName);
            return HandleLoadedAssembly(assembly);
        }
        private Assembly HandleLoadedAssembly(Assembly assembly)
        {
            if (LoadedAssemblies.Contains(assembly))
            {
                return assembly;
            }
            LoadedAssemblies.Add(assembly);
            
            foreach (var dependentAssemblyname in assembly.GetReferencedAssemblies())
            {
                Assembly dependentAssembly;
                try
                {
                    dependentAssembly = Load(dependentAssemblyname);
                }
                catch
                {
                    continue;
                }
                HandleLoadedAssembly(dependentAssembly);
            }
            return assembly;
        }
        private class LoadContext : AssemblyLoadContext
        {
            protected override Assembly Load(AssemblyName assemblyName)
            {
                return Assembly.Load(assemblyName);
            }
        }
    }

    public class MigrateTableOptions
    {
        public DbContextOptions DbOptions { get; set; }
    }
}
