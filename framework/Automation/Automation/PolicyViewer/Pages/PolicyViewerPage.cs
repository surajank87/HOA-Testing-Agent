using AventStack.ExtentReports.Reporter.Config;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.PageObjects;
using SeleniumExtras.WaitHelpers;
using Utility.Enum;
using WindowsInput;
using WindowsInput.Native;

namespace PolicyViewer.Pages
{
    public class PolicyViewerPage(IWebDriver driver) : BasePage(driver)
    {
        [FindsBy(How.Id, "Tenant")]
        private readonly IWebElement tenants;
        [FindsBy(How.XPath, "//a[text()='Search Policy']")]
        private readonly IWebElement searchPolicy;
        [FindsBy(How.XPath, "//form[@action='/PolicyViewer/ViewPolicy']//input[@id='policyFriendlyId']")]
        private readonly IWebElement friendlyIdSearchBox;
        [FindsBy(How.Name, "search")]
        private readonly IWebElement searchButton;
        [FindsBy(How.XPath, "//button[contains(text(), 'Show Stage Details')]")]
        private readonly IWebElement showStageDetailsButton;
        private string _currentQuotePolicyViewerUrl;

        public void NavigateToPolicyViewerAndShowStageDetails(string PolicyViewerUrl, Tenant Tenant, string friendlyId)
        {
            _driver.Navigate().GoToUrl(PolicyViewerUrl);
           
            SelectElement tenantsDropDown = new SelectElement(tenants);
            tenantsDropDown.SelectByValue(Tenant.ToString().ToUpper());
            
            searchPolicy.Click();
            friendlyIdSearchBox.SendKeys(friendlyId);
            searchButton.Click();
            _currentQuotePolicyViewerUrl = RemoveCredentialsFromUrl(_driver.Url);
            showStageDetailsButton.Click();
        }
        private string RemoveCredentialsFromUrl(string Url)
        {
            Uri uri = new Uri(_driver.Url);
            return $"{uri.Scheme}://{uri.Host}{uri.PathAndQuery}";
        }
        public string GetCurrentQuotePolicyViewerUrl()
        {
            return _currentQuotePolicyViewerUrl;
        }
        public string GetSubmissionStatus(string carrier, string lob)
        {
            IWebElement statusField = WaitUntilElementIsVisible(By.XPath($"//tr[td[text()='{carrier}' and following-sibling::td[text()='{lob}']]]/td[8]"));
            return statusField.Text;
        }

        public string GetText(string carrier, string lob, string buttonText)
        {
            By buttonBy = By.XPath($"//tr[td[text()='{carrier}' and following-sibling::td[text()='{lob}']]]/td[15]/a[text()='{buttonText}']");
            IWebElement button = WaitUntilElementIsClickable(buttonBy);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", button);
            try
            {
                button.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", button);
            }
            string policyViewerWindow = _driver.CurrentWindowHandle;
            SwitchToLastWindow();
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
            IWebElement textArea = wait.Until(ExpectedConditions.ElementIsVisible(By.TagName("textarea")));
            string text = textArea.Text;
            _driver.Close();
            _driver.SwitchTo().Window(policyViewerWindow);
            return text;
        }
    }
}
