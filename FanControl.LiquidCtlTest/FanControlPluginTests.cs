using System.Net;

namespace FanControl.LiquidCtlTest;

public class FanControlPluginTests
{

    [SetUp]
    public void Setup()
    {
        try
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return; //TODO Currently only Windows platform is supported, will look into linux later
            var driveLetter = DriveInfo.GetDrives().Select(x => x.Name)
                .Single(name =>
                {
                    var df = new DirectoryInfo(Path.Combine(name, "Fan Control"));
                    return df.Exists;
                });
            
            var configSource = Path.Combine(driveLetter, "Fan Control", "config.yaml");
            if (File.Exists(configSource))
                File.Copy(configSource, Path.Combine(Environment.CurrentDirectory, "config.yaml"), false);
            var liquidCtlSource = Path.Combine(driveLetter, "Fan Control","Plugins" ,"liquidctl.exe");
            if(File.Exists(liquidCtlSource))
                File.Copy(liquidCtlSource, Path.Combine(Environment.CurrentDirectory, "liquidctl.exe"), false);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"Did not locate liquidctl executable. Cause: {e.Message}");
        }
    }
    [TearDown]
    public void TearDown()
    {
        if(File.Exists(Path.Combine(Environment.CurrentDirectory, "liquidctl.exe")))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "liquidctl.exe"));
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
            // Assert.That(pluginSensors.First().Name, Is.Not.Null);
        });
    }
}