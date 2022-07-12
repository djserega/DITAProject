using Microsoft.Extensions.DependencyInjection;
using System;

namespace ITAJira
{
    public interface ITransient { }
    public interface ISingleton { }

    internal class VMLoader
    {
        private static readonly IServiceProvider Provider;

        static VMLoader()
        {
            Logger.Inf("Initializing loader");

            ServiceCollection services = new();

            services.Scan(el =>
                el.FromAssemblyOf<ITransient>()
                    .AddClasses(cl => cl.AssignableTo<ITransient>()).AsSelf().WithTransientLifetime()
                    .AddClasses(cl => cl.AssignableTo<ISingleton>()).AsSelf().WithSingletonLifetime()
                );

            Provider = services.BuildServiceProvider();

            //foreach (ServiceDescriptor service in services.Where(el => el.Lifetime == ServiceLifetime.Singleton))
            //    Provider.GetRequiredService(service.ServiceType);

            Logger.Inf("Complited");
        }

        public static T? Resolve<T>() => Provider.GetRequiredService<T>();

        public ViewModels.MainViewModel? MainViewModel { get => Resolve<ViewModels.MainViewModel>(); }
        public ViewModels.ReportPage? ReportPage { get => Resolve<ViewModels.ReportPage>(); }
    }
}
