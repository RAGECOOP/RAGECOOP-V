using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core.Scripting;
using RageCoop.Core;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Reflection;
using McMaster.NETCore.Plugins;
namespace RageCoop.Server.Scripting
{
	internal class Resources
	{
		private Dictionary<string,ServerResource> LoadedResources=new();
		private readonly Server Server;
		private readonly Logger Logger;
		public Resources(Server server)
		{
			Server = server;
			Logger=server.Logger;
		}
		private List<string> ClientResourceZips=new List<string>();
		public void LoadAll()
		{
			// Client
            {
				var path = Path.Combine("Resources", "Client");
				var tmpDir = Path.Combine("Resources", "Temp","Client");
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
						var zipPath = Path.Combine(tmpDir, Path.GetFileName(resourceFolder))+".res";
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
				var packed = Directory.GetFiles(path, "*.res", SearchOption.TopDirectoryOnly);
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
                    try
                    {
						var name = Path.GetFileName(resource);
						if (LoadedResources.ContainsKey(name))
                        {
							Logger?.Warning($"Resource \"{name}\" has already been loaded, ignoring...");
							continue;
						}
						if (name.ToLower()=="data") { continue; }
						Logger?.Info($"Loading resource: {name}");
						var r = ServerResource.LoadFrom(resource, dataFolder, Logger);
						LoadedResources.Add(r.Name, r);
					}
                    catch(Exception ex)
                    {
						Logger?.Error($"Failed to load resource: {Path.GetFileName(resource)}");
						Logger?.Error(ex);
					}
				}
				foreach (var resource in Directory.GetFiles(path, "*.res", SearchOption.TopDirectoryOnly))
				{
                    try
                    {
						var name = Path.GetFileNameWithoutExtension(resource);
						if (LoadedResources.ContainsKey(name))
						{
							Logger?.Warning($"Resource \"{name}\" has already been loaded, ignoring...");
							continue;
						}
						Logger?.Info($"Loading resource: name");
						var r = ServerResource.LoadFromZip(resource, Path.Combine("Resources", "Temp", "Server"), dataFolder, Logger);
						LoadedResources.Add(r.Name, r);
					}
					catch(Exception ex)
                    {
						Logger?.Error($"Failed to load resource: {Path.GetFileNameWithoutExtension(resource)}");
						Logger?.Error(ex);
                    }
				}

				// Start scripts
				lock (LoadedResources)
				{
					foreach (var r in LoadedResources.Values)
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

        public void UnloadAll()
		{
			lock (LoadedResources)
			{
				foreach (var d in LoadedResources.Values)
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
                    try
                    {
						d.Dispose();
					}
					catch(Exception ex)
                    {
						Logger.Error($"Resource \"{d.Name}\" cannot be unloaded.");
						Logger.Error(ex);
                    }
				}
				LoadedResources.Clear();
			}
		}
		public void SendTo(Client client)
		{
			Task.Run(() =>
			{

				if (ClientResourceZips.Count!=0)
				{
					Logger?.Info($"Sending resources to client:{client.Username}");
					foreach (var rs in ClientResourceZips)
					{
						using (var fs = File.OpenRead(rs))
						{
							Server.SendFile(rs, Path.GetFileName(rs), client);
						}
					}

					Logger?.Info($"Resources sent to:{client.Username}");
				}
				if (Server.GetResponse<Packets.FileTransferResponse>(client, new Packets.AllResourcesSent())?.Response==FileResponse.Loaded)
				{
					client.IsReady=true;
					Server.API.Events.InvokePlayerReady(client);
				}
                else
                {
					Logger?.Warning($"Client {client.Username} failed to load resource.");
                }
			});
		}
	}
}
