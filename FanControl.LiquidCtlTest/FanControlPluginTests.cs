using Moq;

namespace FanControl.LiquidCtlTest;

public class FanControlPluginTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void TestIfPluginLoadedCorrectly()
    {
        var pluginLogger = new Moq.Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>())).Verifiable();
        var dialog = new Mock<IPluginDialog>();
        var plugin = new LiquidCtlPlugin(pluginLogger.Object, dialog.Object);
        Assert.That(plugin, Is.Not.Null);
        pluginLogger.Verify(logger => logger.Log(It.IsAny<String>()), Times.AtLeast(1));
    }

    [Test]
    public void TestIfPluginInitializingCorrectly()
    {
        var pluginLogger = new Moq.Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()))
            .Callback((string msg) => Console.WriteLine($"Log received: {msg}"))
            .Verifiable();
        var dialog = new Mock<IPluginDialog>();

        dialog.Setup(x => x.ShowMessageDialog(It.IsAny<string>())).Returns(() => Task.CompletedTask);
        var plugin = new LiquidCtlPlugin(pluginLogger.Object, dialog.Object);
        plugin.Initialize();
        pluginLogger.Verify(logger => logger.Log(It.IsAny<string>()), Times.AtLeastOnce);
        Assert.That(plugin.IsInited());
    }
}