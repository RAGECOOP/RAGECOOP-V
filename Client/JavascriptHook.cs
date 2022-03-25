using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

using GTA;
using GTA.Native;

namespace CoopClient
{
    /// <summary>
    /// Don't use this!
    /// </summary>
    public class JavascriptHook : Script
    {
        private bool LoadedEngine = false;

        private static List<V8ScriptEngine> ScriptEngines;

        /// <summary>
        /// Don't use this!
        /// </summary>
        public JavascriptHook()
        {
            Tick += Ontick;
        }

        private void Ontick(object sender, EventArgs e)
        {
            if (!Main.MainNetworking.IsOnServer())
            {
                return;
            }

            if (!LoadedEngine)
            {
                LoadedEngine = true;

                ScriptEngines = new List<V8ScriptEngine>();

                string serverAddress = Main.MainSettings.LastServerAddress.Replace(":", ".");

                if (!Directory.Exists("scripts\\resources\\" + serverAddress))
                {
                    try
                    {
                        Directory.CreateDirectory("scripts\\resources\\" + serverAddress);
                    }
                    catch (Exception ex)
                    {
                        // TODO
                    }
                }

                foreach (string script in Directory.GetFiles("scripts\\resources\\" + serverAddress, "*.js"))
                {
                    V8ScriptEngine engine = new V8ScriptEngine();

                    engine.AddHostObject("SHV", new HostTypeCollection(Assembly.LoadFrom("ScriptHookVDotNet3.dll")));
                    engine.AddHostObject("LemonUI", new HostTypeCollection(Assembly.LoadFrom("scripts\\LemonUI.SHVDN3.dll")));
                    engine.AddHostObject("API", new ScriptContext());

                    try
                    {
                        engine.Execute(File.ReadAllText(script));
                    }
                    catch (Exception ex)
                    {
                        // TODO
                    }
                    finally
                    {
                        ScriptEngines.Add(engine);
                    }
                }
            }

            ScriptEngines.ForEach(engine => engine.Script.API.InvokeRender());
        }
    }

    /// <summary>
    /// FOR JAVASCRIPT ONLY!
    /// </summary>
    public class ScriptContext
    {
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler OnRender;

        internal void InvokeRender()
        {
            OnRender?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="args"></param>
        public void CallNative(string hash, params object[] args)
        {
            if (!Hash.TryParse(hash, out Hash ourHash))
            {
                return;
            }

            Function.Call(ourHash, args.Select(o => new InputArgument(o)).ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            Main.MainChat.AddMessage("JAVASCRIPT", message);
        }
    }
}
