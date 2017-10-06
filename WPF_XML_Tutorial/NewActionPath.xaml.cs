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
    /// Interaction logic for NewActionPath.xaml
    /// </summary>
    public partial class NewActionPath : Window
    {
        MainWindow mainWindowCaller;

        public NewActionPath( MainWindow caller)
        {
            InitializeComponent ();
            mainWindowCaller = caller;
            this.Focus ();
            PathIDTextBox.Focus ();
        }

        private void Drag_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_InputPathIDWindow_Click( object sender, MouseButtonEventArgs e )
        {
            this.Close ();
        }

        private void EnterPathIDButton_Click( object sender, RoutedEventArgs e )
        {
            try
            {
                int userInputPathID = Convert.ToInt32 ( PathIDTextBox.Text );
                List<int> curPathIDs = GetPathIDs ();
                if ( !curPathIDs.Contains ( userInputPathID ) )
                {
                    mainWindowCaller.NewPathIDEntered ( userInputPathID );
                    this.Close ();
                }
                else
                {
                    MessageBox.Show ( "Chosen PathID already has an existing AcionPath active in the editor.", "PathID already exists" );
                }


            }
            finally { }
            //catch(Exception ex)
            //{
            //    MessageBox.Show ( "Input a valid PathID\n" + ex.Message, "Error" );
            //}

            
        }

        private List<int> GetPathIDs()
        {
            List<int> pathIDs = new List<int> ();
            ComboBox pathIDCombobox = MainWindow.pathIDComboBox;
            foreach ( ComboBoxItem item in pathIDCombobox.Items )
            {
                if ( Convert.ToString(item.Content) != "New ActionPath" )
                {
                    pathIDs.Add ( Convert.ToInt32 ( Convert.ToString ( item.Content ) ) );
                }
            }
            return pathIDs;
        }
    }
}
