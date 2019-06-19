<Query Kind="Program">
  <NuGetReference>Nito.BrowserBoss</NuGetReference>
  <Namespace>Nito.BrowserBoss</Namespace>
</Query>

void Main()
{
	Boss.Browser.WebDriver.Manage().Window.Maximize();

	// Log in to Cigna
	Boss.Url = "https://www.cigna.com/";
	var cignaWindow = Boss.Browser.WebDriver.CurrentWindowHandle;
	Boss.Click(@"//*[@id=""includes-content""]/div[1]/nav[2]/div/ul/li[2]/a"); //Boss.Click("Log in to myCigna");
	var loginWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != cignaWindow);
	Boss.Browser.WebDriver.Close();
	Boss.Browser.WebDriver.SwitchTo().Window(loginWindow);
	Boss.Write("#username", Util.GetPassword("Cigna Username"));
	Boss.Write("#password", Util.GetPassword("Cigna Password"));
	Boss.Click("Log In");
	Boss.Click("Not Now, Thanks");
	
	// Manage HSA investments
	Boss.Click("Manage HSA");
	Thread.Sleep(3000);
	var hsabankWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != loginWindow);
	Boss.Browser.WebDriver.Close();
	Boss.Browser.WebDriver.SwitchTo().Window(hsabankWindow);
	Boss.Click("Manage Investments");
	Boss.Browser.WebDriver.SwitchTo().Frame(Boss.Find("#ctl00_ctl00_ctl00_ctl00_PageContent_Body_Body_Body_IFrame").WebElement);
	
	// Access Devenir
	Boss.Write("#DevenirInvestmentListOptions", "Access DEVENIR");
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
	if (amount == 0M)
	{
		Boss.Browser.WebDriver.Close();
	}
	else
	{
		Boss.Find("#TransferAmountTextBox").Write(amount.ToString());
		// Leave the window open
	}

	// Open Transaction History and Investment Summary
	Boss.Browser.WebDriver.SwitchTo().Window(devinerWindow);
	Boss.Url = "https://hsainvestment.com/Participant/aspx/authenticated/TransactionHistory.aspx";
	Boss.Click("Show Detailed View");
	Boss.Browser.Script($"window.open('https://hsainvestment.com/Participant/aspx/authenticated/FundBalance.aspx','_blank')");
	Boss.Browser.WebDriver.SwitchTo().Window(devinerWindow);
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