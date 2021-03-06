﻿using System;
using System.Collections.Generic;
using System.IO;
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
        private List<string> tabHeaders = new List<string> ();
        private List<XmlNode> xmlTabNodes = new List<XmlNode> ();
        private List<XmlNode> xmlSubNodes = new List<XmlNode> ();
        private bool mainAttributesPassed = false;
        private XmlElement curElement = null;
        private XmlElement curSubElement = null;
        private MainWindow mainWindowCaller;
        private List<TabItem> visibleTabItems = new List<TabItem> ();

        // Creates instance of XmlDocSave with proper Tabs_XEDITOR element 
        public XmlDocSave( XmlDocument document, string path, MainWindow caller )
        {
            // Create xml document with root node
            xmlDoc = document;
            fileSavePath = path;
            xmlDoc.AppendChild ( xmlDoc.CreateNode ( "element", "root", "" ) );
            mainWindowCaller = caller;
            List<TabItem> mainWindowTabItems = mainWindowCaller.GetTabItems ();
            foreach ( TabItem tabItem in mainWindowTabItems )
            {
                if ( !tabHeaders.Contains ( tabItem.Header ) )
                {
                    tabHeaders.Add ( tabItem.Header as string );
                }
            }
            AddTabHeadersToTabs_XEDITOR ();
        }

        private void AddTabHeadersToTabs_XEDITOR()
        {
            XmlNode tabs_XEDITOR = xmlDoc.CreateNode ( "element", "Tabs_XEDITOR", "" );
            string innerText = String.Join ( ",", tabHeaders );
            tabs_XEDITOR.InnerText = innerText;
            xmlDoc.FirstChild.AppendChild ( tabs_XEDITOR );
        }

        public void SaveAll( List<TabItem> tabItems, ComboBox pathIDComboBox)
        {
            if ( pathIDComboBox == null )
            {
                WriteCurrentOpenTabs ( tabItems, -1 );
                NullifyEmptyNodes ( xmlDoc );
                Save ();
            }
            else
            {
                foreach ( ComboBoxItem item in pathIDComboBox.Items )
                {
                    if ( Convert.ToString ( item.Content ) != MainWindow.NEW_UOP_STRING )
                    {
                        // Will trigger MainWindow.PathIDChange from the OnSelectedItemChanged event
                        pathIDComboBox.SelectedItem = item;
                    }
                    else
                    {
                        continue;
                    }
                    curElement = null;
                    mainAttributesPassed = false;
                    xmlTabNodes.Clear ();
                    xmlSubNodes.Clear ();
                    WriteCurrentOpenTabs ( tabItems, -1 );
                    NullifyEmptyNodes ( xmlDoc.LastChild );
                }
                Save ();
            }
        }

        public static XmlNode NullifyEmptyNodes( XmlNode root)
        {
            foreach ( XmlNode xmlNode in root.ChildNodes )
            {
                if ( xmlNode.InnerXml == "EMPTY" )
                {
                    xmlNode.InnerText = null;
                }
                NullifyEmptyNodes ( xmlNode );
            }
            return root;
        }

        // Called for each PathID
        public XmlNode WriteCurrentOpenTabs( List<TabItem> tabItems, int pathID )
        {
            mainAttributesPassed = false;
            tabItems = mainWindowCaller.MainTabControl.Items.OfType<TabItem> ().ToList();
            foreach ( TabItem tabItem in tabItems )
            {
                if ( tabItem.Visibility == Visibility.Visible || (string) tabItem.Header == mainWindowCaller.activeMainNodeName )
                {
                    if ( (string) tabItem.Header != mainWindowCaller.activeMainNodeName )
                    {
                        visibleTabItems.Add ( tabItem );
                    }
                    string strNodeName =  new String(( (string) tabItem.Header ).Where ( c => !Char.IsWhiteSpace ( c ) ).ToArray());
                    XmlNode xmlTabNode = xmlDoc.CreateNode ( "element", strNodeName, "" );
                    if ( strNodeName == mainWindowCaller.activeMainNodeName )
                    {
                        XmlElement pathIDElement = xmlDoc.CreateElement ( "PathID" );
                        if ( pathID != -1 )
                        {
                            pathIDElement.InnerText = Convert.ToString ( pathID );
                        }
                        else
                        {
                            ComboBoxItem selectedPathIDItem = (ComboBoxItem) mainWindowCaller.PathIDComboBox.SelectedItem;
                            if ( selectedPathIDItem != null )
                            {
                                pathIDElement.InnerText = Convert.ToString ( selectedPathIDItem.Content );
                            }
                        }
                        xmlTabNode.AppendChild ( pathIDElement );
                    }

                    WriteTabInfoToXmlNode ( tabItem, xmlTabNode, pathID );
                    xmlTabNodes.Add ( xmlTabNode );
                }
            }

            // Now for each tab, go through and add any sub nodes 
            foreach ( TabItem tabItem in tabItems )
            {
                if ( tabItem.Visibility == Visibility.Visible || (string) tabItem.Header == mainWindowCaller.activeMainNodeName )
                {
                    string nodeName = (string) tabItem.Header;
                    XmlNode xmlTabNode = GetXmlTabNode ( nodeName );
                    InsertSubNodes ( tabItem, xmlTabNode );
                }
            } 
            
            // Add to xmlDoc any main tab nodes that are not sub nodes
            foreach ( XmlNode xmlTabNode in xmlTabNodes )
            {
                if ( !xmlSubNodes.Contains ( xmlTabNode ) && xmlTabNode.HasChildNodes)
                {
                    xmlDoc.FirstChild.AppendChild ( xmlTabNode );
                }
            }

            return xmlDoc;
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
            // Append children to the main node
            if ( (string) tabItem.Header == mainWindowCaller.activeMainNodeName )
            {
                foreach ( TabItem t in visibleTabItems )
                {
                    XmlNode subNodeToAppend = GetXmlTabNode ( t.Header as string );
                    if ( subNodeToAppend != null )
                    {
                        xmlTabNode.AppendChild ( subNodeToAppend );
                        xmlSubNodes.Add ( subNodeToAppend );
                    }
                }
                return;
            }
            // any other references to sub nodes in tabs
            if ( tabItem.Content != null )
            {
                ListView listView = (ListView) tabItem.Content;
                foreach ( Grid grid in listView.Items.OfType<Grid> () )
                {
                    foreach ( Button tabLinkButton in grid.Children.OfType<Button> () )
                    {
                        string buttonName = (string) tabLinkButton.Content;
                        XmlNode subNodeToAppend = GetXmlTabNode ( buttonName );
                        if ( subNodeToAppend != null )
                        {
                            xmlTabNode.AppendChild ( subNodeToAppend );
                            xmlSubNodes.Add ( subNodeToAppend );
                        }
                    }
                }
            }
            
        }

        private void WriteTabInfoToXmlNode( TabItem tabItem, XmlNode xmlTabNode, int pathID )
        {
            curElement = (XmlElement)xmlTabNode;
            ListView listView = (ListView) tabItem.Content;
            if ( listView != null )
            {
                foreach ( Grid grid in listView.Items.OfType<Grid> () )
                {
                    WriteGridInfoToXml ( grid, xmlTabNode, pathID );
                }
            }
            

        }

        // Given a single grid element in tabs list view, parse info into xml
        private void WriteGridInfoToXml( Grid grid, XmlNode xmlTabNode, int pathID )
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
                        curSubElement = newElement;
                        xmlTabNode.AppendChild ( newElement );
                    }
                }
            }

            // Includes most grid elements
            if ( grid.Children.Count > 1 )
            {
                // PathID logic
                if ( grid.Children[0].GetType () == typeof ( TextBlock ) )
                {
                    TextBlock pathID_TextBlock = (TextBlock) grid.Children[0];
                    if ( pathID_TextBlock.Text == "PathID:" )
                    {
                        mainAttributesPassed = true;
                        return; // Do not save any elements with the name "PathID"
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
                        curSubElement.AppendChild ( newSubElement );
                        curElement = newSubElement;

                    }
                    if ( IsAttribute ( gridChild ) && mainAttributesPassed )
                    {
                        // Need to add this attribute to previous element, which should be curElement
                        TextBlock textBlock = (TextBlock) grid.Children[0];
                        string attribName = ( (string) textBlock.Text ).Replace ( ":", "" );

                        XmlAttribute newAttrib = xmlDoc.CreateAttribute ( attribName );
                        newAttrib.Value = gridChild.Text;
                        curElement.Attributes.Append ( newAttrib );
                    }
                    if ( IsTextField ( gridChild ) )
                    {
                        mainAttributesPassed = true;
                        xmlTabNode.InnerText = (string) gridChild.Text; ;
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
            if ( toolTip != null )
            {
                if ( toolTip.Length < 9 )
                {
                    return false;
                }

                toolTip = toolTip.Substring ( toolTip.Length - 9 ).ToLower ();
                if ( toolTip == "attribute" )
                {
                    return true;
                }
            }
            
            return false;
        }

        private bool IsElement( TextBox textBox )
        {
            string toolTip = (string) textBox.ToolTip;
            if ( toolTip != null )
            {
                if ( toolTip.Length < 7 )
                {
                    return false;
                }

                toolTip = toolTip.Substring ( toolTip.Length - 7 ).ToLower ();
                if ( toolTip == "element" )
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSubElement( TextBox textBox )
        {
            string toolTip = (string) textBox.ToolTip;
            if ( toolTip != null )
            {
                toolTip = toolTip.Substring ( toolTip.Length - 5 ).ToLower ();
                if ( toolTip == "(sub)" )
                {
                    return true;
                }
            }
            
            return false;
        }

        private bool IsTextField( TextBox textBox )
        {
            string toolTip = (string) textBox.ToolTip;
            if ( toolTip != null )
            {
                if ( toolTip == "Text field" )
                {
                    return true;
                }
            }

            return false;
        }

        public void Save()
        {
            xmlDoc.Save ( fileSavePath );
        }

    }
}
