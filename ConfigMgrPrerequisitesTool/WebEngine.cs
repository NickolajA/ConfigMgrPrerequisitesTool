using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConfigMgrPrerequisitesTool
{
    class WebEngine
    {
        public string LinkName { get; set; }
        public string LinkValue { get; set; }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

        public List<WebEngine> LoadWindowsADKVersions()
        {
            //' Construct new link list for all suupported Windows ADK versions
            List<WebEngine> linkList = new List<WebEngine>();

            //' Construct new html objects and load web site into document
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDocument = htmlWeb.Load(@"https://docs.microsoft.com/en-us/windows-hardware/get-started/adk-install");

            //' Parse for latest ADK download
            HtmlNode latestADK = htmlDocument.DocumentNode.SelectSingleNode("//*[@id='main']/p[4]/a");
            string latestADKLink = latestADK.GetAttributeValue("href", "");
            string latestADKText = latestADK.InnerText.Replace(@"&nbsp;", " ").Replace("Download the Windows ADK for", "").Trim();

            //' Create web link object
            WebEngine latestLink = new WebEngine
            {
                LinkName = latestADKText,
                LinkValue = latestADKLink
            };
            linkList.Add(latestLink);

            //' Parse for other ADK downloads
            List<HtmlNode> otherADKList = new List<HtmlNode>();
            HtmlNode otherADK = htmlDocument.DocumentNode.SelectSingleNode("//*[@id='other-adk-downloads']");
            otherADKList.Add(otherADK.SelectSingleNode("//*[@id='main']/table/tbody/tr[1]/td[1]/a"));
            otherADKList.Add(otherADK.SelectSingleNode("//*[@id='main']/table/tbody/tr[2]/td[1]/a"));

            foreach (HtmlNode otherADKNode in otherADKList)
            {
                WebEngine link = new WebEngine
                {
                    LinkName = otherADKNode.InnerText.Replace(@"&nbsp;", " ").Replace("Windows ADK for", "").Trim(),
                    LinkValue = otherADKNode.GetAttributeValue("href", "")
                };
                linkList.Add(link);
            }

            return linkList;
        }
    }
}
