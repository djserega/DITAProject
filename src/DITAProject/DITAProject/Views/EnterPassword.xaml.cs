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

namespace ITAJira.Views
{
    /// <summary>
    /// Interaction logic for EnterPassword.xaml
    /// </summary>
    public partial class EnterPassword : Window
    {
        public EnterPassword()
        {
            InitializeComponent();
            pwd.Focus();
        }

        public string GetPassword()
        {
            return pwd.Password;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OKAndClose();
        }

        private void Pwd_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    OKAndClose();
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
        }

        private void OKAndClose()
        {
            DialogResult = true;
            Close();
        }
    }
}
