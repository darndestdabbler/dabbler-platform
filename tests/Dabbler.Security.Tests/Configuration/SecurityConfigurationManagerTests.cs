using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Moq;

namespace Dabbler.Security.Tests.Configuration;

public sealed class SecurityConfigurationManagerTests
{
    [Fact]
    public async Task EnsureLoadedAsync_LoadsFromStoreOnFirstCall()
    {
        var config = new SecurityConfiguration { Roles = [new RoleDefinition { RoleName = "Admin" }] };
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);

        var manager = new SecurityConfigurationManager(store.Object);
        await manager.EnsureLoadedAsync();

        manager.Current.Roles.Should().ContainSingle(r => r.RoleName == "Admin");
        store.Verify(s => s.LoadAsync(default), Times.Once);
    }

    [Fact]
    public async Task EnsureLoadedAsync_DoesNotLoadTwice()
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(new SecurityConfiguration());

        var manager = new SecurityConfigurationManager(store.Object);
        await manager.EnsureLoadedAsync();
        await manager.EnsureLoadedAsync();

        store.Verify(s => s.LoadAsync(default), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ReloadsFromStore()
    {
        var first = new SecurityConfiguration { Roles = [new RoleDefinition { RoleName = "First" }] };
        var second = new SecurityConfiguration { Roles = [new RoleDefinition { RoleName = "Second" }] };

        var store = new Mock<ISecurityConfigurationStore>();
        store.SetupSequence(s => s.LoadAsync(default))
            .ReturnsAsync(first)
            .ReturnsAsync(second);

        var manager = new SecurityConfigurationManager(store.Object);
        await manager.EnsureLoadedAsync();
        manager.Current.Roles[0].RoleName.Should().Be("First");

        await manager.RefreshAsync();
        manager.Current.Roles[0].RoleName.Should().Be("Second");
    }

    [Fact]
    public async Task RefreshAsync_FiresConfigurationChangedEvent()
    {
        var config = new SecurityConfiguration();
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);

        var manager = new SecurityConfigurationManager(store.Object);
        SecurityConfiguration? received = null;
        manager.ConfigurationChanged += c => { received = c; return Task.CompletedTask; };

        await manager.RefreshAsync();

        received.Should().BeSameAs(config);
    }

    [Fact]
    public async Task SaveAsync_PersistsToStoreAndUpdatesMemory()
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default)).Returns(Task.CompletedTask);
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(new SecurityConfiguration());

        var manager = new SecurityConfigurationManager(store.Object);
        var newConfig = new SecurityConfiguration { Roles = [new RoleDefinition { RoleName = "NewRole" }] };

        await manager.SaveAsync(newConfig);

        store.Verify(s => s.SaveAsync(newConfig, default), Times.Once);
        manager.Current.Should().BeSameAs(newConfig);
    }

    [Fact]
    public async Task SaveAsync_FiresConfigurationChangedEvent()
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default)).Returns(Task.CompletedTask);

        var manager = new SecurityConfigurationManager(store.Object);
        SecurityConfiguration? received = null;
        manager.ConfigurationChanged += c => { received = c; return Task.CompletedTask; };

        var newConfig = new SecurityConfiguration();
        await manager.SaveAsync(newConfig);

        received.Should().BeSameAs(newConfig);
    }
}
