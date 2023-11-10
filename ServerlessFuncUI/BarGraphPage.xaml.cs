/******************************************************************************
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
    public partial class BarGraphPage : Page
    {
        private static Random random = new Random(); // Shared Random instance

        private ChartValues<ObservableValue> meanValues;
        private ChartValues<ObservableValue> medianValues;
        private ChartValues<ObservableValue> highestValues;
        private ChartValues<ObservableValue> lowestValues;

        public BarGraphPage()
        {
            InitializeComponent();

            // Initialize chart values with more data points
            meanValues = new ChartValues<ObservableValue> { new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()) };
            medianValues = new ChartValues<ObservableValue> { new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()) };
            highestValues = new ChartValues<ObservableValue> { new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()) };
            lowestValues = new ChartValues<ObservableValue> { new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()), new ObservableValue(GetRandomValue()) };

            // Set chart data context
            DataContext = this;
        }

        private double GetRandomValue()
        {
            // Replace this with your logic to get random values
            return random.Next(1, 100);
        }

        public ChartValues<ObservableValue> MeanValues { get => meanValues; set => meanValues = value; }
        public ChartValues<ObservableValue> MedianValues { get => medianValues; set => medianValues = value; }
        public ChartValues<ObservableValue> HighestValues { get => highestValues; set => highestValues = value; }
        public ChartValues<ObservableValue> LowestValues { get => lowestValues; set => lowestValues = value; }
    }
}