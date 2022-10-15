using SHVDN;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Console = GTA.Console;

namespace RageCoop.Client.Loader
{

    public class LoaderContext : MarshalByRefObject, IDisposable
    {
        #region PRIMARY-LOADING-LOGIC
        public static ConcurrentDictionary<string, LoaderContext> LoadedDomains => new ConcurrentDictionary<string, LoaderContext>(_loadedDomains);

        private static readonly ConcurrentDictionary<string, LoaderContext> _loadedDomains = new ConcurrentDictionary<string, LoaderContext>();

        public bool UnloadRequested;
        public string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private ScriptDomain CurrentDomain => ScriptDomain.CurrentDomain;
        public static void CheckForUnloadRequest()
        {
            lock (_loadedDomains)
            {
                foreach (var p in _loadedDomains.Values)
                {
                    if (p.UnloadRequested)
                    {
                        Unload(p);
                    }
                }
            }
        }

        public static bool IsLoaded(string dir)
        {
            return _loadedDomains.ContainsKey(Path.GetFullPath(dir).ToLower());
        }
        public static LoaderContext Load(string dir)
        {
            lock (_loadedDomains)
            {
                dir = Path.GetFullPath(dir).ToLower();
                if (IsLoaded(dir))
                {
                    throw new Exception("Already loaded");
                }
                ScriptDomain newDomain = null;
                try
                {
                    dir = Path.GetFullPath(dir).ToLower();
                    Directory.CreateDirectory(dir);
                    Exception e = null;
                    // Load domain in main thread
                    Main.QueueToMainThread(() =>
                    {
                        try
                        {
                            /*
                            var assemblies = new List<string>();
                            assemblies.Add(typeof(DomainLoader).Assembly.Location);
                            assemblies.AddRange(typeof(DomainLoader).Assembly.GetReferencedAssemblies()
                                .Select(x => Assembly.Load(x.FullName).Location)
                                .Where(x => !string.IsNullOrEmpty(x)));
                            */

                            // Delete API assemblies
                            Directory.GetFiles(dir, "ScriptHookVDotNet*", SearchOption.AllDirectories).ToList().ForEach(x => File.Delete(x));
                            var ctxAsm = Path.Combine(dir, "RageCoop.Client.Loader.dll");
                            if (File.Exists(ctxAsm)) { File.Delete(ctxAsm); }

                            newDomain = ScriptDomain.Load(
                                Directory.GetParent(typeof(ScriptDomain).Assembly.Location).FullName, dir);
                            newDomain.AppDomain.SetData("Primary", ScriptDomain.CurrentDomain);
                            newDomain.AppDomain.SetData("Console", ScriptDomain.CurrentDomain.AppDomain.GetData("Console"));
                            var context = (LoaderContext)newDomain.AppDomain.CreateInstanceFromAndUnwrap(
                                typeof(LoaderContext).Assembly.Location,
                                typeof(LoaderContext).FullName, ignoreCase: false,
                                BindingFlags.Instance | BindingFlags.NonPublic,
                                null,
                                new object[] { }
                                , null, null);
                            newDomain.AppDomain.SetData("RageCoop.Client.LoaderContext", context);
                            newDomain.Start();
                            _loadedDomains.TryAdd(dir, context);
                        }
                        catch (Exception ex)
                        {
                            e = ex;
                        }
                    });
                    // Wait till next tick
                    GTA.Script.Yield();
                    if (e != null) { throw e; }
                    return _loadedDomains[dir];
                }
                catch (Exception ex)
                {
                    GTA.UI.Notification.Show(ex.ToString());
                    Console.Error(ex);
                    if (newDomain != null)
                    {
                        ScriptDomain.Unload(newDomain);
                    }
                    throw;
                }
            }
        }

        public static void Unload(LoaderContext domain)
        {
            lock (_loadedDomains)
            {
                Exception ex = null;
                var name = domain.CurrentDomain.Name;
                Console.Info("Unloading domain: " + name);
                Main.QueueToMainThread(() =>
                {
                    try
                    {
                        if (!_loadedDomains.TryRemove(domain.BaseDirectory.ToLower(), out _))
                        {
                            throw new Exception("Failed to remove domain from list");
                        }
                        domain.Dispose();
                        ScriptDomain.Unload(domain.CurrentDomain);
                    }
                    catch (Exception e)
                    {
                        ex = e;
                        GTA.UI.Notification.Show(ex.ToString());
                    }
                });
                GTA.Script.Yield();
                if (ex != null)
                {
                    throw ex;
                }
                Console.Info("Unloaded domain: " + name);
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
        #endregion

        #region LOAD-CONTEXT        


        private LoaderContext()
        {
            AppDomain.CurrentDomain.DomainUnload += (s, e) => Dispose();
            PrimaryDomain.Tick += Tick;
            PrimaryDomain.KeyEvent += KeyEvent;
            Console.Info($"Loaded domain: {AppDomain.CurrentDomain.FriendlyName}, {AppDomain.CurrentDomain.BaseDirectory}");
        }
        public static ScriptDomain PrimaryDomain => AppDomain.CurrentDomain.GetData("Primary") as ScriptDomain;
        public static LoaderContext CurrentContext => AppDomain.CurrentDomain.GetData("RageCoop.Client.LoaderContext") as LoaderContext;
        /// <summary>
        /// Request the current domain to be unloaded
        /// </summary>
        public static void RequestUnload()
        {
            if (PrimaryDomain == null)
            {
                throw new NotSupportedException("Current domain not loaded by the loader therfore cannot be unloaded automatically");
            }
            CurrentContext.UnloadRequested = true;
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
            lock (this)
            {
                if (PrimaryDomain == null) { return; }
                PrimaryDomain.Tick -= Tick;
                PrimaryDomain.KeyEvent -= KeyEvent;
                AppDomain.CurrentDomain.SetData("Primary", null);
            }
        }
        #endregion
    }
}
