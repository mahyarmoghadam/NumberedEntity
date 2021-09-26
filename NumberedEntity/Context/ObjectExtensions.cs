using System;
using System.ComponentModel;
using System.Globalization;

namespace NumberedEntity.Context
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Converts given object to a value or enum type using <see cref="Convert.ChangeType(object,TypeCode)"/> or <see cref="Enum.Parse(Type,string)"/> method.
        /// </summary>
        /// <param name="value">Object to be converted</param>
        /// <typeparam name="T">Type of the target object</typeparam>
        /// <returns>Converted object</returns>
        public static T To<T>(this object value)
            where T : IConvertible
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (typeof(T) == typeof(Guid))
            {
                return (T) TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value.ToString());
            }

            if (!typeof(T).IsEnum) return (T) Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);

            if (Enum.IsDefined(typeof(T), value))
            {
                return (T) Enum.Parse(typeof(T), value.ToString());
            }

            throw new ArgumentException($"Enum type undefined '{value}'.");
        }
    }
}