using System;
using Microsoft.Extensions.DependencyInjection;

namespace NumberedEntity.Models
{
    public static class DependencyInjection
    {
        public static IServiceCollection WithNumbering(this IServiceCollection services,
            Action<NumberingOptions> options)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (options == null) throw new ArgumentNullException(nameof(options));

            services.Configure(options);

            return services;
        }
    }
}