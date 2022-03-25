function testFunction(message)
{
    SHVDN.GTA.UI.Notification.Show(message);
}

var messageCount = 0;

// Called every tick
API.OnTick.connect(function() {
    if (messageCount < 5)
    {
        API.SendLocalMessage("testFunction " + ++messageCount);
    }
});

// Called on connect
API.OnStart.connect(function() {
    testFunction("hello test2.js!");
});

// Called on disconnect
API.OnStop.connect(function() {
    testFunction("bye test2.js!");
});