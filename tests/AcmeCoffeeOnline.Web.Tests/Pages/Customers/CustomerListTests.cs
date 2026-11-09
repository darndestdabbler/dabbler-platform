using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Components.Pages.Customers;
using Bunit;
using Dabbler.Components.Data;
using MudBlazor;
using MudBlazor.Services;

namespace AcmeCoffeeOnline.Web.Tests.Pages.Customers;

public sealed class CustomerListTests : TestContext
{
    public CustomerListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public async Task CanceledLoad_DoesNotShowErrorOrClearTheNextResult()
    {
        var provider = new FirstCallBlocksUntilCanceledProvider();
        var cut = RenderComponent<CustomerList>(p => p
            .Add(c => c.DataProvider, provider)
            .Add(c => c.AutoLoadOnFilterChange, false));

        cut.WaitForAssertion(() => Assert.True(provider.FirstCallStarted));

        await cut.InvokeAsync(() => cut.FindComponent<MudDataGrid<CustomerDto>>().Instance.ReloadServerData());

        cut.WaitForAssertion(() =>
        {
            Assert.True(provider.FirstCallCanceled);
            Assert.Empty(cut.FindComponents<MudAlert>());
            Assert.Contains("1 records", cut.Markup);
            Assert.Contains("ada@example.com", cut.Markup);
        });
    }

    private sealed class FirstCallBlocksUntilCanceledProvider : IGridDataProvider<CustomerDto>
    {
        private int _calls;

        public bool FirstCallStarted { get; private set; }

        public bool FirstCallCanceled { get; private set; }

        public async Task<GridDataResponse<CustomerDto>> FetchAsync(GridDataRequest request, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                FirstCallStarted = true;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    FirstCallCanceled = true;
                    throw;
                }

                throw new TimeoutException("The first load was never canceled.");
            }

            return new GridDataResponse<CustomerDto>
            {
                Items = [new CustomerDto { Id = 1, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" }],
                TotalCount = 1,
            };
        }
    }
}
