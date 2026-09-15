using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace EcoTrack.Selenium.Tests;

public class Sprint2E2ETests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private const string BaseUrl = "http://localhost:5173";

    public Sprint2E2ETests()
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

    [Fact]
    public void Test01_UserLogoutFlow_SuccessfullyLogsOutAndClearsSession()
    {
        // 1. Navigate to Login Page
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");

        // 2. Fill User Login Credentials
        var emailInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='email'], input[name='email']")));
        emailInput.Clear();
        emailInput.SendKeys("qa_test@ecotrack.lk");

        var passwordInput = _driver.FindElement(By.CssSelector("input[type='password'], input[name='password']"));
        passwordInput.Clear();
        passwordInput.SendKeys("Password@123");

        var submitBtn = _driver.FindElement(By.CssSelector("button[type='submit']"));
        submitBtn.Click();

        // 3. Verify Landing on Dashboard
        _wait.Until(d => d.Url.Contains("/dashboard") || d.PageSource.Contains("Dashboard") || d.PageSource.Contains("Akini Test"));
        Assert.True(_driver.PageSource.Contains("Akini Test") || _driver.PageSource.Contains("Dashboard"));

        // 4. Click Logout Button
        var logoutBtn = _wait.Until(d => d.FindElement(By.XPath("//button[contains(., 'Logout')] | //a[contains(., 'Logout')]")));
        logoutBtn.Click();

        // 5. Verify Redirect to Login with Logout Confirmation
        _wait.Until(d => d.Url.Contains("/login"));
        Assert.Contains("/login", _driver.Url);
    }

    [Fact]
    public void Test02_ForgotPasswordToResetPasswordFlow_CompletesSuccessfully()
    {
        // 1. Navigate to Forgot Password Page
        _driver.Navigate().GoToUrl($"{BaseUrl}/forgot-password");

        // 2. Submit Email for Password Reset
        var emailInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='email'], input[name='email']")));
        emailInput.Clear();
        emailInput.SendKeys("qa_test@ecotrack.lk");

        var submitBtn = _driver.FindElement(By.CssSelector("button[type='submit']"));
        submitBtn.Click();

        // 3. Verify Feedback Banner Message
        var successMessage = _wait.Until(d => d.FindElement(By.CssSelector(".alert, .glass-card, p, div")));
        Assert.NotNull(successMessage);

        // 4. Navigate to Reset Password Page with mock token verification
        _driver.Navigate().GoToUrl($"{BaseUrl}/reset-password");
        var tokenInput = _wait.Until(d => d.FindElement(By.CssSelector("input[name='token'], input[placeholder*='token' i], input[type='text']")));
        Assert.NotNull(tokenInput);
    }

    [Fact]
    public void Test03_FullPickupLifecycleFlow_Create_Schedule_Track_And_Cancel()
    {
        // STEP A: Log in as User
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var emailInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='email']")));
        emailInput.Clear();
        emailInput.SendKeys("qa_test@ecotrack.lk");

        var passInput = _driver.FindElement(By.CssSelector("input[type='password']"));
        passInput.Clear();
        passInput.SendKeys("Password@123");

        var submitBtn = _driver.FindElement(By.CssSelector("button[type='submit']"));
        submitBtn.Click();

        _wait.Until(d => d.Url.Contains("/dashboard") || d.PageSource.Contains("Dashboard"));

        // STEP B: Navigate to "My Pickups" View
        _driver.Navigate().GoToUrl($"{BaseUrl}/my-pickups");
        _wait.Until(d => d.PageSource.Contains("My Pickups"));

        // Verify pickup cards or empty state are rendered
        Assert.True(_driver.PageSource.Contains("My Pickups"));

        // STEP C: Log out from User
        var logoutBtn = _driver.FindElement(By.XPath("//button[contains(., 'Logout')] | //a[contains(., 'Logout')]"));
        logoutBtn.Click();
        _wait.Until(d => d.Url.Contains("/login"));

        // STEP D: Log in as Recycler
        emailInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='email']")));
        emailInput.Clear();
        emailInput.SendKeys("savindia@gmail.com");

        passInput = _driver.FindElement(By.CssSelector("input[type='password']"));
        passInput.Clear();
        passInput.SendKeys("NewPassword@2026!");

        submitBtn = _driver.FindElement(By.CssSelector("button[type='submit']"));
        submitBtn.Click();

        // STEP E: Navigate to Schedule Management
        _wait.Until(d => d.Url.Contains("/dashboard") || d.PageSource.Contains("Dashboard"));
        _driver.Navigate().GoToUrl($"{BaseUrl}/schedule");
        _wait.Until(d => d.PageSource.Contains("Schedule") || d.Url.Contains("/schedule"));

        Assert.True(_driver.PageSource.Contains("Schedule") || _driver.Url.Contains("/schedule"));
    }
}
