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
    /// Interaction logic for ModifyTemplateWindow.xaml
    /// </summary>
    public partial class ModifyTemplateWindow : Window
    {
        TemplateXmlNode activeTemplate;
        MainWindow mainWindow;

        public ModifyTemplateWindow( TemplateXmlNode template, MainWindow mainWindow )
        {
            InitializeComponent ();
            activeTemplate = template;
            this.mainWindow = mainWindow;

        }

        private void Delete_Tab_Button_Click( object sender, RoutedEventArgs e )
        {
            // Will be similar to MainWindow's equivalent
            throw new NotImplementedException ();
        }

        private void New_Tab_Button_Click( object sender, RoutedEventArgs e )
        {
            // Will need to create a new tab link button in the main ActionPath tab
            throw new NotImplementedException ();
        }

        private void SaveCommandBinding( object sender, ExecutedRoutedEventArgs e )
        {
            // Need to figure out implementation details 
            
            NewTemplateName inputNameWindow = new NewTemplateName ( this, mainWindow, activeTemplate ); // TODO: need to register changes for activeTemplate
            this.IsEnabled = false;
            inputNameWindow.Show ();
        }

        private void DragRectangle_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            throw new NotImplementedException ();
        }
    }
}
