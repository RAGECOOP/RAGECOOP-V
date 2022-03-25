using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.ClearScript.V8;

using GTA;

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

            if (LoadedEngine)
            {
                return;
            }

            ScriptEngines = new List<V8ScriptEngine>();

            if (!Directory.Exists("scripts\\resources"))
            {
                try
                {
                    Directory.CreateDirectory("scripts\\resources");
                }
                catch (Exception ex)
                {
                    // TODO
                }
            }

            foreach (string script in Directory.GetFiles("scripts\\resources", "*.js"))
            {
                V8ScriptEngine engine = new V8ScriptEngine();

                engine.AddHostObject("script", new ScriptContext());
                
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

            LoadedEngine = true;
        }
    }

    /// <summary>
    /// FOR JAVASCRIPT ONLY!
    /// </summary>
    public class ScriptContext
    {
        /// <summary>
        /// Don't use this!
        /// </summary>
        public void SendMessage(string message)
        {
            Main.MainChat.AddMessage("JAVASCRIPT", message);
        }
    }
}
