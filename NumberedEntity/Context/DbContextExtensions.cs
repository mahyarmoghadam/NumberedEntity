using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace NumberedEntity.Context
{
    public static class DbContextExtensions
    {
        public static IReadOnlyList<EntityEntry> FindChangedEntries(this DbContext context)
        {
            return context.ChangeTracker.Entries()
                .Where(x =>
                    x.State == EntityState.Added ||
                    x.State == EntityState.Modified ||
                    x.State == EntityState.Deleted)
                .ToList();
        }

        public static T PropertyValue<T>(this DbContext dbContext, object entity, string propertyName)
            where T : IConvertible
        {
            var value = dbContext.Entry(entity).Property(propertyName).CurrentValue;
            return value != null ? value.To<T>() : default;
        }

        public static void AcquireDistributedLock(this AppDBContext context, string resource)
        {
            context.ExecuteSqlRawCommand(@"EXEC sp_getapplock @Resource={0}, @LockOwner={1}, 
                        @LockMode={2} , @LockTimeout={3};", resource, "Transaction", "Exclusive", 15000);
        }
    }
}