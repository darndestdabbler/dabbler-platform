using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Services;

public class AuthorizationStateNotifierTests
{
    private static (SecurityConfigurationManager manager, AuthorizationStateNotifier notifier) CreatePair()
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(new SecurityConfiguration());
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default)).Returns(Task.CompletedTask);
        var manager = new SecurityConfigurationManager(store.Object);
        var notifier = new AuthorizationStateNotifier(manager, NullLogger<AuthorizationStateNotifier>.Instance);
        return (manager, notifier);
    }

    [Fact]
    public async Task Subscribe_ThenSaveAsync_FiresSubscriber()
    {
        var (manager, notifier) = CreatePair();
        AuthorizationConfigurationChanged? received = null;

        notifier.Subscribe(evt => { received = evt; return Task.CompletedTask; });
        await manager.SaveAsync(new SecurityConfiguration());

        received.Should().NotBeNull();
        received!.Configuration.Should().NotBeNull();
        received.ChangedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Subscribe_ThenRefreshAsync_FiresSubscriber()
    {
        var (manager, notifier) = CreatePair();
        AuthorizationConfigurationChanged? received = null;

        notifier.Subscribe(evt => { received = evt; return Task.CompletedTask; });
        await manager.RefreshAsync();

        received.Should().NotBeNull();
    }

    [Fact]
    public async Task Unsubscribe_DoesNotFireAfterRemoval()
    {
        var (manager, notifier) = CreatePair();
        int callCount = 0;

        Func<AuthorizationConfigurationChanged, Task> handler = evt =>
        {
            callCount++;
            return Task.CompletedTask;
        };

        notifier.Subscribe(handler);
        notifier.Unsubscribe(handler);
        await manager.SaveAsync(new SecurityConfiguration());

        callCount.Should().Be(0);
    }

    [Fact]
    public async Task MultipleSubscribers_AllFire()
    {
        var (manager, notifier) = CreatePair();
        int count1 = 0, count2 = 0;

        notifier.Subscribe(_ => { count1++; return Task.CompletedTask; });
        notifier.Subscribe(_ => { count2++; return Task.CompletedTask; });
        await manager.SaveAsync(new SecurityConfiguration());

        count1.Should().Be(1);
        count2.Should().Be(1);
    }

    [Fact]
    public async Task FaultingSubscriber_DoesNotBlockOtherSubscribers()
    {
        var (manager, notifier) = CreatePair();
        int count = 0;

        notifier.Subscribe(_ => throw new InvalidOperationException("subscriber fault"));
        notifier.Subscribe(_ => { count++; return Task.CompletedTask; });

        await manager.SaveAsync(new SecurityConfiguration());

        count.Should().Be(1);
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromManager()
    {
        var (manager, notifier) = CreatePair();
        int count = 0;

        notifier.Subscribe(_ => { count++; return Task.CompletedTask; });
        notifier.Dispose();

        await manager.SaveAsync(new SecurityConfiguration());

        count.Should().Be(0);
    }
}
