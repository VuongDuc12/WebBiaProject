using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace WebBiaProject.Services
{
    public static class RegisterBusinessLayer
    {
        public static IServiceCollection AddBusinessService(this IServiceCollection services)
        {
            var assembly = Assembly.GetAssembly(typeof(RegisterBusinessLayer));

            #region Register Handler Services
            var classes = assembly!.ExportedTypes.Where(a => !a.IsInterface && a.Name.EndsWith("Service")).ToList();

            foreach (Type @class in classes)
            {
                foreach (var @interface in @class.GetInterfaces())
                {
                    services.AddScoped(@interface, @class);
                }
            }
            #endregion

            return services;
        }
    }
}
