using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace CoApp.VSE.Core
{
    public class CoAppApplication : Application
    {
        private const string COAPP_PUBLIC_KEY_TOKEN = "1e373a58e25250cb";

        public CoAppApplication()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);
        }

        public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assmName = new AssemblyName(args.Name);

            var r = GetBestAssemblyLoaded(args);
            return r;
        }

        private static Assembly GetBestAssemblyLoaded(ResolveEventArgs args)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assmName = new AssemblyName(args.Name);

            var test = FilterOnName(loadedAssemblies, assmName);

            var count = test.Count();
            if (count == 0)
                return null;
            if (count == 1)
                return test.First();
            
            test = FilterOnVersion(test, assmName);

            count = test.Count();
            if (count == 0)
                return null;
            if (count == 1)
                return test.First();

            test = FilterOnPkt(test, assmName);

            count = test.Count();
            if (count == 0)
                return test.FirstOrDefault(a => PublicKeyTokenToString(a.GetName().GetPublicKeyToken()) == COAPP_PUBLIC_KEY_TOKEN);
            if (count == 1)
                return test.First();
            
            test = FilterOnCulture(test, assmName);

            count = test.Count();
            if (count == 0)
                return null;
            if (count == 1)
                return test.First();

            return null;
        }

        private static IEnumerable<Assembly> FilterOnName(IEnumerable<Assembly> assemblies, AssemblyName assmName)
        {
            return assemblies.Where(a => a.GetName().Name == assmName.Name);
        }

        private static IEnumerable<Assembly> FilterOnVersion(IEnumerable<Assembly> assemblies, AssemblyName assmName)
        {
            if (assmName.Version != null)
            {
                var r = assemblies.Where(a => a.GetName().Version == assmName.Version);
                if (!r.Any())
                    return assemblies;
                else
                    return r;
            }
            
            return assemblies;
        }

        private static IEnumerable<Assembly> FilterOnPkt(IEnumerable<Assembly> assemblies, AssemblyName assmName)
        {
            if (assmName.GetPublicKeyToken() != null)
            {
                var r = assemblies.Where(a => a.GetName().GetPublicKeyToken().SequenceEqual(assmName.GetPublicKeyToken()));
                if (!r.Any())
                    return assemblies;
                else
                    return r;
            }
            
            return assemblies;
        }

        private static IEnumerable<Assembly> FilterOnCulture(IEnumerable<Assembly> assemblies, AssemblyName assmName)
        {
            if (assmName.CultureInfo != null)
            {
                var r = assemblies.Where(a => a.GetName().CultureInfo == assmName.CultureInfo);
                if (!r.Any())
                    return assemblies;
                else
                    return r;
            }

            return assemblies;
        }

        private static string PublicKeyTokenToString(byte[] pkt)
        {
            return pkt.Select(x => x.ToString("x2")).Aggregate((x, y) => x + y);
        }
    }
}