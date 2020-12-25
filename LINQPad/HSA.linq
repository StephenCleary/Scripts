<Query Kind="Program">
  <NuGetReference>Nito.BrowserBoss</NuGetReference>
  <Namespace>Nito.BrowserBoss</Namespace>
  <Namespace>OpenQA.Selenium.Interactions</Namespace>
</Query>

void Main()
{
	Boss.Browser.WebDriver.Manage().Window.Maximize();
	
	// Log in to Cigna
	Boss.Url = "https://my.cigna.com/";
	var loginWindow = Boss.Browser.WebDriver.CurrentWindowHandle;
	Thread.Sleep(1000);
	Boss.Write("#username", Util.GetPassword("Cigna Username"));
	Boss.Write("#password", Util.GetPassword("Cigna Password"));
	Boss.Click("Log In");
	Boss.Click("Close");
	//try { Boss.Click("Not Now, Thanks"); } catch { }
	
	// Manage HSA investments
	Boss.Click("Manage HSA");
	Thread.Sleep(3000);
	var hsabankWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != loginWindow);
	Boss.Browser.WebDriver.Close();
	Boss.Browser.WebDriver.SwitchTo().Window(hsabankWindow);
	Thread.Sleep(3000);
	var actions = new Actions(Boss.Browser.WebDriver);
	actions.MoveToElement(Boss.Find("Accounts").WebElement);
	actions.Click(Boss.Find("Manage Investments").WebElement);
	actions.Perform();
	Boss.Browser.WebDriver.SwitchTo().Frame(Boss.Find("#ctl00_ctl00_ctl00_ctl00_PageContent_Body_Body_Body_IFrame").WebElement);

	// Access Devenir
	Boss.Write("#DevenirInvestmentListOptions", "Access DEVENIR");
	Thread.Sleep(2000);
	Boss.Browser.WebDriver.SwitchTo().Alert().Accept();
	Thread.Sleep(3000);
	var devinerWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != hsabankWindow);
	
	// Determine if we have any amount to transfer from HSA Bank to Devenir
	Boss.Browser.WebDriver.SwitchTo().Window(hsabankWindow);
	Boss.Browser.WebDriver.SwitchTo().Frame(Boss.Find("#ctl00_ctl00_ctl00_ctl00_PageContent_Body_Body_Body_IFrame").WebElement);
	Boss.Write("#DevenirInvestmentListOptions", "TransferToInvestments");
	Boss.Write("#ContentPlaceHolder1_TransferFromDropDownList", "HSA ****3832");
	Thread.Sleep(3000);
	var amount = ParseBalance(Boss.Find("#ContentPlaceHolder1_FundBalanceLabel").Read());
	if (amount == 1000M)
	{
		Boss.Browser.WebDriver.Close();
	}
	else
	{
		Boss.Find("#TransferAmountTextBox").Write((amount - 1000M).ToString());
		// Leave the window open
	}

	// Open Transaction History and Investment Summary
	Boss.Browser.WebDriver.SwitchTo().Window(devinerWindow);
	Boss.Browser.Script($"window.open('https://account.hsainvestments.com/activity/transactions','_blank')");
	var transactionsWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != devinerWindow && x != hsabankWindow);
	Boss.Browser.WebDriver.SwitchTo().Window(transactionsWindow);
	Boss.Click(".transactions-date-picker");
	Boss.Click("This Year");
	Boss.Click("Date");
	Boss.Click("Date");
}

decimal ParseBalance(string balanceLabelText)
{
	var dollar = balanceLabelText.IndexOf('$');
	if (dollar == -1)
		throw new InvalidOperationException($"Could not parse balance label text {balanceLabelText}");
	var endOfAmount = balanceLabelText.IndexOf(' ', dollar);
	if (endOfAmount == -1)
		throw new InvalidOperationException($"Could not parse balance label text {balanceLabelText}");
	return decimal.Parse(balanceLabelText.Substring(dollar + 1, endOfAmount - dollar - 1));
}