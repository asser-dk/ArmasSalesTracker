namespace Asser.ArmasSalesTracker.Configuration
{
    using Ninject.Extensions.Conventions;
    using Ninject.Modules;

    public class ArmasSalesTrackerModule : NinjectModule
    {
        public override void Load()
        {
            Kernel.Bind(x => x.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
        }
    }
}
