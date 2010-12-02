﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.Properties;
using System.Globalization;
using System.Web.Script.Serialization;

namespace Signum.Web
{
    public class DatePickerOptions
    {
        public string Format { get; set; }

        bool showAge = false;
        /// <summary>
        /// If true it will show age next no datetimepicker and will refresh if the value of datepicker changes
        /// </summary>
        public bool ShowAge
        {
            get { return showAge; }
            set { showAge = value; }
        }
        bool changeMonth = true;
        public bool ChangeMonth
        {
            get { return changeMonth; }
            set { changeMonth = value; }
        }

        bool changeYear = true;
        public bool ChangeYear
        {
            get { return changeYear; }
            set { changeYear = value; }
        }

        int firstDay = 1;
        public int FirstDay
        {
            get { return firstDay; }
            set { firstDay = value; }
        }

        string yearRange = "-90:+10";
        public string YearRange
        {
            get { return yearRange; }
            set { yearRange = value; }
        }

        string showOn = "button";
        public string ShowOn
        {
            get { return showOn; }
            set { showOn = value; }
        }

        bool buttonImageOnly = true;
        public bool ButtonImageOnly
        {
            get { return buttonImageOnly; }
            set { buttonImageOnly = value; }
        }

        string buttonText = Resources.ShowCalendar;
        public string ButtonText
        {
            get { return buttonText; }
            set { buttonText = value; }
        }

        string buttonImageSrc = "Scripts/jqueryui/images/calendar.png";
        public string ButtonImageSrc
        {
            get { return buttonImageSrc; }
            set { buttonImageSrc = value; }
        }

        string minDate;
        public string MinDate
        {
            get { return minDate; }
            set { minDate = value; }
        }

        string maxDate;
        public string MaxDate
        {
            get { return maxDate; }
            set { maxDate = value; }
        }

        bool constrainInput;
        public bool ConstrainInput
        {
            get { return constrainInput; }
            set { constrainInput = value; }
        }

        [ThreadStatic]
        static string defaultculture;
        public static string DefaultCulture
        {
            get { return defaultculture ?? CultureInfo.CurrentCulture.Name.Substring(0, 2); }
            set { defaultculture = value; }
        }
    }

    public static class CalendarHelper
    {
        public static Action<HtmlHelper, StringBuilder> IncludeCss;
        public static string jQueryPrefix = "";
        //jQuery ui DatePicker

        public static MvcHtmlString Calendar(this HtmlHelper helper, string elementId, DatePickerOptions settings)
        {
            StringBuilder sb = new StringBuilder();

            if (IncludeCss != null)
                IncludeCss(helper, sb);
            else
                sb.AppendLine(helper.ScriptCss(
                    "~/scripts/jqueryui/" + jQueryPrefix + "ui.core.css",
                    "~/scripts/jqueryui/" + jQueryPrefix + "ui.datepicker.css",
                    "~/scripts/jqueryui/" + jQueryPrefix + "ui.theme.css").ToHtmlString());

            var context = helper.ViewContext.HttpContext;

            if (context.Items["jqCalendar"] == null)
            {
                sb.AppendLine(GetLocalizationVariables());
                sb.AppendLine(ScriptHtmlHelper.ScriptsJs(helper, "~/signum/scripts/SF_jquery-ui-datepicker-extension.js").ToHtmlString());
                context.Items["jqCalendar"] = true;
            }

            sb.AppendLine(
                "<script type=\"text/javascript\">\n" + 
                "$(function(){\n" +
                "$(\"#" + elementId + "\").datepicker({ " + OptionsToString(settings) +" });\n" + 
                "});\n" + 
                "</script>");

            return MvcHtmlString.Create(sb.ToString());
        }

        static string OptionsToString(DatePickerOptions settings)
        {
            return "changeMonth:{0}, changeYear:{1}, firstDay:{2}, yearRange:'{3}', showOn:'{4}', buttonImageOnly:{5}, buttonText:'{6}', buttonImage:'{7}', constrainInput: {8}{9}{10}{11}".Formato(
                settings.ChangeMonth ? "true" : "false",
                settings.ChangeYear ? "true" : "false",
                settings.FirstDay,
                settings.YearRange,
                settings.ShowOn,
                settings.ButtonImageOnly ? "true" : "false",
                settings.ButtonText,
                settings.ButtonImageSrc,
                settings.ConstrainInput ? "true" : "false",
                (settings.MinDate.HasText() ? ", minDate: " + settings.MinDate : ""),
                (settings.MaxDate.HasText() ? ", maxDate: " + settings.MaxDate : ""),
                (settings.Format.HasText() ? ", dateFormat: '" + FormatToString(settings.Format) + "'" : "")
                );
        }

        internal static string FormatToString(string dateFormat)
        {
            switch (dateFormat)
            { 
                case "d":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                case "D":
                    return CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;
                case "f":
                    return CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                case "F":
                    return CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
                case "g":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                case "G":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
                case "m":
                case "M":
                    return CultureInfo.CurrentCulture.DateTimeFormat.MonthDayPattern;
                case "r":
                case "R":
                    return CultureInfo.CurrentCulture.DateTimeFormat.RFC1123Pattern;
                case "s":
                    return CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern;
                case "t":
                    return CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                case "T":
                    return CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
                case "u":
                    return CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern;
                case "U":
                    return CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
                case "y":
                case "Y":
                    return CultureInfo.CurrentCulture.DateTimeFormat.YearMonthPattern;
            }
            return dateFormat;
        }

        static string GetLocalizationVariables()
        {
            string shortCultureName = DatePickerOptions.DefaultCulture;

            StringBuilder sb = new StringBuilder();
            sb.Append("<script type=\"text/javascript\">");
            sb.Append("$.datepicker.regional['" + shortCultureName + "'] = ");

            var config = new {
                closeText = Resources.CalendarClose,
                prevText = Resources.CalendarPrevious,
                nextText = Resources.CalendarNext,
                currentText = Resources.CalendarToday,
                monthNames = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames,
                monthNamesShort = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames,
                dayNames = CultureInfo.CurrentCulture.DateTimeFormat.DayNames,
                dayNamesShort = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames,
                dayNamesMin = CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames,
                dateFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern,
                firstDay = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek
            };

            sb.Append(new JavaScriptSerializer().Serialize(config));

            sb.Append(";");
	        sb.Append("$.datepicker.setDefaults($.datepicker.regional['" + shortCultureName + "']);");
            sb.Append("</script>");

            return sb.ToString();
        }
    }
}
