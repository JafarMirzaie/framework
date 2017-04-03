﻿using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.React.Selenium.ModalProxies
{
    public class MessageModalProxy : ModalProxy
    {
        public RemoteWebDriver Selenium { get; private set; }

        public MessageModalProxy(IWebElement element) : base(element)
        {
            this.Selenium = element.GetDriver();
        
            if (!this.Element.HasClass("modal", "fade", "in"))
                throw new InvalidOperationException("Not a valid modal");
        }

        public IWebElement GetButton(MessageModalButton button)
        {
            var className =
                button == MessageModalButton.Yes ? "sf-yes-button": 
                button == MessageModalButton.No ? "sf-no-button":
                button == MessageModalButton.Ok ? "sf-ok-button" :
                button == MessageModalButton.Cancel ? "sf-cancel-button" :
            throw new NotImplementedException("Unexpected button");

            return this.Element.FindElement(By.ClassName(className));
        }

        public void Click(MessageModalButton button)
        {
            this.GetButton(button).ButtonClick();
        }

        public static string GetMessageText(RemoteWebDriver selenium, MessageModalProxy modal)
        {
            Message = modal.Element.FindElement(By.ClassName("text-warning")).Text;
            return Message;
        }

        public static string GetMessageTitle(RemoteWebDriver selenium, MessageModalProxy modal)
        {
            Title = modal.Element.FindElement(By.ClassName("modal-title")).Text;
            return Title;
        }

        public static string Message { get; set; }
        public static string Title { get; set; }
    }

    public static class MessageModalProxyExtensions
    {
        public static bool IsMessageModalPresent(this RemoteWebDriver selenium)
        {
            try
            {
                var ret = GetMessageModal(selenium);
                if (ret != null)
                    return true;
                return false;
            }
            catch (NoAlertPresentException)
            {
                return false;
            }
        }

        public static MessageModalProxy GetMessageModal(this RemoteWebDriver selenium)
        {
            try
            {
                var element = selenium.FindElementByClassName("message-modal");
                return new MessageModalProxy(element);
            }
            catch (Exception e)
            {
                throw e;
            }

        }  

        public static void CloseMessageModal(this RemoteWebDriver selenium, MessageModalButton button)
        {
            var messagePresent = selenium.Wait(() => IsMessageModalPresent(selenium));

            var message = selenium.Wait(() => GetMessageModal(selenium)); 

            message.Click(button);
        }
    }

    public enum MessageModalButton
    {
        Yes,
        No,
        Ok,
        Cancel
    }
}
