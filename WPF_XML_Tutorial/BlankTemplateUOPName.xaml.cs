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
    /// Interaction logic for BlankTemplateUOPName.xaml
    /// </summary>
    public partial class BlankTemplateUOPName : Window
    {
        private TemplateListWindow templateListWindowCaller;
        private MainWindow mainWindowCaller;

        public BlankTemplateUOPName( TemplateListWindow caller, MainWindow mainWindowCaller )
        {
            InitializeComponent ();
            templateListWindowCaller = caller;
            this.mainWindowCaller = mainWindowCaller;
        }

        private void Drag_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_BlankTemplateUOPNameWindow_Click( object sender, MouseButtonEventArgs e )
        {
            mainWindowCaller.IsEnabled = true;
            this.Close ();
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
            string newMainNodeName = NewMainNodeNameTextBox.Text.Trim ();
            if ( !newMainNodeName.All ( char.IsLetter ) )
            {
                MessageBox.Show ( "New xml node only accepts letters in its name.\nPlease try again.", "Error" );
                return;
            }
            else
            {
                templateListWindowCaller.UserEnteredNewMainNodeName ( newMainNodeName );
                this.Close ();
            }
        }
    }
}
