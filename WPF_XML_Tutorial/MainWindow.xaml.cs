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
        #region INI settings
        public static string INI_FILEPATH = @"Config\Settings.ini";
        public static string NEW_UOP_STRING;
        public static int FONT_SIZE;
        public bool? autoGenPathID;
        private INIFile iniFile;
        private Mode editorMode;
        private enum Mode { general, cSep }
        #endregion

        #region Main Editor window variables
        private string xmlFilePath;
        private string rootName;
        public int currentPathID = -1;
        public int numTabs;
        public int UOPsParsed = 0;
        public int GRID_WIDTH = 698;
        public string activeMainNodeName = "";
        private bool setUpPathIDComboBox = false;
        private bool parsedPathIDForCurrentUOP = true;
        private bool pathIDSectionHighlighted = false;
        private struct SubAttribute
        {
            public XmlAttribute XmlAttribute
            {
                get; set;
            }

            public string ElementName
            {
                get; set;
            }

            public SubAttribute( XmlAttribute xmlAttribute, string elemName )
            {
                XmlAttribute = xmlAttribute;
                ElementName = elemName;
            }
        }
        private XmlDocument xmlDoc;
        private List<string> tabHeaders = null;
        private List<TabItem> tabItems = new List<TabItem> ();
        private List<TextBox> textBoxes = new List<TextBox> ();
        private List<Button> tabLinkButtons = new List<Button> ();
        private List<UOPXmlNode> UOPXmlNodes = new List<UOPXmlNode> ();
        private List<TemplateXmlNode> templateXmlNodes = new List<TemplateXmlNode> ();
        private UndoableType undoableCommand = UndoableType.none;
        private enum UndoableType { none, delUOP, delTab, delElem, createElem, createAttrib, delAttrib, delSubAttrib, delElemHeader };
        private Stack<TabItem> deletedTabs = new Stack<TabItem> ();
        private Stack<UOPXmlNode> deletedUOPs = new Stack<UOPXmlNode> ();
        private Stack<XmlAttribute> deletedXmlAttributes = new Stack<XmlAttribute> ();
        private Stack<SubAttribute> deletedXmlSubAttributes = new Stack<SubAttribute> ();
        private Stack<XmlElement> deletedXmlElements = new Stack<XmlElement> ();
        private Stack<string> deletedElementHeaders = new Stack<string> ();
        private Stack<Grid> createdXmlAttributes = new Stack<Grid> ();
        private Stack<Grid> createdXmlElements = new Stack<Grid> ();
        private Dictionary<int, XmlNode> pathIDHistories = new Dictionary<int, XmlNode> ();
        #endregion

        #region Template variables
        private bool isTemplateWindow = false;
        private MainWindow mainEditorWindow;
        private TemplateXmlNode templateXmlNode;
        private MenuItem templateTabsMenuItem;
        #endregion

        public MainWindow( string filePath, List<TemplateXmlNode> existingTemplates = null, 
                bool isTemplateWindow = false, TemplateXmlNode templateXmlNodeParam = null, MainWindow caller = null )
        {
            InitializeComponent ();
            HandleINI ();

            switch ( editorMode )
            {
                case Mode.general:
                    NEW_UOP_STRING = "New XML node";
                    RemoveUOP.Header = "Remove current xml node";
                    PathIDTextBlock.Text = "ID:";
                    PathIDComboBox.Margin = new Thickness ( 28, 28, 0, 0 );
                    ActiveUOPNameTextBox.Margin = new Thickness ( 76, 25, 0, 0 );
                    ActiveUOPNameTextBox.Width = 172;
                    
                    break;
                case Mode.cSep:
                    NEW_UOP_STRING = "New UnitOperation";
                    RemoveUOP.Header = "Remove current UOP";
                    break;
            }

            KeyboardNavigation.SetTabNavigation ( MainTabControl, KeyboardNavigationMode.None );
            KeyboardNavigation.SetTabNavigation ( MainWindowMenuBar, KeyboardNavigationMode.None );
            MainTabControl.SelectionChanged += new SelectionChangedEventHandler ( TabChanged );
            var mainTabControlItems = CollectionViewSource.GetDefaultView ( MainTabControl.ItemsSource );
            MainTabControl.SelectedIndex = 1;
            PathIDComboBox.SelectionChanged += new SelectionChangedEventHandler ( SavePathIDState );
            PathIDComboBox.SelectionChanged += new SelectionChangedEventHandler ( PathIDChanged );
            PathIDComboBox.IsTabStop = false;

            // Setup available templates
            if ( existingTemplates != null )
            {
                templateXmlNodes = existingTemplates;
            }
            else
            {
                // If existingTemplates is null, add the starting templates
                XmlDocument helperXmlDoc = new XmlDocument ();
                // string defaultUOPTemplateFilePath = Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName + @"\Resources\Templates\DefaultcSepTemplate.xml";
                string[] templateFilePaths = Directory.GetFiles ( Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName + @"\Resources\Templates" );
                foreach ( string templateFilePath in templateFilePaths )
                {
                    if ( templateFilePath.Substring ( templateFilePath.LastIndexOf ( "." ) ) != ".xml" )
                    {
                        throw new Exception ( "Error -- Directory .\\Resources\\Templates can only contain .xml files." );
                    }
                    helperXmlDoc.Load ( templateFilePath );
                    string strHeaders = helperXmlDoc.LastChild.FirstChild.InnerText;
                    XmlNode mainTemplateXmlNode = helperXmlDoc.LastChild.LastChild;
                    string templateName = GetTemplateName ( templateFilePath );
                    TemplateXmlNode defaultTemplateXmlNode = new TemplateXmlNode ( mainTemplateXmlNode, templateName,
                        new List<string> ( strHeaders.Split ( ',' ).Select ( s => s.Replace ( " ", "" ) ) ), mainTemplateXmlNode.Name );
                    templateXmlNodes.Add ( defaultTemplateXmlNode );
                }
            }

            if ( isTemplateWindow )
            {
                MainWindowMenuBar.Height = 28;
                TemplateWindowHeader.Visibility = Visibility.Visible;
                ActiveUOPNameTextBox.Visibility = Visibility.Hidden;
                StemCell_Logo.Visibility = Visibility.Hidden;
                PathIDComboBox.Visibility = Visibility.Hidden;
                PathIDTextBlock.Visibility = Visibility.Hidden;
                MainTabControl.Margin = new Thickness ( 0, 78, 0, 0 );
                mainEditorWindow = caller;
                this.isTemplateWindow = true;
                this.templateXmlNode = templateXmlNodeParam;
                xmlDoc = new XmlDocument ();
                XmlNode xml = xmlDoc.CreateNode ( XmlNodeType.Element, "root", "" );
                XmlNode node = templateXmlNodeParam.XmlNode;
                XmlNode importNode = xml.OwnerDocument.ImportNode ( node, true );
                xml.AppendChild( importNode );
                xmlDoc.AppendChild ( xml );
                tabHeaders = CopyRefStringList ( templateXmlNodeParam.TabHeaders );
                foreach ( string header in tabHeaders )
                {
                    TabItem newTabItem = new TabItem ();
                    newTabItem.Header = header;
                    newTabItem.FontSize = FONT_SIZE;
                    newTabItem.IsTabStop = false;
                    newTabItem.Content = new ListView ();
                    MainTabControl.Items.Add ( newTabItem );
                    tabItems.Add ( newTabItem );
                }
                ReadAllTabInformation ();

                // Menu changes
                MainWindowMenuBar.Width = 174;
                FileMenu.Visibility = Visibility.Collapsed;
                Open_Button.Visibility = Visibility.Collapsed;
                RemoveUOP.Visibility = Visibility.Collapsed;
                DeleteMenu.Visibility = Visibility.Collapsed;
                NewMenu.Visibility = Visibility.Collapsed;
                Save_Button.Visibility = Visibility.Collapsed;
                New_Button.Visibility = Visibility.Collapsed;

                MenuItem TabMenuOptions = new MenuItem ();
                templateTabsMenuItem = TabMenuOptions;
                TabMenuOptions.Header = "Tabs";
                TabMenuOptions.FontSize = FONT_SIZE - 2;
                MenuItem deleteTabMenuItem = new MenuItem ();
                deleteTabMenuItem.Header = "Remove current tab";
                deleteTabMenuItem.Icon = new Image
                {
                    Source = new BitmapImage ( new Uri ( @"C:\projects\WPF_XML_Tutorial\WPF_XML_Tutorial\Images\ui-tab--minus.png" ) )
                };
                deleteTabMenuItem.Click += new RoutedEventHandler ( Delete_Tab_Button_Click );
                MenuItem addTabMenuItem = new MenuItem ();
                addTabMenuItem.Header = "Add new tab";
                addTabMenuItem.Icon = new Image
                {
                    Source = new BitmapImage ( new Uri ( @"C:\projects\WPF_XML_Tutorial\WPF_XML_Tutorial\Images\ui-tab--plus.png" ) )
                };
                addTabMenuItem.Click += new RoutedEventHandler ( AddTab_Click );
                TabMenuOptions.Items.Add ( deleteTabMenuItem );
                TabMenuOptions.Items.Add ( addTabMenuItem );
                MainWindowMenuBar.Items.Add ( TabMenuOptions );

                MenuItem SaveTemplate = new MenuItem ();
                SaveTemplate.Header = "Save Template";
                SaveTemplate.FontSize = FONT_SIZE - 2;
                SaveTemplate.Click += new RoutedEventHandler ( SaveTemplate_Click );
                MainWindowMenuBar.Items.Add ( SaveTemplate );

                if ( templateXmlNodeParam.Name.ToLower () == "blank template" )
                {
                    templateTabsMenuItem.FontWeight = FontWeights.Bold;
                    deleteTabMenuItem.FontWeight = FontWeights.Normal;
                }

                // Set tabs not in this.templateXmlNode.tabHeaders to not be visible
                SetTemplateTabVisibilty ();
                
            }
            else
            {
                if ( filePath != "" )
                {
                    xmlFilePath = filePath;
                    xmlDoc = new XmlDocument ();
                    xmlDoc.Load ( xmlFilePath );

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
                                        newTabItem.Header = header;
                                        newTabItem.FontSize = FONT_SIZE;
                                        newTabItem.IsTabStop = false;
                                        newTabItem.Content = new ListView ();
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
                }
                FinalLogic ();
            }
        }

        private void HandleINI()
        {
            if ( File.Exists ( INI_FILEPATH ) )
            {
                iniFile = new INIFile ( INI_FILEPATH );

                #region Read settings
                // Editor mode
                string currentMode = iniFile.Read ( "mode", "user_settings" );
                SetMode ( currentMode );
                // Font size 
                string fontSizeStr = iniFile.Read ( "fontSize", "user_settings" );
                int fontSize = Convert.ToInt32 ( fontSizeStr );
                FONT_SIZE = fontSize;
                // Auto PathID
                string autoPathIDStr = iniFile.Read ( "autoPathID", "user_settings" );
                if ( autoPathIDStr.ToLower () == "true" )
                {
                    autoGenPathID = true;
                }
                else if ( autoPathIDStr.ToLower () == "false" )
                {
                    autoGenPathID = false;
                }
                #endregion
            }
            else
            {
                iniFile = new INIFile ( INI_FILEPATH );
                iniFile.Write ( "mode", "general", "user_settings" );
                iniFile.Write ( "fontSize", "18", "user_settings" );
                iniFile.Write ( "autoPathID", "", "user_settings" ); // TODO
            }
        }

        public void SetMode( string currentMode )
        {
            switch ( currentMode.ToLower () )
            {
                case "general":
                    editorMode = Mode.general;
                    break;

                case "csep":
                    editorMode = Mode.cSep;
                    break;
            }

        }

        private List<string> CopyRefStringList( List<string> originalList )
        {
            List<string> copyList = new List<string> ();
            foreach ( string str in originalList )
            {
                string copyStr = String.Copy ( str );
                copyList.Add ( copyStr );
            }
            return copyList;
        }

        // Helper function
        private string GetTemplateName( string templateFilePath )
        {
            return templateFilePath.Substring ( templateFilePath.LastIndexOf ( '\\' ) + 1 );
        }

        // Helper function
        private void SetTemplateTabVisibilty()
        {
            foreach ( TabItem tabItem in MainTabControl.Items )
            {
                if ( !this.templateXmlNode.TabHeaders.Contains ( tabItem.Header ) )
                {
                    tabItem.Visibility = Visibility.Collapsed;
                }
                else
                {
                    switch ( editorMode )
                    {
                        case Mode.general:
                            tabItem.Visibility = Visibility.Visible;
                            break;

                        case Mode.cSep:
                            if ( (string) tabItem.Header != activeMainNodeName )
                            {
                                tabItem.Visibility = Visibility.Visible;
                            }
                            break;

                    }
                }
            }
        }

        // Helper for directly before MainWindow is shown to user
        public void FinalLogic()
        {
            ResetAllTabs ();
            SetTabVisibility ();

            if ( PathIDComboBox.Items.Count == 1 )
            {
                tabHeaders = new List<string> ();
                tabItems = new List<TabItem> ();
                MainTabControl.Items.Clear ();
            }
        }

        private void SetTabVisibility()
        {
            foreach ( TabItem tabItem in MainTabControl.Items )
            {
                ListView listView = tabItem.Content as ListView;

                if ( listView == null || listView.Items.Count == 0 )
                {
                    tabItem.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Set tab to be visible if not empty and not the activeMainNodeTab
                    if ( (string) tabItem.Header != activeMainNodeName )
                    {
                        tabItem.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // tabItem is the active main node
                        switch ( editorMode )
                        {
                            case Mode.general:
                                tabItem.Visibility = Visibility.Visible;
                                break;
                            case Mode.cSep:
                                tabItem.Visibility = Visibility.Collapsed;
                                break;
                        }
                    }
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
                if ( !isTemplateWindow )
                {
                    TextPathIDOverlay.Visibility = Visibility.Visible;
                    NumPathIDOverlay.Visibility = Visibility.Visible;
                }
            }
            RepopulateTextBoxes ();
        }

        private void RepopulateTextBoxes()
        {
            // Clear textBoxes and then re-populate with the new active tab's textboxes
            textBoxes.Clear ();
            TabItem activeTab = (TabItem) MainTabControl.SelectedItem;
            if ( activeTab != null )
            {
                ListView listView = (ListView) activeTab.Content;
                if ( listView != null )
                {
                    foreach ( Grid grid in listView.Items.OfType<Grid> () )
                    {
                        List<TextBox> tempList = grid.Children.OfType<TextBox> ().ToList ();
                        if ( tempList != null && tempList.Count > 0 )
                        {
                            TextBox textBox = (TextBox) tempList[0];
                            textBoxes.Add ( textBox );
                            textBox.GotKeyboardFocus += EMPTYTextBox_GotKeyboardFocus;
                        }
                    }
                }
            }
        }

        public void BindWidth( FrameworkElement bindMe, FrameworkElement toMe )
        {
            Binding b = new Binding ();
            b.Mode = BindingMode.OneWay;
            b.Source = toMe.ActualWidth;
            bindMe.SetBinding ( FrameworkElement.WidthProperty, b );
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
                    // variable xmlNodes would be a list including all <UnitOperations>
                    xmlNodes = node.ChildNodes;
                    break;
                }
            }
           
            // For each main node (xmlNode would be one UnitOperation), check if it has a tab of it's own
            // If it does not have a tab specified in <Tabs_XEDITOR>, ignore it
            foreach ( XmlNode xmlNode in xmlNodes )
            {
                if ( tabHeaders.Contains ( xmlNode.Name ) )
                {
                    // Set active main node 
                    if ( xmlNode.Name != "Tabs_XEDITOR" )
                    {
                        activeMainNodeName = xmlNode.Name;
                    }

                    // Get matching tabItem for xmlNode
                    // NOTE: tabItem.Header is same as matching xmlNode.Name
                    TabItem tabItem = new TabItem ();
                    foreach ( TabItem curTabItem in tabItems )
                    {
                        if ( (curTabItem.Header as string) == xmlNode.Name )
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

                    // Top level xml comments
                    if ( xmlNode.NodeType == XmlNodeType.Comment )
                    {
                        // TODO - figure out comment display formatting
                    }
                }
            }
        }

        // Parse info for a single tab 
        private void RecursiveParseTabInfo( TabItem tabItem, XmlNode xmlNode )
        {
            // Check for tabItem saved history
            XmlNode xmlNodeHistory; 
            int pathID = GetPathID ( xmlNode );
            if ( pathIDHistories.ContainsKey ( pathID ) && ( pathID != -1 ) )
            {
                xmlNodeHistory = pathIDHistories[pathID];
                xmlNode = xmlNodeHistory;
            }

            // If xmlNode child node/element has its own tab in <Tabs_XEDITOR> link to its tab
            ListView listView = new ListView ();

            // Every time a new UnitOperation is sent to this method, it resets all of the current tabs
            if ( xmlNode.Name == activeMainNodeName && ( xmlNode.NodeType == XmlNodeType.Element ) )
            {
                HandleParsingUOP ( xmlNode, tabItem );
            }

            // Handle attributes (header, new attibute button, display all xmlNode attributes)
            HandleAttributes ( xmlNode, listView);
            listView.Items.Add ( new Separator () );

            // Handle text field
            HandleTextField ( xmlNode, listView );

            // Handle elements (header, new element button, display all xmlNode elements)
            HandleElements ( xmlNode, listView );
            
            // ListView construction is over, now set as the tabItem content
            tabItem.Content = listView;
        }

        private void HandleElements( XmlNode xmlNode, ListView listView )
        {
            // Display element header 
            TextBlock elementTitleTextBlock = new TextBlock ();
            elementTitleTextBlock.Text = "Elements:";
            elementTitleTextBlock.FontWeight = FontWeights.Bold;
            elementTitleTextBlock.FontSize = FONT_SIZE - 2;
            elementTitleTextBlock.TextDecorations = TextDecorations.Underline;
            listView.Items.Add ( elementTitleTextBlock );

            #region New element button

            Button newElementButton = new Button ();
            newElementButton.IsTabStop = false;
            newElementButton.Content = "Add new";
            newElementButton.Click += new RoutedEventHandler ( newElementButton_Click );
            newElementButton.Background = new SolidColorBrush ( Colors.LightGray );
            newElementButton.BorderBrush = new SolidColorBrush ( Colors.Transparent );
            newElementButton.FontSize = FONT_SIZE - 6;

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
                    ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
                    newGrid.AddTabButton ( xmlChildNode.Name );
                    
                    listView.Items.Add ( newGrid );

                    // Get matching tabItem for xmlNode
                    // NOTE: tabItem.Header is same as matching xmlNode.Name
                    TabItem tabItem2 = new TabItem ();
                    foreach ( TabItem curTabItem in tabItems )
                    {
                        if ( ( curTabItem.Header as string ) == xmlChildNode.Name )
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
        }

        private void HandleTextField( XmlNode xmlNode, ListView listView )
        {
            // If xmlNode contains only text, then display
            if ( xmlNode.HasChildNodes )
            {
                if ( xmlNode.FirstChild.NodeType == XmlNodeType.Text )
                {
                    ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
                    newGrid.AddTextBlock ( xmlNode.Name, toolTip: "Text field" );
                    newGrid.AddTextBox ( xmlNode.FirstChild.Value, toolTip: "Text field" );
                    newGrid.AddContextMenu ();
                    listView.Items.Add ( newGrid );
                }
            }
        }

        private void HandleAttributes( XmlNode xmlNode, ListView listView)
        {
            // Display attribute header
            TextBlock attribTitleTextBlock = new TextBlock ();
            attribTitleTextBlock.Text = "Attributes:";
            attribTitleTextBlock.FontSize = FONT_SIZE - 2;
            attribTitleTextBlock.TextDecorations = TextDecorations.Underline;
            attribTitleTextBlock.FontWeight = FontWeights.Bold;
            listView.Items.Add ( attribTitleTextBlock );

            #region New attribute button

            Button newAttributeButton = new Button ();
            newAttributeButton.Content = "Add new";
            newAttributeButton.IsTabStop = false;
            newAttributeButton.Click += new RoutedEventHandler ( newAttributeButton_Click );
            newAttributeButton.Background = new SolidColorBrush ( Colors.LightGray );
            newAttributeButton.BorderBrush = new SolidColorBrush ( Colors.Transparent );
            newAttributeButton.FontSize = FONT_SIZE - 6;

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
                    ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
                    newGrid.AddTextBlock ( attribute.Name, toolTip: xmlNode.Name + "'s attribute" );
                    newGrid.AddTextBox ( attribute.Value, toolTip: attribute.Name + " attribute" );
                    newGrid.AddContextMenu ();

                    listView.Items.Add ( newGrid );
                }
            }
        }

        private void HandleParsingUOP( XmlNode xmlNode, TabItem tabItem)
        {
            // Increment number of UOPs parsed
            UOPsParsed++;
            parsedPathIDForCurrentUOP = false;
            // Add UOPXmlNode to UOPXmlNodes, this makes it possible to switch between different UnitOperations
            int pathId = GetPathID ( xmlNode );

            List<string> curTabHeaders = new List<string> ();
            foreach ( TabItem curTabItem in MainTabControl.Items.OfType<TabItem> () )
            {
                if ( curTabItem.Visibility == Visibility.Visible )
                {
                    curTabHeaders.Add ( curTabItem.Header as String );
                }
            }
            UOPXmlNode newUOPXmlNode = new UOPXmlNode ( xmlNode, pathId, curTabHeaders, activeMainNodeName );
            if ( !UOPXmlNodes.Contains ( newUOPXmlNode ) )
            {
                UOPXmlNodes.Add ( newUOPXmlNode );
            }

            if ( UOPsParsed > 1 )
            {
                // If given new UnitOperation to parse, window should only display information for new UOP
                ResetAllTabs ();
            }

            // Display activeMainNodeTab base on editorMode
            switch ( editorMode )
            {
                case Mode.general:
                    tabItem.Visibility = Visibility.Visible;
                    break;
                case Mode.cSep:
                    tabItem.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        // Switches keyboard focus to the next textbox. Called for all appropriate textboxes
        public void OnTabPressed( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Tab )
            {
                TextBox curTextbox = sender as TextBox;
                int index = textBoxes.IndexOf ( curTextbox );
                // Go back to first textbox if currently at the last one
                if ( index + 1 == textBoxes.Count )
                {
                    Keyboard.Focus ( textBoxes[0] );
                    FocusManager.SetFocusedElement ( textBoxes[0].Parent, textBoxes[0] );
                }
                else
                {
                    Keyboard.Focus ( textBoxes[index + 1] );
                    FocusManager.SetFocusedElement ( textBoxes[index + 1].Parent, textBoxes[index + 1] );
                }
            }
        }

        // Helper function; requires xmlNode parameter is only ever passed as a <UnitOperation> xml element
        private int GetPathID( XmlNode xmlNode )
        {
            foreach ( XmlNode xmlAPChildNode in xmlNode.ChildNodes )
            {
                if ( xmlAPChildNode.Name == "PathID") // Currently requires all UOPs to have this child node
                {
                    if ( xmlAPChildNode.FirstChild != null )
                    {
                        return Convert.ToInt32 ( xmlAPChildNode.FirstChild.Value );
                    }
                    else
                    {
                        //MessageBox.Show ( "No PathID specified in xml document.", "PathID Error" );
                    }
                }
            }
            return -1;
        }

        // Method for parsing xml info into listVew for a sub-element without it's own tab
        private void ParseChildElementWithoutOwnTab( ref ListView listView, XmlNode xmlChildNode, bool isSubElement )
        {
            // Special behaviour if currently parsing a PathID
            if ( xmlChildNode.Name == "PathID" )
            {
                HandleParsingPathID ( xmlChildNode, listView );
            }
            else
            {
                HandleParsingElements ( xmlChildNode, listView, isSubElement );
            }

            HandleParsingSubAttributes ( xmlChildNode, listView );

            foreach ( XmlNode xmlGrandChildNode in xmlChildNode.ChildNodes )
            {
                ParseChildElementWithoutOwnTab ( ref listView, xmlGrandChildNode, true );
            }

            // Insert separator at the end of a non-text child node 
            if ( isSubElement && isLastSubElement ( xmlChildNode ) &&
                ( xmlChildNode.NodeType != XmlNodeType.Text ) )
            {
                listView.Items.Add ( new Separator () );
            }
        }

        private void HandleParsingSubAttributes( XmlNode xmlChildNode, ListView listView )
        {
            // Display attributes, since in this case the node will not have it's own tab to show them in
            if ( xmlChildNode.Attributes != null )
            {
                if ( xmlChildNode.Attributes.Count > 0 )
                {
                    foreach ( XmlAttribute attribute in xmlChildNode.Attributes )
                    {
                        ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
                        newGrid.AddTextBlock ( ( "[" + xmlChildNode.Name + " attribute] " + attribute.Name ), toolTip: "Attribute" );
                        newGrid.AddTextBox ( attribute.Value, toolTip: xmlChildNode.Name + "'s attribute" );
                        newGrid.AddContextMenu ();

                        listView.Items.Add ( newGrid );
                    }
                }
            }
        }

        private void HandleParsingElements( XmlNode xmlChildNode, ListView listView, bool isSubElement )
        {
            if ( xmlChildNode.HasChildNodes )
            {
                // Case that xmlChildNode is not EMPTY and contains ONLY text
                // Parse text elements such as <help>, <source>, <destination> etc..
                if ( xmlChildNode.FirstChild.NodeType == XmlNodeType.Text && xmlChildNode.ChildNodes.Count == 1 )
                {
                    ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
                    newGrid.AddContextMenu ();
                    // MenuItem sendMenuItem = new MenuItem ();
                    // sendMenuItem.Header = "Send to own tab";
                    // sendMenuItem.Click += SendToOwnTabItem_Click;
                    // rightClickMenu.Items.Add ( sendMenuItem );

                    if ( isSubElement )
                    {
                        newGrid.AddTextBlock ( xmlChildNode.Name, toolTip: "Element (sub)" );
                        newGrid.AddTextBox ( xmlChildNode.FirstChild.Value, toolTip: xmlChildNode.Name + " element (sub)" );
                    }
                    else
                    {
                        newGrid.AddTextBlock ( xmlChildNode.Name, toolTip: "Element" );
                        newGrid.AddTextBox ( xmlChildNode.FirstChild.Value, toolTip: xmlChildNode.Name + " element" );
                    }

                    listView.Items.Add ( newGrid );
                }

                // Let's say there is a non-text child element in <UnitOperation> that isn't given its own tab in <Tabs_XEDITOR>
                // This is where it is handled, because we still want to print its info
                if ( xmlChildNode.NodeType == XmlNodeType.Element && xmlChildNode.ChildNodes.Count > 1 )
                {
                    foreach ( XmlNode subNode in xmlChildNode.ChildNodes )
                    {
                        if ( subNode.NodeType == XmlNodeType.Text )
                        {
                            // Not supported in current version
                        }
                    }
                }
                if ( ( ( xmlChildNode.NodeType == XmlNodeType.Element ) && ( xmlChildNode.FirstChild.NodeType != XmlNodeType.Text ) )
                  || ( ( xmlChildNode.NodeType == XmlNodeType.Element ) && ( xmlChildNode.FirstChild.NodeType == XmlNodeType.Text ) && ( xmlChildNode.ChildNodes.Count > 1 ) ) )
                {
                    ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
                    newGrid.AddTextBlock ( xmlChildNode.Name, "", header: true );
                    newGrid.AddContextMenu ();
                    
                    listView.Items.Add ( newGrid );
                }
            }
            else
            {
                // Child node containing no children
                // For now, displaying "EMPTY" if it's an empty element
                if ( xmlChildNode.NodeType != XmlNodeType.Text && xmlChildNode.NodeType != XmlNodeType.Comment ) // Errors otherwise
                {
                    ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
                    if ( isSubElement )
                    {
                        newGrid.AddTextBlock ( xmlChildNode.Name, toolTip: "Empty element (sub)" );
                        newGrid.AddTextBox ( "", toolTip: xmlChildNode.Name + " element (sub)", empty: true );
                    }
                    else
                    {
                        newGrid.AddTextBlock ( xmlChildNode.Name, toolTip: "Empty element" );
                        newGrid.AddTextBox ( "", toolTip: xmlChildNode.Name + " element", empty: true );
                    }
                    
                    newGrid.AddContextMenu ();
                    // MenuItem sendMenuItem = new MenuItem ();
                    // sendMenuItem.Header = "Send to own tab";
                    // sendMenuItem.Click += SendToOwnTabItem_Click;
                    // rightClickMenu.Items.Add ( sendMenuItem );
                    
                    listView.Items.Add ( newGrid );
                }
            }
        }

        private void HandleParsingPathID( XmlNode xmlChildNode, ListView listView )
        {
            if ( this.isTemplateWindow )
            {
                // Do nothing
            }
            else
            {
                if ( xmlChildNode.FirstChild == null )
                {
                    // Case that it's parsing an empty PathID xml node
                    if ( PathIDComboBox.Items.Count == 0
                        || Convert.ToString ( ( (ComboBoxItem) PathIDComboBox.Items[PathIDComboBox.Items.Count - 1] ).Content ) != NEW_UOP_STRING )
                    {
                        ComboBoxItem addNewUnitOperationItem = new ComboBoxItem ();
                        addNewUnitOperationItem.Content = NEW_UOP_STRING;
                        PathIDComboBox.Items.Add ( addNewUnitOperationItem );
                        PathIDTextBlock.ToolTip = "Current PathID - Change to switch active UnitOperation";
                        setUpPathIDComboBox = true;
                    }
                }
                else if ( !parsedPathIDForCurrentUOP )
                {
                    parsedPathIDForCurrentUOP = true;
                    // Add PathID to PathIDComboBox
                    if ( setUpPathIDComboBox == false )
                    {
                        setUpPathIDComboBox = true;
                        // Initialize PathIDComboBox with event handling for switching paths
                        ComboBoxItem addNewUnitOperationItem = new ComboBoxItem ();
                        addNewUnitOperationItem.Content = NEW_UOP_STRING;
                        PathIDComboBox.Items.Add ( addNewUnitOperationItem );
                        ComboBoxItem newPathID = new ComboBoxItem ();
                        newPathID.Content = xmlChildNode.FirstChild.Value;
                        AddNewPathID ( newPathID );
                    }
                    else
                    {
                        // Add new PathID to the PathIDComboBox
                        ComboBoxItem newPathID = new ComboBoxItem ();
                        newPathID.Content = xmlChildNode.FirstChild.Value;
                        if ( !ContainsPathID ( PathIDComboBox, xmlChildNode.FirstChild.Value ) )
                        {
                            AddNewPathID ( newPathID );
                        }
                    }
                }
            }
        }

        private bool isLastSubElement( XmlNode xmlChildNode )
        {
            XmlNode parentNode = xmlChildNode.ParentNode;
            if ( parentNode.LastChild == xmlChildNode )
            {
                return true;
            }
            return false;
        }

        // Only called for elements with/that could have sub-items
        private void DeleteItemWithSubElems_Click( object sender, RoutedEventArgs e )
        {
            MenuItem deleteItem = (MenuItem) sender;
            Grid grid = ( (ContextMenu) deleteItem.Parent ).PlacementTarget as Grid;
            ListView listView = grid.Parent as ListView;
            int index = listView.Items.IndexOf ( grid );
            // Check if element has any sub items
            if ( (listView.Items.Count != index + 1) && listView.Items[index + 1] is Grid )
            {
                TextBlock textBlock = ( (Grid) listView.Items[index + 1] ).Children[0] as TextBlock;
                if ( textBlock != null )
                {
                    string toolTip = (string) textBlock.ToolTip;
                    if ( toolTip != null )
                    {
                        toolTip = toolTip.Substring ( toolTip.Length - 5 ).ToLower ();
                        if ( toolTip == "(sub)" || toolTip == "ibute" ) // Fix for sub item having its own attributes
                        {
                            MessageBox.Show ( "Cannot delete elements with existing sub-items.", "Error" );
                            return;
                        }
                    }
                }
            }
            listView.Items.Remove ( grid );
            string elemHeader = grid.Children.OfType<TextBlock> ().First ().Text.Replace ( ":", "" );
            deletedElementHeaders.Push ( elemHeader );
            undoableCommand = UndoableType.delElemHeader;
            
        }

        // Only called for an attribute or an element -- not for elements with sub-items
        public void DeleteItem_Click( object sender, RoutedEventArgs e )
        {
            MenuItem deleteItem = (MenuItem) sender;
            Grid grid = ( (ContextMenu) deleteItem.Parent ).PlacementTarget as Grid;
            // Get rid of undoable creation of this grid
            ListView listView = grid.Parent as ListView;
            int index = listView.Items.IndexOf ( grid );
            TextBlock senderTextBlock = grid.Children[0] as TextBlock;
            string senderToolTip = (string) senderTextBlock.ToolTip;

            // If an attribute, just delete and add a new XmlAttribute to deletedXmlAttributes
            if (senderToolTip != null && senderToolTip.Length >= 9 
                && ( senderToolTip.Substring ( senderToolTip.Length - 9 ).ToLower () == "attribute"))
            {
                // Remove attribute grid
                listView.Items.Remove ( grid );
                // Create and save deletedXmlAttribute (undoable action)
                TextBlock attribNameTextBlock = grid.Children.OfType<TextBlock> ().First ();
                TextBox attribValueTextBox = grid.Children.OfType<TextBox> ().First ();
                string subAttribName = attribNameTextBlock.Text.Replace ( ":", "" );
                string subAttribValue = attribValueTextBox.Text;
                if ( subAttribName[0] == '[' )
                {
                    // TODO: figure out undoing editor sub attributes. Check for same TODO not far below
                    // subAttribName = "TODO";
                    string elementName = GetSubAttributeElementName ( subAttribName );
                    subAttribName = subAttribName.Substring ( subAttribName.LastIndexOf ( " " ) + 1 );
                    XmlAttribute deletedXmlSubAttribute = new XmlDocument ().CreateAttribute ( subAttribName );
                    deletedXmlSubAttribute.Value = subAttribValue;
                    SubAttribute subAttribute = new SubAttribute ( deletedXmlSubAttribute, elementName );
                    deletedXmlSubAttributes.Push ( subAttribute );
                    undoableCommand = UndoableType.delSubAttrib;
                }
                else
                {
                    XmlAttribute deletedXmlAttribute = new XmlDocument ().CreateAttribute ( subAttribName );
                    deletedXmlAttribute.Value = subAttribValue;
                    deletedXmlAttributes.Push ( deletedXmlAttribute );
                    undoableCommand = UndoableType.delAttrib;
                }
                
                return;
            }
            else
            {
                // If an element, delete all element attributes
                // Also add a new XmlElement to deletedXmlElements
                undoableCommand = UndoableType.delElem;
                XmlDocument helperXmlDoc = new XmlDocument ();
                TextBlock elemNameTextBlock = grid.Children.OfType<TextBlock> ().First ();
                TextBox elemValueTextBox = grid.Children.OfType<TextBox> ().First ();
                string elemName = elemNameTextBlock.Text.Replace(":","");
                string elemValue = elemValueTextBox.Text;
                string elemToolTip = elemNameTextBlock.ToolTip as string;
                elemToolTip = elemToolTip.Substring ( elemToolTip.Length - 5 );
                XmlElement deletedXmlElement = null;
                XmlElement parentXmlElement = null;
                if ( elemToolTip.ToLower () == "(sub)" )
                {
                    // Find parent element with helper function
                    // Attach to parent
                    // Make sure that when undo is called, element sub-elements get put into the right place and all attributes are returned
                    string parentStr = FindSubElementParentString ( grid );
                    parentXmlElement = helperXmlDoc.CreateElement ( parentStr );
                    XmlElement importNode = helperXmlDoc.CreateElement ( elemName );
                    importNode.InnerText = elemValue;
                    deletedXmlElement = (XmlElement)  parentXmlElement.OwnerDocument.ImportNode ( importNode, true );
                    parentXmlElement.AppendChild ( deletedXmlElement );
                }
                else
                {
                    deletedXmlElement = helperXmlDoc.CreateElement ( elemName );
                    deletedXmlElement.InnerText = elemValue;
                }

                while ( true )
                {
                    if ( listView.Items.Count == index + 1)
                    {
                        break;
                    }
                    var next = listView.Items[index + 1];
                    if ( next is TextBlock || next is Separator ) { break; }
                    if ( ( (Grid) next ).Children.OfType<TextBlock> ().Count () == 0 ) { break; }
                    TextBlock textBlock = ( (Grid) next ).Children.OfType<TextBlock> ().First ();
                    if ( textBlock == null ){ break; }
                    string toolTip = textBlock.ToolTip as string;
                    // Check if next is an attribute
                    if ( ( next is Grid && ( toolTip != null && toolTip.Length >= 9 ) )
                        && toolTip.Substring ( toolTip.Length - 9 ).ToLower () == "attribute" )
                    {
                        // Remove and temp save all attributes
                        listView.Items.Remove ( next );
                        TextBlock subAttribNameTextBlock = ( (Grid) next ).Children.OfType<TextBlock> ().First ();
                        TextBox subAttribValueTextBox = ( (Grid) next ).Children.OfType<TextBox> ().First ();
                        string subAttribName = subAttribNameTextBlock.Text.Replace ( ":", "" );
                        string subAttribValue = subAttribValueTextBox.Text;
                        subAttribName = GetSubAttributeName ( deletedXmlElement.Name, subAttribName );
                        XmlAttribute subDeletedXmlAttribute = helperXmlDoc.CreateAttribute ( subAttribName );
                        subDeletedXmlAttribute.Value = subAttribValue;
                        deletedXmlElement.Attributes.Append ( subDeletedXmlAttribute );
                    }
                    else
                    {
                        break;
                    }
                }
                listView.Items.Remove ( grid );
                deletedXmlElements.Push ( deletedXmlElement );
            }
        }

        // Helper function
        private string GetSubAttributeElementName( string subAttribStr )
        {
            return subAttribStr.Substring ( 1, subAttribStr.IndexOf ( " " ) - 1 );
        }

        // Helper function
        private string GetSubAttributeName( string elementName, string subAttribTextBlockStr )
        {
            int index = 13 + elementName.Length;
            string subAttribName = subAttribTextBlockStr.Substring ( index );
            return subAttribName;
        }

        private string FindSubElementParentString( Grid elementGrid )
        {
            ListView listView = elementGrid.Parent as ListView;
            int index = listView.Items.IndexOf ( elementGrid );
            TextBlock subElementParentTextBlock = null;
            for ( int i = (index - 1); i >= 0; i-- )
            {
                if ( listView.Items[i] is Grid )
                {
                    subElementParentTextBlock = ( listView.Items[i] as Grid ).Children.OfType<TextBlock> ().First();
                    if ( subElementParentTextBlock.FontWeight == FontWeights.Bold )
                    {
                        break;
                    }
                }
            }
            return subElementParentTextBlock.Text.Replace ( ":", "" );

        }

        public void EMPTYTextBox_GotKeyboardFocus( object sender, KeyboardFocusChangedEventArgs e )
        {
            TextBox emptyTextBox = sender as TextBox;
            if ( emptyTextBox.Text == "EMPTY" )
            {
                emptyTextBox.Clear ();
            }
        }

        private void AddNewPathID( ComboBoxItem newPathID )
        {
            // Need to insert the new ComboBoxItem into the proper place 
            // (before "New UnitOperation" option) 
            if ( PathIDComboBox.Items.Count <= 1 )
            {
                PathIDComboBox.Items.Insert ( 0, newPathID );
            }
            else
            {
                int i = 0;
                foreach ( ComboBoxItem item in PathIDComboBox.Items )
                {
                    if ( Convert.ToString ( item.Content ) != NEW_UOP_STRING )
                    {
                        if ( Convert.ToInt32 ( item.Content ) > Convert.ToInt32 ( newPathID.Content ) )
                        {
                            PathIDComboBox.Items.Insert ( i, newPathID );
                            return;
                        }
                        i++;
                    }
                }
                PathIDComboBox.Items.Insert ( i, newPathID );
            }
        }

        private void RemoveGridFromListViewParent( Grid grid )
        {
            if ( grid.Parent != null )
            {
                ListView parent = (ListView) grid.Parent;
                parent.Items.Remove ( grid );
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
            if ( PathIDComboBox.SelectedIndex != -1 )
            {
                ActiveUOPNameTextBox.IsEnabled = true;
                // Change PathID overlay
                NumPathIDOverlay.Text = Convert.ToString ( ( (ComboBoxItem) PathIDComboBox.SelectedItem ).Content );
                
                ComboBoxItem item = PathIDComboBox.SelectedItem as ComboBoxItem;
                if ( Convert.ToString ( item.Content ) == NEW_UOP_STRING ) 
                {
                    if ( (bool) autoGenPathID )
                    {
                        int autoPathID = GeneratePathID ();
                        NewPathIDEntered ( autoPathID );
                    }
                    else
                    {
                        NewUnitOperation newUOPWindow = new NewUnitOperation ( this );
                        newUOPWindow.Owner = this;
                        newUOPWindow.Show ();
                        this.IsEnabled = false;
                    }
                }
                else
                {
                    string strPathID = Convert.ToString ( item.Content );
                    int intPathID = Convert.ToInt32 ( strPathID );
                    SwitchCurrentUnitOperation ( intPathID );
                    UnHighlightPathIDSection ();
                }
            }
            else
            {
                ActiveUOPNameTextBox.Text = "";
                ActiveUOPNameTextBox.IsEnabled = false;
            }
            RepopulateTextBoxes ();      
        }

        private int GeneratePathID()
        {
            int i = 0;
            foreach ( ComboBoxItem comboBoxItem in PathIDComboBox.Items.OfType<ComboBoxItem> () )
            {
                if ( ( comboBoxItem.Content as string ) != NEW_UOP_STRING )
                {
                    i++;
                    string str = comboBoxItem.Content.ToString();
                    int pathID = Convert.ToInt32 ( str );
                    if ( i == pathID )
                    {
                        continue;
                    }
                    else
                    {
                        return i;
                    }
                }
            }
            return i + 1;
        }

        private void SavePathIDState( object sender, SelectionChangedEventArgs e )
        {
            // Save any changes to the currently active tabs before switching to a new active UOP
            // At this point, the selected PathID has been changed but the tabItems list has not yet been updated
            XmlDocSave historyDocSave = new XmlDocSave ( new XmlDocument (), "", this );
            XmlNode savedActiveTabsState = historyDocSave.WriteCurrentOpenTabs ( tabItems, currentPathID );
            // If exists, delete previous saved state and overwrite with new saved state
            if ( pathIDHistories.ContainsKey ( currentPathID ) )
            {
                pathIDHistories.Remove ( currentPathID );
            }
            if ( currentPathID != -1 )
            {
                pathIDHistories.Add ( currentPathID, savedActiveTabsState.FirstChild.LastChild );
            }
        }

        public void NewPathIDEntered( int pathID )
        {
            // Allow user to select template for new UOP
            TemplateListWindow templateListWindow = new TemplateListWindow ( this, "Select", pathID );
            templateListWindow.Owner = this;
            templateListWindow.Show ();
            this.IsEnabled = false;
        }

        public void UserSelectedTemplate(TemplateXmlNode templateXmlNode, int pathID)
        {
            this.tabHeaders = CopyRefStringList ( templateXmlNode.TabHeaders );
            XmlNode newXmlNode = templateXmlNode.XmlNode.CloneNode ( true );
            TabItem newMainNodeTabItem = new TabItem ();
            newMainNodeTabItem.FontSize = FONT_SIZE;
            newMainNodeTabItem.Header = templateXmlNode.MainNodeName;
            if ( !TabItemsContainsHeader ( newMainNodeTabItem.Header as string ) )
            {
                tabItems.Add ( newMainNodeTabItem );
            }
            if ( !MainTabControlContainsHeader ( newMainNodeTabItem.Header as string ) )
            {
                MainTabControl.Items.Add ( newMainNodeTabItem );
            }
            foreach ( string tabHeader in tabHeaders )
            {
                TabItem newTabItem = new TabItem ();
                newTabItem.FontSize = FONT_SIZE;
                newTabItem.Header = tabHeader;
                if ( !MainTabControlContainsHeader ( tabHeader ) )
                {
                    MainTabControl.Items.Add ( newTabItem );
                }
                if ( !TabItemsContainsHeader ( tabHeader ) )
                {
                    tabItems.Add ( newTabItem );
                }
            }

            // Need to set the PathID into the newXmlNode so that it isn't empty
            foreach ( XmlNode xmlNode in newXmlNode.ChildNodes )
            {
                if ( xmlNode.Name == "PathID" && xmlNode.NodeType == XmlNodeType.Element )
                {
                    xmlNode.InnerText = Convert.ToString ( pathID );
                }
            }

            UOPXmlNode newUnitOperation = new UOPXmlNode ( newXmlNode, pathID, templateXmlNode.TabHeaders, templateXmlNode.MainNodeName );
            UOPXmlNodes.Remove ( newUnitOperation ); // to get rid of old one if it exists (compares by PathID)
            UOPXmlNodes.Add ( newUnitOperation );
            ComboBoxItem newPathIDItem = new ComboBoxItem ();
            newPathIDItem.Content = pathID;
            AddNewPathID ( newPathIDItem );
            PathIDComboBox.SelectionChanged -= SavePathIDState;
            PathIDComboBox.SelectedIndex = PathIDComboBox.Items.IndexOf ( newPathIDItem ); // Switch to new PathID
            PathIDComboBox.SelectionChanged += SavePathIDState;
        }

        private void MoveMainTabToFront()
        {
            TabItem mainTabItem = GetTabItemWithHeader ( activeMainNodeName );
            MainTabControl.Items.Remove ( mainTabItem );
            MainTabControl.Items.Insert ( 0, mainTabItem );
        }

        // Helper function
        private bool MainTabControlContainsHeader( string tabHeader )
        {
            foreach ( TabItem tabItem in MainTabControl.Items )
            {
                if ( tabItem.Header as string == tabHeader )
                {
                    return true;
                }
            }
            return false;
        }

        // Helper function
        private bool TabItemsContainsHeader( string tabHeader )
        {
            foreach ( TabItem tabItem in tabItems )
            {
                if ( (string)tabItem.Header == tabHeader )
                {
                    return true;
                }
            }
            return false;
        }

        // Displays new UOP and sub-tabs associated with param pathID in window
        private void SwitchCurrentUnitOperation( int? pathID )
        {
            if (pathID != null)
            {
                currentPathID = (int) pathID;
            }
            XmlNode unitOperation = null;

            UOPXmlNode UOPXmlNode = GetUOPXmlNodeWithPathID ( pathID );
            unitOperation = UOPXmlNode.XmlNode;
            this.activeMainNodeName = UOPXmlNode.MainNodeName;
            this.tabHeaders = CopyRefStringList ( UOPXmlNode.TabHeaders );
            ActiveUOPNameTextBox.Text = activeMainNodeName;

            // Now get corresponding tabItem..
            TabItem UOPTabItem = GetTabItemWithHeader ( activeMainNodeName );
            if ( UOPTabItem == null )
            {
                // new main tabitem
                UOPTabItem = new TabItem ();
                UOPTabItem.Header = activeMainNodeName;
                UOPTabItem.FontSize = FONT_SIZE;
                MainTabControl.Items.Add ( UOPTabItem );
                tabItems.Add ( UOPTabItem );
            }
            if ( unitOperation == null )
            {
                throw new Exception ( "Error: var unitOperation should not be null." );
            }
            RecursiveParseTabInfo ( UOPTabItem, unitOperation );
            SetTabVisibility ();
            PopulateTabLinkButtons ();
            MoveMainTabToFront ();

            // Set active tab to be the first tabItem in MainTabControl with Visibility.Visible
            foreach ( TabItem tabItem in MainTabControl.Items )
            {
                if ( tabItem.Visibility == Visibility.Visible )
                {
                    MainTabControl.SelectedItem = tabItem;
                    foreach ( TextBox textBox in textBoxes )
                    {
                        textBox.GotKeyboardFocus += EMPTYTextBox_GotKeyboardFocus;
                    }
                    break;
                }
            }
        }

        private UOPXmlNode GetUOPXmlNodeWithPathID( int? pathID )
        {
            // Get proper XmlNode for UOP, PathIDs must be the same
            foreach ( UOPXmlNode UOPXmlNode in UOPXmlNodes )
            {
                if ( UOPXmlNode.PathID == pathID )
                {
                    return UOPXmlNode;
                }
            }
            return null;
        }

        // Helper called whenever the active UOP is changed
        private void PopulateTabLinkButtons()
        {
            TabItem mainTabItem = GetTabItemWithHeader ( activeMainNodeName );
            foreach ( Grid grid in ( mainTabItem.Content as ListView ).Items.OfType<Grid>() )
            {
                List<Button> tempList = grid.Children.OfType<Button> ().ToList ();
                if ( tempList != null && tempList.Count != 0 )
                {
                    Button tabLinkButton = tempList[0];
                    if ( tabLinkButton != null )
                    {
                        tabLinkButtons.Add ( tabLinkButton );
                    }
                }
                
            }
        }

        // Helper for retrieving a certain tabItem from list tabItems
        private TabItem GetTabItemWithHeader( string tabItemHeader )
        {
            AssertTabItems ();

            foreach ( TabItem tabItem in tabItems )
            {
                if ( (string)tabItem.Header == tabItemHeader )
                {
                    return tabItem;
                }
            }
            return null;
        }

        // Assert that the tabItems list contains all the same items as MainTabControl.Items
        private void AssertTabItems()
        {
            if ( !tabItems.TrueForAll ( t => MainTabControl.Items.Contains ( t ) ) )
            {
                throw new Exception ( "TabItems conflict with MainTabControl.Items" );
            }
            if ( !MainTabControl.Items.OfType<TabItem> ().ToList ().TrueForAll ( t => tabItems.Contains ( t ) ) )
            {
                throw new Exception ( "TabItems conflict with MainTabControl.Items" );
            }
        }

        // Resets to zero tabs. Only called when about to reconstruct tabs, due to new active UOP
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
            if ( isTemplateWindow )
            {
                string message = "Caution -- this will exit the template editor without saving.\nProceed?";
                string header = "Warning";
                MessageBoxButton msgBoxButtons = MessageBoxButton.YesNo;
                MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
                if ( msgBoxResult == MessageBoxResult.Yes )
                {
                    this.Close ();
                    mainEditorWindow.IsEnabled = true;
                }
                else if ( msgBoxResult == MessageBoxResult.No )
                {
                    return;
                }
            }
            else
            {
                // Ask user if they want to save templates
                if ( NewTemplateExists() )
                {
                    string message = "Do you want to save your current templates before exiting the editor?";
                    string header = "Save templates?";
                    MessageBoxButton msgBoxButtons = MessageBoxButton.YesNoCancel;
                    MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
                    switch ( msgBoxResult )
                    {
                        case MessageBoxResult.Yes:
                            // Save templates
                            string projectFilePath = Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName;
                            string templateSavePath = projectFilePath + @"\Resources\Templates\";
                            SaveTemplates ( templateSavePath );
                            break;
                        case MessageBoxResult.No:
                            break;
                        case MessageBoxResult.Cancel:
                            return;
                    }
                }
                this.Close ();
            }
        }

        private bool NewTemplateExists()
        {
            string[] templateFilePaths = Directory.GetFiles ( Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName + @"\Resources\Templates" );
            List<string> templateNames = new List<string> ();
            foreach ( string templateFilePath in templateFilePaths )
            {
                string templateName = templateFilePath.Substring ( templateFilePath.LastIndexOf ( "\\" ) + 1 );
                templateNames.Add ( templateName );
            }
            foreach ( TemplateXmlNode templateXmlNode in templateXmlNodes )
            {
                if ( !templateNames.Contains ( templateXmlNode.Name ) )
                {
                    return true;
                }
            }
            return false;
        }

        private void SaveTemplates( string templateSavePath )
        {
            foreach ( TemplateXmlNode template in templateXmlNodes )
            {
                XmlDocument xmlDoc = new XmlDocument ();
                // Get template's main xml element body
                xmlDoc.LoadXml ( template.XmlNode.OuterXml );
                XmlElement mainTemplateXmlElement = xmlDoc.DocumentElement;
                // Set root for xml document
                xmlDoc.LoadXml ( xmlDoc.CreateElement ( "root" ).OuterXml );
                // Create and append <Tabs_XEDITOR> xml element
                string strTabsXEDITOR = String.Join ( ",", template.TabHeaders.ToArray() );
                XmlElement tabsXEDITORXmlElement = xmlDoc.CreateElement ( "Tabs_XEDITOR" );
                tabsXEDITORXmlElement.InnerText = strTabsXEDITOR;
                xmlDoc.DocumentElement.AppendChild ( tabsXEDITORXmlElement );
                // Append template main xml element
                xmlDoc.DocumentElement.AppendChild ( mainTemplateXmlElement );
                // Name and save
                string templateFileName;
                templateFileName = template.Name.Replace ( " ", "" ).Trim ();
                if ( !templateFileName.Contains(".") || templateFileName.Substring ( templateFileName.LastIndexOf ( "." ) ) != ".xml" )
                {
                    templateFileName = template.Name.Replace ( " ", "" ).Trim () + ".xml";
                }
                string curTemplateSavePath = templateSavePath + templateFileName;
                if ( !File.Exists ( curTemplateSavePath ) )
                {
                    xmlDoc.Save ( curTemplateSavePath );
                }
            }
        }

        public void gotoTabButton_Click( object sender, RoutedEventArgs e )
        {
            // Get matching tabItem for button click
            // NOTE: tabItem.Header is same as matching button.Tag
            TabItem tabItem = new TabItem ();
            foreach ( TabItem curTabItem in tabItems )
            {
                if ( ( (string) curTabItem.Header ) == (string) ( (Button) sender ).Tag )
                {
                    tabItem = curTabItem;
                }
            }
            MainTabControl.SelectedIndex = MainTabControl.Items.IndexOf ( tabItem );
        }

        private void newAttributeButton_Click( object sender, RoutedEventArgs e )
        {
            NewElemOrAttrib newAttributeWindow = new NewElemOrAttrib ( this, "attribute" );
            newAttributeWindow.Owner = this;
            newAttributeWindow.Show ();
            this.IsEnabled = false;
        }

        private void newElementButton_Click( object sender, RoutedEventArgs e )
        {
            NewElemOrAttrib newElementWindow = new NewElemOrAttrib ( this, "element" );
            newElementWindow.Owner = this;
            newElementWindow.Show ();
            this.IsEnabled = false;
        }

        public void AddNewAttribute( string name, string value, bool undoable, string elementName = null)
        {
            TabItem currentTabItem = MainTabControl.SelectedItem as TabItem;
            ListView currentListView = currentTabItem.Content as ListView;
            // Need to create the grid with appropriate TextBlock and TextBox, then insert at proper spot in currentListView
            ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
            newGrid.AddContextMenu ();

            if ( elementName == null )
            {
                newGrid.AddTextBlock ( name, toolTip: "Attribute" );
                newGrid.AddTextBox ( value, toolTip: "Attribute" );
                currentListView.Items.Insert ( 2, newGrid );
            }
            else
            {
                int elementIndex = GetElementIndex ( currentListView, elementName );
                newGrid.AddTextBlock ( "[" + elementName + " attribute] " + name, toolTip: "Attribute" );
                newGrid.AddTextBox ( value, toolTip: "Attribute" );
                currentListView.Items.Insert ( elementIndex + 1, newGrid );
            }

            if ( undoable )
            {
                createdXmlAttributes.Push ( newGrid );
                undoableCommand = UndoableType.createAttrib;
            }
        }

        public void AddNewElement( string name, string value, bool undoable, bool isSubElem = false, string parentNodeName = null )
        {
            TabItem currentTabItem = MainTabControl.SelectedItem as TabItem;
            ListView currentListView = currentTabItem.Content as ListView;
            // Need to create the grid with appropriate TextBlock and TextBox, then insert at proper spot in currentListView
            ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
            if ( isSubElem )
            {
                newGrid.AddTextBlock ( name, toolTip: "Element (sub)" );
                newGrid.AddTextBox ( value, toolTip: "Element" );
            }
            else
            {
                newGrid.AddTextBlock ( name, toolTip: "Element" );
                newGrid.AddTextBox ( value, toolTip: "Element" );
            }

            newGrid.AddContextMenu ();
            // MenuItem sendItem = new MenuItem ();
            // sendItem.Header = "Send to own tab";
            // sendItem.Click += SendToOwnTabItem_Click;
            // rightClickMenu.Items.Add ( sendItem );

            if ( isSubElem )
            {
                // Insert after parent
                int parentIndex = GetParentElementHeaderIndex ( currentListView, parentNodeName );
                int numParentAttributes = GetNumParentAttributes ( currentListView, parentNodeName );
                currentListView.Items.Insert ( parentIndex + numParentAttributes + 1, newGrid );
            }
            else
            {
                int insertIndex = GetElementHeaderIndex ( currentListView );
                currentListView.Items.Insert ( insertIndex + 2, newGrid );
            }

            if ( undoable )
            {
                createdXmlElements.Push ( newGrid );
                undoableCommand = UndoableType.createElem;
            }
        }

        // Too many errors -- unused for now
        private void SendToOwnTabItem_Click( object sender, RoutedEventArgs e )
        {
            throw new NotImplementedException ();
            //MenuItem sendItem = (MenuItem) sender;
            //Grid grid = ( sendItem.Parent as ContextMenu ).PlacementTarget as Grid;
            //string newTabName = grid.Children.OfType<TextBlock> ().First ().Text.Replace ( ":", "" );
            //NewTabEntered ( newTabName );
            //RemoveGridFromListViewParent ( grid );
            //grid = null;
            //tabHeaders = CopyRefStringList ( tabHeaders );
            //tabHeaders.Add ( newTabName );
            //GetUOPXmlNodeWithPathID ( currentPathID ).TabHeaders.Add ( newTabName );
        }

        // Helper function
        private int GetElementIndex( ListView currentListView, string elementName )
        {
            foreach ( Grid grid in currentListView.Items.OfType<Grid> () )
            {
                TextBlock elemTextBlock = null;
                if ( grid.Children.OfType<TextBlock> ().Count () > 0 )
                {
                    elemTextBlock = grid.Children.OfType<TextBlock> ().First ();
                }
                if ( elemTextBlock != null && elemTextBlock.Text == elementName + ":" )
                {
                    return currentListView.Items.IndexOf ( grid );
                }
            }
            return -1;
        }

        // Helper function
        private int GetNumParentAttributes( ListView listView, string parentNodeName )
        {
            int numAttributes = 0;
            foreach ( Grid grid in listView.Items.OfType<Grid> () )
            {
                TextBlock textBlock = null;
                if ( grid.Children.OfType<TextBlock> ().Count () > 0 )
                {
                    textBlock = grid.Children.OfType<TextBlock> ().First ();
                }
                if ( textBlock != null && textBlock.Text == parentNodeName + ":" && textBlock.FontWeight == FontWeights.Bold )
                {
                    int index = listView.Items.IndexOf ( grid ) + 1; // Start at parent index + 1
                    while ( true )
                    {
                        if ( listView.Items.Count == index )
                        {
                            return numAttributes;
                        }
                        Grid attribGrid = listView.Items[index] as Grid;
                        if ( attribGrid == null )
                        {
                            return numAttributes;
                        }
                        TextBlock attribTextBlock = attribGrid.Children.OfType<TextBlock> ().First ();
                        if ( attribTextBlock == null )
                        {
                            return numAttributes;
                        }
                        string toolTip = attribTextBlock.ToolTip as string;
                        if ( toolTip != null && toolTip.Length >= 9 && ( toolTip.Substring ( toolTip.Length - 9 ).ToLower () == "attribute" ) )
                        {
                            numAttributes++;
                        }
                        else
                        {
                            return numAttributes;
                        }
                        index++;
                    }
                }
            }
            return -1;
        }

        // Helper function
        private int GetParentElementHeaderIndex( ListView listView, string parentNodeName )
        {
            foreach ( Grid grid in listView.Items.OfType<Grid> () )
            {
                TextBlock textBlock = null;
                if ( grid.Children.OfType<TextBlock> ().Count() > 0 )
                {
                    textBlock = grid.Children.OfType<TextBlock> ().First ();
                }
                if ( textBlock != null && textBlock.Text ==  parentNodeName + ":" && textBlock.FontWeight == FontWeights.Bold )
                {
                    return listView.Items.IndexOf ( grid );
                }
            }
            return -1;
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

        private void Delete_AP_Button_Click( object sender, RoutedEventArgs e )
        {
            if ( currentPathID == -1 )
            {
                MessageBox.Show ( "No PathID selected.\nPlease select the PathID you want to remove and try again.", "Error" );
                return;
            }

            string message = "";
            string header = "";
            switch ( editorMode )
            {
                case Mode.general:
                    header = "Delete active xml node";
                    message = String.Format ( "Are you sure you want to remove xml node with PathID: {0}?", currentPathID );
                    break;

                case Mode.cSep:
                    header = "Delete active UOP";
                    message = String.Format ( "Are you sure you want to remove UOP with PathID: {0}?", currentPathID );
                    break;

            }
            MessageBoxButton msgBoxButtons = MessageBoxButton.YesNo;
            MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
            if ( msgBoxResult == MessageBoxResult.Yes )
            {
                DeleteActiveUOP ();
            }
            else if ( msgBoxResult == MessageBoxResult.No )
            {
                // User cancelled, do nothing
            }
        }

        private void DeleteActiveUOP()
        {
            undoableCommand = UndoableType.delUOP;

            ComboBoxItem removeItem = null;
            foreach ( ComboBoxItem item in PathIDComboBox.Items )
            {
                if (Convert.ToString(item.Content) != NEW_UOP_STRING )
                {
                    if ( Convert.ToInt32 ( item.Content ) == currentPathID )
                    {
                        removeItem = item;
                    }
                }
            }
            // Delete previous saved state
            if ( pathIDHistories.ContainsKey ( currentPathID ) )
            {
                pathIDHistories.Remove ( currentPathID );
            }
            XmlDocSave historyDocSave = new XmlDocSave ( new XmlDocument (), "", this );
            XmlNode savedActiveTabsState = historyDocSave.WriteCurrentOpenTabs ( tabItems, currentPathID );
            List<string> curTabHeaders = new List<string> ();
            foreach ( TabItem tabItem in MainTabControl.Items.OfType<TabItem>() )
            {
                if ( tabItem.Visibility == Visibility.Visible )
                {
                    curTabHeaders.Add ( tabItem.Header as String );
                }
            }
            UOPXmlNode removedUOP = new UOPXmlNode ( savedActiveTabsState.FirstChild.LastChild, currentPathID, curTabHeaders, activeMainNodeName );
            deletedUOPs.Push ( removedUOP );
            PathIDComboBox.Items.Remove ( removeItem );
            ResetAllTabs ();
            SetTabVisibility ();
            GoToFirstVisibleTab ();
            PathIDComboBox.SelectedIndex = -1;
            currentPathID = -1;

            // Prompt to create new UOP if PathIDComboBox is empty
            if ( PathIDComboBox.Items.Count == 1 )
            {
                HighlightPathIDSection ();
            }
        }

        // Helper function
        public void HighlightPathIDSection()
        {
            if ( pathIDSectionHighlighted == false )
            {
                switch ( editorMode )
                {
                    case Mode.general:
                        PathIDTextBlock.FontWeight = FontWeights.Bold;
                        Thickness pathIDComboBoxMarginsGeneral = PathIDComboBox.Margin;
                        PathIDComboBox.Margin = new Thickness ( pathIDComboBoxMarginsGeneral.Left + 3, pathIDComboBoxMarginsGeneral.Top,
                            pathIDComboBoxMarginsGeneral.Right, pathIDComboBoxMarginsGeneral.Bottom );
                        PathIDComboBox.Width -= 3;
                        pathIDSectionHighlighted = true;
                        break;

                    case Mode.cSep:
                        PathIDTextBlock.FontWeight = FontWeights.Bold;
                        Thickness pathIDComboBoxMarginscSep = PathIDComboBox.Margin;
                        PathIDComboBox.Margin = new Thickness ( pathIDComboBoxMarginscSep.Left + 5, pathIDComboBoxMarginscSep.Top,
                            pathIDComboBoxMarginscSep.Right, pathIDComboBoxMarginscSep.Bottom );
                        PathIDComboBox.Width -= 5;
                        pathIDSectionHighlighted = true;
                        break;
                }
            }
        }

        // Helper function
        public void UnHighlightPathIDSection()
        {
            if ( pathIDSectionHighlighted )
            {
                switch ( editorMode )
                {
                    case Mode.general:
                        PathIDTextBlock.FontWeight = FontWeights.Normal;
                        Thickness pathIDComboBoxMarginsGeneral = PathIDComboBox.Margin;
                        PathIDComboBox.Margin = new Thickness ( pathIDComboBoxMarginsGeneral.Left - 3, pathIDComboBoxMarginsGeneral.Top,
                            pathIDComboBoxMarginsGeneral.Right, pathIDComboBoxMarginsGeneral.Bottom );
                        PathIDComboBox.Width += 3;
                        pathIDSectionHighlighted = false;
                        break;

                    case Mode.cSep:
                        PathIDTextBlock.FontWeight = FontWeights.Normal;
                        Thickness pathIDComboBoxMarginscSep = PathIDComboBox.Margin;
                        PathIDComboBox.Margin = new Thickness ( pathIDComboBoxMarginscSep.Left - 5, pathIDComboBoxMarginscSep.Top,
                            pathIDComboBoxMarginscSep.Right, pathIDComboBoxMarginscSep.Bottom );
                        PathIDComboBox.Width += 5;
                        pathIDSectionHighlighted = false;
                        break;
                }
            }
        }

        private void Delete_Tab_Button_Click( object sender, RoutedEventArgs e )
        {
            if ( PathIDComboBox.SelectedIndex == -1 )
            {
                MessageBox.Show ( "No active tab to delete.", "Error" );
                return;
            }

            if ( ( MainTabControl.SelectedItem as TabItem ).Visibility == Visibility.Visible )
            {
                if ( MainTabControl.SelectedIndex == 0 )
                {
                    MessageBox.Show ( "Cannot delete the main tab.\nIf you are trying to delete a UOP, select that option in the \"Delete\" drop down menu.", "Error" );
                    return;
                }

                string message = String.Format ( "Are you sure you want to remove tab with header \"{0}\"?", ( (TabItem) MainTabControl.SelectedItem ).Header );
                string header = "Delete active Tab";
                MessageBoxButton msgBoxButtons = MessageBoxButton.YesNo;
                MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
                if ( msgBoxResult == MessageBoxResult.Yes )
                {
                    DeleteActiveTab ();
                }
                else if ( msgBoxResult == MessageBoxResult.No )
                {
                    // User cancelled, do nothing
                }

                if ( NoVisibleTabs () )
                {
                    ( MainTabControl.SelectedItem as TabItem ).Content = new ListView ();
                }
            }
            else
            {
                string message = String.Format ( "No tab selected.", ( (TabItem) MainTabControl.SelectedItem ).Header );
                string header = "Error";
                MessageBoxResult msgBoxResult = MessageBox.Show ( message, header );
            }

            
        }

        // Returns true if there are no visible tabs in MainTabControl
        private bool NoVisibleTabs()
        {
            foreach ( TabItem tabItem in MainTabControl.Items )
            {
                if ( tabItem.Visibility == Visibility.Visible )
                {
                    return false;
                }
            }
            return true;
        }

        private void DeleteActiveTab()
        {
            if ( isTemplateWindow )
            {
                PopulateTabLinkButtons ();
            }

            undoableCommand = UndoableType.delTab;
            // Make tab visibility collapsed -- this makes it easier to undo
            // When saving, only include tabs which have Visibility.Visible
            TabItem activeTab = MainTabControl.SelectedItem as TabItem;
            activeTab.Visibility = Visibility.Collapsed;
            deletedTabs.Push ( activeTab );
            GoToFirstVisibleTab ();

            // Make the tab link button disappear
            Button tabLinkButton = null;
            foreach ( Button curButton in tabLinkButtons )
            {
                if ( (string) activeTab.Header == (string) curButton.Content )
                {
                    tabLinkButton = curButton;
                    tabLinkButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void GoToFirstVisibleTab()
        {
            foreach ( TabItem tabItem in MainTabControl.Items )
            {
                if ( tabItem.Visibility == Visibility.Visible )
                {
                    MainTabControl.SelectedIndex = MainTabControl.Items.IndexOf ( tabItem );
                    return;
                }
            }
        }

        private void OpenCommandBinding( object sender, ExecutedRoutedEventArgs e )
        {
            if ( PathIDComboBox.Items.Count > 1 )
            {
                string message = "Save changes to xml document?\nOpening a new xml file will close this one.\nUnsaved changes will be lost.";
                string header = "Caution - save changes?";
                MessageBoxButton msgBoxButtons = MessageBoxButton.YesNoCancel;
                MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
                if ( msgBoxResult == MessageBoxResult.Yes )
                {
                    // Save the document first then continue with open command
                    Save_Button.Command.Execute ( null );
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
                MainWindow mainWindow = new MainWindow ( filePath, templateXmlNodes );
                mainWindow.Show ();
                this.Close ();

            }
        }

        private void SaveCommandBinding( object sender, ExecutedRoutedEventArgs e )
        {
            if ( PathIDComboBox.Items.Count == 1 )
            {
                MessageBox.Show ( "Unable to save.\nNo current xml active in the editor.", "Error" );
                return;
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
            XmlDocSave xmlDocSave = new XmlDocSave ( xmlDocTosave, fileSavePath, this );

            xmlDocSave.SaveAll ( tabItems, PathIDComboBox );
        }

        private void NewDocumentCommandBinding( object sender, ExecutedRoutedEventArgs e )
        {
            if ( PathIDComboBox.Items.Count > 1 )
            {
                string message = "Save changes to xml document?\nCreating a new xml file will close this one.\nUnsaved changes will be lost.";
                string header = "Caution - save changes?";
                MessageBoxButton msgBoxButtons = MessageBoxButton.YesNoCancel;
                MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
                if ( msgBoxResult == MessageBoxResult.Yes )
                {
                    // Save the document first then continue with open command
                    Save_Button.Command.Execute ( null );
                }
                else if ( msgBoxResult == MessageBoxResult.No )
                {
                    // Continue with new xml file command without saving
                }
                else if ( msgBoxResult == MessageBoxResult.Cancel )
                {
                    // Cancel command, no need to save
                    return;
                }
            }

            // Accesses the DefaultcSepTemplate.xml file
            string projectFilePath = Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName;
            ResetAllTabs ();
            MainWindow mainWindow = new MainWindow ( projectFilePath + @"\Resources\Templates\DefaultcSepTemplate.xml", templateXmlNodes );
            mainWindow.Show ();
            this.Close ();
        }

        private void UndoCommandBinding( object sender, ExecutedRoutedEventArgs e )
        {
            if ( undoableCommand == UndoableType.none )
            {
                MessageBox.Show ( "Currently no command to undo.\nUndoable commands include creation and deletion of elements & attributes, as well as deletion of tabs & Unit Operations.", "Error" );
                return;
            }

            UndoableType undoType = undoableCommand;
            undoableCommand = UndoableType.none;
            string message = "";
            switch ( undoType )
            {
                case UndoableType.delUOP:
                    switch ( editorMode )
                    {
                        case Mode.general:
                            message = "Undo previous xml node deletion?";
                            break;
                        case Mode.cSep:
                            message = "Undo previous UOP deletion?";
                            break;
                    }
                    break;

                case UndoableType.delTab:
                    message = "Undo previous tab deletion?";
                    break;

                case UndoableType.delAttrib:
                    message = "Undo previous attribute deletion?";
                    break;

                case UndoableType.delSubAttrib:
                    message = "Undo previous sub-attribute deletion?";
                    break;

                case UndoableType.delElem:
                    message = "Undo previous element deletion?";
                    break;

                case UndoableType.createAttrib:
                    message = "Undo previous attribute creation?";
                    break;

                case UndoableType.createElem:
                    message = "Undo previous element creation?";
                    break;
                case UndoableType.delElemHeader:
                    message = "Undo previous element header deletion?";
                    break;
            }

            string header = "Undo";
            MessageBoxButton msgBoxButtons = MessageBoxButton.YesNo;
            MessageBoxResult msgBoxResult = MessageBox.Show ( message, header, msgBoxButtons );
            if ( msgBoxResult == MessageBoxResult.Yes )
            {
                // Continue with undo
            }
            else if ( msgBoxResult == MessageBoxResult.No )
            {
                // Cancel undo action
                undoableCommand = undoType;
                return;
            }

            switch ( undoType )
            {
                case UndoableType.delUOP:
                    UOPXmlNode deletedUOP = deletedUOPs.Pop ();
                    XmlNode deletedXmlNode = deletedUOP.XmlNode;
                    int pathID = deletedUOP.PathID;
                    pathIDHistories.Add ( pathID, deletedXmlNode );
                    ComboBoxItem pathIDComboBoxItem = new ComboBoxItem () { Content = pathID };
                    AddNewPathID ( pathIDComboBoxItem );
                    PathIDComboBox.SelectedItem = pathIDComboBoxItem;
                    break;

                case UndoableType.delTab:
                    TabItem deletedTab = deletedTabs.Pop ();
                    deletedTab.Visibility = Visibility.Visible;
                    MainTabControl.SelectedIndex = MainTabControl.Items.IndexOf ( deletedTab );
                    // Make the tab link button appear
                    Button tabLinkButton = null;
                    foreach ( Button curButton in tabLinkButtons )
                    {
                        if ( (string) deletedTab.Header == (string) curButton.Content )
                        {
                            tabLinkButton = curButton;
                            tabLinkButton.Visibility = Visibility.Visible;
                        }
                    }
                    break;

                case UndoableType.delAttrib:
                    XmlAttribute deletedXmlAttribute = deletedXmlAttributes.Pop ();
                    string attribValue = deletedXmlAttribute.Value;
                    if (attribValue == ""){ attribValue = "EMPTY"; }
                    AddNewAttribute ( deletedXmlAttribute.Name, attribValue, undoable: false );
                    break;

                case UndoableType.delSubAttrib:
                    SubAttribute subAttribute = deletedXmlSubAttributes.Pop ();
                    XmlAttribute deletedXmlSubAttribute = subAttribute.XmlAttribute;
                    string subAttribValue = deletedXmlSubAttribute.Value;
                    if ( subAttribValue == "" )
                    {
                        subAttribValue = "EMPTY";
                    }
                    AddNewAttribute ( deletedXmlSubAttribute.Name, subAttribValue, undoable: false, elementName: subAttribute.ElementName );
                    break;

                case UndoableType.delElem:
                    XmlElement deletedXmlElement = deletedXmlElements.Pop ();
                    string innerText = deletedXmlElement.InnerText;
                    if( innerText == ""){ innerText = "EMPTY"; }
                    if ( deletedXmlElement.ParentNode != null )
                    {
                        AddNewElement ( deletedXmlElement.Name, innerText, undoable: false, isSubElem: true, 
                            parentNodeName: deletedXmlElement.ParentNode.Name );
                    }
                    else
                    {
                        AddNewElement ( deletedXmlElement.Name, innerText, undoable: false, isSubElem: false );
                    }

                    foreach ( XmlAttribute attribute in deletedXmlElement.Attributes )
                    {
                        AddNewAttribute ( attribute.Name, attribute.Value, undoable: false, elementName: deletedXmlElement.Name );
                    }
                    break;

                case UndoableType.delElemHeader:
                    string elemHeader = deletedElementHeaders.Pop ();
                    AddElementHeader ( elemHeader );
                    break;
                   
                case UndoableType.createAttrib:
                    Grid attributeGrid = createdXmlAttributes.Pop ();
                    ( attributeGrid.Parent as ListView ).Items.Remove ( attributeGrid );
                    break;

                case UndoableType.createElem:
                    Grid elementGrid = createdXmlElements.Pop ();
                    ( elementGrid.Parent as ListView ).Items.Remove ( elementGrid );
                    break;
            }
        }

        private void AddElementHeader( string elemHeader )
        {
            ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
            newGrid.AddTextBlock ( elemHeader, "", header: true );
            ListView listView = ( MainTabControl.SelectedItem as TabItem ).Content as ListView;
            listView.Items.Add ( newGrid );
        }

        public List<TemplateXmlNode> GetAvailableTemplates()
        {
            return templateXmlNodes;
        }

        public List<TabItem> GetTabItems()
        {
            return tabItems;
        }

        private void Modify_Template_Click( object sender, RoutedEventArgs e )
        {
            // Will need to be able to parse -- check implementation for new file creation
            // Should be able to choose which template to edit, not just the default one
            TemplateListWindow templatesWindow = new TemplateListWindow ( this, "Modify" );
            templatesWindow.Owner = this;
            templatesWindow.Show ();
            this.IsEnabled = false;
        }

        public void NewTemplateEntered( TemplateXmlNode newTemplate )
        {
            this.IsEnabled = true;
            templateXmlNodes.Add ( newTemplate );
        }

        private void SaveTemplate_Click( object sender, RoutedEventArgs e )
        {
            if ( editorMode == Mode.cSep && MainTabControl.Items.Count == 1 )
            {
                MessageBox.Show ( "Not able to save a template with no elements.", "Error" );
                return;
            }
            XmlDocSave helperDocSave = new XmlDocSave ( new XmlDocument (), "", this );
            XmlNode savedActiveTabsState = helperDocSave.WriteCurrentOpenTabs ( tabItems, currentPathID );
            XmlDocSave.NullifyEmptyNodes ( savedActiveTabsState );
            XmlDocument xmlDoc = new XmlDocument ();
            string xmlString = "<" + activeMainNodeName + ">" + savedActiveTabsState.FirstChild.LastChild.InnerXml + "</" + activeMainNodeName + ">";
            xmlDoc.LoadXml ( xmlString );
            List<String> curTabHeaders = new List<string> ();
            curTabHeaders.Add ( activeMainNodeName );
            foreach ( TabItem tabItem in MainTabControl.Items.OfType<TabItem> () )
            {
                if ( tabItem.Visibility == Visibility.Visible && ( tabItem.Header as string ) != activeMainNodeName ) 
                {
                    curTabHeaders.Add ( tabItem.Header as string ); 
                }
            }
            TemplateXmlNode newTemplateXmlNode = new TemplateXmlNode ( xmlDoc.DocumentElement, "", curTabHeaders, activeMainNodeName);
            NewTemplateName inputTemplateNameWindow = new NewTemplateName ( this, mainEditorWindow, newTemplateXmlNode );
            inputTemplateNameWindow.Owner = this;
            inputTemplateNameWindow.Show ();
            this.IsEnabled = false;
        }

        private void AddTab_Click( object sender, RoutedEventArgs e )
        {
            if ( !isTemplateWindow && PathIDComboBox.SelectedIndex == -1 )
            {
                MessageBox.Show ( "Cannot add a tab when no xml is selected.", "Error" );
                return;
            }

            // Open a new window to give the user the options for the following:
            // New tab's name 
            // Whether or not there should be a tab link button? NOT SURE, the editor might not even save tabs that don't have tab link buttons iirc
            // TODO: above

            NewTabName newTabNameWindow = new NewTabName ( this );
            newTabNameWindow.Owner = this;
            newTabNameWindow.Show ();
            this.IsEnabled = false;
        }

        public void NewTabEntered( string tabName )
        {
            if ( isTemplateWindow )
            {
                templateTabsMenuItem.FontWeight = FontWeights.Normal;
            }

            TabItem newTabItem1 = new TabItem ();
            newTabItem1.Header = tabName;
            newTabItem1.FontSize = FONT_SIZE;
            MainTabControl.Items.Add ( newTabItem1 );
            tabItems.Add ( newTabItem1 );

            TabItem newTabItem2 = new TabItem ();
            newTabItem2.Visibility = Visibility.Collapsed;
            newTabItem2.Header = tabName;
            newTabItem2.FontSize = FONT_SIZE;
            if ( mainEditorWindow != null )
            {
                mainEditorWindow.tabItems.Add ( newTabItem2 );
                mainEditorWindow.MainTabControl.Items.Add ( newTabItem2 );
            }

            // create new tabLinkButton
            // add to tabLinkButtons
            // create new grid and add to grid
            // add grid to the end of the listView in the main tab 

            // Set up button grid
            ItemGrid newGrid = new ItemGrid ( this, GRID_WIDTH );
            newGrid.AddTabButton ( tabName );

            TabItem mainTab = GetTabItemWithHeader ( activeMainNodeName );
            ListView mainTabListView = ( (ListView) mainTab.Content );
            mainTabListView.Items.Add ( newGrid );

            string strNodeName = new String ( tabName.Where ( c => !Char.IsWhiteSpace ( c ) ).ToArray () );
            RecursiveParseTabInfo ( newTabItem1, new XmlDocument ().CreateNode ( XmlNodeType.Element, strNodeName, "" ) );
            RecursiveParseTabInfo ( newTabItem2, new XmlDocument ().CreateNode ( XmlNodeType.Element, strNodeName, "" ) );
            MainTabControl.SelectedItem = newTabItem1;
        }

        private void MainWindow_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            // Adjust StemCell logo
            if ( this.ActualWidth <= 747 )
            {
                StemCellLogoBorder.HorizontalAlignment = HorizontalAlignment.Left;
                StemCellLogoBorder.Margin = new Thickness ( 252, 4, 0, 0 );
            }
            else
            {
                StemCellLogoBorder.HorizontalAlignment = HorizontalAlignment.Center;
                StemCellLogoBorder.Margin = new Thickness ( 252, 4, 213, 0 );
            }

            // Change grid sizes
            foreach ( TabItem tabItem in MainTabControl.Items )
            {
                foreach ( ItemGrid itemGrid in (tabItem.Content as ListView).Items.OfType<ItemGrid>() )
                {
                    itemGrid.Width = this.ActualWidth - 33;
                    if ( this.ActualWidth > ( GRID_WIDTH / 2 ) + 100 )
                    {
                        itemGrid.ColumnDefinitions[0].Width = new GridLength ( GRID_WIDTH / 2 );
                    }
                    else
                    {
                        itemGrid.ColumnDefinitions[0].Width = new GridLength ();
                    }
                }
            }
        }

        private void ActiveUOPNameTextBox_KeyUp( object sender, KeyEventArgs e )
        {
            string newUOPName = ActiveUOPNameTextBox.Text;
            UOPXmlNode curUOPXmlNode = GetUOPXmlNodeWithPathID ( currentPathID );
            curUOPXmlNode.MainNodeName = newUOPName;
            XmlNode newXmlNode = ChangeXMLNodeRootName ( curUOPXmlNode.XmlNode, newUOPName );
            if ( newXmlNode == null )
            {
                ActiveUOPNameTextBox.Text = activeMainNodeName;
                return;
            }
            else
            {
                curUOPXmlNode.XmlNode = newXmlNode;
            }

            if ( MainTabControlContainsHeader ( newUOPName ) )
            {
                return;
            }
            if ( PathIDComboBox.SelectedIndex != -1 )
            {
                TabItem mainTabItem = GetTabItemWithHeader ( activeMainNodeName );
                if ( mainTabItem != null )
                {
                    if ( MainTabControlContainsHeader ( newUOPName ) )
                    {
                        MainTabControl.Items.Remove ( mainTabItem );
                        mainTabItem = GetTabItemWithHeader ( newUOPName );
                    }
                    else
                    {
                        mainTabItem.Header = newUOPName;
                    }
                    DeleteDuplicateTabs ();
                    mainTabItem = GetTabItemWithHeader ( newUOPName );
                }

                tabHeaders.Remove ( activeMainNodeName );
                if ( !tabHeaders.Contains ( newUOPName ) )
                {
                    tabHeaders.Add ( newUOPName );
                }
                this.activeMainNodeName = newUOPName;
            }
        }

        private void DeleteDuplicateTabs()
        {
            List<TabItem> toRemove = new List<TabItem> ();
            TabItem[] tempArray = new TabItem[tabItems.Count];
            tabItems.CopyTo ( tempArray );
            for ( int i = 0; i < tabItems.Count; i++ )
            {
                for ( int j = i + 1; j < tabItems.Count; j++ )
                {
                    if ( (string)tempArray[i].Header == (string)tempArray[j].Header )
                    {
                        if ( !toRemove.Contains ( tempArray[j] ) )
                        {
                            toRemove.Add ( tempArray[j] );
                        }
                    }
                }
            }
            foreach ( TabItem tabItem in toRemove )
            {
                tabItems.Remove ( tabItem );
                MainTabControl.Items.Remove ( tabItem );
            }
        }

        private XmlNode ChangeXMLNodeRootName( XmlNode xmlNode, string newName )
        {
            try
            {
                string innerXml = xmlNode.InnerXml;
                XmlDocument helperXmlDoc = new XmlDocument ();
                XmlElement newXmlElement = helperXmlDoc.CreateElement ( newName );
                newXmlElement.InnerXml = innerXml;
                return newXmlElement;
            }
            catch (Exception e )
            {
                return null;
            }
            
        }

        private void ActiveUOPNameTextBox_PreviewKeyUp( object sender, KeyEventArgs e)
        {
            //if ( !Char.IsLetter ( (char) e.Key ) )
            //{
            //    e.Handled = true;
            //}

            if ( e.Key == Key.Space )
            {
                e.Handled = true;
            }

            if ( e.Key == Key.Back && ActiveUOPNameTextBox.Text.Length == 1 )
            {
                e.Handled = true;
            }
        }

        private void ActiveUOPNameTextBox_PreviewKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Space )
            {
                e.Handled = true;
            }

            if ( e.Key == Key.Back && ActiveUOPNameTextBox.Text.Length == 1 )
            {
                e.Handled = true;
            }
        }

        private void Settings_Button_Click( object sender, RoutedEventArgs e )
        {
            // TODO: implement settings menu
            EditSettingsWindow editSettingsWindow = new EditSettingsWindow ( this );
            this.IsEnabled = false;
            editSettingsWindow.Owner = this;
            editSettingsWindow.Show ();
        }
    }
}
