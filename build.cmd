cd "%~dp0"
git submodule update --init
if exist "bin" rmdir /s /q "bin"
dotnet build -c Release
dotnet build -c API Core\RageCoop.Core.csproj
dotnet build -c API Server\RageCoop.Server.csproj
dotnet build -c API Client\RageCoop.Client.Scripting.csproj
del .\bin\API\RageCoop.Server.exe
if exist .\bin\Release\Client\SubProcess rmdir .\bin\Release\Client\SubProcess /s /q 