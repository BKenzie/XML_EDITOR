using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WPF_XML_Tutorial
{
    class ItemGrid : Grid
    {
        private MainWindow mainWindowCaller;

        public ItemGrid()
        {

        }

        public ItemGrid( MainWindow mainWindow, int width )
        {
            this.mainWindowCaller = mainWindow;
            this.Width = width;

            this.ShowGridLines = false;
            this.ColumnDefinitions.Add ( new ColumnDefinition () );
            this.ColumnDefinitions.Add ( new ColumnDefinition () );
            this.RowDefinitions.Add ( new RowDefinition () );
        }

        public void AddNewColumn()
        {
            this.ColumnDefinitions.Add ( new ColumnDefinition () );
        }

        public void AddNewRow()
        {
            this.RowDefinitions.Add ( new RowDefinition () );
        }

        public void AddTabButton( string name )
        {
            // Code for buttons linking to other tabs
            Button gotoTab_Button = new Button ();
            gotoTab_Button.Content = name;
            gotoTab_Button.Tag = name;
            gotoTab_Button.Background = new SolidColorBrush ( Colors.LightGray );
            gotoTab_Button.BorderBrush = new SolidColorBrush ( Colors.Transparent );

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
            gotoTab_Button.Style = customButtonStyle;

            gotoTab_Button.Click += mainWindowCaller.gotoTabButton_Click;

            Grid.SetRow ( gotoTab_Button, 0 );
            Grid.SetColumn ( gotoTab_Button, 1 );
            this.Children.Add ( gotoTab_Button );
        }

        public void AddTextBlock( string textValue, string toolTip, bool header = false )
        {
            if ( header )
            {
                TextBlock headerTextBlock = new TextBlock ();
                headerTextBlock.Text = textValue + ":";
                headerTextBlock.FontSize = 16;
                headerTextBlock.TextDecorations = TextDecorations.Underline;
                headerTextBlock.FontWeight = FontWeights.Bold;
                Grid.SetRow ( headerTextBlock, 0 );
                Grid.SetColumn ( headerTextBlock, 0 );
                this.Children.Add ( headerTextBlock );
            }
            else
            {
                TextBlock textBlock = new TextBlock ();
                textBlock.Text = textValue + ":";
                textBlock.ToolTip = toolTip;
                Grid.SetRow ( textBlock, 0 );
                Grid.SetColumn ( textBlock, 0 );
                this.Children.Add ( textBlock );
            }
        }

        public void AddTextBox( string textValue, string toolTip, bool empty = false )
        {
            if ( empty )
            {
                TextBox textBoxNodeText = new TextBox ();
                textBoxNodeText.KeyDown += new KeyEventHandler ( mainWindowCaller.OnTabPressed );
                textBoxNodeText.AppendText ( "EMPTY" );
                textBoxNodeText.ToolTip = toolTip;
                textBoxNodeText.AcceptsReturn = true;
                textBoxNodeText.GotKeyboardFocus += mainWindowCaller.EMPTYTextBox_GotKeyboardFocus;
                Grid.SetRow ( textBoxNodeText, 0 );
                Grid.SetColumn ( textBoxNodeText, 1 );
                this.Children.Add ( textBoxNodeText );
            }
            else
            {
                TextBox textBoxNodeText = new TextBox ();
                textBoxNodeText.KeyDown += new KeyEventHandler ( mainWindowCaller.OnTabPressed );
                textBoxNodeText.AppendText ( textValue );
                textBoxNodeText.ToolTip = toolTip;
                textBoxNodeText.AcceptsReturn = true;
                Grid.SetRow ( textBoxNodeText, 0 );
                Grid.SetColumn ( textBoxNodeText, 1 );
                this.Children.Add ( textBoxNodeText );
            }
        }

        public void AddContextMenu()
        {
            ContextMenu rightClickMenu = new ContextMenu ();
            MenuItem deleteItem = new MenuItem ();
            deleteItem.Header = "Delete text element";
            deleteItem.Click += mainWindowCaller.DeleteItem_Click;
            rightClickMenu.Items.Add ( deleteItem );
            this.ContextMenu = rightClickMenu;
        }
    }
}
