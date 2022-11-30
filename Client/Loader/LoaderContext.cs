using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GTA.UI;
using SHVDN;
using Console = GTA.Console;

namespace RageCoop.Client.Loader
{
    public class LoaderContext : MarshalByRefObject, IDisposable
    {
        #region PRIMARY-LOADING-LOGIC

        public static ConcurrentDictionary<string, LoaderContext> LoadedDomains =>
            new ConcurrentDictionary<string, LoaderContext>(_loadedDomains);

        private static readonly ConcurrentDictionary<string, LoaderContext> _loadedDomains =
            new ConcurrentDictionary<string, LoaderContext>();

        public bool UnloadRequested;
        public string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private ScriptDomain CurrentDomain => ScriptDomain.CurrentDomain;

        public static void CheckForUnloadRequest()
        {
            lock (_loadedDomains)
            {
                foreach (var p in _loadedDomains.Values)
                    if (p.UnloadRequested)
                        Unload(p);
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
                if (IsLoaded(dir)) throw new Exception("Already loaded");
                ScriptDomain newDomain = null;
                try
                {
                    dir = Path.GetFullPath(dir).ToLower();
                    Directory.CreateDirectory(dir);

                    // Delete API assemblies
                    Directory.GetFiles(dir, "ScriptHookVDotNet*", SearchOption.AllDirectories).ToList()
                        .ForEach(x => File.Delete(x));


                    newDomain = ScriptDomain.Load(
                        Directory.GetParent(typeof(ScriptDomain).Assembly.Location).FullName, dir);

                    newDomain.AppDomain.SetData("Primary", ScriptDomain.CurrentDomain);

                    newDomain.AppDomain.SetData("Console",
                        ScriptDomain.CurrentDomain.AppDomain.GetData("Console"));

                    // Copy to target domain base directory
                    // Delete loader assembly
                    var loaderPath = Path.Combine(dir, Path.GetFileName(typeof(LoaderContext).Assembly.Location));
                    if (File.Exists(loaderPath)) File.Delete(loaderPath);

                    var context = (LoaderContext)newDomain.AppDomain.CreateInstanceFromAndUnwrap(
                        typeof(LoaderContext).Assembly.Location,
                        typeof(LoaderContext).FullName, false,
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        new object[] { }
                        , null, null);

                    newDomain.AppDomain.SetData("RageCoop.Client.LoaderContext", context);
                    newDomain.Start();

                    _loadedDomains.TryAdd(dir, context);
                    return _loadedDomains[dir];
                }
                catch (Exception ex)
                {
                    Notification.Show(ex.ToString());
                    Console.Error(ex);
                    if (newDomain != null) ScriptDomain.Unload(newDomain);
                    throw;
                }
            }
        }

        private static Assembly ResolveLoader(object sender, ResolveEventArgs args)
        {
            Log.Message(Log.Level.Debug, "resolving assembly " + args.Name);
            if (args.Name == typeof(LoaderContext).Assembly.GetName().Name) return typeof(LoaderContext).Assembly;
            return null;
        }

        public static void Unload(LoaderContext domain)
        {
            lock (_loadedDomains)
            {
                var name = domain.CurrentDomain.Name;
                Console.Info("Unloading domain: " + name);
                if (!_loadedDomains.TryRemove(domain.BaseDirectory.ToLower(), out _))
                    throw new Exception("Failed to remove domain from list");
                domain.Dispose();
                ScriptDomain.Unload(domain.CurrentDomain);
                Console.Info("Unloaded domain: " + name);
            }
        }

        public static void Unload(string dir)
        {
            lock (_loadedDomains)
            {
                Unload(_loadedDomains[Path.GetFullPath(dir).ToLower()]);
            }
        }

        public static void TickAll()
        {
            lock (_loadedDomains)
            {
                foreach (var c in _loadedDomains.Values) c.DoTick();
            }
        }

        public static void KeyEventAll(Keys keys, bool status)
        {
            lock (_loadedDomains)
            {
                foreach (var c in _loadedDomains.Values) c.DoKeyEvent(keys, status);
            }
        }

        public static void UnloadAll()
        {
            lock (_loadedDomains)
            {
                foreach (var d in _loadedDomains.Values.ToArray()) Unload(d);
            }
        }

        #endregion

        #region LOAD-CONTEXT

        private readonly Action _domainDoTick;
        private readonly Action<Keys, bool> _domainDoKeyEvent;

        private LoaderContext()
        {
            AppDomain.CurrentDomain.DomainUnload += (s, e) => Dispose();

            var tickMethod = typeof(ScriptDomain).GetMethod("DoTick", BindingFlags.Instance | BindingFlags.NonPublic);
            var doKeyEventMethod =
                typeof(ScriptDomain).GetMethod("DoKeyEvent", BindingFlags.Instance | BindingFlags.NonPublic);

            // Create delegates to avoid using reflection to call method each time, which is slow
            _domainDoTick = (Action)Delegate.CreateDelegate(typeof(Action), CurrentDomain, tickMethod);
            _domainDoKeyEvent =
                (Action<Keys, bool>)Delegate.CreateDelegate(typeof(Action<Keys, bool>), CurrentDomain,
                    doKeyEventMethod);

            Console.Info(
                $"Loaded domain: {AppDomain.CurrentDomain.FriendlyName}, {AppDomain.CurrentDomain.BaseDirectory}");
        }

        public static ScriptDomain PrimaryDomain => AppDomain.CurrentDomain.GetData("Primary") as ScriptDomain;

        public static LoaderContext CurrentContext =>
            (LoaderContext)AppDomain.CurrentDomain.GetData("RageCoop.Client.LoaderContext");

        /// <summary>
        ///     Request the current domain to be unloaded
        /// </summary>
        public static void RequestUnload()
        {
            if (PrimaryDomain == null)
                throw new NotSupportedException(
                    "Current domain not loaded by the loader therefore cannot be unloaded automatically");
            CurrentContext.UnloadRequested = true;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void DoTick()
        {
            _domainDoTick();
        }

        public void DoKeyEvent(Keys key, bool status)
        {
            _domainDoKeyEvent(key, status);
        }

        public void Dispose()
        {
            lock (this)
            {
                if (PrimaryDomain == null) return;
                AppDomain.CurrentDomain.SetData("Primary", null);
            }
        }

        #endregion
    }
}