using Newtonsoft.Json;

namespace FanControl.LiquidCtlTest;

public class LiquidctlDeviceTests
{
    [Test]
    public void TestCreateLiquidCtlDeviceWithFanData()
    {
        var pluginLogger = new Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()))
            .Callback((string msg) => Console.WriteLine($"{msg}"))
            .Verifiable();
        var statusJson =
            JsonConvert.DeserializeObject<LiquidctlStatusJSON>(File.ReadAllText("tests_fan_status.json"));
        var device = new LiquidctlDevice(statusJson, pluginLogger.Object);
        Assert.Multiple(() =>
        {
            Assert.That(device is not null);
            Assert.That(device is { HasFanSpeed: true });
            Assert.That(device is { HasPumpDuty: false });
            Assert.That(device is { HasLiquidTemperature: false });
            Assert.That(device is { LiquidTemperatureSensor: null });
            Assert.That(device is { PumpSpeedSensor: null });
            Assert.That(device is { PumpDutyController: null });
            Assert.That(device is { FanSpeedSensors.Count: 3 });
            Assert.That(device is { FanControlSensors.Count: 3 });
        });
    }
    
}