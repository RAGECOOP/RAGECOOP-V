var testVar = 0;
var testVar2 = false;

// Called every frame
API.OnRender.connect(function() {
    if (!testVar2)
    {
        testVar2 = true;

        SHV.GTA.UI.Notification.Show("test2.js loaded!");
    }

    if (testVar < 10)
    {
        API.SendMessage("Test2 number: " + ++testVar);
    }
});