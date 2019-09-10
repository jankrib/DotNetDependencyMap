using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Microsoft.Win32;

namespace DependencyMapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();

            var ofd = new OpenFileDialog()
            {
                Multiselect = false
            };

            var success = ofd.ShowDialog();
            if (success.HasValue && success.Value)
            {
                var fn = ofd.FileName;

                var assembly = Assembly.LoadFrom(fn);

                Map = DependencyMap.CreateFrom(assembly);
                DataContext = this;
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        public DependencyMap Map { get; }

        public Canvas VisualMap
        {
            get
            {

                var canvas = new Canvas();

                canvas.Children.Add(new Button() { Content = "Test" });

                return canvas;
            }
        }

    }
}
