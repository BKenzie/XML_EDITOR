﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace WPF_XML_Tutorial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string xmlFilePath;
        private string rootName;
        private List<string> tabHeaders;
        private List<TabItem> tabItems = new List<TabItem> ();

        private XmlDocument xmlDoc;
        private List<ActionPathXmlNode> actionPathXmlNodes = new List<ActionPathXmlNode>();
        public static ComboBox pathIDComboBox;

        Grid pathIDGrid = new Grid
        {
            Width = GRID_WIDTH,
        };
        // private XmlDocument xmlDocToSave; // Thinking I'll save everything only once user presses Save_Button
        // Then I'll have the program run through the open window and parse back to the xmlDocToSave

        public int numTabs;
        public int apParsed = 0;

        public const int GRID_WIDTH = 595;

        public MainWindow( string filePath )
        {
            InitializeComponent ();

            // Initialize the window elements depending on the XML doc 
            // NOTE FOR SELF: when writing/editing the xmlDoc, consider using and serializing to a StringBuilder. -> comment: may be only for new xml file creation?
            xmlFilePath = filePath;
            xmlDoc = new XmlDocument ();
            xmlDoc.Load ( xmlFilePath );

            #region Open and Save buttons
            Style customButtonStyle = new Style ();
            customButtonStyle.TargetType = typeof ( Button );
            MultiDataTrigger trigger = new MultiDataTrigger ();
            Condition condition = new Condition ();
            condition.Binding = new Binding () { Path = new PropertyPath ( "IsMouseOver" ), RelativeSource = RelativeSource.Self };
            condition.Value = true;
            Setter foregroundSetter = new Setter ();
            foregroundSetter.Property = Button.ForegroundProperty;
            foregroundSetter.Value = Brushes.DarkOrange;
            Setter cursorSetter = new Setter ();
            cursorSetter.Property = Button.CursorProperty;
            cursorSetter.Value = Cursors.Hand;
            Setter textSetter = new Setter ();
            textSetter.Property = Button.FontWeightProperty;
            textSetter.Value = FontWeights.ExtraBold;
            Setter setter = new Setter ();
            trigger.Conditions.Add ( condition );
            trigger.Setters.Add ( foregroundSetter );
            trigger.Setters.Add ( cursorSetter );
            trigger.Setters.Add ( textSetter );
            customButtonStyle.Triggers.Clear ();
            customButtonStyle.Triggers.Add ( trigger );
            Open_New_Button.Style = customButtonStyle;
            Save_Button.Style = customButtonStyle;
            #endregion


            using ( FileStream fileStream = new FileStream ( xmlFilePath, FileMode.Open ) )
            using ( XmlReader reader = XmlReader.Create ( fileStream ) )
            {
                while ( reader.Read () )
                {
                    if ( reader.NodeType == XmlNodeType.Element )
                    {
                        // Needs to have the <Tabs_XEDITOR> element in order for this program to work
                        if ( reader.Name == "Tabs_XEDITOR" )
                        {
                            ReadTabHeaderInformation ( reader );
                            numTabs = tabHeaders.Count ();
                            // Populate list of tabItems
                            foreach ( string header in tabHeaders )
                            {
                                TabItem newTabItem = new TabItem ();
                                newTabItem.Name = header;
                                newTabItem.Header = header;
                                newTabItem.FontSize = 15;
                                MainTabControl.Items.Add ( newTabItem );
                                tabItems.Add ( newTabItem );
                            }
                            ReadAllTabInformation ();
                            break;
                        }
                    }
                }
            }
            // Send error message if <Tabs_XEDITOR> is empty or not found
            if ( tabHeaders == null )
            {
                MessageBox.Show ( "No tabs specified in xml file under <Tabs_XEDITOR> <Show_Tabs>", "Error" );
            }

            // Right before MainWindow is shown to user
            ComboBox pathIdComboBox = (ComboBox) pathIDGrid.Children[1];
            if (pathIDComboBox.Items.Count > 1)
            {
                ResetAllTabs ();
                DisplayPathID ();
            }
            

        }

        private void DisplayPathID()
        {
            TabItem firstTabItem = tabItems[0];
            ListView listView = new ListView ();
            listView.Items.Add ( pathIDGrid );
            firstTabItem.Content = listView;

        }

        private void ReadTabHeaderInformation( XmlReader reader )
        {
            // Get to Node <ShowTabs>
            while ( reader.NodeType != XmlNodeType.Text )
            {
                reader.Read ();
            }
            // Get string of tab headers
            string strTabHeaders = reader.Value;
            tabHeaders = new List<string> ( strTabHeaders.Split ( ',' ).Select ( s => s.Replace ( " ", "" ) ) );
        }


        // Read information for tabItems
        // Should contain ActionPath info such as PathID number (XmlNodeType.Attribute?), but not child elements which have their own tab pages
        private void ReadAllTabInformation()
        {
            XmlNodeList xmlNodes = xmlDoc.ChildNodes;
            // Skip over root node, get list of main nodes in xml file
            // Should be the root node if not an XML Declaration ... I think
            // TODO: Actually learn XML. (all the different node types at least)
            foreach ( XmlNode node in xmlDoc.ChildNodes )
            {
                // var node would be the root node
                if ( node.NodeType == XmlNodeType.Element )
                {
                    rootName = node.Name;
                    // var xmlNodes would be a list including all ActionPaths 
                    xmlNodes = node.ChildNodes;
                    break;
                }
            }
        
           
            // For each main node, check if it has a tab of it's own
            // If it does not have a tab specified in <Tabs_XEDITOR>, ignore it <--------------------------- TODO: Determine proper functionality
            foreach ( XmlNode xmlNode in xmlNodes )
            {
                if ( tabHeaders.Contains ( xmlNode.Name ) )
                {
                    // Get matching tabItem for xmlNode
                    // NOTE: tabItem.Name is same as matching xmlNode.Name
                    TabItem tabItem = new TabItem ();
                    foreach ( TabItem curTabItem in tabItems )
                    {
                        if ( curTabItem.Name == xmlNode.Name )
                        {
                            tabItem = curTabItem;
                        }
                    }
                    // Parse tab elements/info 
                    RecursiveParseTabInfo ( tabItem, xmlNode );

                }
                else
                {
                    // Ignore element that is not given tab in <Tabs_XEDITOR>

                    // TODO: parse xml comments
                }

            }
            
        }

        // Parse info for a single tab --------------------------------------------------------------------------------------------------------------------------
        private void RecursiveParseTabInfo( TabItem tabItem, XmlNode xmlNode )
        {
            // If xmlNode child node/element has its own tab in <Tabs_XEDITOR> link to its tab
            ListView listView = new ListView (); // KEEP THIS HERE
            // ^ so when there are two ActionPaths, the last one processed in this method is the only one to show
            // This makes is so every time a new ActionPath is sent to this method, it can reset all the current tabs
            if ( xmlNode.Name == "ActionPath" && ( xmlNode.NodeType == XmlNodeType.Element ) )
            {
                // Increment number of ActionPaths parsed
                apParsed++;
                // Add ActionPath node to actionPathXmlNodes, this makes it possible to switch between different ActionPaths
                ActionPathXmlNode newAPNode = new ActionPathXmlNode ( xmlNode, GetActionPathID ( xmlNode ) );
                actionPathXmlNodes.Add ( newAPNode );
                if ( apParsed > 1 )
                {
                    // If given new ActionPath to parse, then only want to display information for new ActionPAth
                    ResetAllTabs (); // Might want to change to only reset ActionPath sub-tabs
                }
            }


            if ( xmlNode.Attributes.Count > 0 )
            {
                // Display all node attributes
                foreach ( XmlAttribute attribute in xmlNode.Attributes )
                {
                    if ( xmlNode.Attributes.Item(0) == attribute )
                    {
                        TextBlock attribTitleTextBlock = new TextBlock ();
                        attribTitleTextBlock.Text = "Attributes:";
                        attribTitleTextBlock.FontSize = 16;
                        attribTitleTextBlock.TextDecorations = TextDecorations.Underline;
                        attribTitleTextBlock.FontWeight = FontWeights.Bold;

                        listView.Items.Add ( attribTitleTextBlock );
                    }

                    Grid newGrid = new Grid
                    {
                        Width = GRID_WIDTH,
                    };
                    newGrid.ShowGridLines = false;
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.RowDefinitions.Add ( new RowDefinition () );

                    TextBlock textBlock = new TextBlock ();
                    textBlock.Text = attribute.Name + ":";
                    textBlock.ToolTip = xmlNode.Name + "'s attribute";
                    textBlock.Name = attribute.Name;
                    Grid.SetRow ( textBlock, 0 );
                    Grid.SetColumn ( textBlock, 0 );
                    newGrid.Children.Add ( textBlock );

                    TextBox textBoxAttrib = new TextBox ();
                    textBoxAttrib.AcceptsReturn = true;
                    textBoxAttrib.Text = ( attribute.Value );
                    textBoxAttrib.ToolTip = attribute.Name + " attribute";
                    Grid.SetRow ( textBoxAttrib, 0 );
                    Grid.SetColumn ( textBoxAttrib, 1 );
                    newGrid.Children.Add ( textBoxAttrib );
                    
                    listView.Items.Add ( newGrid );
                }
                listView.Items.Add ( new Separator () );
                listView.Items.Add ( new Separator () );
            }

            // If xmlNode contains only text, then display
            // Shouldn't happen for ActionPaths as far as I know
            if ( xmlNode.HasChildNodes )
            {
                if ( xmlNode.FirstChild.NodeType == XmlNodeType.Text )
                {
                    Grid newGrid = new Grid
                    {
                        Width = GRID_WIDTH,
                    };
                    newGrid.ShowGridLines = false;
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.RowDefinitions.Add ( new RowDefinition () );

                    TextBlock textBlock = new TextBlock ();
                    textBlock.Text = xmlNode.Name + ":";
                    textBlock.Name = xmlNode.Name;
                    Grid.SetRow ( textBlock, 0 );
                    Grid.SetColumn ( textBlock, 0 );
                    newGrid.Children.Add ( textBlock );

                    TextBox textBoxNodeText = new TextBox ();
                    textBoxNodeText.AppendText ( xmlNode.FirstChild.Value );
                    Grid.SetRow ( textBoxNodeText, 0 );
                    Grid.SetColumn ( textBoxNodeText, 1 );
                    newGrid.Children.Add ( textBoxNodeText );

                    listView.Items.Add ( newGrid );
                }
            }


            XmlNodeList xmlChildNodes = xmlNode.ChildNodes;
            if ( xmlChildNodes != null )
            {
                TextBlock elementTitleTextBlock = new TextBlock ();
                elementTitleTextBlock.Text = "Elements:";
                elementTitleTextBlock.FontWeight = FontWeights.Bold;
                elementTitleTextBlock.FontSize = 16;
                elementTitleTextBlock.TextDecorations = TextDecorations.Underline;

                listView.Items.Add ( elementTitleTextBlock );
            }

            foreach ( XmlNode xmlChildNode in xmlChildNodes )
            {
                // Parse child tab elements/info if they are in <Tabs_XEDITOR>
                if ( tabHeaders.Contains ( xmlChildNode.Name ) )
                {
                    // Set up button grid
                    Grid newGrid = new Grid
                    {
                        Width = GRID_WIDTH,
                    };

                    newGrid.ShowGridLines = false;
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.RowDefinitions.Add ( new RowDefinition () );
                    newGrid.RowDefinitions.Add ( new RowDefinition () );

                    #region BUTTON CODE

                    // Code for buttons linking to other tabs
                    Button gotoTab_Button = new Button ();
                    gotoTab_Button.Content = xmlChildNode.Name;
                    gotoTab_Button.Tag = xmlChildNode.Name;
                    gotoTab_Button.Height = 37;
                    gotoTab_Button.Width = 160;
                    gotoTab_Button.Background = new SolidColorBrush ( Colors.LightGray );
                    //gotoTab_Button.Foreground = new SolidColorBrush ( Colors.DarkOrange );
                    gotoTab_Button.BorderBrush = new SolidColorBrush ( Colors.Transparent );

                    Style customButtonStyle = new Style ();
                    customButtonStyle.TargetType = typeof ( Button );
                    MultiDataTrigger trigger = new MultiDataTrigger ();
                    Condition condition = new Condition ();
                    condition.Binding = new Binding () { Path = new PropertyPath ( "IsMouseOver" ), RelativeSource = RelativeSource.Self };
                    condition.Value = true;
                    Setter foregroundSetter = new Setter ();
                    foregroundSetter.Property = Button.ForegroundProperty;
                    foregroundSetter.Value = Brushes.DarkOrange;
                    Setter cursorSetter = new Setter ();
                    cursorSetter.Property = Button.CursorProperty;
                    cursorSetter.Value = Cursors.Hand;
                    Setter textSetter = new Setter ();
                    textSetter.Property = Button.FontWeightProperty;
                    textSetter.Value = FontWeights.ExtraBold;
                    Setter setter = new Setter ();
                    trigger.Conditions.Add ( condition );
                    trigger.Setters.Add ( foregroundSetter );
                    trigger.Setters.Add ( cursorSetter );
                    trigger.Setters.Add ( textSetter );

                    customButtonStyle.Triggers.Clear ();
                    customButtonStyle.Triggers.Add ( trigger );
                    gotoTab_Button.Style = customButtonStyle;
                    
                    gotoTab_Button.Click += gotoTabButton_Click;
                    
                    Rectangle gotoTabRect = new Rectangle ();
                    gotoTabRect.Fill = new SolidColorBrush ( Colors.Transparent );
                    gotoTabRect.Width = 120;
                    gotoTabRect.Height = 14;
                    Grid.SetRow ( gotoTabRect, 0 );
                    Grid.SetColumn ( gotoTabRect, 1 );
                    newGrid.Children.Add ( gotoTabRect );

                    Grid.SetRow ( gotoTab_Button, 1 );
                    Grid.SetColumn ( gotoTab_Button, 2 );
                    newGrid.Children.Add ( gotoTab_Button );
#endregion

                    listView.Items.Add ( newGrid );

                    // Get matching tabItem for xmlNode
                    // NOTE: tabItem.Name is same as matching xmlNode.Name
                    TabItem tabItem2 = new TabItem ();
                    foreach ( TabItem curTabItem in tabItems )
                    {
                        if ( curTabItem.Name == xmlChildNode.Name )
                        {
                            tabItem2 = curTabItem;
                        }
                    }
                    RecursiveParseTabInfo ( tabItem2, xmlChildNode );
                }
                else
                {
                    // This is where we need to display child node info that doesn't have its own tab
                    // ie ValveState has a <help> child node which should be visible 
                    ParseChildElementWithoutOwnTab ( ref listView, xmlChildNode, false );
                    
                }
            }

            // ListView construction is over, now set as the tabItem content
            tabItem.Content = listView;
        }


        // Helper function; requires xmlNode param is only ever passed as an ActionPath xml element
        private int GetActionPathID( XmlNode xmlNode )
        {
            foreach ( XmlNode xmlAPChildNode in xmlNode.ChildNodes )
            {
                if ( xmlAPChildNode.Name == "PathID") // Currently requires all ActionPaths to have this child node
                {
                    if ( xmlAPChildNode.FirstChild != null )
                    {
                        return Convert.ToInt32 ( xmlAPChildNode.FirstChild.Value );
                    }
                    else
                    {
                        MessageBox.Show ( "No ActionPath PathID specified in xml document.", "PathID Error" );
                    }
                }
            }
            return -1;
        }


        // Method for parsing xml info into listVew for a sub-element without it's own tab
        private void ParseChildElementWithoutOwnTab( ref ListView listView, XmlNode xmlChildNode, bool isSubElement )
        {
            #region PathID code
            // Special behaviour if currently parsing ActionPath's PathID
            if ( xmlChildNode.Name == "PathID" )
            {
                // Created a ComboBox for PathIDs if does not already exist
                // else add PathID to existing ComboBox
                if ( pathIDComboBox == null )
                {
                    // Initialize pathIDComboBox with event handling for switching paths
                    pathIDComboBox = new ComboBox ();
                    pathIDComboBox.SelectionChanged += new SelectionChangedEventHandler ( PathIDChanged );
                    ComboBoxItem newPathID = new ComboBoxItem ();
                    newPathID.Content = xmlChildNode.FirstChild.Value; // PathID value
                    pathIDComboBox.Items.Add ( newPathID );
                    
                    Grid.SetRow ( pathIDComboBox, 0 );
                    Grid.SetColumn ( pathIDComboBox, 1 );

                    // Add PathIDComboBox to the listView
                    pathIDGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    pathIDGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    pathIDGrid.RowDefinitions.Add ( new RowDefinition () );

                    TextBlock pathIDTextBlock = new TextBlock ();
                    pathIDTextBlock.Text = "PathID:";
                    pathIDTextBlock.ToolTip = "Current PathID - Change to switch active ActionPath";
                    Grid.SetRow ( pathIDTextBlock, 0 );
                    Grid.SetColumn ( pathIDTextBlock, 0 );

                    pathIDGrid.Children.Add ( pathIDTextBlock );
                    pathIDGrid.Children.Add ( pathIDComboBox );
                    RemoveGridFromListViewParent ( pathIDGrid );
                    listView.Items.Add ( pathIDGrid );
                    
                }
                else
                {
                    ComboBoxItem newPathID = new ComboBoxItem ();
                    newPathID.Content = xmlChildNode.FirstChild.Value;
                    if ( !ContainsPathID(pathIDComboBox, xmlChildNode.FirstChild.Value ) )
                    {
                        pathIDComboBox.Items.Add ( newPathID );
                    }

                    if ( !listView.Items.Contains ( pathIDGrid ) )
                    {
                        RemoveGridFromListViewParent ( pathIDGrid );
                        listView.Items.Add ( pathIDGrid );
                        //pathIDComboBox.SelectedIndex = pathIDComboBox.Items.Count - 1; // TODO: Figure out why this does not work

                    }
                }
                #endregion

            }
            else
            {
                if ( xmlChildNode.HasChildNodes )
                {
                    // Things like <help>, <source>, <destination> etc..
                    if ( xmlChildNode.FirstChild.NodeType == XmlNodeType.Text )
                    {
                        Grid newGrid = new Grid
                        {
                            Width = GRID_WIDTH,
                        };

                        newGrid.ShowGridLines = false;
                        newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        newGrid.RowDefinitions.Add ( new RowDefinition () );

                        TextBlock nameTextBlock = new TextBlock ();
                        nameTextBlock.Text = xmlChildNode.Name + ":";
                        
                        
                        nameTextBlock.Name = xmlChildNode.Name;
                        Grid.SetRow ( nameTextBlock, 0 );
                        Grid.SetColumn ( nameTextBlock, 0 );
                        newGrid.Children.Add ( nameTextBlock );

                        TextBox textBoxNodeText = new TextBox ();
                        textBoxNodeText.AppendText ( xmlChildNode.FirstChild.Value );
                        Grid.SetRow ( textBoxNodeText, 0 );
                        Grid.SetColumn ( textBoxNodeText, 1 );
                        newGrid.Children.Add ( textBoxNodeText );

                        if ( isSubElement )
                        {
                            nameTextBlock.ToolTip = "Element (sub)";
                            textBoxNodeText.ToolTip = xmlChildNode.Name + " element (sub)";
                        }
                        else
                        {
                            nameTextBlock.ToolTip = "Element";
                            textBoxNodeText.ToolTip = xmlChildNode.Name + " element";
                        }

                        listView.Items.Add ( newGrid );
                    }

                    // Let's say there is a child element in ActionPath that isn't given its own tab in <Tabs_XEDITOR>
                    // This is where it is handled, because we still want to print its info
                    if ( ( xmlChildNode.NodeType == XmlNodeType.Element ) && ( xmlChildNode.FirstChild.NodeType != XmlNodeType.Text ) )
                    {
                        Grid newGrid = new Grid
                        {
                            Width = GRID_WIDTH,
                        };

                        newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        newGrid.RowDefinitions.Add ( new RowDefinition () );

                        TextBlock subNodeNameTextBlock = new TextBlock ();
                        subNodeNameTextBlock.Text = xmlChildNode.Name + ":";
                        subNodeNameTextBlock.FontSize = 16;
                        subNodeNameTextBlock.TextDecorations = TextDecorations.Underline;
                        subNodeNameTextBlock.FontWeight = FontWeights.Bold;

                        Grid.SetColumn ( subNodeNameTextBlock, 0 );
                        Grid.SetRow ( subNodeNameTextBlock, 0 );
                        newGrid.Children.Add ( subNodeNameTextBlock );

                        listView.Items.Add ( newGrid );
                        
                    }
                }
                else
                {
                    // Child node containing no children
                    // For now, displaying "EMPTY" 

                    if ( xmlChildNode.NodeType != XmlNodeType.Text ) // Errors otherwise
                    {
                        Grid newGrid = new Grid
                        {
                            Width = GRID_WIDTH,
                        };

                        newGrid.ShowGridLines = false;
                        newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        newGrid.RowDefinitions.Add ( new RowDefinition () );

                        TextBlock nameTextBlock = new TextBlock ();
                        nameTextBlock.Text = xmlChildNode.Name + ":";
                        nameTextBlock.Name = xmlChildNode.Name;
                        
                        Grid.SetRow ( nameTextBlock, 0 );
                        Grid.SetColumn ( nameTextBlock, 0 );
                        newGrid.Children.Add ( nameTextBlock );

                        TextBox textBoxNodeText = new TextBox ();
                        textBoxNodeText.AppendText ( "EMPTY" );
                        Grid.SetRow ( textBoxNodeText, 0 );
                        Grid.SetColumn ( textBoxNodeText, 1 );
                        newGrid.Children.Add ( textBoxNodeText );

                        listView.Items.Add ( newGrid );

                        if ( isSubElement )
                        {
                            nameTextBlock.ToolTip = "Empty element (sub)";
                            textBoxNodeText.ToolTip = xmlChildNode.Name + " element (sub)";
                        }
                        else
                        {
                            nameTextBlock.ToolTip = "Empty element";
                            textBoxNodeText.ToolTip = xmlChildNode.Name + " element";
                        }
                    }
                    
                }
            }

            // Display attributes, since in this case the node will not have it's own tab to show them in
            if ( xmlChildNode.Attributes != null )
            {
                if ( xmlChildNode.Attributes.Count > 0 )
                {
                    foreach ( XmlAttribute attribute in xmlChildNode.Attributes )
                    {
                        Grid newGrid = new Grid
                        {
                            Width = GRID_WIDTH,
                        };

                        newGrid.ShowGridLines = false;
                        newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        newGrid.RowDefinitions.Add ( new RowDefinition () );

                        TextBlock textBlockSubAttrib = new TextBlock ();
                        textBlockSubAttrib.Text = "[" + xmlChildNode.Name + " attribute] " + attribute.Name + ":";
                        textBlockSubAttrib.ToolTip = "Attribute";
                        textBlockSubAttrib.Name = attribute.Name;
                        Grid.SetRow ( textBlockSubAttrib, 0 );
                        Grid.SetColumn ( textBlockSubAttrib, 0 );
                        newGrid.Children.Add ( textBlockSubAttrib );

                        TextBox attributeTextBox = new TextBox ();
                        attributeTextBox.Text = attribute.Value;
                        attributeTextBox.ToolTip = xmlChildNode.Name + "'s attribute";
                        Grid.SetRow ( attributeTextBox, 0 );
                        Grid.SetColumn ( attributeTextBox, 1 );
                        newGrid.Children.Add ( attributeTextBox );

                        listView.Items.Add ( newGrid );
                    }
                }
            }

            foreach ( XmlNode xmlGrandChildNode in xmlChildNode.ChildNodes )
            {
                ParseChildElementWithoutOwnTab ( ref listView, xmlGrandChildNode, true ); ///// ADD PARAM isSubElem = true, add to tooltip///////////////////////////////
                ///////////////////////////////////////////////////////////////////////////////////THIS IS WHERE I LEFT OFF, Press any key to continue..

            }
            //listView.Items.Add ( new Separator () );
            //listView.Items.Add ( new Separator () );
        }

        private void RemoveGridFromListViewParent( Grid pathIDGrid )
        {
            if ( pathIDGrid.Parent != null )
            {
                ListView parent = (ListView) pathIDGrid.Parent;
                parent.Items.Remove ( pathIDGrid );
            }
            
        }

        // Helper function
        private bool ContainsPathID( ComboBox pathIDComboBox, string pathID )
        {
            foreach ( ComboBoxItem comboItem in pathIDComboBox.Items )
            {
                if ( (string)comboItem.Content == pathID )
                {
                    return true;
                }
            }
            return false;
        }

        // Event handler for pathIdComboBox selection changed
        private void PathIDChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = pathIDComboBox.SelectedItem as ComboBoxItem;
            string strPathID = (string)item.Content;
            int intPathID = Convert.ToInt32 ( strPathID );

            SwitchCurrentActionPath ( intPathID );
            
        }

        // Displays new ActionPath and sub-tabs associated with param pathID in window
        private void SwitchCurrentActionPath( int? pathID )
        {
            XmlNode actionPath = null;
            // Get proper XmlNode for actionPath, pathIDs must be the same
            foreach ( ActionPathXmlNode actionPathXmlNode in actionPathXmlNodes)
            {
                if ( actionPathXmlNode.PathID == pathID )
                {
                    actionPath = actionPathXmlNode.XmlNode;
                }
            }
            // Now get corresponding tabItem..
            TabItem apTabItem = GetTabItemWithHeader ( "ActionPath" );
            RecursiveParseTabInfo ( apTabItem, actionPath ); 
        }

        // Helper for retrieving a certain tabItem from list tabItems
        private TabItem GetTabItemWithHeader( string tabItemHeader )
        {
            foreach ( TabItem tabItem in tabItems )
            {
                if ( (string)tabItem.Header == tabItemHeader )
                {
                    return tabItem;
                }
            }
            return null;
        }

        // Resets to zero tabs. Only called when about to reconstruct tabs, due to new active ActionPath
        private void ResetAllTabs()
        {
            foreach ( TabItem tabItem in tabItems )
            {
                if ( tabItem.Content != null )
                {
                    ListView listView = (ListView) tabItem.Content;
                    listView.Items.Clear ();
                }
            }
        }

        private void DragRectangle_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }


        private void Close_Button_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            this.Close ();
        }

        private void gotoTabButton_Click( object sender, RoutedEventArgs e )
        {
            // Get matching tabItem for button click
            // NOTE: tabItem.Name is same as matching xmlNode.Name
            TabItem tabItem = new TabItem ();
            foreach ( TabItem curTabItem in tabItems )
            {
                if ( curTabItem.Name == (string) ( (Button) sender ).Tag )
                {
                    tabItem = curTabItem;
                }
            }
            MainTabControl.SelectedIndex = MainTabControl.Items.IndexOf ( tabItem );
        }

        private void Open_New_Button_Click( object sender, RoutedEventArgs e )
        {
            pathIDComboBox = null;

            // Get the user chosen XML file
            OpenFileDialog openFileDialog = new OpenFileDialog ();
            openFileDialog.Filter = "XML files (*.XML)|*.XML|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            string filePath = "";
            Nullable<bool> result = openFileDialog.ShowDialog ();
            if ( result == true )
            {
                filePath = openFileDialog.FileName;
            }
            else
            {
                // Wholly unnecessary
                MessageBox.Show ( "Error: error.", "ERROR" );
            }

            if ( filePath != "" )
            {
                // TODO: Check if XML file is in the proper format 
                // If it is, pass the XML fileName to MainWindow and initialize it

                MainWindow mainWindow = new MainWindow ( filePath );
                mainWindow.Show ();
                this.Close ();

            }
        }

        private void Save_Button_Click( object sender, RoutedEventArgs e )
        {
            if (pathIDComboBox.SelectedIndex == -1)
            {
                MessageBox.Show ( "That will fix all your problems. Don't ask why.", "Select a PathID" );
                return;
            }

            // Get file name/location to save from user
            SaveFileDialog saveFileDialog = new SaveFileDialog ();
            saveFileDialog.Filter = "XML files (*.XML)|*.XML|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.ShowDialog ();
            string fileSavePath = saveFileDialog.FileName;
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            XmlDocument xmlDocTosave = new XmlDocument ();
            XmlDocSave xmlDocSave = new XmlDocSave ( xmlDocTosave, tabHeaders, fileSavePath );

            xmlDocSave.WriteCurrentOpenTabs ( tabItems );
            xmlDocSave.Save ();

        }

    }
}
