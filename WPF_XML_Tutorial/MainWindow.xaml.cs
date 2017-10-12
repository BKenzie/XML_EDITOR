using Microsoft.Win32;
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
        private List<string> tabHeaders = null;
        private List<TabItem> tabItems = new List<TabItem> ();
        private XmlDocument xmlDoc;
        private List<ActionPathXmlNode> actionPathXmlNodes = new List<ActionPathXmlNode>();
        Dictionary<int, XmlNode> pathIDHistories = new Dictionary<int, XmlNode> ();
        public static ComboBox pathIDComboBox;
        public int currentPathID = -1;
        public int numTabs;
        public int apParsed = 0;
        public const int GRID_WIDTH = 698;
        Grid pathIDGrid = new Grid
        {
            Width = GRID_WIDTH,
        };

        public MainWindow( string filePath )
        {
            InitializeComponent ();
            
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

            MainTabControl.SelectionChanged += new SelectionChangedEventHandler ( TabChanged );

            // Initialize the window elements depending on the XML doc 
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
                                newTabItem.FontSize = 18;
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
                MessageBox.Show ( "No tabs specified in xml file under <Tabs_XEDITOR>", "Error" );
                return;
            }

            // Right before MainWindow is shown to user
            if ( pathIDGrid.Children != null && pathIDGrid.Children.Count != 0 )
            {
                ComboBox pathIdComboBox = (ComboBox) pathIDGrid.Children[1];
                if ( pathIDComboBox.Items.Count >= 1 )
                {
                    ResetAllTabs ();
                    DisplayPathID ();
                }
            }
            
            
        }

        // Event handler for PathID overlay not appearing on ActionPath tab
        private void TabChanged( object sender, SelectionChangedEventArgs e )
        {
            int currentIndex = ( (TabControl) sender ).SelectedIndex;
            if ( currentIndex == 0 )
            {
                TextPathIDOverlay.Visibility = Visibility.Hidden;
                NumPathIDOverlay.Visibility = Visibility.Hidden;
            }
            else
            {
                TextPathIDOverlay.Visibility = Visibility.Visible;
                NumPathIDOverlay.Visibility = Visibility.Visible;
            }
        }

        public void BindWidth( FrameworkElement bindMe, FrameworkElement toMe )
        {
            Binding b = new Binding ();
            b.Mode = BindingMode.OneWay;
            b.Source = toMe.ActualWidth;
            bindMe.SetBinding ( FrameworkElement.WidthProperty, b );
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
            // Get to tab header text data
            // NOTE: supports any format where the header text is in any sub-node of <Tabs_XEDITOR>
            while ( reader.NodeType != XmlNodeType.Text )
            {
                reader.Read ();
            }
            // Get string of tab headers
            string strTabHeaders = reader.Value;
            tabHeaders = new List<string> ( strTabHeaders.Split ( ',' ).Select ( s => s.Replace ( " ", "" ) ) );
        }

        // Read information for tabItems
        // Tabs contain ActionPath info such as PathID number, but not child elements which have their own tab pages
        private void ReadAllTabInformation()
        {
            XmlNodeList xmlNodes = xmlDoc.ChildNodes;
            // Skip over root node, get list of main nodes in xml file
            // Should be the root node if not an XML Declaration ... I think
            foreach ( XmlNode node in xmlDoc.ChildNodes )
            {
                // variable node would be the root node
                if ( node.NodeType == XmlNodeType.Element )
                {
                    rootName = node.Name;
                    // variable xmlNodes would be a list including all ActionPaths 
                    xmlNodes = node.ChildNodes;
                    break;
                }
            }
        
           
            // For each main node (xmlNode would be one ActionPath), check if it has a tab of it's own
            // If it does not have a tab specified in <Tabs_XEDITOR>, ignore it
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

                    // TODO: parse xml comments at top level 
                    if ( xmlNode.NodeType == XmlNodeType.Comment )
                    {
                        ////////////////////////////////////////
                    }
                }
            }
        }

        // Parse info for a single tab 
        private void RecursiveParseTabInfo( TabItem tabItem, XmlNode xmlNode )
        {
            // Check for tabItem saved history
            XmlNode xmlNodeHistory; 
            int pathID = GetActionPathID ( xmlNode );
            if ( pathIDHistories.ContainsKey ( pathID ) && ( pathID != -1 ) )
            {
                xmlNodeHistory = pathIDHistories[pathID];
                xmlNode = xmlNodeHistory;
            }


            // If xmlNode child node/element has its own tab in <Tabs_XEDITOR> link to its tab
            ListView listView = new ListView ();
            // BindWidth ( MainTabControl, this );
            // BindWidth ( listView, MainTabControl );
                        
            // Every time a new ActionPath is sent to this method, it resets all of the current tabs
            if ( xmlNode.Name == "ActionPath" && ( xmlNode.NodeType == XmlNodeType.Element ) )
            {
                // Increment number of ActionPaths parsed
                apParsed++;
                // Add ActionPath node to actionPathXmlNodes, this makes it possible to switch between different ActionPaths
                int actionPathId = GetActionPathID ( xmlNode );

                //currentPathID = actionPathId; should be able to get rid of this line
                ActionPathXmlNode newAPNode = new ActionPathXmlNode ( xmlNode, actionPathId );
                if ( !actionPathXmlNodes.Contains ( newAPNode ) )
                {
                    actionPathXmlNodes.Add ( newAPNode );
                }
               
                
                if ( apParsed > 1 )
                {
                    // If given new ActionPath to parse, window should only display information for new ActionPath
                    ResetAllTabs (); 
                }
            }

            // Display attribute header
            TextBlock attribTitleTextBlock = new TextBlock ();
            attribTitleTextBlock.Text = "Attributes:";
            attribTitleTextBlock.FontSize = 16;
            attribTitleTextBlock.TextDecorations = TextDecorations.Underline;
            attribTitleTextBlock.FontWeight = FontWeights.Bold;
            listView.Items.Add ( attribTitleTextBlock );

            #region New attribute button

            Button newAttributeButton = new Button ();
            newAttributeButton.Content = "Add new"; 
            newAttributeButton.Click += new RoutedEventHandler ( newAttributeButton_Click );
            newAttributeButton.Background = new SolidColorBrush ( Colors.LightGray );
            newAttributeButton.BorderBrush = new SolidColorBrush ( Colors.Transparent );
            newAttributeButton.FontSize = 10;
            newAttributeButton.Height = 20;
            newAttributeButton.Width = 50;

            Style customButtonStyleAttrib = new Style ();
            customButtonStyleAttrib.TargetType = typeof ( Button );
            MultiDataTrigger triggerAttrib = new MultiDataTrigger ();
            Condition conditionAttrib = new Condition ();
            conditionAttrib.Binding = new Binding () { Path = new PropertyPath ( "IsMouseOver" ), RelativeSource = RelativeSource.Self };
            conditionAttrib.Value = true;
            Setter foregroundSetterAttrib = new Setter ();
            foregroundSetterAttrib.Property = Button.ForegroundProperty;
            foregroundSetterAttrib.Value = Brushes.DarkOrange;
            Setter cursorSetterAttrib = new Setter ();
            cursorSetterAttrib.Property = Button.CursorProperty;
            cursorSetterAttrib.Value = Cursors.Hand;
            Setter textSetterAttrib = new Setter ();
            textSetterAttrib.Property = Button.FontWeightProperty;
            textSetterAttrib.Value = FontWeights.ExtraBold;

            triggerAttrib.Conditions.Add ( conditionAttrib );
            triggerAttrib.Setters.Add ( foregroundSetterAttrib );
            triggerAttrib.Setters.Add ( cursorSetterAttrib );
            triggerAttrib.Setters.Add ( textSetterAttrib );

            customButtonStyleAttrib.Triggers.Clear ();
            customButtonStyleAttrib.Triggers.Add ( triggerAttrib );
            newAttributeButton.Style = customButtonStyleAttrib;
            #endregion

            listView.Items.Add ( newAttributeButton );

            // Display all node attributes 
            if ( xmlNode.Attributes.Count > 0 )
            {
                foreach ( XmlAttribute attribute in xmlNode.Attributes )
                {
                    Grid newGrid = new Grid ()
                    {
                        Width = GRID_WIDTH,
                    };
                    // BindWidth ( newGrid, listView); 
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

                    ContextMenu rightClickMenu = new ContextMenu ();
                    MenuItem deleteItem = new MenuItem ();
                    deleteItem.Header = "Delete attribute";
                    deleteItem.Click += DeleteItem_Click;
                    rightClickMenu.Items.Add ( deleteItem );
                    newGrid.ContextMenu = rightClickMenu;

                    listView.Items.Add ( newGrid );
                }
            }

            listView.Items.Add ( new Separator () );
            listView.Items.Add ( new Separator () );

            // If xmlNode contains only text, then display
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
                    textBlock.ToolTip = "Text field";
                    Grid.SetRow ( textBlock, 0 );
                    Grid.SetColumn ( textBlock, 0 );
                    newGrid.Children.Add ( textBlock );

                    TextBox textBoxNodeText = new TextBox ();
                    textBoxNodeText.AppendText ( xmlNode.FirstChild.Value );
                    textBoxNodeText.ToolTip = "Text field";
                    Grid.SetRow ( textBoxNodeText, 0 );
                    Grid.SetColumn ( textBoxNodeText, 1 );
                    newGrid.Children.Add ( textBoxNodeText );

                    ContextMenu rightClickMenu = new ContextMenu ();
                    MenuItem deleteItem = new MenuItem ();
                    deleteItem.Header = "Delete text element";
                    deleteItem.Click += DeleteItem_Click;
                    rightClickMenu.Items.Add ( deleteItem );
                    newGrid.ContextMenu = rightClickMenu;

                    listView.Items.Add ( newGrid );
                }
            }

            // Display element header 
            TextBlock elementTitleTextBlock = new TextBlock ();
            elementTitleTextBlock.Text = "Elements:";
            elementTitleTextBlock.FontWeight = FontWeights.Bold;
            elementTitleTextBlock.FontSize = 16;
            elementTitleTextBlock.TextDecorations = TextDecorations.Underline;
            listView.Items.Add ( elementTitleTextBlock );

            #region New element button

            Button newElementButton = new Button ();
            newElementButton.Content = "Add new";
            newElementButton.Click += new RoutedEventHandler ( newElementButton_Click );
            newElementButton.Background = new SolidColorBrush ( Colors.LightGray );
            newElementButton.BorderBrush = new SolidColorBrush ( Colors.Transparent );
            newElementButton.FontSize = 10;
            newElementButton.Height = 20;
            newElementButton.Width = 50;

            Style customButtonStyleElement = new Style ();
            customButtonStyleElement.TargetType = typeof ( Button );
            MultiDataTrigger triggerElem = new MultiDataTrigger ();
            Condition conditionElem = new Condition ();
            conditionElem.Binding = new Binding () { Path = new PropertyPath ( "IsMouseOver" ), RelativeSource = RelativeSource.Self };
            conditionElem.Value = true;
            Setter foregroundSetterElem = new Setter ();
            foregroundSetterElem.Property = Button.ForegroundProperty;
            foregroundSetterElem.Value = Brushes.DarkOrange;
            Setter cursorSetterElem = new Setter ();
            cursorSetterElem.Property = Button.CursorProperty;
            cursorSetterElem.Value = Cursors.Hand;
            Setter textSetterElem = new Setter ();
            textSetterElem.Property = Button.FontWeightProperty;
            textSetterElem.Value = FontWeights.ExtraBold;

            triggerElem.Conditions.Add ( conditionElem );
            triggerElem.Setters.Add ( foregroundSetterElem );
            triggerElem.Setters.Add ( cursorSetterElem );
            triggerElem.Setters.Add ( textSetterElem );

            customButtonStyleElement.Triggers.Clear ();
            customButtonStyleElement.Triggers.Add ( triggerElem );
            newElementButton.Style = customButtonStyleElement;
            #endregion

            listView.Items.Add ( newElementButton );

            XmlNodeList xmlChildNodes = xmlNode.ChildNodes;
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
                        //MessageBox.Show ( "No ActionPath PathID specified in xml document.", "PathID Error" );
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
                if ( xmlChildNode.FirstChild == null )
                {
                    // Creates a ComboBox for PathIDs if does not already exist
                    // else add PathID to existing ComboBox
                    if ( pathIDComboBox == null )
                    {
                        // Initialize pathIDComboBox with event handling for switching paths
                        InitializePathIDComboBox ();
                    }

                    if (pathIDComboBox.Items.Count == 0 || 
                        Convert.ToString(((ComboBoxItem)pathIDComboBox.Items[pathIDComboBox.Items.Count - 1]).Content) != "New ActionPath")
                    {
                        ComboBoxItem addNewActionPath = new ComboBoxItem ();
                        addNewActionPath.Content = "New ActionPath";
                        pathIDComboBox.Items.Add ( addNewActionPath );
                    }
                    
                    TextBlock pathIDTextBlock = new TextBlock ();
                    pathIDTextBlock.Text = "PathID:";
                    pathIDTextBlock.ToolTip = "Current PathID - Change to switch active ActionPath";
                    Grid.SetRow ( pathIDTextBlock, 0 );
                    Grid.SetColumn ( pathIDTextBlock, 0 );

                    if ( pathIDGrid.ColumnDefinitions.Count == 0 )
                    {
                        pathIDGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        pathIDGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
                        pathIDGrid.RowDefinitions.Add ( new RowDefinition () );
                    }
                    

                    pathIDGrid.Children.Add ( pathIDTextBlock );
                    if ( pathIDComboBox.Parent != null )
                    {
                        Grid parent = (Grid) pathIDComboBox.Parent;
                        parent.Children.Remove ( pathIDComboBox );
                    }
                    pathIDGrid.Children.Add ( pathIDComboBox );
                    RemoveGridFromListViewParent ( pathIDGrid );
                    listView.Items.Add ( pathIDGrid );
                }
                else
                {
                    // Created a ComboBox for PathIDs if does not already exist
                    // else add PathID to existing ComboBox
                    if ( pathIDComboBox == null )
                    {
                        // Initialize pathIDComboBox with event handling for switching paths
                        pathIDComboBox = new ComboBox ();
                        ComboBoxItem addNewActionPath = new ComboBoxItem ();
                        addNewActionPath.Content = "New ActionPath";
                        pathIDComboBox.Items.Add ( addNewActionPath );
                        pathIDComboBox.SelectionChanged += new SelectionChangedEventHandler ( PathIDChanged );
                        ComboBoxItem newPathID = new ComboBoxItem ();
                        newPathID.Content = xmlChildNode.FirstChild.Value; // PathID value
                        AddNewPathID ( pathIDComboBox, newPathID );

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
                        if ( !ContainsPathID ( pathIDComboBox, xmlChildNode.FirstChild.Value ) )
                        {
                            AddNewPathID ( pathIDComboBox, newPathID );
                        }

                        if ( !listView.Items.Contains ( pathIDGrid ) )
                        {
                            RemoveGridFromListViewParent ( pathIDGrid );
                            listView.Items.Add ( pathIDGrid );
                        }
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

                        ContextMenu rightClickMenu = new ContextMenu ();
                        MenuItem deleteItem = new MenuItem ();
                        deleteItem.Header = "Delete element";
                        deleteItem.Click += DeleteItem_Click;
                        rightClickMenu.Items.Add ( deleteItem );
                        newGrid.ContextMenu = rightClickMenu;

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

                        ContextMenu rightClickMenu = new ContextMenu ();
                        MenuItem deleteItem = new MenuItem ();
                        deleteItem.Header = "Delete element";
                        deleteItem.Click += DeleteItem_Click;
                        rightClickMenu.Items.Add ( deleteItem );
                        newGrid.ContextMenu = rightClickMenu;

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

                        ContextMenu rightClickMenu = new ContextMenu ();
                        MenuItem deleteItem = new MenuItem ();
                        deleteItem.Header = "Delete element";
                        deleteItem.Click += DeleteItem_Click;
                        rightClickMenu.Items.Add ( deleteItem );
                        newGrid.ContextMenu = rightClickMenu;

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

                        ContextMenu rightClickMenu = new ContextMenu ();
                        MenuItem deleteItem = new MenuItem ();
                        deleteItem.Header = "Delete attribute";
                        deleteItem.Click += DeleteItem_Click;
                        rightClickMenu.Items.Add ( deleteItem );
                        newGrid.ContextMenu = rightClickMenu;

                        listView.Items.Add ( newGrid );
                    }
                }
            }

            foreach ( XmlNode xmlGrandChildNode in xmlChildNode.ChildNodes )
            {
                ParseChildElementWithoutOwnTab ( ref listView, xmlGrandChildNode, true );
            }
            //listView.Items.Add ( new Separator () );
            //listView.Items.Add ( new Separator () );
        }

        private void InitializePathIDComboBox()
        {

            pathIDComboBox = new ComboBox ();
            pathIDComboBox.SelectionChanged += new SelectionChangedEventHandler ( PathIDChanged );
            Grid.SetRow ( pathIDComboBox, 0 );
            Grid.SetColumn ( pathIDComboBox, 1 );
        }

        private void AddNewPathID( ComboBox pathIDComboBox, ComboBoxItem newPathID )
        {
            // Need to insert the new ComboBoxItem into the proper place 
            // (before "New ActionPath" option) 
            if ( pathIDComboBox.Items.Count <= 1 )
            {
                pathIDComboBox.Items.Insert ( 0, newPathID );
            }
            else
            {
                int i = 0;
                foreach ( ComboBoxItem item in pathIDComboBox.Items )
                {
                    if ( Convert.ToString ( item.Content ) != "New ActionPath" )
                    {
                        if ( Convert.ToInt32 ( item.Content ) > Convert.ToInt32 ( newPathID.Content ) )
                        {
                            pathIDComboBox.Items.Insert ( i, newPathID );
                            return;
                        }
                        i++;
                    }
                }
                pathIDComboBox.Items.Insert ( i, newPathID );
            }
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
                if ( Convert.ToString ( comboItem.Content ) == pathID )
                {
                    return true;
                }
            }
            return false;
        }

        // Event handler for pathIdComboBox selection changed
        private void PathIDChanged(object sender, SelectionChangedEventArgs e)
        {

            if ( pathIDComboBox.SelectedIndex != -1 )
            {
                // Change PathID overlay
                NumPathIDOverlay.Text = Convert.ToString ( ( (ComboBoxItem) pathIDComboBox.SelectedItem ).Content );
                // Want to save any changes to the currently active tabs before switching to a new active ActionPath
                // At this point, the selected PathID has been changed but the tabItems list has not yet been updated
                XmlDocSave historyDocSave = new XmlDocSave ( new XmlDocument (), tabHeaders, "" );
                XmlNode savedActiveTabsState = historyDocSave.WriteCurrentOpenTabs ( tabItems, currentPathID );
                // If exists, delete previous saved state and overwrite with new saved state
                if ( pathIDHistories.ContainsKey ( currentPathID ) )
                {
                    pathIDHistories.Remove ( currentPathID );
                }
                pathIDHistories.Add ( currentPathID, savedActiveTabsState.FirstChild.LastChild );

                ComboBoxItem item = pathIDComboBox.SelectedItem as ComboBoxItem;
                if ( Convert.ToString ( item.Content ) == "New ActionPath" )
                {
                    NewActionPath newActionPathWindow = new NewActionPath ( this );
                    newActionPathWindow.Show ();
                    this.IsEnabled = false;
                }
                else
                {
                    string strPathID = Convert.ToString ( item.Content );
                    int intPathID = Convert.ToInt32 ( strPathID );
                    SwitchCurrentActionPath ( intPathID );
                }
            }
            
        }

        public void NewPathIDEntered( int pathID )
        {
            XmlDocument helperXmlDoc = new XmlDocument ();
            string templateFilePath = Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName + @"\Resources\ActionPathsTemplate.xml";
            helperXmlDoc.Load ( templateFilePath );
            XmlNode newXmlNode = helperXmlDoc.LastChild.LastChild; // this retrieves just the template <ActionPath> node
            // Need to set the PathID into the newXmlNode so that it isn't empty
            foreach ( XmlNode xmlNode in newXmlNode.ChildNodes )
            {
                if ( xmlNode.Name == "PathID" && xmlNode.NodeType == XmlNodeType.Element )
                {
                    xmlNode.InnerText = Convert.ToString ( pathID );
                }
            }
            ActionPathXmlNode newActionPath = new ActionPathXmlNode ( newXmlNode, pathID );
            actionPathXmlNodes.Add ( newActionPath );
            ComboBoxItem newPathIDItem = new ComboBoxItem ();
            newPathIDItem.Content = pathID;
            AddNewPathID ( pathIDComboBox, newPathIDItem );
            
            pathIDComboBox.SelectedIndex = pathIDComboBox.Items.IndexOf ( newPathIDItem );
        }

        // Displays new ActionPath and sub-tabs associated with param pathID in window
        private void SwitchCurrentActionPath( int? pathID )
        {
            if (pathID != null)
            {
                currentPathID = (int) pathID;
            }
            XmlNode actionPath = null;
            // Get proper XmlNode for ActionPath, PathIDs must be the same
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
            string message = "Save changes to xml document?\nOpening a new xml file will close this one.\nUnsaved changes will be lost.";
            string header = "Caution - save changes?";
            MessageBoxButton msgBoxButtons = MessageBoxButton.YesNoCancel;
            MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
            if ( msgBoxResult == MessageBoxResult.Yes )
            {
                // Save the document first then continue with open command
                Save_Button.RaiseEvent ( new RoutedEventArgs ( System.Windows.Controls.Primitives.ButtonBase.ClickEvent ) );
            }
            else if ( msgBoxResult == MessageBoxResult.No )
            {
                // Continue open command without saving
            }
            else if ( msgBoxResult == MessageBoxResult.Cancel )
            {
                // Cancel open command, no need to save
                return;
            }

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

            if ( filePath != "" )
            {
                // TODO: Check if XML file is in the proper format 
                // If it is, pass the XML fileName to MainWindow and initialize it
                pathIDComboBox = null;
                MainWindow mainWindow = new MainWindow ( filePath );
                mainWindow.Show ();
                this.Close ();

            }
        }

        private void Save_Button_Click( object sender, RoutedEventArgs e )
        {
            if ( pathIDComboBox == null )
            {
                InitializePathIDComboBox ();
            }

            if (pathIDComboBox.SelectedIndex == -1)
            {
                // TODO -- might not need at in current implementation state, actually
                // MessageBox.Show ( "PathID unselected.", "Select a PathID" );
                // return;
            }

            // Get file name/location to save from user
            SaveFileDialog saveFileDialog = new SaveFileDialog ();
            saveFileDialog.Filter = "XML files (*.XML)|*.XML|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.ShowDialog ();
            string fileSavePath = saveFileDialog.FileName;
            if ( fileSavePath == "" )
            {
                MessageBox.Show ( "File save aborted" );
                return;
            }
            XmlDocument xmlDocTosave = new XmlDocument ();
            XmlDocSave xmlDocSave = new XmlDocSave ( xmlDocTosave, tabHeaders, fileSavePath );

            xmlDocSave.SaveAll ( tabItems, pathIDComboBox );

        }

        private void newAttributeButton_Click( object sender, RoutedEventArgs e )
        {
            NewElemOrAttrib newAttributeWindow = new NewElemOrAttrib ( this, "attribute" );
            newAttributeWindow.Show ();
            this.IsEnabled = false;
        }

        private void newElementButton_Click( object sender, RoutedEventArgs e )
        {
            NewElemOrAttrib newElementWindow = new NewElemOrAttrib ( this, "element" );
            newElementWindow.Show ();
            this.IsEnabled = false;
        }

        public void AddNewAttribute( string name, string value )
        {
            TabItem currentTabItem = MainTabControl.SelectedItem as TabItem;
            ListView currentListView = currentTabItem.Content as ListView;
            // Need to create the grid with appropriate TextBlock and TextBox, then insert at proper spot in currentListView
            Grid newGrid = new Grid
            {
                Width = GRID_WIDTH,
            };

            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
            newGrid.RowDefinitions.Add ( new RowDefinition () );

            TextBlock textBlock = new TextBlock ();
            textBlock.Text = name + ":";
            textBlock.ToolTip = "Attribute";
            textBlock.Name = name;
            Grid.SetRow ( textBlock, 0 );
            Grid.SetColumn ( textBlock, 0 );
            newGrid.Children.Add ( textBlock );

            TextBox textBoxAttrib = new TextBox ();
            textBoxAttrib.AcceptsReturn = true;
            textBoxAttrib.Text = ( value );
            textBoxAttrib.ToolTip = "Attribute";
            Grid.SetRow ( textBoxAttrib, 0 );
            Grid.SetColumn ( textBoxAttrib, 1 );
            newGrid.Children.Add ( textBoxAttrib );

            ContextMenu rightClickMenu = new ContextMenu ();
            MenuItem deleteItem = new MenuItem ();
            deleteItem.Header = "Delete attribute";
            deleteItem.Click += DeleteItem_Click;
            rightClickMenu.Items.Add ( deleteItem );
            newGrid.ContextMenu = rightClickMenu;

            currentListView.Items.Insert ( 2, newGrid );

        }

        private void DeleteItem_Click( object sender, RoutedEventArgs e )
        {
            MenuItem deleteItem = (MenuItem) sender;
            Grid grid = ( (ContextMenu) deleteItem.Parent ).PlacementTarget as Grid;
            ListView listView = grid.Parent as ListView;
            listView.Items.Remove ( grid );
        }

        public void AddNewElement( string name, string value )
        {
            TabItem currentTabItem = MainTabControl.SelectedItem as TabItem;
            ListView currentListView = currentTabItem.Content as ListView;
            // Need to create the grid with appropriate TextBlock and TextBox, then insert at proper spot in currentListView
            Grid newGrid = new Grid
            {
                Width = GRID_WIDTH,
            };

            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
            newGrid.ColumnDefinitions.Add ( new ColumnDefinition () );
            newGrid.RowDefinitions.Add ( new RowDefinition () );

            TextBlock textBlock = new TextBlock ();
            textBlock.Text = name + ":";
            textBlock.ToolTip = "Element";
            textBlock.Name = name;
            Grid.SetRow ( textBlock, 0 );
            Grid.SetColumn ( textBlock, 0 );
            newGrid.Children.Add ( textBlock );

            TextBox textBoxAttrib = new TextBox ();
            textBoxAttrib.AcceptsReturn = true;
            textBoxAttrib.Text = ( value );
            textBoxAttrib.ToolTip = "Element";
            Grid.SetRow ( textBoxAttrib, 0 );
            Grid.SetColumn ( textBoxAttrib, 1 );
            newGrid.Children.Add ( textBoxAttrib );

            ContextMenu rightClickMenu = new ContextMenu ();
            MenuItem deleteItem = new MenuItem ();
            deleteItem.Header = "Delete element";
            deleteItem.Click += DeleteItem_Click;
            rightClickMenu.Items.Add ( deleteItem );
            newGrid.ContextMenu = rightClickMenu;

            int insertIndex = GetElementHeaderIndex ( currentListView );
            currentListView.Items.Insert ( insertIndex + 2, newGrid );
        }

        // Helper function
        private int GetElementHeaderIndex( ListView listView )
        {
            foreach ( TextBlock textBlock in listView.Items.OfType<TextBlock>() )
            {
                if ( textBlock.Text == "Elements:" )
                {
                    return listView.Items.IndexOf ( textBlock );
                }
            }
            return -1;
        }
    }
}
