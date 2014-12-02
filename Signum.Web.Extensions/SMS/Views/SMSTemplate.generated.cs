﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Signum.Web.SMS.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    
    #line 1 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Engine;
    
    #line default
    #line hidden
    
    #line 6 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Engine.SMS;
    
    #line default
    #line hidden
    
    #line 3 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Entities;
    
    #line default
    #line hidden
    
    #line 2 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Entities.SMS;
    
    #line default
    #line hidden
    
    #line 7 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Utilities;
    
    #line default
    #line hidden
    
    #line 4 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Web;
    
    #line default
    #line hidden
    
    #line 8 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Web.Mailing;
    
    #line default
    #line hidden
    
    #line 5 "..\..\SMS\Views\SMSTemplate.cshtml"
    using Signum.Web.SMS;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/SMS/Views/SMSTemplate.cshtml")]
    public partial class SMSTemplate : System.Web.Mvc.WebViewPage<dynamic>
    {
        public SMSTemplate()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n");

            
            #line 10 "..\..\SMS\Views\SMSTemplate.cshtml"
Write(Html.ScriptCss("~/SMS/Content/SMS.css"));

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n");

            
            #line 12 "..\..\SMS\Views\SMSTemplate.cshtml"
 using (var tc = Html.TypeContext<SMSTemplateEntity>())
{   
    
            
            #line default
            #line hidden
            
            #line 14 "..\..\SMS\Views\SMSTemplate.cshtml"
Write(Html.ValueLine(tc, s => s.Name));

            
            #line default
            #line hidden
            
            #line 14 "..\..\SMS\Views\SMSTemplate.cshtml"
                                    
    
    using (var tc2 = tc.SubContext().Do(sc => sc.LabelColumns = new BsColumn(4)))
    {
        tc2.LabelColumns = new BsColumn(3);

            
            #line default
            #line hidden
WriteLiteral("    <div");

WriteLiteral(" class=\"row\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"col-sm-8\"");

WriteLiteral(">\r\n");

WriteLiteral("            ");

            
            #line 21 "..\..\SMS\Views\SMSTemplate.cshtml"
       Write(Html.ValueLine(tc2, s => s.Active));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 22 "..\..\SMS\Views\SMSTemplate.cshtml"
       Write(Html.ValueLine(tc2, s => s.StartDate));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 23 "..\..\SMS\Views\SMSTemplate.cshtml"
       Write(Html.ValueLine(tc2, s => s.EndDate));

            
            #line default
            #line hidden
WriteLiteral("\r\n        </div>\r\n");

            
            #line 25 "..\..\SMS\Views\SMSTemplate.cshtml"
        
            
            #line default
            #line hidden
            
            #line 25 "..\..\SMS\Views\SMSTemplate.cshtml"
          tc2.LabelColumns = new BsColumn(8);
            
            #line default
            #line hidden
WriteLiteral("\r\n        <div");

WriteLiteral(" class=\"col-sm-4\"");

WriteLiteral(">\r\n");

WriteLiteral("            ");

            
            #line 27 "..\..\SMS\Views\SMSTemplate.cshtml"
       Write(Html.ValueLine(tc2, s => s.Certified));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 28 "..\..\SMS\Views\SMSTemplate.cshtml"
       Write(Html.ValueLine(tc2, s => s.EditableMessage));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 29 "..\..\SMS\Views\SMSTemplate.cshtml"
       Write(Html.ValueLine(tc2, s => s.RemoveNoSMSCharacters));

            
            #line default
            #line hidden
WriteLiteral("\r\n        </div>\r\n    </div>\r\n");

            
            #line 32 "..\..\SMS\Views\SMSTemplate.cshtml"
    }
    
    
            
            #line default
            #line hidden
            
            #line 34 "..\..\SMS\Views\SMSTemplate.cshtml"
Write(Html.ValueLine(tc, s => s.From));

            
            #line default
            #line hidden
            
            #line 34 "..\..\SMS\Views\SMSTemplate.cshtml"
                                     
    
            
            #line default
            #line hidden
            
            #line 35 "..\..\SMS\Views\SMSTemplate.cshtml"
Write(Html.ValueLine(tc, s => s.MessageLengthExceeded));

            
            #line default
            #line hidden
            
            #line 35 "..\..\SMS\Views\SMSTemplate.cshtml"
                                                     
    

            
            #line default
            #line hidden
WriteLiteral("    <div");

WriteLiteral(" class=\"row\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"col-sm-7\"");

WriteLiteral(">\r\n");

WriteLiteral("            ");

            
            #line 39 "..\..\SMS\Views\SMSTemplate.cshtml"
       Write(Html.EntityTabRepeater(tc, e => e.Messages, er =>
            {
                er.PreserveViewData = true;
            }));

            
            #line default
            #line hidden
WriteLiteral("\r\n        </div>\r\n        <div");

WriteLiteral(" class=\"col-sm-5\"");

WriteLiteral(">\r\n            <fieldset>\r\n                <legend>");

            
            #line 46 "..\..\SMS\Views\SMSTemplate.cshtml"
                   Write(SmsMessage.Replacements.NiceToString());

            
            #line default
            #line hidden
WriteLiteral("</legend>\r\n\r\n");

WriteLiteral("                ");

            
            #line 48 "..\..\SMS\Views\SMSTemplate.cshtml"
           Write(Html.EntityCombo(tc, s => s.AssociatedType, ec =>
        {
            ec.LabelColumns = new BsColumn(5);
            ec.Data = SMSLogic.RegisteredDataObjectProviders();
            ec.ComboHtmlProperties["class"] = "sf-associated-type";
            ec.AttachFunction = SMSClient.Module["attachAssociatedType"](ec, Url.Action<SMSController>(s => s.GetLiteralsForType()));
        }));

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n");

WriteLiteral("                ");

            
            #line 56 "..\..\SMS\Views\SMSTemplate.cshtml"
            Write(new HtmlTag("select").Attr("multiple", "multiple").Id("sfLiterals").Class("form-control").ToHtml());

            
            #line default
            #line hidden
WriteLiteral("\r\n                <input");

WriteLiteral(" type=\"button\"");

WriteLiteral(" class=\"btn sf-button\"");

WriteLiteral(" id=\"sfInsertLiteral\"");

WriteAttribute("value", Tuple.Create(" value=\"", 2046), Tuple.Create("\"", 2087)
            
            #line 57 "..\..\SMS\Views\SMSTemplate.cshtml"
       , Tuple.Create(Tuple.Create("", 2054), Tuple.Create<System.Object, System.Int32>(SmsMessage.Insert.NiceToString()
            
            #line default
            #line hidden
, 2054), false)
);

WriteLiteral(" />\r\n\r\n            </fieldset>\r\n        </div>\r\n    </div>\r\n");

WriteLiteral("    <script>\r\n");

WriteLiteral("        ");

            
            #line 63 "..\..\SMS\Views\SMSTemplate.cshtml"
    Write(SMSClient.Module["init"](Url.Action<SMSController>(s => s.RemoveNoSMSCharacters("")),
    Url.Action<SMSController>(s => s.GetDictionaries())));

            
            #line default
            #line hidden
WriteLiteral("\r\n    </script>\r\n");

            
            #line 66 "..\..\SMS\Views\SMSTemplate.cshtml"
}

            
            #line default
            #line hidden
        }
    }
}
#pragma warning restore 1591
