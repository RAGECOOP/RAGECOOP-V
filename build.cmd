doc
dotnet build RageCoop.Client/RageCoop.Client.csproj --no-restore --configuration Release -o RageCoop.Client/bin/RageCoop
dotnet publish RageCoop.Server/RageCoop.Server.csproj --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -r win-x64 -o RageCoop.Server/bin/win-x64 -c Release
dotnet publish RageCoop.Server/RageCoop.Server.csproj --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -r linux-x64 -o RageCoop.Server/bin/linux-x64 -c Release
dotnet publish RageCoop.Server/RageCoop.Server.csproj --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -r linux-arm -o RageCoop.Server/bin/linux-arm -c Release