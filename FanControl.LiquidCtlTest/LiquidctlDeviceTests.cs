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
            JsonConvert.DeserializeObject<LiquidCtlStatusJson>(File.ReadAllText("tests_fan_status.json"));
        var device = new LiquidCtlDevice(statusJson, pluginLogger.Object);
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

    [Test]
    public void TestCreateLiquidCtlDeviceWithPumpData()
    {
        var pluginLogger = new Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()))
            .Callback((string msg) => Console.WriteLine($"{msg}"))
            .Verifiable();
        var statusJson =
            JsonConvert.DeserializeObject<LiquidCtlStatusJson>(File.ReadAllText("tests_pump_temp_status.json"));
        var device = new LiquidCtlDevice(statusJson, pluginLogger.Object);
        Assert.Multiple(() =>
        {
            Assert.That(device is not null);
            Assert.That(device is { HasFanSpeed: false });
            Assert.That(device is { HasPumpDuty: true });
            Assert.That(device is { HasLiquidTemperature: true });
            Assert.That(device is { LiquidTemperatureSensor: not null });
            Assert.That(device is { PumpSpeedSensor: not null });
            Assert.That(device?.PumpSpeedSensor?.Name is "Pump - NZXT Kraken Pump",$"{device?.PumpSpeedSensor?.Name}");
            Assert.That(device is { PumpDutyController: not null });
            Assert.That(device?.PumpDutyController?.Name is "Pump Control - NZXT Kraken Pump",$"{device?.PumpDutyController?.Name}");
            Assert.That(device is { LiquidTemperatureSensor: not null });
            Assert.That(device?.LiquidTemperatureSensor?.Name is "Liquid Temp. - NZXT Kraken Pump",$"{device?.LiquidTemperatureSensor?.Name}");
            Assert.That(device is { FanSpeedSensors.Count: 0});
            Assert.That(device is { FanControlSensors.Count: 0 });
        });
    }
    
}