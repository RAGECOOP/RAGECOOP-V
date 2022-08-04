using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core;
using System.IO;
using System.Reflection;

namespace RageCoop.Client.Scripting
{
    internal class ResourceDomain:MarshalByRefObject
    {
        public AppDomain Domain;
        public ResourceDomain()
        {

        }
        public static void CleanUp()
        {
            foreach(var d in Util.GetAppDomains())
            {
                if (d.FriendlyName.StartsWith("RageCoop"))
                {
                    AppDomain.Unload(d);
                }
            }
        }
        public static ResourceDomain Create()
        {
            // AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Resolve);

            AppDomainSetup ads = new AppDomainSetup
            {
                ApplicationBase = Directory.GetParent(typeof(ResourceDomain).Assembly.Location).FullName,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile =
                AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ShadowCopyFiles = "true",
            };

            // Create the second AppDomain.
            AppDomain ad = AppDomain.CreateDomain("RageCoop.ResourceDomain", null, ads);
            ad.AssemblyResolve+=Resolve;
            // ad.AssemblyResolve+=Resolve;
            ResourceDomain domain=(ResourceDomain)ad.CreateInstanceAndUnwrap(
                typeof(ResourceDomain).Assembly.FullName,
                typeof(ResourceDomain).FullName
            );
            domain.Domain=ad;
            Main.Logger.Debug("Hi:"+domain.Hi());
            return domain;
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var refed=typeof(ResourceDomain).Assembly.GetReferencedAssemblies();
            if (args.Name==typeof(SHVDN.ScriptDomain).Assembly.FullName)
            {
                return typeof(SHVDN.ScriptDomain).Assembly;
            }
            else if (args.Name==typeof(GTA.Native.Function).Assembly.FullName)
            {
                return typeof(GTA.Native.Function).Assembly;
            }
            foreach (var a in refed)
            {
                if (a.Name.Equals(assemblyName))
                {
                    return Assembly.Load(a);
                }
            }
            if (assemblyName.Equals(typeof(ResourceDomain).Assembly.FullName))
            {
                return typeof(ResourceDomain).Assembly;
            }
            return null;
        }

        public static void Destroy(ResourceDomain d)
        {
            AppDomain.Unload(d.Domain);
        }
        public string Hi()
        {
            GTA.UI.Notification.Show("Hi");
            return AppDomain.CurrentDomain.FriendlyName;
        }
    }
}
