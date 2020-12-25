<Query Kind="Statements">
  <NuGetReference>Nito.BrowserBoss</NuGetReference>
  <Namespace>Nito.BrowserBoss</Namespace>
  <Namespace>OpenQA.Selenium</Namespace>
</Query>

Boss.Browser.WebDriver.Manage().Window.Maximize();

// Log in to MPerks
Boss.Url = "https://www.meijer.com/mperks";
Boss.Click("Sign In");
Boss.Write("[name='email']", "meijer.ourteddybear@xoxy.net");
Boss.Click("Next");
Boss.Write("[name='password']", Util.GetPassword("mPerks password"));
Boss.Click("Sign In");

Boss.Click("Coupons");

while (true)
{
	try { Boss.Click(".coupons-grid__load-more"); }
	catch { break; }
}

try { Boss.Click(".coupons-grid__button--clip"); }
catch (UnhandledAlertException)	{ }
