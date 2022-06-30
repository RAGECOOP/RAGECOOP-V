using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Reflection;
namespace RageCoop.Server.Scripting
{
	internal class Resources : ResourceLoader
	{
		private readonly Server Server;
		public Resources(Server server) : base("RageCoop.Server.Scripting.ServerScript", server.Logger)
		{
			Server = server;
		}
		private List<string> ClientResourceZips=new List<string>();
		public void LoadAll()
		{
			// Client
            {
				var path = Path.Combine("Resources", "Client");
				var tmpDir = Path.Combine("Resources", "Temp");
				Directory.CreateDirectory(path);
				if (Directory.Exists(tmpDir))
				{
					Directory.Delete(tmpDir, true);
				}
				Directory.CreateDirectory(tmpDir);
				var resourceFolders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
				if (resourceFolders.Length!=0)
				{
					foreach (var resourceFolder in resourceFolders)
					{
						// Pack client side resource as a zip file
						Logger?.Info("Packing client-side resource: "+resourceFolder);
						var zipPath = Path.Combine(tmpDir, Path.GetFileName(resourceFolder))+".zip";
						try
						{
							using (ZipFile zip = ZipFile.Create(zipPath))
							{
								zip.BeginUpdate();
								foreach (var dir in Directory.GetDirectories(resourceFolder, "*", SearchOption.AllDirectories))
								{
									zip.AddDirectory(dir.Substring(resourceFolder.Length+1));
								}
								foreach (var file in Directory.GetFiles(resourceFolder, "*", SearchOption.AllDirectories))
								{
									zip.Add(file, file.Substring(resourceFolder.Length+1));
								}
								zip.CommitUpdate();
								zip.Close();
								ClientResourceZips.Add(zipPath);
							}
						}
						catch (Exception ex)
						{
							Logger?.Error($"Failed to pack client resource:{resourceFolder}");
							Logger?.Error(ex);
						}
					}
				}
				var packed = Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly);
				if (packed.Length>0)
				{
					ClientResourceZips.AddRange(packed);
				}
			}

			// Server
            {
				var path = Path.Combine("Resources", "Server");
				var dataFolder = Path.Combine(path, "data");
				Directory.CreateDirectory(path);
				foreach (var resource in Directory.GetDirectories(path))
				{
					if (Path.GetFileName(resource).ToLower()=="data") { continue; }
					Logger?.Info($"Loading resource: {Path.GetFileName(resource)}");
					LoadResource(resource, dataFolder);
				}
				foreach (var resource in Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly))
				{
					Logger?.Info($"Loading resource: {Path.GetFileName(resource)}");
					LoadResource(new ZipFile(resource), dataFolder);
				}

				// Start scripts
				lock (LoadedResources)
				{
					foreach (var r in LoadedResources)
					{
						foreach (ServerScript s in r.Scripts)
						{
							s.API=Server.API;
							try
							{
								Logger?.Debug("Starting script:"+s.CurrentFile.Name);
								s.OnStart();
							}
							catch (Exception ex) { Logger?.Error($"Failed to start resource: {r.Name}"); Logger?.Error(ex); }
						}
					}
				}
			}
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

			if (ClientResourceZips.Count!=0)
			{
				Task.Run(() =>
				{
					Logger?.Info($"Sending resources to client:{client.Username}");
					
					
					Logger?.Info($"Resources sent to:{client.Username}");

				});
			}
            else
            {
				client.IsReady=true;
            }
		}
	}
}
