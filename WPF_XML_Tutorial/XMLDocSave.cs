using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;

namespace WPF_XML_Tutorial
{
    class XmlDocSave
    {
        private XmlDocument xmlDoc;
        private string fileSavePath;
        private List<string> tabHeaders;

        // Creates instance of XmlDocSave with proper Tabs_XEDITOR element 
        public XmlDocSave( XmlDocument document, List<string> headers, string path )
        {
            // Create xml document with root node
            xmlDoc = document;
            fileSavePath = path;
            xmlDoc.AppendChild ( xmlDoc.CreateNode ( "element", "root", "" ) );


            tabHeaders = headers;
            AddTabHeadersToTabs_XEDITOR ();
        }

        // Will be called for each PathID <------------------------------------------------------------- TODO
        public void WriteCurrentOpenTabs(List<TabItem> tabItems)
        {
            foreach ( TabItem tabItem in tabItems )
            {
                ListView listView = (ListView)tabItem.Content;

            }
        }

        private void AddTabHeadersToTabs_XEDITOR()
        {
            XmlNode tabs_XEDITOR = xmlDoc.CreateNode ( "element", "Tabs_XEDITOR", "" );
            string innerText = String.Join ( ",", tabHeaders );
            tabs_XEDITOR.InnerText = innerText;
            xmlDoc.FirstChild.AppendChild ( tabs_XEDITOR );

            // testing
            xmlDoc.Save ( fileSavePath );
        }
    }
}
