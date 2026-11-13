namespace AcmeCoffeeOnline.Web.StubMode;

public sealed record StubCustomerRow(
    int CustomerId,
    string Name,
    string City,
    string Tier,
    string OrganizationId);
