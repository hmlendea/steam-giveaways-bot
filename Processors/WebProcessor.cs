using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SteamGiveawaysBot.Processors
{
    public class WebProcessor : IProcessor
    {

        public string Name => GetType().Name.Replace("Processor", string.Empty);

        public List<string> WindowHandles { get; private set; }

        public List<string> DriverWindowHandles => driver.WindowHandles.ToList();

        public string CurrentProcessorHandle { get; private set; }

        protected Random Random { get; set; }

        readonly IWebDriver driver;

        protected const int DefaultWait = 500;

        static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        public WebProcessor(IWebDriver driver)
        {
            this.driver = driver;

            Random = new Random();

            WindowHandles = new List<string>();
            NewHandle();
        }

        public void Close()
        {
            foreach(string handle in WindowHandles.ToList())
            {
                CloseHandle(handle);
            }

            driver.SwitchTo().Window(driver.WindowHandles[0]);
        }

        protected void FocusHandle(string handle)
        {
            if (handle == driver.CurrentWindowHandle)
            {
                return;
            }

            if (!WindowHandles.Contains(handle))
            {
                throw new ArgumentOutOfRangeException("The specified handle does not belong to this processor");
            }
            
            CurrentProcessorHandle = handle;
            driver.SwitchTo().Window(handle);
        }

        protected void FocusHandle(int index)
        {
            FocusHandle(WindowHandles[index]);
        }

        protected string NewHandle(string url = "about:blank")
        {
            driver.SwitchTo().Window(driver.WindowHandles[0]);
            string newTabScript =
                "var d=document,a=d.createElement('a');" +
                "a.target='_blank';a.href='" + url + "';" +
                "a.innerHTML='new tab';" +
                "d.body.appendChild(a);" +
                "a.click();" +
                "a.parentNode.removeChild(a);";

            List<string> oldWindowHandles = driver.WindowHandles.ToList();

            IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)driver;
            scriptExecutor.ExecuteScript(newTabScript);

            List<string> newWindowHandles = driver.WindowHandles.ToList();
            string openedWindowHandle = newWindowHandles.Except(oldWindowHandles).Single();

            WindowHandles.Add(openedWindowHandle);

            FocusHandle(openedWindowHandle);

            return openedWindowHandle;
        }

        protected void CloseHandle(string handle)
        {
            if (!WindowHandles.Contains(handle))
            {
                throw new ArgumentOutOfRangeException("The specified handle does not belong to this processor");
            }
            
            driver.SwitchTo().Window(handle).Close();
            WindowHandles.Remove(handle);
        }

        protected void GoToUrl(string url)
        {
            FocusHandle(CurrentProcessorHandle);

            if (driver.Url == url)
            {
                return;
            }

            driver.Navigate().GoToUrl(url);
        }

        protected void SwitchToIframe(int index)
        {
            FocusHandle(CurrentProcessorHandle);
            driver.SwitchTo().Frame(index);
        }

        protected void SwitchToIframe(By selector)
        {
            SwitchToIframe(selector, DefaultTimeout);
        }

        protected void SwitchToIframe(By selector, TimeSpan timeout)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout)
            {
                try
                {
                    IWebElement iframe = GetElement(selector);
                    driver.SwitchTo().Frame(iframe);
                }
                finally
                {
                    Wait(DefaultWait);
                }
            }
        }

        protected void ExecuteScript(string script)
        {
            FocusHandle(CurrentProcessorHandle);

            IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)driver;
            scriptExecutor.ExecuteScript(script);
        }

        protected IWebElement GetElement(By selector)
        {
            return GetElement(selector, DefaultTimeout);
        }

        protected IWebElement GetElement(By selector, TimeSpan timeout)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout)
            {
                try
                {
                    IWebElement element = driver.FindElement(selector);
                    
                    if (element != null && element.Displayed)
                    {
                        return element;
                    }
                }
                catch {  }
                finally
                {
                    Wait(DefaultWait);
                }
            }

            return null;
        }

        protected List<IWebElement> GetElements(By selector)
        {
            return GetElements(selector, DefaultTimeout);
        }

        protected List<IWebElement> GetElements(By selector, TimeSpan timeout)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout)
            {
                try
                {
                    List<IWebElement> elements = driver.FindElements(selector).ToList();

                    if (elements != null && elements.Count > 0)
                    {
                        return elements;
                    }
                }
                catch {  }
                finally
                {
                    Wait(DefaultWait);
                }
            }

            return null;
        }

        protected void Wait(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        protected void WaitUntilTextLength(By selector, int length)
        {
            FocusHandle(CurrentProcessorHandle);

            bool conditionMet = false;

            while (!conditionMet)
            {
                Wait(DefaultWait);
                conditionMet = GetElement(selector).GetAttribute("value").Length == length;
            }
        }

        protected void WaitUntilAnyElementExists(params By[] selectors)
        {
            FocusHandle(CurrentProcessorHandle);

            bool conditionMet = false;

            while (!conditionMet)
            {
                conditionMet = selectors.Any(ElementVisible);
                Wait(DefaultWait);
            }
        }

        protected void WaitUntilElementExists(By selector)
        {
            FocusHandle(CurrentProcessorHandle);

            bool conditionMet = false;

            while (!conditionMet)
            {
                Wait(DefaultWait);
                conditionMet = ElementVisible(selector);
            }
        }

        protected bool ElementVisible(By selector)
        {
            FocusHandle(CurrentProcessorHandle);

            try
            {
                IWebElement element = driver.FindElement(selector);
                return element.Displayed;
            }
            catch
            {
                return false;
            }
        }

        protected void SendKeys(By selector, string text)
        {
            IWebElement element = GetElement(selector);
            element.SendKeys(text);
        }

        protected void SetText(By selector, string text)
        {
            IWebElement element = GetElement(selector);
            element.Clear();
            element.SendKeys(text);

            if (!GetElement(selector).Text.Equals(text) &&
                !GetElement(selector).GetAttribute("value").Equals(text))
            {
                element.Clear();
                SetText(selector, text);
            }
        }

        protected void ClickAny(params By[] selectors)
        {
            bool clicked = false;

            foreach (By selector in selectors)
            {
                if (ElementVisible(selector))
                {
                    Click(selector);

                    clicked = true;
                    break;
                }
            }

            if (!clicked)
            {
                // TODO: Throw a proper exception
                throw new Exception("No element to click");
            }
        }

        protected void Click(By selector)
        {
            IWebElement element = GetElement(selector);
            element.Click();
        }

        protected void SelectByIndex(By selector, int index)
        {
            IWebElement element = GetElement(selector);
            SelectElement selectElement = new SelectElement(element);

            selectElement.SelectByIndex(index);
        }

        protected void SelectByValue(By selector, object value)
        {
            IWebElement element = GetElement(selector);
            SelectElement selectElement = new SelectElement(element);

            string stringValue;

            if (value is string)
            {
                stringValue = (string)value;
            }
            else
            {
                stringValue = value.ToString();
            }

            selectElement.SelectByValue(stringValue);
        }

        protected void SelectRandom(By selector)
        {
            IWebElement element = GetElement(selector);
            SelectElement selectElement = new SelectElement(element);
            
            int option = Random.Next(0, selectElement.Options.Count);
            selectElement.SelectByIndex(option);
        }

        protected string GetSelectValue(By selector)
        {
            IWebElement element = GetElement(selector);
            SelectElement selectElement = new SelectElement(element);
            
            return selectElement.SelectedOption.Text;
        }
    }
}
