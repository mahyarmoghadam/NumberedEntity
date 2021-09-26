using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using NumberedEntity.Models;

namespace NumberedEntity.Context
{
    public class AppDBContext : DbContext
    {
        private readonly DbContextOptions _options;
        private readonly IOptions<NumberingOptions> _numberingOptions;

        public AppDBContext(DbContextOptions options, IOptions<NumberingOptions> numberingOptions) : base(options)
        {
            _options = options;
            _numberingOptions = numberingOptions;
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyNumberedEntityConfiguration();
            base.OnModelCreating(modelBuilder);
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            int result;

            try
            {
                var entryList = this.FindChangedEntries();

                ExecuteNumberedEntity(entryList);

                result = await base.SaveChangesAsync(true, cancellationToken);
            }
            catch (DbUpdateConcurrencyException e)
            {
                throw new DbUpdateConcurrencyException(e.Message, e);
            }
            catch (DbUpdateException e)
            {
                throw new DbUpdateException(e.Message, e);
            }

            return result;
        }
        private void ExecuteNumberedEntity(IReadOnlyList<EntityEntry> entryList)
        {
            foreach (var entry in entryList)
            {
                if (entry.Entity is INumberedEntity)
                {
                    var metadata = entry;
                    var entity = entry.Entity as INumberedEntity;

                    var options = _numberingOptions.Value[entity.GetType()].ToList();
                    foreach (var option in options)
                    {
                        if (!string.IsNullOrEmpty(this.PropertyValue<string>(entity, option.FieldName))) return;

                        bool retry;
                        string number;
                        do
                        {
                            number = NewNumber(entity, option);
                            retry = !IsUniqueNumber(entity, number, option.Fields);
                        } while (retry);

                        this.Entry(entity).Property(option.FieldName).CurrentValue = number;
                    }
                }
            }
        }
        private bool IsUniqueNumber(INumberedEntity entity, string number, IEnumerable<string> fields)
        {
            fields = fields.ToList();
            using var command = this.Database.GetDbConnection().CreateCommand();
            var parameterNames = fields.Aggregate(string.Empty,
                (current, fieldName) => $"{current} AND [t0].[{fieldName}] = @{fieldName} ");

            var tableName = this.Entry(entity).Metadata.GetTableName();
            command.CommandText = $@"SELECT
                    (CASE
                WHEN EXISTS(
                    SELECT NULL AS [EMPTY]
                        FROM [{tableName}] AS [t0]
                        WHERE [t0].[Number] = @Number {parameterNames}
                ) THEN 1
                ELSE 0
                END) [Value]";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@Number";
            parameter.Value = number;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);

            foreach (var field in fields)
            {
                var p = command.CreateParameter();

                var value = this.Entry(entity).Property(field).CurrentValue;

                p.Value = value;
                p.ParameterName = $"@{field}";
                p.DbType = SqlHelper.TypeMapping[value.GetType()];

                command.Parameters.Add(p);
            }

            if (command.Connection != null) command.Connection.Open();
            var result = command.ExecuteScalar();

            return !Convert.ToBoolean(result);
        }
        private string NewNumber(INumberedEntity entity, NumberedEntityOption option)
        {
            var key = CreateEntityKey(entity, option.Fields);

            this.AcquireDistributedLock(key);

            var number = option.Start.ToString(CultureInfo.InvariantCulture);

            var numberedEntity =
                this.Set<Models.NumberedEntity>().AsNoTracking().FirstOrDefault(a => a.EntityName == key);
            if (numberedEntity == null)
            {
                this.ExecuteSqlRawCommand(
                    query: "INSERT INTO [dbo].[NumberedEntity]([EntityName], [NextValue]) VALUES(@p0,@p1)",
                    key,
                    option.Start + option.IncrementBy
                );
            }
            else
            {
                number = numberedEntity.NextValue.ToString(CultureInfo.InvariantCulture);
                this.ExecuteSqlRawCommand(
                    "UPDATE [dbo].[NumberedEntity] SET [NextValue] = @p0 WHERE [Id] = @p1 ",
                    numberedEntity.NextValue + option.IncrementBy, numberedEntity.Id);
            }

            if (!string.IsNullOrEmpty(option.Prefix))
                number = option.Prefix + number;

            return number;
        }
        public int ExecuteSqlRawCommand(string query, params object[] parameters)
        {
            return Database.ExecuteSqlRaw(query, parameters);
        }
        private string CreateEntityKey(INumberedEntity entity, IEnumerable<string> fields)
        {
            var type = entity.GetType();

            var key = type.FullName;

            foreach (var field in fields)
            {
                var value = this.Entry(entity).Property(field).CurrentValue;
                value = NormalizeValue(value);

                key += $"_{field}_{value}";
            }

            return key;
        }
        private static object NormalizeValue(object value)
        {
            switch (value)
            {
                case DateTimeOffset dateTimeOffset:
                    value = dateTimeOffset.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                    break;
                case DateTime dateTime:
                    value = dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                    break;
            }

            return value;
        }
    }
}