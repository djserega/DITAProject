using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

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

            Logger.Inf("Complited");
        }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        public static T? Resolve<T>() => Provider.GetRequiredService<T>();
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

#pragma warning disable CA1822 // Mark members as static
        public ViewModels.MainViewModel? MainViewModel { get => Resolve<ViewModels.MainViewModel>(); }
        public ViewModels.ReportPage? ReportPage { get => Resolve<ViewModels.ReportPage>(); }
#pragma warning restore CA1822 // Mark members as static
    }
}
