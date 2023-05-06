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


namespace Renumber
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class frmViewPlan : Window
    {
        public frmViewPlan()
        {
            InitializeComponent();           
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        internal int GetStartNumber()
        {
            string selectedNum = cmbNumber.SelectedItem.ToString();
            int returnValue = Convert.ToInt32(selectedNum);

            return returnValue;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }


        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
