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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ITAJira
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                ViewModels.MainViewModel.ShowReportEvent += (_, _) => 
                {
                    if (FramePage.CanGoBack)
                        FramePage.RemoveBackEntry(); 
                };
                ViewModels.MainViewModel.HideReportEvent += (_, _) =>
                {
                    if (FramePage.CanGoBack)
                        FramePage.RemoveBackEntry();
                };
            }
            catch (Exception ex)
            {
                Logger.Err(ex);
            }
        }
    }
}
