using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITAJira
{
    public interface ITransient { }
    public interface ISingleton { }

    internal class VMLoader
    {
        private static readonly IServiceProvider Provider;

        static VMLoader()
        {
            ServiceCollection services = new();

            services.Scan(el =>
                el.FromAssemblyOf<ITransient>()
                    .AddClasses(cl => cl.AssignableTo<ITransient>()).AsSelf().WithTransientLifetime()
                    .AddClasses(cl => cl.AssignableTo<ISingleton>()).AsSelf().WithSingletonLifetime()
                );

            Provider = services.BuildServiceProvider();

            foreach (ServiceDescriptor service in services.Where(el => el.Lifetime == ServiceLifetime.Singleton))
                Provider.GetRequiredService(service.ServiceType);
        }

        public static T? Resolve<T>() => Provider.GetService<T>();

        public ViewModels.MainViewModel? MainViewModel { get => Resolve<ViewModels.MainViewModel>(); }
    }
}
