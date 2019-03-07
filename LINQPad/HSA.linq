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
	Boss.Click("Log in to myCigna");
	var loginWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != cignaWindow);
	Boss.Browser.WebDriver.Close();
	Boss.Browser.WebDriver.SwitchTo().Window(loginWindow);
	var credentials = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HSAcredentials.txt")).Split('\n').Select(x => x.Trim()).Where(x => x != "").ToList();
	Boss.Write("#username", credentials[0]);
	Boss.Write("#password", credentials[1]);
	Boss.Click("login");
	Boss.Click(" Not Now, Thanks "); // TODO: ignore whitespace
	
	// Manage HSA investments
	Boss.Click("Manage HSA ");
	Thread.Sleep(3000);
	var hsabankWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != loginWindow);
	Boss.Browser.WebDriver.Close();
	Boss.Browser.WebDriver.SwitchTo().Window(hsabankWindow);
	Boss.Click("Manage Investments");
	Boss.Browser.WebDriver.SwitchTo().Frame(Boss.Find("#ctl00_ctl00_ctl00_ctl00_PageContent_Body_Body_Body_IFrame").WebElement);
	
	// Access Devenir
	new OpenQA.Selenium.Support.UI.SelectElement(Boss.Find("#DevenirInvestmentListOptions").WebElement).SelectByValue("AccessDevenir");
	//Boss.Write("#DevenirInvestmentListOptions", "Access DEVENIR"); // TODO: does not work well with iframes
	Boss.Browser.WebDriver.SwitchTo().Alert().Accept();
	Thread.Sleep(3000);
	var devinerWindow = Boss.Browser.WebDriver.WindowHandles.Single(x => x != hsabankWindow);
	
	// Determine if we have any amount to transfer from HSA Bank to Devenir
	Boss.Browser.WebDriver.SwitchTo().Window(hsabankWindow);
	Boss.Browser.WebDriver.SwitchTo().Frame(Boss.Find("#ctl00_ctl00_ctl00_ctl00_PageContent_Body_Body_Body_IFrame").WebElement);
	new OpenQA.Selenium.Support.UI.SelectElement(Boss.Find("#DevenirInvestmentListOptions").WebElement).SelectByValue("TransferToInvestments");
	new OpenQA.Selenium.Support.UI.SelectElement(Boss.Find("#ContentPlaceHolder1_TransferFromDropDownList").WebElement).SelectByText("HSA ****3832");
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