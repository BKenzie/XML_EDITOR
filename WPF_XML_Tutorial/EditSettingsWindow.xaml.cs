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
using System.Windows.Shapes;

namespace WPF_XML_Tutorial
{
    /// <summary>
    /// Interaction logic for EditSettingsWindow.xaml
    /// </summary>
    public partial class EditSettingsWindow : Window
    {
        MainWindow mainWindowCaller;
        INIFile iniFile = null;

        public EditSettingsWindow( MainWindow mainWindow )
        {
            InitializeComponent ();
            iniFile = new INIFile ( MainWindow.INI_FILEPATH );
            mainWindowCaller = mainWindow;
            this.Topmost = true;
            ReadCurrentFontSize ();
            ReadCurrentEditorMode ();
            ReadCurrentPathIDMode ();

        }

        private void ReadCurrentPathIDMode()
        {
            // Read the active autoPathID mode from the ini and fill checkbox selection
            string autoPathID = iniFile.Read ( "autoPathID", "user_settings" );
            if ( autoPathID == "true" )
            {
                AutoGenPathIDCheckBox.IsChecked = true;
            }
            else if ( autoPathID == "false" )
            {
                AutoGenPathIDCheckBox.IsChecked = false;
            }
        }

        private void ReadCurrentEditorMode()
        {
            // Read the active editor mode from the ini and fill EditorModeComboBox selection
            string currentMode = iniFile.Read ( "mode", "user_settings" );
            switch ( currentMode.ToLower() )
            {
                case "general":
                    EditorModeComboBox.SelectedIndex = 0;
                    break;

                case "csep":
                    EditorModeComboBox.SelectedIndex = 1;
                    break;
            }
        }

        private void ReadCurrentFontSize()
        {
            FontSizeSettingTextBox.Text = iniFile.Read ( "fontSize", "user_settings" );
        }

        private void ApplyButton_Click( object sender, RoutedEventArgs e )
        {
            MessageBoxResult result = MessageBox.Show ( "Applying new settings will refresh the main editor window.\nUnsaved work will be lost. Proceed?", 
                "Warning", MessageBoxButton.YesNo );
            if ( result == MessageBoxResult.Yes )
            {
                // Continue with applying new settings
            }
            else if ( result == MessageBoxResult.No )
            {
                return;
            }

            // Write the settings to the ini file and then create a new MainWindow instance
            
            // Editor mode setting
            string selectedMode = ( ( EditorModeComboBox.SelectedItem as ComboBoxItem ).Content as string );
            iniFile.Write ( "mode", selectedMode, "user_settings" );
            // Font size setting
            if ( FontSizeSettingTextBox.Text != null || (string) FontSizeSettingTextBox.Text != "" )
            {
                int fontSize = Convert.ToInt32 ( (string) FontSizeSettingTextBox.Text );
                iniFile.Write ( "fontSize", fontSize.ToString (), "user_settings" );
            }
            // Auto generate PathID setting
            bool? isChecked = AutoGenPathIDCheckBox.IsChecked;
            string isCheckedStr = isChecked.ToString ().ToLower ();
            iniFile.Write ( "autoPathID", isCheckedStr, "user_settings" );

            // Close previous main editor window and open one with new settings
            string projectFilePath = Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName;
            MainWindow newMainWindow = new MainWindow ( projectFilePath + @"\Resources\Templates\DefaultcSepTemplate.xml", mainWindowCaller.GetAvailableTemplates () );
            newMainWindow.Show ();
            newMainWindow.HighlightPathIDSection ();
            mainWindowCaller.Close ();
            this.Close ();
        }
        private void Canvas_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            this.DragMove ();
        }

        private void Close_Button_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            mainWindowCaller.IsEnabled = true;
            this.Close ();
        }

        private void FontSizeSettingTextBox_PreviewKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Back )
            {
                return;
            }
            if ( FontSizeSettingTextBox.Text.Length >= 2 )
            {
                e.Handled = true;
                return;
            }

            try
            {
                char c = e.Key.ToString ()[1];
                if ( !Char.IsDigit ( c ) )
                {
                    e.Handled = true;
                }
            }
            catch(Exception ex)
            {
                e.Handled = true;
            }
        }
    }
}
