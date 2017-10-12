﻿using System;
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
    /// Interaction logic for NewElemOrAttrib.xaml
    /// </summary>
    public partial class NewElemOrAttrib : Window
    {
        private MainWindow mainWindowCaller;
        public enum Type { Attribute, Element };
        private Type type;

        public NewElemOrAttrib( MainWindow caller, string strType )
        {
            InitializeComponent ();
            mainWindowCaller = caller;
            this.Focus ();
            this.Topmost = true;
            if ( strType == "attribute" )
            {
                this.type = Type.Attribute;
            }
            else if ( strType == "element" )
            {
                this.type = Type.Element;
            }
        }

        private void Drag_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_InputPathIDWindow_Click( object sender, MouseButtonEventArgs e )
        {
            mainWindowCaller.IsEnabled = true;
            MainWindow.pathIDComboBox.SelectedIndex = -1;
            this.Close ();
        }

        private void EnterButton_Click( object sender, RoutedEventArgs e )
        {
            string name = NewNameTextBox.Text;
            string value = NewValueTextBox.Text;

            if ( name.Contains ( " " ) )
            {
                MessageBox.Show ( "Name cannot contain any whitespaces.", "Error" );
            }
            else if ( name == "" )
            {
                MessageBox.Show ( "Name parameter must not be empty.", "Error" );
            }
            else if ( name.Any(char.IsDigit) )
            {
                MessageBox.Show ( "Name parameter must not contain any numbers.", "Error" );
            }
            else 
            {
                if ( this.type == Type.Attribute )
                {
                    mainWindowCaller.AddNewAttribute ( name, value );
                    mainWindowCaller.IsEnabled = true;
                    this.Close ();
                }
                else if ( this.type == Type.Element )
                {
                    mainWindowCaller.AddNewElement ( name, value );
                    mainWindowCaller.IsEnabled = true;
                    this.Close ();
                }
            }
        }

        private void TextBox_KeyUp( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Return )
            {
                EnterButton.RaiseEvent ( new RoutedEventArgs ( System.Windows.Controls.Primitives.ButtonBase.ClickEvent ) );
            }
        }
    }
}