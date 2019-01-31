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

        protected const int DefaultWait = 333;

        static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

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

        protected void FocusHandle(int index) => FocusHandle(WindowHandles[index]);
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

        protected string NewHandle() => NewHandle("about:blank");
        protected string NewHandle(string url)
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

        protected void GoToIframe(By selector)
        {
            string iframeSource = GetSource(selector);
            GoToUrl(iframeSource);
        }

        protected void SwitchToIframe(int index)
        {
            FocusHandle(CurrentProcessorHandle);
            driver.SwitchTo().Frame(index);
        }
        protected void SwitchToIframe(By selector) => SwitchToIframe(selector, DefaultTimeout);
        protected void SwitchToIframe(By selector, TimeSpan timeout)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout)
            {
                try
                {
                    IWebElement iframe = GetElement(selector, timeout);
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

        protected void AcceptAlert() => AcceptAlert(DefaultTimeout);
        protected void AcceptAlert(TimeSpan timeout)
        {
            IAlert alert = GetAlert(timeout);
            alert.Accept();
        }

        protected void DismissAlert() => DismissAlert(DefaultTimeout);
        protected void DismissAlert(TimeSpan timeout)
        {
            IAlert alert = GetAlert(timeout);
            alert.Dismiss();
        }

        protected List<IWebElement> GetElements(By selector) => GetElements(selector, DefaultTimeout);

        protected string GetAttribute(By selector, string attribute) => GetAttribute(selector, attribute, DefaultTimeout);
        protected string GetAttribute(By selector, string attribute, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);
            return element.GetAttribute(attribute);
        }

        protected string GetClass(By selector) => GetClass(selector, DefaultTimeout);
        protected string GetClass(By selector, TimeSpan timeout) => GetAttribute(selector, "class", timeout);

        protected IEnumerable<string> GetClasses(By selector) => GetClasses(selector, DefaultTimeout);
        protected IEnumerable<string> GetClasses(By selector, TimeSpan timeout) => GetAttribute(selector, "class", timeout).Split(' ');

        protected string GetHyperlink(By selector) => GetHyperlink(selector, DefaultTimeout);
        protected string GetHyperlink(By selector, TimeSpan timeout) => GetAttribute(selector, "href", timeout);

        protected string GetSource(By selector) => GetSource(selector, DefaultTimeout);
        protected string GetSource(By selector, TimeSpan timeout) => GetAttribute(selector, "src", timeout);

        protected string GetValue(By selector) => GetValue(selector, DefaultTimeout);
        protected string GetValue(By selector, TimeSpan timeout) => GetAttribute(selector, "value", timeout);

        protected string GetText(By selector) => GetText(selector, DefaultTimeout);
        protected string GetText(By selector, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);
            return element.Text;
        }

        protected string GetSelectedText(By selector) => GetSelectedText(selector, DefaultTimeout);
        protected string GetSelectedText(By selector, TimeSpan timeout)
        {
            SelectElement element = GetSelectElement(selector, timeout);
            return element.SelectedOption.Text;
        }

        protected void SetText(By selector, string text) => SetText(selector, text, DefaultTimeout);
        protected void SetText(By selector, string text, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);

            element.Clear();
            element.SendKeys(text);

            if (!GetElement(selector, timeout).Text.Equals(text) &&
                !GetElement(selector, timeout).GetAttribute("value").Equals(text))
            {
                element.Clear();
                SetText(selector, text);
            }
        }

        protected void AppendText(By selector, string text) => AppendText(selector, text, DefaultTimeout);
        protected void AppendText(By selector, string text, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);
            element.SendKeys(text);
        }

        protected bool HasClass(By selector, string className) => HasClass(selector, className, DefaultTimeout);
        protected bool HasClass(By selector, string className, TimeSpan timeout)
        {
            IEnumerable<string> classes = GetClasses(selector, timeout);
            return classes.Contains(className);
        }

        protected bool IsSelected(By selector) => IsSelected(selector, DefaultTimeout);
        protected bool IsSelected(By selector, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);
            return element.Selected;
        }

        protected void Wait() => Wait(DefaultWait);
        protected void Wait(TimeSpan timeSpan) => Wait((int)timeSpan.TotalMilliseconds);
        protected void Wait(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        protected void WaitUntilTextLength(By selector, int length) => WaitUntilTextLength(selector, length, DefaultTimeout);
        protected void WaitUntilTextLength(By selector, int length, bool waitIndefinetely)
        {
            if (waitIndefinetely)
            {
                WaitUntilTextLength(selector, length, TimeSpan.FromDays(873));
            }
            else
            {
                WaitUntilTextLength(selector, length, DefaultTimeout);
            }
        }
        protected void WaitUntilTextLength(By selector, int length, TimeSpan timeout)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout)
            {
                bool conditionMet =
                    GetValue(selector, timeout).Length == length ||
                    GetText(selector, timeout).Length == length;

                if (conditionMet)
                {
                    break;
                }

                Wait();
            }
        }

        protected void WaitUntilAnyElementExists(params By[] selectors) => WaitUntilAnyElementExists(DefaultTimeout, selectors);
        protected void WaitUntilAnyElementExists(bool waitIndefinetely, params By[] selectors)
        {
            if (waitIndefinetely)
            {
                WaitUntilAnyElementExists(TimeSpan.FromDays(873), selectors);
            }
            else
            {
                WaitUntilAnyElementExists(DefaultTimeout, selectors);
            }
        }
        protected void WaitUntilAnyElementExists(TimeSpan timeout, params By[] selectors)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout &&
                   selectors.Any(ElementVisible))
            {
                Wait();
            }
        }

        protected void WaitUntilElementExists(By selector) => WaitUntilElementExists(selector, DefaultTimeout);
        protected void WaitUntilElementExists(By selector, bool waitIndefinetely)
        {
            if (waitIndefinetely)
            {
                WaitUntilElementExists(selector, TimeSpan.FromDays(873));
            }
            else
            {
                WaitUntilElementExists(selector, DefaultTimeout);
            }
        }
        protected void WaitUntilElementExists(By selector, TimeSpan timeout)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout &&
                   !ElementVisible(selector))
            {
                Wait();
            }
        }

        protected bool AnyElementVisible(params By[] selectors) => selectors.Any(ElementVisible);
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

        protected void Click(By selector) => Click(selector, DefaultTimeout);
        protected void Click(By selector, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);
            element.Click();
        }
        protected void Click(By selector, bool waitToExist)
        {
            if (waitToExist)
            {
                WaitUntilElementExists(selector);
            }

            Click(selector);
        }

        protected void CheckCheckbox(By selector) => CheckCheckbox(selector, DefaultTimeout);
        protected void CheckCheckbox(By selector, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);

            if (!element.Selected)
            {
                Click(selector, timeout);
            }
        }

        protected void UncheckCheckbox(By selector) => UncheckCheckbox(selector, DefaultTimeout);
        protected void UncheckCheckbox(By selector, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);

            if (element.Selected)
            {
                Click(selector, timeout);
            }
        }

        protected void SelectByIndex(By selector, int index) => SelectByIndex(selector, index, DefaultTimeout);
        protected void SelectByIndex(By selector, int index, TimeSpan timeout)
        {
            SelectElement element = GetSelectElement(selector, timeout);
            element.SelectByIndex(index);
        }

        protected void SelectByValue(By selector, object value) => SelectByValue(selector, value, DefaultTimeout);
        protected void SelectByValue(By selector, object value, TimeSpan timeout)
        {
            SelectElement element = GetSelectElement(selector, timeout);

            string stringValue;

            if (value is string)
            {
                stringValue = (string)value;
            }
            else
            {
                stringValue = value.ToString();
            }

            element.SelectByValue(stringValue);
        }

        protected void SelectRandom(By selector) => SelectRandom(selector, DefaultTimeout);
        protected void SelectRandom(By selector, TimeSpan timeout)
        {
            SelectElement element = GetSelectElement(selector, timeout);

            int option = Random.Next(0, element.Options.Count);
            element.SelectByIndex(option);
        }
        
        private IWebElement GetElement(By selector, TimeSpan timeout)
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
                    Wait();
                }
            }

            return null;
        }
        
        private SelectElement GetSelectElement(By selector, TimeSpan timeout)
        {
            IWebElement element = GetElement(selector, timeout);
            SelectElement selectElement = new SelectElement(element);

            return selectElement;
        }
        
        private List<IWebElement> GetElements(By selector, TimeSpan timeout)
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
 
        private IAlert GetAlert(TimeSpan timeout)
        {
            FocusHandle(CurrentProcessorHandle);

            DateTime beginTime = DateTime.Now;

            while (DateTime.Now - beginTime < timeout)
            {
                try
                {
                    IAlert alert = driver.SwitchTo().Alert();
                    return alert;
                }
                catch {  }
                finally
                {
                    Wait();
                }
            }

            return null;
        }
    }
}
