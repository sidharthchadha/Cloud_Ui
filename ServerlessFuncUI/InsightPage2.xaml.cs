﻿/******************************************************************************
 * Filename    = BarGraphPage.xaml.cs
 *
 * Author      = Sidharth Chadha
 * 
 * Project     = ServerlessFuncUI
 *
 * Description = Defines the View of the Bar Graph Page.
 *****************************************************************************/
using LiveCharts.Defaults;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ServerlessFunc;
using Cloud_UX;
using System.Diagnostics;
using System.Collections;


namespace ServerlessFuncUI
{
    /// <summary>
    /// Interaction logic for BarGraphPage
    /// </summary>
    public partial class InsightPage2 : Page
    {
        
        private readonly InsightsApi _insightsApi;
        public static string InsightPath = "https://serverlessfunc20231121082343.azurewebsites.net/api/insights";
        public string hostname;
        public InsightPage2(string hostname)
        {
            this.InitializeComponent();
            _insightsApi = new InsightsApi(InsightPath);
            this.hostname = hostname;
        }

        private async void OnGetFailedStudentsClick(object sender, RoutedEventArgs e)
        {
            // Assuming you have the hostname and testName from some input fields.
            
            string testName = TestNameTextBox.Text;

            try
            {
                var failedStudents = await _insightsApi.GetFailedStudentsGivenTest(hostname, testName);

                // Assuming you want to display the results in a ListView.
                resultListBox.ItemsSource = failedStudents;
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately (e.g., display an error message).
                // You might want to log the exception for debugging purposes.
            }
        }

    }
}