using System;
using System.Windows;
using System.ComponentModel;


namespace FireAdmin
{
    /// <summary>
    /// Interaction logic for TestConsole.xaml
    /// </summary>
    public partial class FireAdminMainUI : Window
    {
        private void EnableDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var move = sender as Window;
            if (move != null)
            {
                Window win = Window.GetWindow(move);
                win.DragMove();
            }
        }

        public FireAdminMainUI()
        {
            try
            {
                InitializeComponent();

                var viewModel = new ViewModels.FireAdminMainViewModel();
                this.DataContext = viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }
        }
    }
}
