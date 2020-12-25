<Query Kind="Statements">
  <NuGetReference>Nito.BrowserBoss</NuGetReference>
  <Namespace>Nito.BrowserBoss</Namespace>
  <Namespace>OpenQA.Selenium</Namespace>
  <Namespace>OpenQA.Selenium.Interactions</Namespace>
</Query>

Boss.Browser.WebDriver.Manage().Window.Maximize();

// Log in to Standard
Boss.Url = "https://standard.com/";
var flashWindow = Boss.Browser.WebDriver.CurrentWindowHandle;
Boss.FindElements("Log In").First().Click();
var loginWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != flashWindow);
Boss.Browser.WebDriver.Close();
Boss.Browser.WebDriver.SwitchTo().Window(loginWindow);
Boss.Browser.WebDriver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie("deviceToken", Util.GetPassword("401(k) cookie"), "/"));
Boss.Write("#lgn-username-input", Util.GetPassword("401(k) username"));
Boss.Write("#lgn-password-input", Util.GetPassword("401(k) password"));
Boss.Click("#login-button");

// Open transaction history
Boss.Click("Go to My Account");
var actions = new Actions(Boss.Browser.WebDriver);
actions.MoveToElement(Boss.Find("My Account").WebElement);
actions.Click(Boss.Find("Account Activity").WebElement);
actions.Perform();
Boss.Click("#pageViewDropdown");
Boss.Click("By Investment");

// Open current balance
var transactionHistoryWindow = Boss.Browser.WebDriver.CurrentWindowHandle;
Boss.Browser.Script($"window.open('/retirement/employee/investments','_blank')");
var currentBalanceWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != transactionHistoryWindow);
Boss.Browser.WebDriver.SwitchTo().Window(currentBalanceWindow);
Boss.Click("#ViewByButton");
Boss.Click("Source & Investment");

Boss.Browser.WebDriver.SwitchTo().Window(transactionHistoryWindow);