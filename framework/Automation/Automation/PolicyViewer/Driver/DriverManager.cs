using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyViewer.Driver
{
    public class DriverManager
    {
        private static ThreadLocal<IWebDriver?> _driver = new ThreadLocal<IWebDriver?>();
        private DriverManager()
        {

        }

        public static IWebDriver GetDriver()
        {
            if (_driver.Value == null)
            {
                InitializeDriver();
            }
            return _driver.Value!;
        }

        private static void InitializeDriver()
        {
            var chromeOptions = new ChromeOptions();
            // chromeOptions.AddArgument("--headless"); // Uncomment if headless mode is needed
            // Temporary fix to handle ElementClickInterceptedException
            chromeOptions.AddArgument("force-device-scale-factor=0.50");
            _driver.Value = new ChromeDriver(chromeOptions);
        }
        public static void QuitDriver()
        {
            if (_driver.Value != null)
            {
                _driver.Value.Quit();
                _driver.Value = null;
            }
        }
    }
}
