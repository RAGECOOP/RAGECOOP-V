import { createServer } from 'net';
import { readFileSync } from 'fs';

var serverList = [];
const blockedIps = readFileSync('blocked.txt', 'utf-8').split('\n');

const server = createServer();

server.on('connection', (socket) =>
{
    if (blockedIps.includes(socket.remoteAddress))
    {
        console.log(`IP '${socket.remoteAddress}' blocked`);
        socket.destroy();
        return;
    }
    
    var lastData = 0;

    const remoteAddress = socket.remoteAddress + ":" + socket.remotePort;

    socket.on('data', async (data) =>
    {
        // NOT SPAM!
        if (lastData !== 0 && (Date.now() - lastData) < 14500)
        {
            console.log("[WARNING] Spam from %s", remoteAddress);
            socket.destroy();
            return;
        }

        lastData = Date.now();

        var incomingMessage;
        try
        {
            incomingMessage = await JSON.parse(data.toString());
        }
        catch
        {
            socket.destroy();
            return;
        }

        if (incomingMessage.method)
        {
            if (incomingMessage.method === 'POST' && incomingMessage.data)
            {
                // Check if the server is already in the serverList
                const alreadyExist = serverList.some((val) =>
                {
                    const found = val.remoteAddress === remoteAddress;

                    if (found)
                    {
                        // Replace old data with new data
                        val.data = { ...val.data, ...incomingMessage.data };
                    }

                    return found;
                });

                // Server doesn't exist in serverList so add the server
                if (!alreadyExist)
                {
                    serverList.push({ remoteAddress: remoteAddress, data: incomingMessage.data });
                }
                return;
            }
            else if (incomingMessage.method === 'GET')
            {
                socket.write(JSON.stringify(serverList));
                return;
            }
        }

        // method or data does not exist or method is not POST or GET
        socket.destroy();
    });

    socket.on('close', () => serverList = serverList.filter(val => val.remoteAddress !== remoteAddress));

    socket.on('error', (e) => { /*console.error(e)*/ });
});

server.listen(11000, () => console.log("MasterServer started!"));