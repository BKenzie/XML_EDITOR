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
        private List<XmlNode> xmlTabNodes = new List<XmlNode> ();
        private List<XmlNode> xmlSubNodes = new List<XmlNode> ();
        private bool mainAttributesPassed = false;
        private XmlElement curElement = null;


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
                XmlNode xmlTabNode = xmlDoc.CreateNode ( "element", (string)tabItem.Header, "" ); 

                WriteTabInfoToXmlNode ( tabItem, xmlTabNode );
                xmlTabNodes.Add ( xmlTabNode );
            }

            // Now for each tab, go through and add any sub nodes 
            foreach ( TabItem tabItem in tabItems )
            {
                string nodeName = (string)tabItem.Header;
                XmlNode xmlTabNode = GetXmlTabNode ( nodeName );
                InsertSubNodes ( tabItem, xmlTabNode ); 
            }
            
            // Add to xmlDoc any main tab nodes that are not sub nodes
            foreach ( XmlNode xmlTabNode in xmlTabNodes )
            {
                if ( !xmlSubNodes.Contains ( xmlTabNode ) )
                {
                    xmlDoc.FirstChild.AppendChild ( xmlTabNode );
                }
            }
        }

        // Helper function
        private XmlNode GetXmlTabNode( string nodeName )
        {
            foreach ( XmlNode xmlTabNode in xmlTabNodes )
            {
                if ( xmlTabNode.Name == nodeName )
                {
                    return xmlTabNode;
                }
            }
            return null;
        }

        // Goes through all tabLinkButtons in any tab and then appends that xmlTabNode to proper parent node
        private void InsertSubNodes( TabItem tabItem, XmlNode xmlTabNode )
        {
            ListView listView = (ListView)tabItem.Content;
            foreach ( Grid grid in listView.Items.OfType<Grid>() )
            {
                foreach ( Button tabLinkButton in grid.Children.OfType<Button> () )
                {
                    string buttonName = (string)tabLinkButton.Content;
                    XmlNode subNodeToAppend = GetXmlTabNode ( buttonName );
                    xmlTabNode.AppendChild ( subNodeToAppend );
                    xmlSubNodes.Add ( subNodeToAppend );
                }
            }
        }

        private void WriteTabInfoToXmlNode( TabItem tabItem, XmlNode xmlTabNode )
        {
            curElement = (XmlElement)xmlTabNode;
            ListView listView = (ListView) tabItem.Content;
            foreach ( Grid grid in listView.Items.OfType<Grid> () )
            {
                WriteGridInfoToXml ( grid, xmlTabNode );
            }

        }

        // Given a single grid element in tabs list view, parse info into xml
        private void WriteGridInfoToXml( Grid grid, XmlNode xmlTabNode )
        {
            // Sub element
            if ( grid.Children.Count == 1 )
            {
                foreach ( TextBlock subElemTextblock in grid.Children.OfType<TextBlock> () )
                {
                    if ( subElemTextblock.Text != "Elements:" && subElemTextblock.Text != "Attributes:" )
                    {
                        mainAttributesPassed = true;
                        string elementName = subElemTextblock.Text.Substring ( 0, ( (string) subElemTextblock.Text ).Length - 1 );
                        XmlElement newElement = xmlDoc.CreateElement ( elementName );
                        curElement = newElement;
                        xmlTabNode.AppendChild ( newElement );
                    }
                }
            }

            // Does not include buttons or Attributes/Elements headers
            if ( grid.Children.Count > 1 )
            {
                // PathID logic
                if ( grid.Children[0].GetType () == typeof ( TextBlock ) )
                {
                    TextBlock pathID_TextBlock = (TextBlock) grid.Children[0];
                    if ( pathID_TextBlock.Text == "PathID:" )
                    {
                        mainAttributesPassed = true;
                        XmlElement pathIDElement = xmlDoc.CreateElement ( "PathID" );
                        ComboBoxItem selectedPathIDItem = (ComboBoxItem)MainWindow.pathIDComboBox.SelectedItem;
                        pathIDElement.InnerText = (string)selectedPathIDItem.Content;
                        xmlTabNode.AppendChild ( pathIDElement );
                        curElement = pathIDElement;

                    }
                }
                
                foreach ( var gridChild in grid.Children.OfType<TextBox> () )
                {
                    // Determine if dealing with an attribute or an element
                    // Do not want to add child node attributes to main xmlTabNode
                    if ( IsAttribute( gridChild ) && !mainAttributesPassed) 
                    {
                        TextBlock textBlock = (TextBlock)grid.Children[0];
                        string attribName = GetAttributeName ( textBlock );
                        
                        
                        
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
                    if ( IsSubElement ( gridChild ) )
                    {
                        mainAttributesPassed = true;

                        TextBlock textBlock = (TextBlock) grid.Children[0];
                        string subElementName = (string) textBlock.Text.Substring ( 0, ( (string) textBlock.Text ).Length - 1 );

                        XmlElement newSubElement = xmlDoc.CreateElement ( subElementName );
                        newSubElement.InnerText = (string) gridChild.Text;
                        curElement.AppendChild ( newSubElement );
                        curElement = newSubElement;

                    }
                    if ( IsAttribute ( gridChild ) && mainAttributesPassed )
                    {
                        // Need to add this attribute to previous element, which should be curElement
                        TextBlock textBlock = (TextBlock) grid.Children[0];
                        string attribName = (string) textBlock.Name;

                        XmlAttribute newAttrib = xmlDoc.CreateAttribute ( attribName );
                        newAttrib.Value = gridChild.Text;
                        curElement.Attributes.Append ( newAttrib );
                    }

                }
                
            }
    
        }

        private string GetAttributeName( TextBlock textBlock )
        {
            string attribName = "";
            if ( ( (string) textBlock.Text )[0] != '[' )
            {
                attribName = (string) textBlock.Text.Substring ( 0, ( (string) textBlock.Text ).Length - 1 );
            }
            else
            {
                int sourceElemNameLength = curElement.Name.Length;
                attribName = textBlock.Text.Substring ( sourceElemNameLength + 13 );
                attribName = attribName.Substring ( 0, attribName.Length - 1 );
            }
            return attribName;
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

        private bool IsSubElement( TextBox textBox )
        {
            string toolTip = (string) textBox.ToolTip;
            toolTip = toolTip.Substring ( toolTip.Length - 5 ).ToLower ();
            if ( toolTip == "(sub)" )
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
