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
        public ModifyTemplateWindow()
        {
            InitializeComponent ();
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
            // Should the user have access to a list of templates they can choose from?
            // Need to still have the default ActionPath template, not overwritten by this method
            throw new NotImplementedException ();

            NewTemplateName inputNameWindow = new NewTemplateName ();
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
