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
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()));
        var dialog = new Mock<IPluginDialog>();
        var plugin = new LiquidCtlPlugin(pluginLogger.Object,dialog.Object);
        Assert.That(plugin, Is.Not.Null); 
        pluginLogger.Verify(logger => logger.Log(It.IsAny<String>()), Times.AtLeast(1));
        
    }

    [Test] public void TestIfPluginInitializingCorrectly()
    {
        var pluginLogger = new Moq.Mock<IPluginLogger>();
        pluginLogger.Setup(x => x.Log(It.IsAny<string>()));
        var dialog = new Mock<IPluginDialog>();
        var task = new Mock<Task>();
        dialog.Setup(x => x.ShowMessageDialog(It.IsAny<string>())).Returns(() => task.Object);
        var plugin = new LiquidCtlPlugin(pluginLogger.Object,dialog.Object);
        plugin.Initialize();
        Assert.That(plugin.IsInited());
        pluginLogger.Verify(logger => logger.Log(It.IsAny<String>()), Times.Once);
        
    }
}