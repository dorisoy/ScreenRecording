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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ComeCapture.TestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.MouseMove += MainWindow_MouseMove;
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Mouse.Captured == null) Mouse.Capture(CatchPoint);
                ShowPoint.Text = PointToScreen(e.GetPosition(this)).ToString();
            }
        }

        private void btnScreen_Click(object sender, RoutedEventArgs e)
        {
            ComeCapture.MainWindow window = new ComeCapture.MainWindow();
            ShowScreenScale.Text = ComeCapture.MainWindow.ScreenScale.ToString();
            window.Show();
        }

    }
}
