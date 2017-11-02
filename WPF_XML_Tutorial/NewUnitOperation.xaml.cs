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
    /// Interaction logic for NewUnitOperation.xaml
    /// </summary>
    public partial class NewUnitOperation : Window
    {
        private MainWindow mainWindowCaller;

        public NewUnitOperation( MainWindow caller)
        {
            InitializeComponent ();
            mainWindowCaller = caller;
            this.Focus ();
            this.Topmost = true;
            PathIDTextBox.Focus ();
        }

        private void Drag_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_InputPathIDWindow_Click( object sender, MouseButtonEventArgs e )
        {
            mainWindowCaller.IsEnabled = true;
            mainWindowCaller.PathIDComboBox.SelectedIndex = -1;
            mainWindowCaller.currentPathID = -1;
            mainWindowCaller.FinalLogic ();
            mainWindowCaller.HighlightPathIDSection ();
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
                    if ( userInputPathID >= 0 )
                    {
                        mainWindowCaller.NewPathIDEntered ( userInputPathID );
                        this.Close ();
                    }
                    else
                    {
                        MessageBox.Show ( "New PathID must not be a negative number.", "PathID value error" );
                    }
                }
                else
                {
                    MessageBox.Show ( "Chosen PathID already exists and is active in the editor.", "PathID already exists" );
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show ( "Input a valid PathID\n" + ex.Message, "Error" );
            }
        }

        private List<int> GetPathIDs()
        {
            List<int> pathIDs = new List<int> ();
            ComboBox pathIDCombobox = mainWindowCaller.PathIDComboBox;
            foreach ( ComboBoxItem item in pathIDCombobox.Items )
            {
                if ( Convert.ToString(item.Content) != "New UnitOperation" )
                {
                    pathIDs.Add ( Convert.ToInt32 ( Convert.ToString ( item.Content ) ) );
                }
            }
            return pathIDs;
        }

        private void PathIDTextBox_KeyUp( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Return )
            {
                EnterPathIDButton.RaiseEvent ( new RoutedEventArgs ( System.Windows.Controls.Primitives.ButtonBase.ClickEvent ) );
            }
        }
    }
}
