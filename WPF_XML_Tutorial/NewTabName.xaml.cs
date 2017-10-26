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
    /// Interaction logic for NewTabName.xaml
    /// </summary>
    public partial class NewTabName : Window
    {
        MainWindow mainWindowCaller;

        public NewTabName(MainWindow caller)
        {
            InitializeComponent ();
            mainWindowCaller = caller;
            this.Topmost = true;
        }

        private void Drag_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_NewTabNameWindow_Click( object sender, MouseButtonEventArgs e )
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
            string tabName = NewTabNameTextBox.Text;
            if ( tabName == null || tabName.Trim() == "" )
            {
                return;
            }
            if ( TabNameAlreadyExists ( tabName.Trim () ) )
            {
                MessageBox.Show ( "Chosen tab name is already active in the editor.\nPlease choose a new name and try again.", "Tab name already exists" );
                return;
            }

            // Need to call a newTab function in mainwindow with the tab name
            mainWindowCaller.NewTabEntered ( tabName.Trim() );
            mainWindowCaller.IsEnabled = true;
            this.Close ();
        }

        private bool TabNameAlreadyExists( string name )
        {
            foreach ( TabItem tabItem in mainWindowCaller.GetTabItems () )
            {
                if ( (tabItem.Header as String).Trim () == name )
                {
                    return true;
                }
            }
            return false;
        }
    }
}
