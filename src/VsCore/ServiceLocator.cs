namespace CoApp.VisualStudio.VsCore
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio.ComponentModelHost;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    /// <summary>
    /// This class unifies all the different ways of getting services within visual studio.
    /// </summary>
    public static class ServiceLocator
    {
        public static TService GetInstance<TService>() where TService : class
        {
            // Special case IServiceProvider
            if (typeof(TService) == typeof(IServiceProvider))
            {
                return (TService)GetServiceProvider();
            }

            return GetDTEService<TService>() ??
                   GetComponentModelService<TService>() ??
                   GetGlobalService<TService, TService>();
        }

        public static TInterface GetGlobalService<TService, TInterface>() where TInterface : class
        {
            return (TInterface)Package.GetGlobalService(typeof(TService));
        }

        private static TService GetDTEService<TService>() where TService : class
        {
            var dte = GetGlobalService<SDTE, DTE>();
            return (TService)QueryService(dte, typeof(TService));
        }

        private static TService GetComponentModelService<TService>() where TService : class
        {
            IComponentModel componentModel = GetGlobalService<SComponentModel, IComponentModel>();
            return componentModel.GetService<TService>();
        }

        private static IServiceProvider GetServiceProvider()
        {
            var dte = GetGlobalService<SDTE, DTE>();
            return GetServiceProvider(dte);
        }

        private static object QueryService(_DTE dte, Type serviceType)
        {
            Guid guidService = serviceType.GUID;
            Guid riid = guidService;
            var serviceProvider = dte as VsServiceProvider;

            IntPtr servicePtr;
            int hr = serviceProvider.QueryService(ref guidService, ref riid, out servicePtr);

            if (hr != VsConstants.S_OK)
            {
                // We didn't find the service so return null
                return null;
            }

            object service = null;

            if (servicePtr != IntPtr.Zero)
            {
                service = Marshal.GetObjectForIUnknown(servicePtr);
                Marshal.Release(servicePtr);
            }

            return service;

        }

        private static IServiceProvider GetServiceProvider(_DTE dte)
        {
            IServiceProvider serviceProvider = new ServiceProvider(dte as VsServiceProvider);
            Debug.Assert(serviceProvider != null, "Service provider is null");
            return serviceProvider;
        }
    }
}