

# Getting started
Here you can learn how to create your first resource

## Directory structure

Below is the server's directory structure
```
ServerRoot
│   Settings.xml   
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
This file will be generated first time you started the, you can change the server's configuration option by editing it, refer to [this](api/RageCoop.Server.ServerSettings.html) for detailed description. 

### Resources
Each directory or zip in represents one resource, which consists of several dlls, and is isolated from another resource.

## Server Reource
The resource will be running at server side, here's how to create one:
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
5. Implement `OnStart()` and `OnStop()`:
```
public class MyFirstResource :ServerScript
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
```
6. That's it! Now you can have some fun by using the [API](api/RageCoop.Server.Scripting.API.html) instance, please refer to the [GiHub](https://github.com/RAGECOOP/GTAV-RESOURCES) for more examples.
