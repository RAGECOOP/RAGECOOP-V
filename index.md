
# Getting started

## Resources and Scripts

A **Script** stands for class that inherits from RageCoop's script class ( [ServerScript](api/RageCoop.Server.Scripting.ServerScript.html) and [ClientScript](api/RageCoop.Client.Scripting.ClientScript.html) ) and will be loaded at runtime, one assembly can have multiple scripts in it.

A **Resource** consists of one or more assemblies and other files. Server-side resource will be loaded at startup and is isolated from other resources, while client-side resource will be sent to each client and loaded after they connected to the server. A **Resource** can either be in a folder or packed inside a zip archive.


## Directory structure

Below is the server's directory structure
```
ServerRoot
│   Settings.xml   
|   RageCoop.Server.exe
│
└───Resources
    └───Server
    │   │   RageCoop.Resources.Management.zip
    │   │   RageCoop.Resources.FreeRoam.Server.zip
    │   │   
    │
    │───Client
    │   │   RageCoop.Resources.FreeRoam.Client.zip
    │   │
    │
    └───Temp
```
### Settings.xml 

This file will be generated first time you started the server, you can then change the server's configuration option by editing it, refer to [ServerSettings](api/RageCoop.Server.ServerSettings.html) for detailed description. 

## Server Reource

To create a server resource:
1. Create a C# class library project targeting .NET 6.0.
2. Add reference to **RageCoop.Server.dll** and **RageCoop.Core.dll**.
3. Add following namespace(s):
    ```
    using RageCoop.Server.Scripting;
    
    // Optional
    using RageCoop.Server;
    using RageCoop.Core.Scripting;
    using RageCoop.Core;
    
    ```
4. Inherit a class from [ServerScript](api/RageCoop.Server.Scripting.ServerScript.html).
5. Implement `OnStart()` and `OnStop()`, your cs file should look like this:
    ```
    using RageCoop.Server.Scripting;
    
    namespace NiceGuy.MyFirstResource
    {
        public class Main : ServerScript
        {
             public override void OnStart()
             {
                 // Initiate your script here
             }
             public override void OnStop()
             {
                 // Free all resources and perform cleanup
             }
        }
    }
    ```
6. Now you can have some fun by using the [API](api/RageCoop.Server.Scripting.API.html) instance, please refer to the [GitHub repo](https://github.com/RAGECOOP/GTAV-RESOURCES) for more examples.
7. For convenience, you can create a symlink in `ServerRoot/Resources/Server/NiceGuy.MyFirstResource` targeting your output folder:
    ```
    mklink /d ServerRoot/Resources/Server/NiceGuy.MyFirstResource C:/MyRepos/NiceGuy.MyFirstResource/bin/Debug
    ```
8. That's it! Start your server and you should see your resource loading.