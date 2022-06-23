using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;
using System.IO;
using Ionic.Zip;
using System.Reflection;
namespace RageCoop.Server.Scripting
{
	internal class Resources : ResourceLoader
	{
		public Resources() : base("RageCoop.Server.Scripting.ServerScript", Program.Logger) { }

		public static bool HasClientResources = false;
		public void LoadAll()
		{
            #region CLIENT
            var path = Path.Combine("Resources", "Client");
			Directory.CreateDirectory(path);
			var clientResources = Directory.GetDirectories(path);
			if (clientResources.Length!=0)
			{
				// Pack client side resources as a zip file
				Logger.Info("Packing client-side resources");

				try
				{
					var zippath = Path.Combine(path, "Resources.zip");
					if (File.Exists(zippath))
					{
						File.Delete(zippath);
					}
					using (ZipFile zip = new ZipFile())
					{
						zip.AddDirectory(path);
						zip.Save(zippath);
					}
					HasClientResources=true;
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to pack client resources");
					Logger.Error(ex);
				}
			}
            #endregion

            #region SERVER
            path = Path.Combine("Resources", "Server");
			Directory.CreateDirectory(path);
			foreach (var resource in Directory.GetDirectories(path))
			{
				Logger.Info($"Loading resource: {Path.GetFileName(resource)}");
				LoadResource(resource);
			}

			// Start scripts
            lock (LoadedResources)
            {
				foreach (var d in LoadedResources)
				{
					foreach (var s in d.Scripts)
					{
						(s as ServerScript).CurrentResource = d;
                        try
						{
							s.OnStart();
						}
						catch(Exception ex) {Logger.Error($"Failed to start resource: {d.Name}"); Logger.Error(ex); }
					}
				}
			}
            #endregion
        }

        public void StopAll()
		{
			lock (LoadedResources)
			{
				foreach (var d in LoadedResources)
				{
					foreach (var s in d.Scripts)
					{
                        try
                        {

							s.OnStop();
						}
						catch(Exception ex)
                        {
							Logger?.Error(ex);
                        }
					}
				}
			}
		}
		public void SendTo(Client client)
		{

			string path;
			if (HasClientResources && File.Exists(path = Path.Combine("Resources", "Client", "Resources.zip")))
			{
				Task.Run(() =>
				{
					Logger.Info($"Sending resources to client:{client.Username}");
					Server.SendFile(path, "Resources.zip", client);
					Logger.Info($"Resources sent to:{client.Username}");

				});
			}
            else
            {
				client.IsReady=true;
            }
		}
	}
}
