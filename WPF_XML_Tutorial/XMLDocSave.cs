using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace WPF_XML_Tutorial
{
    class XmlDocSave
    {
        private XmlDocument xmlDoc;
        private string fileSavePath;
        private List<string> tabHeaders;
        private bool mainAttributesPassed = false;

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

        private void AddTabHeadersToTabs_XEDITOR()
        {
            XmlNode tabs_XEDITOR = xmlDoc.CreateNode ( "element", "Tabs_XEDITOR", "" );
            string innerText = String.Join ( ",", tabHeaders );
            tabs_XEDITOR.InnerText = innerText;
            xmlDoc.FirstChild.AppendChild ( tabs_XEDITOR );
        }

        // Will be called for each PathID <------------------------------------------------------------- TODO
        public void WriteCurrentOpenTabs( List<TabItem> tabItems )
        {
            mainAttributesPassed = false;
            foreach ( TabItem tabItem in tabItems )
            {
                XmlNode xmlTabNode = xmlDoc.CreateNode ( "element", (string)tabItem.Header, "" ); // TODO: might need a list class field containing these 
                WriteTabInfoToXmlNode ( tabItem, xmlTabNode );
                xmlDoc.FirstChild.AppendChild ( xmlTabNode ); // might not want to add the new child node if it's a sub node in ActionPath /////////////////////////
            }
        }

        private void WriteTabInfoToXmlNode( TabItem tabItem, XmlNode xmlTabNode )
        {
            ListView listView = (ListView) tabItem.Content;
            foreach ( Grid grid in listView.Items.OfType<Grid> () )
            {
                WriteGridInfoToXml ( grid, xmlTabNode );
            }

        }

        // Given a single grid element in tabs list view, parse info into xml
        private void WriteGridInfoToXml( Grid grid, XmlNode xmlTabNode )
        {
            // Does not include buttons or Attributes/Elements headers
            if ( grid.Children.Count > 1 )
            {
                // PathID logic
                TextBlock pathID_TextBlock = (TextBlock) grid.Children[0];
                
                if ( pathID_TextBlock.Text == "PathID:" )
                {
                    XmlElement pathIDElement = xmlDoc.CreateElement ( "PathID" );
                    // pathIDElement.InnerText = ////////////////////////////////////////////////THIS IS WHERE I LEFT OFF, Press any key to continue..
                }

                XmlElement curElement = null;
                foreach ( var gridChild in grid.Children.OfType<TextBox> () )
                {
                    // Determine if dealing with an attribute or an element
                    // Do not want to add child node attributes to main xmlTabNode
                    if ( IsAttribute( gridChild ) && !mainAttributesPassed) 
                    {
                        TextBlock textBlock = (TextBlock)grid.Children[0];
                        string attribName = (string) textBlock.Text.Substring ( 0, ((string) textBlock.Text).Length - 1 );
                        
                        XmlAttribute newAttrib = xmlDoc.CreateAttribute ( attribName );
                        newAttrib.Value = gridChild.Text;
                        xmlTabNode.Attributes.Append ( newAttrib );
                        

                    }
                    if ( IsElement ( gridChild ) )
                    {
                        mainAttributesPassed = true; 

                        TextBlock textBlock = (TextBlock) grid.Children[0];
                        string elementName = (string) textBlock.Text.Substring ( 0, ( (string) textBlock.Text ).Length - 1 );

                        XmlElement newElement = xmlDoc.CreateElement ( elementName );
                        curElement = newElement;
                        newElement.InnerText = (string)gridChild.Text;
                        xmlTabNode.AppendChild ( newElement );

                    }
                    if ( IsAttribute ( gridChild ) && mainAttributesPassed )
                    {
                        // Need to add this attribute to previous element, which should be curElement
                        TextBlock textBlock = (TextBlock) grid.Children[0];
                        string attribName = (string) textBlock.Text.Substring ( 0, ( (string) textBlock.Text ).Length - 1 );////////////////

                        XmlAttribute newAttrib = xmlDoc.CreateAttribute ( attribName );
                        newAttrib.Value = gridChild.Text;
                        curElement.Attributes.Append ( newAttrib );
                    }

                }
            }


        }

        private bool IsAttribute( TextBox textBox )
        {
            string toolTip = (string) textBox.ToolTip;
            toolTip = toolTip.Substring ( toolTip.Length - 9 ).ToLower ();
            if ( toolTip == "attribute" )
            {
                return true;
            }
            return false;
        }

        private bool IsElement( TextBox textBox )
        {
            string toolTip = (string) textBox.ToolTip;
            toolTip = toolTip.Substring ( toolTip.Length - 7 ).ToLower ();
            if ( toolTip == "element" )
            {
                return true;
            }
            return false;
        }

        public void Save()
        {
            xmlDoc.Save ( fileSavePath );
        }

    }
}
