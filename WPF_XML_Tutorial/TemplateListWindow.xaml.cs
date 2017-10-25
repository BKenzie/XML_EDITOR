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
    /// Interaction logic for TemplateListWindow.xaml
    /// </summary>
    public partial class TemplateListWindow : Window
    {
        private MainWindow mainWindowCaller;
        private int pathID;
        private enum Mode{ Modify, Select };
        private Mode currentMode;

        public TemplateListWindow( MainWindow caller, string strMode, int pathID = -1 )
        {
            InitializeComponent ();
            this.Topmost = true;
            this.pathID = pathID;
            mainWindowCaller = caller;
            TemplateSelectButton.Content = strMode;
            if ( strMode.ToLower () == "modify" )
            {
                currentMode = Mode.Modify;
            }
            else if ( strMode.ToLower () == "select" )
            {
                currentMode = Mode.Select;
            }

            foreach ( TemplateXmlNode template in mainWindowCaller.GetAvailableTemplates () )
            {
                ListBoxItem listBoxItem = new ListBoxItem ();
                listBoxItem.Content = template.Name;
                listBoxItem.FontSize = 20;
                TemplatesListBox.Items.Add ( listBoxItem );
                TemplatesListBox.Items.Add ( new Separator () );
            }

        }

        private void DragRectangle_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            DragMove ();
        }

        private void Close_Button_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            this.Close ();
            mainWindowCaller.IsEnabled = true;
        }

        private void TemplateSelectButton_Click( object sender, RoutedEventArgs e )
        {
            if ( currentMode == Mode.Modify )
            {
                ListBoxItem selectedItem = TemplatesListBox.SelectedItem as ListBoxItem;
                string name = selectedItem.Content as string;
                TemplateXmlNode templateXmlNode = GetTemplateXmlNodeWithName ( name );
                MainWindow modifyTemplateWindow = new MainWindow ( "", mainWindowCaller.GetAvailableTemplates (), isTemplateWindow: true, templateXmlNodeParam: templateXmlNode, caller: mainWindowCaller );
                modifyTemplateWindow.Show ();
                this.Close ();
            }
            else if ( currentMode == Mode.Select )
            {
                ListBoxItem selectedItem = TemplatesListBox.SelectedItem as ListBoxItem;
                string name = selectedItem.Content as string;
                TemplateXmlNode templateXmlNode = GetTemplateXmlNodeWithName ( name );
                mainWindowCaller.UserSelectedTemplate ( templateXmlNode, pathID );
                this.Close ();
            }
        }

        private TemplateXmlNode GetTemplateXmlNodeWithName( string name )
        {
            foreach ( TemplateXmlNode template in mainWindowCaller.GetAvailableTemplates () )
            {
                if ( template.Name == name )
                {
                    return template;
                }
            }
            return null;
        }
    }
}
