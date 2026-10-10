using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace EcoTrack.Selenium.Tests;

public class Sprint4E2ETests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private const string BaseUrl = "http://localhost:5173";

    public Sprint4E2ETests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    public void Dispose()
    {
        try
        {
            _driver.Quit();
            _driver.Dispose();
        }
        catch { }
    }

    #region Helper: Login as specified user

    private void LoginAs(string email, string password)
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var emailInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='email'], input[name='email']")));
        emailInput.Clear();
        emailInput.SendKeys(email);

        var passInput = _driver.FindElement(By.CssSelector("input[type='password'], input[name='password']"));
        passInput.Clear();
        passInput.SendKeys(password);

        var submitBtn = _driver.FindElement(By.CssSelector("button[type='submit']"));
        submitBtn.Click();

        _wait.Until(d => d.Url.Contains("/dashboard") || d.PageSource.Contains("Dashboard"));
    }

    #endregion

    #region E2E-01: Recycler adds a driver and assigns to Scheduled pickup

    [Fact]
    public void E2E_01_RecyclerAddsDriverAndAssignsToScheduledPickup()
    {
        // 1. Log in as Recycler
        LoginAs("recycler@ecotrack.lk", "RecyclerPassword@2026!");

        // 2. Navigate to Add Driver flow
        _driver.Navigate().GoToUrl($"{BaseUrl}/drivers/add");
        var driverNameInput = _wait.Until(d => d.FindElement(By.CssSelector("input[name='driverName'], input[placeholder*='name']")));
        driverNameInput.Clear();
        driverNameInput.SendKeys("Test Driver R1");

        var assignPickupBtn = _driver.FindElement(By.CssSelector("button[type='submit'], button:has(text('Assign Pickup'))"));
        assignPickupBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Scheduled") || d.Url.Contains("/scheduled"));

        // 3. Verify driver assigned to Scheduled pickup
        Assert.Contains("Scheduled", _driver.PageSource);
    }

    #endregion

    #region E2E-02: Recorder logs a drop-off -> pickup shows "Collected"

    [Fact]
    public void E2E_02_RecorderLogsDropOff_PickupShowsCollected()
    {
        // 1. Log in as Recycler
        LoginAs("recycler@ecotrack.lk", "RecyclerPassword@2026!");

        // 2. Navigate to pickups and log drop-off
        _driver.Navigate().GoToUrl($"{BaseUrl}/pickups/active");
        _wait.Until(d => d.PageSource.Contains("Active") || d.Url.Contains("/pickups/active"));

        // Find the first active pickup and log drop-off
        var dropOffBtn = _wait.Until(d => d.FindElement(By.CssSelector("button:has(text('Log Drop-off')), button:has(text('Collect'))")));
        dropOffBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Collected") || d.PageSource.Contains("Collected"));

        // 3. Verify pickup shows "Collected"
        Assert.Contains("Collected", _driver.PageSource);
    }

    #endregion

    #region E2E-03: Buyer orders an item -> Pending payment -> recycler marks Paid -> refund cancels the order

    [Fact]
    public void E2E_03_BuyerOrdersItem_PaidThenRefunded()
    {
        // 1. Log in as Buyer
        LoginAs("buyer@ecotrack.lk", "BuyerPassword@2026!");

        // 2. Navigate to Marketplace and order an item
        _driver.Navigate().GoToUrl($"{BaseUrl}/marketplace");
        _wait.Until(d => d.PageSource.Contains("Marketplace") || d.Url.Contains("/marketplace"));

        var orderBtn = _wait.Until(d => d.FindElement(By.CssSelector("button:has(text('Order')), button:has(text('Buy'))")));
        orderBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Pending") || d.Url.Contains("/pending-payment"));

        // 3. Verify status is Pending Payment
        Assert.Contains("Pending", _driver.PageSource);

        // 4. Log in as Recycler and mark as Paid
        LoginAs("recycler@ecotrack.lk", "RecyclerPassword@2026!");

        _driver.Navigate().GoToUrl($"{BaseUrl}/recycler/orders");
        _wait.Until(d => d.PageSource.Contains("Orders") || d.Url.Contains("/recycler/orders"));

        var markPaidBtn = _wait.Until(d => d.FindElement(By.CssSelector("button:has(text('Mark Paid')), button:has(text('Pay'))")));
        markPaidBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Paid") || d.PageSource.Contains("Completed"));

        // 5. Log in as Buyer and trigger refund
        LoginAs("buyer@ecotrack.lk", "BuyerPassword@2026!");

        _driver.Navigate().GoToUrl($"{BaseUrl}/buyer/orders");
        _wait.Until(d => d.PageSource.Contains("Orders") || d.Url.Contains("/buyer/orders"));

        var refundBtn = _wait.Until(d => d.FindElement(By.CssSelector("button:has(text('Refund')), button:has(text('Cancel Order'))")));
        refundBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Cancelled") || d.PageSource.Contains("Refunded"));

        // 6. Verify order is cancelled/refunded
        Assert.Contains("Cancelled", _driver.PageSource);
    }

    #endregion

    #region E2E-04: Sales report loads, filters by category and exports

    [Fact]
    public void E2E_04_SalesReportLoadsFiltersAndExports()
    {
        // 1. Log in as Admin
        LoginAs("admin@ecotrack.lk", "AdminPassword@2026!");

        // 2. Navigate to Sales Report
        _driver.Navigate().GoToUrl($"{BaseUrl}/admin/sales-report");
        _wait.Until(d => d.PageSource.Contains("Sales Report") || d.Url.Contains("/sales-report"));

        // 3. Filter by category (use first available option)
        var categoryDropdown = _wait.Until(d => d.FindElement(By.CssSelector("select[name*='category'], select[aria-label*='category']")));
        var firstOption = categoryDropdown.FindElement(By.TagName("option"));
        firstOption.Click();

        // 4. Apply filter
        var applyBtn = _wait.Until(d => d.FindElement(By.CssSelector("button[type='submit'], button:has(text('Apply')), button:has(text('Filter'))")));
        applyBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Filtered") || d.PageSource.Contains("Results"));

        // 5. Export report
        var exportBtn = _wait.Until(d => d.FindElement(By.CssSelector("button:has(text('Export')), a:has(text('Export'))")));
        exportBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Exported") || d.PageSource.Contains("CSV") || d.PageSource.Contains("Download"));

        // 6. Verify export completed
        Assert.Contains("Exported", _driver.PageSource);
    }

    #endregion

    #region E2E-05: Scheduling a pickup and placing an order produce notifications

    [Fact]
    public void E2E_05_SchedulingAndOrderingProduceNotifications()
    {
        // 1. Schedule a pickup as Recycler
        LoginAs("recycler@ecotrack.lk", "RecyclerPassword@2026!");

        _driver.Navigate().GoToUrl($"{BaseUrl}/schedule");
        _wait.Until(d => d.PageSource.Contains("Schedule") || d.Url.Contains("/schedule"));

        var schedulePickupBtn = _wait.Until(d => d.FindElement(By.CssSelector("button:has(text('Schedule Pickup')), button:has(text('Create Pickup'))")));
        schedulePickupBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Scheduled") || d.PageSource.Contains("Notification"));

        // 2. Place an order as Buyer
        LoginAs("buyer@ecotrack.lk", "BuyerPassword@2026!");

        _driver.Navigate().GoToUrl($"{BaseUrl}/marketplace");
        _wait.Until(d => d.PageSource.Contains("Marketplace") || d.Url.Contains("/marketplace"));

        var orderBtn = _wait.Until(d => d.FindElement(By.CssSelector("button:has(text('Order')), button:has(text('Buy'))")));
        orderBtn.Click();

        _wait.Until(d => d.PageSource.Contains("Pending") || d.Url.Contains("/pending-payment"));

        // 3. Verify notifications exist (toast or notification center)
        bool hasNotification = _driver.PageSource.Contains("Notification") ||
                             _driver.PageSource.Contains("Toast") ||
                             _driver.FindElements(By.ClassName("notification")).Count > 0;

        Assert.True(hasNotification, "Expected notification for scheduling/ordering");
    }

    #endregion

    #region E2E-06: Admin sees the audit log entry and finds it by item ID

    [Fact]
    public void E2E_06_AdminSeesAuditLogAndFindsByItemId()
    {
        // 1. Log in as Admin
        LoginAs("admin@ecotrack.lk", "AdminPassword@2026!");

        // 2. Navigate to Audit Log
        _driver.Navigate().GoToUrl($"{BaseUrl}/admin/audit-log");
        _wait.Until(d => d.PageSource.Contains("Audit Log") || d.Url.Contains("/audit-log"));

        // 3. Find entry by item ID
        var searchInput = _wait.Until(d => d.FindElement(By.CssSelector("input[name*='id'], input[placeholder*='ID'], input[type='search']")));
        searchInput.Clear();
        searchInput.SendKeys("ITEM-001"); // Known test item ID

        var searchBtn = _driver.FindElement(By.CssSelector("button:has(text('Search'), text('Find'), text('Go'))"));
        searchBtn.Click();

        _wait.Until(d => d.PageSource.Contains("ITEM-001") || d.PageSource.Contains("Found"));

        // 4. Verify audit log entry is found
        Assert.Contains("ITEM-001", _driver.PageSource);

        // 5. Verify entry details are visible
        Assert.Contains("Action", _driver.PageSource);
        Assert.Contains("Timestamp", _driver.PageSource);
    }

    #endregion
}
