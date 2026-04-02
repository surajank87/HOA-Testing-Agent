using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.PageObjects;
using SeleniumExtras.WaitHelpers;

namespace PolicyViewer.Pages
{
    public abstract class BasePage
    {
        protected IWebDriver _driver;
        protected WebDriverWait _wait;
        public BasePage(IWebDriver driver)
        {
            _driver = driver;
            PageFactory.InitElements(driver, this);
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
        }
        protected void SwitchToLastWindow()
        {
            _wait.Until(driver => driver.WindowHandles.Count > 1);
            string[] windowlist = _driver.WindowHandles.ToArray();
            _driver.SwitchTo().Window(windowlist[^1]);
        }
        protected IWebElement WaitUntilElementIsVisible(By locator)
        {
            return _wait.Until(ExpectedConditions.ElementIsVisible(locator));
        }

        protected IWebElement WaitUntilElementIsClickable(By locator)
        {
            return _wait.Until(ExpectedConditions.ElementToBeClickable(locator));
        }
    }
}
