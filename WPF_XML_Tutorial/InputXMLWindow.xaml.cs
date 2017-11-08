using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.IO;
using System.Reflection;

namespace WPF_XML_Tutorial
{
    /// <summary>
    /// Interaction logic for InputXMLWindow.xaml
    /// </summary>
    public partial class InputXMLWindow : Window
    {
        public InputXMLWindow()
        {
            InitializeComponent ();

            #region Button Code

            NewXML_Button.Background = new SolidColorBrush ( Colors.LightGray );
            NewXML_Button.BorderBrush = new SolidColorBrush ( Colors.Transparent );
            ChooseXMLButton.Background = new SolidColorBrush ( Colors.LightGray );
            ChooseXMLButton.BorderBrush = new SolidColorBrush ( Colors.Transparent );

            Style customButtonStyle = new Style ();
            customButtonStyle.TargetType = typeof ( Button );
            MultiDataTrigger trigger = new MultiDataTrigger ();
            Condition condition = new Condition ();
            condition.Binding = new Binding () { Path = new PropertyPath ( "IsMouseOver" ), RelativeSource = RelativeSource.Self };
            condition.Value = true;
            Setter foregroundSetter = new Setter ();
            foregroundSetter.Property = Button.ForegroundProperty;
            foregroundSetter.Value = Brushes.DarkOrange;
            Setter cursorSetter = new Setter ();
            cursorSetter.Property = Button.CursorProperty;
            cursorSetter.Value = Cursors.Hand;
            Setter textSetter = new Setter ();
            textSetter.Property = Button.FontWeightProperty;
            textSetter.Value = FontWeights.ExtraBold;
            trigger.Conditions.Add ( condition );
            trigger.Setters.Add ( foregroundSetter );
            trigger.Setters.Add ( cursorSetter );
            trigger.Setters.Add ( textSetter );

            customButtonStyle.Triggers.Clear ();
            customButtonStyle.Triggers.Add ( trigger );
            NewXML_Button.Style = customButtonStyle;
            ChooseXMLButton.Style = customButtonStyle;
            #endregion
        }

        private void Close_Button_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            this.Close ();
        }

        private void Drag_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void XMLButton_Click( object sender, RoutedEventArgs e )
        {
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

                MainWindow mainWindow = new MainWindow (filePath);
                mainWindow.Show ();
                this.Close ();
            }
        }

        public void NewXML_Click( object sender, RoutedEventArgs e )
        {
            // Accesses the DefaultcSepTemplate.xml file
            // There is likely a more direct way to do this
            string projectFilePath = Directory.GetParent ( Directory.GetCurrentDirectory () ).Parent.FullName;
            MainWindow mainWindow = new MainWindow ( projectFilePath + @"\Resources\Templates\DefaultcSepTemplate.xml" );
            mainWindow.Show ();
            mainWindow.HighlightPathIDSection ();
            this.Close ();
        }

    }
}
