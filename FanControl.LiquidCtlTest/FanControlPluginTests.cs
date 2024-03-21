using System.Net;

namespace FanControl.LiquidCtlTest;

public class FanControlPluginTests
{
    private static void RecursiveCopy(string sourceDirectory, string targetDirectory)
    {
        // Create the target directory if it doesn't exist
        Directory.CreateDirectory(targetDirectory);

        // Get all files in the source directory and copy them to the target directory
        foreach (var file in Directory.GetFiles(sourceDirectory))
        {
            var targetFile = Path.Combine(targetDirectory, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }

        // Recursively copy subdirectories
        foreach (var directory in Directory.GetDirectories(sourceDirectory))
        {
            var targetSubDirectory = Path.Combine(targetDirectory, Path.GetFileName(directory));
            RecursiveCopy(directory, targetSubDirectory);
        }
    }

    private static void RecursiveDelete(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory)) return;

        // Delete files within the directory
        foreach (var file in Directory.GetFiles(targetDirectory))
        {
            File.Delete(file);
        }

        // Recursive delete for subdirectories
        foreach (var subDirectory in Directory.GetDirectories(targetDirectory))
        {
            RecursiveDelete(subDirectory);
        }

        // Delete the directory itself
        Directory.Delete(targetDirectory);
    }


    [SetUp]
    public void Setup()
    {
        try
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return; //TODO Currently only Windows platform is supported, will look into linux later
            var driveLetter = DriveInfo.GetDrives().Select(x => x.Name)
                .Single(name =>
                {
                    var df = new DirectoryInfo(Path.Combine(name, "Fan Control"));
                    return df.Exists;
                });

            var fanCtrlPluginSrc = Path.Combine(driveLetter, "Fan Control", "Plugins");
            var liquidCtlSrc = Path.Combine(fanCtrlPluginSrc, "liquidCtl");
            if (!Directory.Exists(liquidCtlSrc)) return;
            var sourceDir = Directory.CreateDirectory(fanCtrlPluginSrc);
            if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, "liquidCtl")))
            {
                var destinationDir = Environment.CurrentDirectory;
                if (File.Exists(Path.Combine(destinationDir, "liquidCtl", "liquidCtl.exe"))) return;
            }
            else
            {
                var targetDir = Path.Combine(Environment.CurrentDirectory, "liquidCtl");
                RecursiveCopy(liquidCtlSrc, targetDir);
            }
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"Did not locate liquidctl executable. Cause: {e.Message}");
        }
    }

    [TearDown]
    public void TearDown()
    {
        RecursiveDelete(Path.Combine(Environment.CurrentDirectory, "liquidCtl"));
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "config.yaml")))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "config.yaml"));
    }

    [Test]
    public void TestIfPluginLoadedCorrectly()
    {
        var pluginLogger = new Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>())).Verifiable();
        var dialog = new Mock<IPluginDialog>();
        var plugin = new LiquidCtlPlugin(pluginLogger.Object, dialog.Object);
        Assert.That(plugin, Is.Not.Null);
        pluginLogger.Verify(logger => logger.Log(It.IsAny<string>()), Times.AtLeast(1));
    }

    [Test]
    public void TestIfPluginInitializingCorrectly()
    {
        var pluginLogger = new Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()))
            .Callback((string msg) => Console.WriteLine($"Log received: {msg}"))
            .Verifiable();
        var dialog = new Mock<IPluginDialog>();

        dialog.Setup(x => x.ShowMessageDialog(It.IsAny<string>())).Returns(() => Task.CompletedTask);
        var plugin = new LiquidCtlPlugin(pluginLogger.Object, dialog.Object);
        plugin.Initialize();
        pluginLogger.Verify(logger => logger.Log(It.IsAny<string>()), Times.AtLeastOnce);
        Assert.That(plugin.HasInitialized());
    }

    [Test]
    public void TestIfContainerGettingLoadedWithSensors()
    {
        var pluginLogger = new Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()))
            .Callback((string msg) => Console.WriteLine($"Log received: {msg}"))
            .Verifiable();
        var dialog = new Mock<IPluginDialog>();
        dialog.Setup(x => x.ShowMessageDialog(It.IsAny<string>())).Returns(() => Task.CompletedTask);

        var mockSensorContainer = new Mock<IPluginSensorsContainer>();
        var pluginSensors = new List<IPluginSensor>();
        mockSensorContainer.Setup(x => x.FanSensors).Returns(pluginSensors);

        var pluginControlSensors = new List<IPluginControlSensor>();
        mockSensorContainer.Setup(x => x.ControlSensors).Returns(pluginControlSensors);

        var plugin = new LiquidCtlPlugin(pluginLogger.Object, dialog.Object);
        plugin.Initialize();
        pluginLogger.Verify(logger => logger.Log(It.IsAny<string>()), Times.AtLeastOnce);
        Assert.That(plugin.HasInitialized());

        // Load sequence
        plugin.Load(mockSensorContainer.Object);
        mockSensorContainer.Verify(x => x.FanSensors, Times.Once);
        mockSensorContainer.Verify(x => x.ControlSensors, Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(pluginSensors, Is.Not.Empty);
            Assert.That(pluginControlSensors, Is.Not.Empty);
            Assert.That(pluginSensors, Has.Count.EqualTo(3));
            Assert.That(pluginSensors, Has.Count.EqualTo(pluginControlSensors.Count));
            Assert.That(pluginSensors.First() is not null);
            Assert.That(pluginControlSensors.First() is not null);
            Assert.That(pluginSensors.First().Name, Is.EqualTo("Fan 1 - NZXT RGB & Fan Controller (3+6 channels)"));
            Assert.That(pluginSensors.First().Id,
                Is.EqualTo(
                    "\\\\?\\HID#VID_1E71&PID_2019#8&1364b162&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}-fanRPM1"));
            Assert.That(pluginControlSensors.First().Name,
                Is.EqualTo("Fan 1 Control - NZXT RGB & Fan Controller (3+6 channels)"));
            Assert.That(pluginControlSensors.First().Id,
                Is.EqualTo(
                    "\\\\?\\HID#VID_1E71&PID_2019#8&1364b162&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}-fanCtrl1"));
        });
    }

    [Test]
    public void TestIfPluginGettingSensorData()
    {
        var pluginLogger = new Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()))
            .Callback((string msg) => Console.WriteLine($"Log received: {msg}"))
            .Verifiable();
        var dialog = new Mock<IPluginDialog>();
        dialog.Setup(x => x.ShowMessageDialog(It.IsAny<string>())).Returns(() => Task.CompletedTask);

        var mockSensorContainer = new Mock<IPluginSensorsContainer>();
        var pluginSensors = new List<IPluginSensor>();
        mockSensorContainer.Setup(x => x.FanSensors).Returns(pluginSensors);

        var pluginControlSensors = new List<IPluginControlSensor>();
        mockSensorContainer.Setup(x => x.ControlSensors).Returns(pluginControlSensors);
        var plugin = new LiquidCtlPlugin(pluginLogger.Object, dialog.Object);
        plugin.Initialize();

        // Load sequence
        plugin.Load(mockSensorContainer.Object);
        mockSensorContainer.Verify(x => x.FanSensors, Times.Once);
        mockSensorContainer.Verify(x => x.ControlSensors, Times.Once);

        // Update the sensor statuses
        plugin.Update();

        Assert.Multiple(() =>
        {
            Assert.That(pluginSensors.First() is not null);
            Assert.That(pluginControlSensors.First() is not null);
            pluginSensors.ToList().ForEach((IPluginSensor sensor) =>
            {
                Assert.That(sensor.Value, Is.GreaterThan(500)); // 500 is the bottom speed of the fans in question
            });
            pluginControlSensors.ToList().ForEach((IPluginControlSensor ctrlSensor) =>
            {
                Assert.That(ctrlSensor.Value, Is.EqualTo(25)); // 25% is the default duty cycle of the NZXT controller
            });

            Assert.That(pluginSensors.First().Value, Is.Not.EqualTo(pluginSensors.Last().Value));
        });
    }


    [Test]
    public async Task TestIfPluginSettingFanDuty()
    {
        var pluginLogger = new Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()))
            .Callback((string msg) => Console.WriteLine($"Log received: {msg}"))
            .Verifiable();
        var dialog = new Mock<IPluginDialog>();
        dialog.Setup(x => x.ShowMessageDialog(It.IsAny<string>())).Returns(() => Task.CompletedTask);

        var mockSensorContainer = new Mock<IPluginSensorsContainer>();
        var pluginSensors = new List<IPluginSensor>();
        mockSensorContainer.Setup(x => x.FanSensors).Returns(pluginSensors);

        var pluginControlSensors = new List<IPluginControlSensor>();
        mockSensorContainer.Setup(x => x.ControlSensors).Returns(pluginControlSensors);
        var plugin = new LiquidCtlPlugin(pluginLogger.Object, dialog.Object);
        plugin.Initialize();

        // Load sequence
        plugin.Load(mockSensorContainer.Object);
        mockSensorContainer.Verify(x => x.FanSensors, Times.Once);
        mockSensorContainer.Verify(x => x.ControlSensors, Times.Once);

        Assert.That(pluginControlSensors.First(), Is.InstanceOf(typeof(IPluginControlSensor)));
        ((IPluginControlSensor)pluginControlSensors.First()).Set(100);
        await Task.Delay(1000);
        plugin.Update();
        Assert.Multiple(() =>
        {
            Assert.That(pluginControlSensors.First().Value, Is.EqualTo(100));
            Assert.That(pluginSensors.First().Value, Is.GreaterThanOrEqualTo(1900)); //The rated max speed of the fans are around 2000 so achieving top speed on case mounted fan will be less
        });
        ((IPluginControlSensor)pluginControlSensors.First()).Set(25); //Setting back to default
    }
}