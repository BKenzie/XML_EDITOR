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

namespace WPF_XML_Tutorial
{
    /// <summary>
    /// Interaction logic for NewTemplateName.xaml
    /// </summary>
    public partial class NewTemplateName : Window
    {
        MainWindow mainWindowCaller;
        MainWindow mainEditorWindow;
        TemplateXmlNode newTemplate;

        public NewTemplateName( MainWindow caller, MainWindow mainWindow, TemplateXmlNode template )
        {
            InitializeComponent ();
            mainWindowCaller = caller;
            this.Topmost = true;
            this.mainEditorWindow = mainWindow;
            this.newTemplate = new TemplateXmlNode ( template.XmlNode, template.Name );
        }

        private void Drag_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_NewTemplateNameWindow_Click( object sender, MouseButtonEventArgs e )
        {
            this.Close ();
            mainWindowCaller.IsEnabled = true;
        }

        private void TextBox_KeyUp( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Return )
            {
                EnterButton.RaiseEvent ( new RoutedEventArgs ( System.Windows.Controls.Primitives.ButtonBase.ClickEvent ) );
            }
        }

        private void EnterButton_Click( object sender, RoutedEventArgs e )
        {
            // Going to need to return to the MainWindow 
            // Need to add the new ActionPath template to the list of templates 
            // TODO: implement that list of templates and an option for the user to choose which template they want to use

            mainWindowCaller.Close ();
            newTemplate.Name = NewTemplateNameTextBox.Text;
            mainEditorWindow.NewTemplateEntered ( newTemplate );
            this.Close ();

        }
    }
}
