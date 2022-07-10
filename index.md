
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
    │   │   RageCoop.Resources.Race.zip
    │   │   
    │
    │───Client
    │   │   RageCoop.Resources.Race.Client.zip
    │   │
    │
    └───Temp
```
### Settings.xml 

This file will be generated first time you started the server, you can then change the server's configuration option by editing it, refer to [ServerSettings](api/RageCoop.Server.ServerSettings.html) for detailed description. 

## Server Resource

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
4. Inherit from [ServerScript](api/RageCoop.Server.Scripting.ServerScript.html).
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


## Client Resource

1. Create a C# class library project targeting .NET Framework 4.8.
2. Add reference to **RageCoop.Client.dll** and **RageCoop.Core.dll**.
3. Add following namespace(s):
    ```
    using RageCoop.Client.Scripting;
    
    // Optional
    using RageCoop.Core.Scripting;
    using RageCoop.Core;
    
    ```
4. Inherit from [ClientScript](api/RageCoop.Client.Scripting.ClientScript.html).
5. Implement `OnStart()` and `OnStop()`, your cs file should look like this:
    ```
    using RageCoop.Server.Scripting;
    
    namespace NiceGuy.MyFirstClientResource
    {
        public class Main : ClientScript
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
6. Now you can use anything from SHVDN to control client behaviour by adding a reference to **ScriptHookVDotNet3.dll**
7. For convenience, you can create a symlink in `ServerRoot/Resources/Client/NiceGuy.MyFirstResource` targeting your output folder:
    ```
    mklink /d ServerRoot/Resources/Client/NiceGuy.MyFirstClientResource C:/MyRepos/NiceGuy.MyFirstClientResource/bin/Debug
    ```
8. That's it! When a client connects the resource will be sent and loaded at client side.
