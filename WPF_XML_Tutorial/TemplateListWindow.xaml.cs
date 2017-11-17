using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Xml;

namespace WPF_XML_Tutorial
{
    /// <summary>
    /// Interaction logic for TemplateListWindow.xaml
    /// </summary>
    public partial class TemplateListWindow : Window
    {
        private MainWindow mainWindowCaller;
        private int pathID;
        private enum Mode { Modify, Select };
        private Mode currentMode;

        public TemplateListWindow( MainWindow caller, string strMode, int pathID = -1 )
        {
            InitializeComponent ();
            this.Topmost = true;
            this.pathID = pathID;
            mainWindowCaller = caller;
            TemplateSelectButton.Content = strMode;
            if ( strMode.ToLower () == "modify" )
            {
                currentMode = Mode.Modify;
            }
            else if ( strMode.ToLower () == "select" )
            {
                currentMode = Mode.Select;
            }

            if ( currentMode == Mode.Modify )
            {
                ListBoxItem blankTemplateListBoxItem = new ListBoxItem ();
                blankTemplateListBoxItem.Content = "Create a new template";
                blankTemplateListBoxItem.FontSize = 20;
                blankTemplateListBoxItem.FontWeight = FontWeights.Bold;
                //blankTemplateListBoxItem.Foreground = new SolidColorBrush ( Colors.DarkGray );
                blankTemplateListBoxItem.MouseDoubleClick += new MouseButtonEventHandler ( mouseDoubleClick );
                TemplatesListBox.Items.Add ( blankTemplateListBoxItem );
                TemplatesListBox.Items.Add ( new Separator () );
            }

            foreach ( TemplateXmlNode template in mainWindowCaller.GetAvailableTemplates () )
            {
                ListBoxItem listBoxItem = new ListBoxItem ();
                listBoxItem.Content = template.Name;
                listBoxItem.FontSize = 20;
                listBoxItem.MouseDoubleClick += new MouseButtonEventHandler ( mouseDoubleClick );
                TemplatesListBox.Items.Add ( listBoxItem );
                TemplatesListBox.Items.Add ( new Separator () );
            }

        }

        private void mouseDoubleClick( object sender, MouseButtonEventArgs e )
        {
            TemplateSelectButton.RaiseEvent ( new RoutedEventArgs ( System.Windows.Controls.Primitives.ButtonBase.ClickEvent ) );
        }

        private void DragRectangle_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            mainWindowCaller.IsEnabled = true;
            mainWindowCaller.PathIDComboBox.SelectedIndex = -1;
            mainWindowCaller.currentPathID = -1;
            mainWindowCaller.FinalLogic ();
            this.Close ();
        }

        private void TemplateSelectButton_Click( object sender, RoutedEventArgs e )
        {
            if ( TemplatesListBox.SelectedIndex == -1 )
            {
                MessageBox.Show ( "Please select a template and try again.", "No template selected" );
                return;
            }

            if ( currentMode == Mode.Modify )
            {
                ListBoxItem selectedItem = TemplatesListBox.SelectedItem as ListBoxItem;
                string name = selectedItem.Content as string;
                TemplateXmlNode templateXmlNode = GetTemplateXmlNodeWithName ( name );

                if ( name.ToLower () == "create a new template" )
                {
                    BlankTemplateUOPName mainNodeNameWindow = new BlankTemplateUOPName ( this, mainWindowCaller );
                    mainNodeNameWindow.Show ();
                    this.Close ();
                }
                else
                {
                    MainWindow modifyTemplateWindow = new MainWindow ( "", mainWindowCaller.GetAvailableTemplates (),
                                 isTemplateWindow: true, templateXmlNodeParam: templateXmlNode, caller: mainWindowCaller );
                    modifyTemplateWindow.Show ();
                    this.Close ();
                }
            }
            else if ( currentMode == Mode.Select )
            {
                ListBoxItem selectedItem = TemplatesListBox.SelectedItem as ListBoxItem;
                string name = selectedItem.Content as string;
                TemplateXmlNode templateXmlNode = GetTemplateXmlNodeWithName ( name );
                mainWindowCaller.UserSelectedTemplate ( templateXmlNode, pathID );
                mainWindowCaller.IsEnabled = true;
                this.Close ();
            }
        }

        private TemplateXmlNode GetTemplateXmlNodeWithName( string name )
        {
            foreach ( TemplateXmlNode template in mainWindowCaller.GetAvailableTemplates () )
            {
                if ( template.Name == name )
                {
                    return template;
                }
            }
            return null;
        }

        public void UserEnteredNewMainNodeName( string mainNodeName )
        {
            List<string> blankTabHeaders = new List<string> ();
            blankTabHeaders.Add ( mainNodeName );
            XmlDocument helperXmlDoc = new XmlDocument ();
            XmlNode blankXmlNode = helperXmlDoc.CreateNode ( "element", mainNodeName, "" );
            TemplateXmlNode blankTemplateXmlNode = new TemplateXmlNode ( blankXmlNode, "Blank Template", blankTabHeaders, mainNodeName );
            MainWindow modifyBlankTemplateWindow = new MainWindow ( "", mainWindowCaller.GetAvailableTemplates (),
                        isTemplateWindow: true, templateXmlNodeParam: blankTemplateXmlNode, caller: mainWindowCaller );
            modifyBlankTemplateWindow.Show ();
            this.Close ();
        }
    }
}
