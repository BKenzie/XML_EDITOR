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
        private List<string> tabHeaders;
        private List<TabItem> tabItems = new List<TabItem> ();
        private XmlDocument xmlDoc;
        public int numTabs;

        public MainWindow( string filePath )
        {
            InitializeComponent ();

            // Initialize the window elements depending on the XML doc 
            // NOTE FOR SELF: when writing/editing the xmlDoc, consider using and serializing to a StringBuilder. -> comment: may be only for new xml file creation?
            xmlFilePath = filePath;
            xmlDoc = new XmlDocument ();
            xmlDoc.Load ( xmlFilePath );

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
                            ReadTabHeaderInformation ( reader ); // check errors since not passed by reference (can't in using statement)
                            numTabs = tabHeaders.Count ();
                            foreach ( string header in tabHeaders )
                            {
                                TabItem newTabItem = new TabItem ();
                                newTabItem.Name = header;
                                newTabItem.Header = header;
                                newTabItem.FontSize = 14;
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
            // TODO: figure out format and logic behind displaying XML info in each tabItem
            // Going to need a way to access the appropriate tabItem, likely by naming tabItems their header names while creating them

            // Skip over root node, get list of main nodes in xml file
            XmlNodeList xmlNodes = xmlDoc.FirstChild.ChildNodes;
            // For each main node, check if it has a tab of it's own
            // If it does not have a tab specified in <Tabs_XEDITOR>, ignore it <--------------------------- TODO: DOUBLE CHECK THIS
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
                    // Do nothing
                }
            }
        }

        // Parse info for a single tab --------------------------------------------------------------------------------------------------------------------------
        private void RecursiveParseTabInfo( TabItem tabItem, XmlNode xmlNode )
        {
            // If xmlNode child node or element has its own tab in <Tabs_XEDITOR> link to its tab
            ListView listView = new ListView (); // KEEP THIS HERE
            // ^ so when there are two ActionPaths, the last one processed in this method is the only one to show
            // This makes is so every time a new ActionPath is sent to this method, it can reset all the current tabs
            if ( xmlNode.Name == "ActionPath" && xmlNode.NodeType == XmlNodeType.Element )
            {
                ResetAllTabs ();
            }

            if ( xmlNode.Attributes.Count > 0 )
            {
                // Display all node attributes
                foreach ( XmlAttribute attribute in xmlNode.Attributes )
                {
                    Grid newGrid = new Grid
                    {
                        Width = 495,
                    };
                    newGrid.ShowGridLines = false;
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.RowDefinitions.Add ( new RowDefinition () );

                    TextBlock textBlock = new TextBlock ();
                    textBlock.Text = attribute.Name + ":";
                    Grid.SetRow ( textBlock, 0 );
                    Grid.SetColumn ( textBlock, 0 );
                    newGrid.Children.Add ( textBlock );

                    TextBox textBoxAttrib = new TextBox ();
                    textBoxAttrib.Text = ( attribute.Value );
                    Grid.SetRow ( textBoxAttrib, 0 );
                    Grid.SetColumn ( textBoxAttrib, 1 );
                    newGrid.Children.Add ( textBoxAttrib );

                    listView.Items.Add ( new Separator () );
                    listView.Items.Add ( newGrid );
                }

            }

            // If xmlNode contains only text, then display
            // Shouldn't happen for ActionPaths as far as I know
            if ( xmlNode.HasChildNodes )
            {
                if ( xmlNode.FirstChild.NodeType == XmlNodeType.Text )
                {
                    Grid newGrid = new Grid
                    {
                        Width = 495,
                    };
                    newGrid.ShowGridLines = false;
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.RowDefinitions.Add ( new RowDefinition () );

                    TextBlock textBlock = new TextBlock ();
                    textBlock.Text = xmlNode.Name + ":";
                    Grid.SetRow ( textBlock, 0 );
                    Grid.SetColumn ( textBlock, 0 );
                    newGrid.Children.Add ( textBlock );

                    TextBox textBoxNodeText = new TextBox ();
                    textBoxNodeText.AppendText ( xmlNode.FirstChild.Value );
                    Grid.SetRow ( textBoxNodeText, 0 );
                    Grid.SetColumn ( textBoxNodeText, 1 );
                    newGrid.Children.Add ( textBoxNodeText );

                    listView.Items.Add ( new Separator () );
                    listView.Items.Add ( newGrid );
                }
            }


            // Parse child tab elements/info if they are in <Tabs_XEDITOR>
            XmlNodeList xmlChildNodes = xmlNode.ChildNodes;
            foreach ( XmlNode xmlChildNode in xmlChildNodes )
            {
                if ( tabHeaders.Contains ( xmlChildNode.Name ) )
                {
                    // xmlChildNode has it's own tab
                    // Set up button to go to tab
                    Grid newGrid = new Grid
                    {
                        Width = 495,
                    };

                    newGrid.ShowGridLines = false;
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                    newGrid.RowDefinitions.Add ( new RowDefinition () );

                    Button gotoTab_Button = new Button ();
                    gotoTab_Button.Content = xmlChildNode.Name;
                    gotoTab_Button.Tag = xmlChildNode.Name;
                    gotoTab_Button.Click += gotoTabButton_Click;
                    Grid.SetRow ( gotoTab_Button, 0 );
                    Grid.SetColumn ( gotoTab_Button, 1 );
                    newGrid.Children.Add ( gotoTab_Button );

                    listView.Items.Add ( new Separator () );
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
                    // 
                    if ( xmlChildNode.HasChildNodes )
                    {
                        if ( xmlChildNode.FirstChild.NodeType == XmlNodeType.Text )
                        {
                            Grid newGrid = new Grid
                            {
                                Width = 495,
                            };

                            newGrid.ShowGridLines = false;
                            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                            newGrid.RowDefinitions.Add ( new RowDefinition () );

                            TextBlock nameTextBlock = new TextBlock ();
                            nameTextBlock.Text = xmlChildNode.Name + ":";
                            Grid.SetRow ( nameTextBlock, 0 );
                            Grid.SetColumn ( nameTextBlock, 0 );
                            newGrid.Children.Add ( nameTextBlock );

                            TextBox textBoxNodeText = new TextBox ();
                            textBoxNodeText.AppendText ( xmlChildNode.FirstChild.Value );
                            Grid.SetRow ( textBoxNodeText, 0 );
                            Grid.SetColumn ( textBoxNodeText, 1 );
                            newGrid.Children.Add ( textBoxNodeText );

                            listView.Items.Add ( new Separator () );
                            listView.Items.Add ( newGrid );
                        }
                    }
                    else
                    {
                        // Check if not text -> counts as child node with no child nodes of its own
                        if ( xmlChildNode.NodeType != XmlNodeType.Text )
                        {
                            // TODO: Determine what to do if this child node does not have any text or children. ie empty node
                            // For now displaying "EMPTY" 

                            Grid newGrid = new Grid
                            {
                                Width = 495,
                            };

                            newGrid.ShowGridLines = false;
                            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                            newGrid.RowDefinitions.Add ( new RowDefinition () );

                            TextBlock nameTextBlock = new TextBlock ();
                            nameTextBlock.Text = xmlChildNode.Name + ":";
                            Grid.SetRow ( nameTextBlock, 0 );
                            Grid.SetColumn ( nameTextBlock, 0 );
                            newGrid.Children.Add ( nameTextBlock );

                            TextBox textBoxNodeText = new TextBox ();
                            textBoxNodeText.AppendText ( "EMPTY" );
                            Grid.SetRow ( textBoxNodeText, 0 );
                            Grid.SetColumn ( textBoxNodeText, 1 );
                            newGrid.Children.Add ( textBoxNodeText );

                            listView.Items.Add ( new Separator () );
                            listView.Items.Add ( newGrid );
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
                                    Width = 495,
                                };

                                newGrid.ShowGridLines = false;
                                newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                                newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                                newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                                newGrid.RowDefinitions.Add ( new RowDefinition () );

                                TextBlock textBlockSubAttrib = new TextBlock ();
                                textBlockSubAttrib.Text = "-->" + xmlChildNode.Name + "'s attribute";
                                Grid.SetRow ( textBlockSubAttrib, 0 );
                                Grid.SetColumn ( textBlockSubAttrib, 0 );
                                newGrid.Children.Add ( textBlockSubAttrib );

                                TextBlock textBlockSubAttribName = new TextBlock ();
                                textBlockSubAttribName.Text = attribute.Name + ":";
                                Grid.SetRow ( textBlockSubAttribName, 0 );
                                Grid.SetColumn ( textBlockSubAttribName, 1 );
                                newGrid.Children.Add ( textBlockSubAttribName );

                                TextBox attributeTextBox = new TextBox ();
                                attributeTextBox.Text = attribute.Value;
                                Grid.SetRow ( attributeTextBox, 0 );
                                Grid.SetColumn ( attributeTextBox, 2 );
                                newGrid.Children.Add ( attributeTextBox );

                                listView.Items.Add ( newGrid );
                            }
                        }
                    }
                }
            }

            // ListView construction is over, now set as the tabItem content
            tabItem.Content = listView;
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
            MainTabControl = new TabControl (); // Might want to double check this part..
            MainTabControl.Items.Clear ();/////////////////////////////////////////////////////////////////////////////////////KEEP WORKING ON THIS PART, YEAH
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
    }
}
