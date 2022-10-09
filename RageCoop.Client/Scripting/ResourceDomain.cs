using RageCoop.Core;
using Console = GTA.Console;
using SHVDN;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace RageCoop.Client.Scripting
{
    internal class ResourceDomain : MarshalByRefObject, IDisposable
    {
        public static ConcurrentDictionary<string, ResourceDomain> LoadedDomains => new ConcurrentDictionary<string, ResourceDomain>(_loadedDomains);
        static readonly ConcurrentDictionary<string, ResourceDomain> _loadedDomains = new ConcurrentDictionary<string, ResourceDomain>();
        public static ScriptDomain PrimaryDomain;
        public string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private ScriptDomain CurrentDomain => ScriptDomain.CurrentDomain;
        private ResourceDomain(ScriptDomain primary, string[] apiPaths)
        {
            foreach (var apiPath in apiPaths)
            {
                try
                {
                    Assembly.LoadFrom(apiPath);
                }
                catch
                {

                }
            }
            PrimaryDomain = primary;

            // Bridge to current ScriptDomain
            primary.Tick += Tick;
            primary.KeyEvent += KeyEvent;
            AppDomain.CurrentDomain.SetData("Primary", false);
            Console.WriteLine("Loaded scondary domain: " + AppDomain.CurrentDomain.Id + " " + Util.IsPrimaryDomain);
        }
        public static bool IsLoaded(string dir)
        {
            return _loadedDomains.ContainsKey(Path.GetFullPath(dir).ToLower());
        }
        public void SetupScripts()
        {
            foreach(var s in ScriptDomain.CurrentDomain.RunningScripts)
            {
            }
        }
        public static ResourceDomain Load(string dir = @"RageCoop\Scripts\Debug")
        {
            lock (_loadedDomains)
            {
                dir = Path.GetFullPath(dir).ToLower();
                if (!Util.IsPrimaryDomain)
                {
                    throw new InvalidOperationException("Cannot load in another domain");
                }
                if (IsLoaded(dir))
                {
                    throw new Exception("Already loaded");
                }
                ScriptDomain domain = null;
                try
                {
                    dir = Path.GetFullPath(dir);
                    Directory.CreateDirectory(dir);

                    // Copy test script
                    // File.Copy(@"M:\SandBox-Shared\repos\RAGECOOP\RAGECOOP-V\bin\Debug\TestScript.dll", Path.Combine(dir, Path.GetFileName("TestScript.dll")), true);

                    // Load domain in main thread
                    Main.QueueToMainThread(() =>
                    {

                        var api = new List<string>();
                        api.Add(typeof(ResourceDomain).Assembly.Location);
                        api.AddRange(typeof(ResourceDomain).Assembly.GetReferencedAssemblies()
                            .Select(x => Assembly.Load(x.FullName).Location)
                            .Where(x => !string.IsNullOrEmpty(x)));

                        domain = ScriptDomain.Load(Directory.GetParent(typeof(ScriptDomain).Assembly.Location).FullName, dir);
                        domain.AppDomain.SetData("Console", ScriptDomain.CurrentDomain.AppDomain.GetData("Console"));
                        domain.AppDomain.SetData("RageCoop.Client.API", API.GetInstance());
                        _loadedDomains.TryAdd(dir, (ResourceDomain)domain.AppDomain.CreateInstanceFromAndUnwrap(typeof(ResourceDomain).Assembly.Location, typeof(ResourceDomain).FullName, false, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { ScriptDomain.CurrentDomain, api.ToArray() }, null, null));
                        domain.Start();
                    });

                    // Wait till next tick
                    GTA.Script.Yield();
                    return _loadedDomains[dir];
                }
                catch (Exception ex)
                {
                    GTA.UI.Notification.Show(ex.ToString());
                    Main.Logger.Error(ex);
                    if (domain != null)
                    {
                        ScriptDomain.Unload(domain);
                    }
                    throw;
                }
            }
        }

        public static void Unload(ResourceDomain domain)
        {
            lock (_loadedDomains)
            {
                Main.QueueToMainThread(() =>
                {
                    domain.Dispose();
                    ScriptDomain.Unload(domain.CurrentDomain);
                    _loadedDomains.TryRemove(domain.BaseDirectory, out _);
                });
                GTA.Script.Yield();
            }
        }
        public static void Unload(string dir)
        {
            Unload(_loadedDomains[Path.GetFullPath(dir).ToLower()]);
        }
        public static void UnloadAll()
        {
            lock (_loadedDomains)
            {
                foreach (var d in _loadedDomains.Values.ToArray())
                {
                    Unload(d);
                }
            }
        }

        private void Tick(object sender, EventArgs args)
        {
            CurrentDomain.DoTick();
        }

        private void KeyEvent(Keys keys, bool status)
        {
            CurrentDomain.DoKeyEvent(keys, status);
        }

        public void Dispose()
        {
            PrimaryDomain.Tick -= Tick;
            PrimaryDomain.KeyEvent -= KeyEvent;
        }
    }
}
