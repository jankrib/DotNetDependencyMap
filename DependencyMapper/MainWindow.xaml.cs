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

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register("SelectedNode", typeof(Node), typeof(MainWindow), new PropertyMetadata(null));


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


                Map = DependencyMap.CreateFrom(fn);
                DataContext = this;
            }
            else
            {
                Application.Current.Shutdown();
            }
        }



        public DependencyMap Map { get; }

        public IEnumerable<Node> Nodes => Map.Nodes.OrderBy(x => x.Name.Name).ThenBy(x => x.Name.Version);



        public Node SelectedNode
        {
            get { return (Node)GetValue(SelectedNodeProperty); }
            set { SetValue(SelectedNodeProperty, value); }
        }

        public Canvas VisualMap
        {
            get
            {
                return GenerateVisualMap();
            }
        }


        private Canvas GenerateVisualMap()
        {
            var canvas = new Canvas();

            var lookup = new Dictionary<Node, Button>();
            var levels = Map.Nodes.GroupBy(x => x.Level);

            canvas.Width = 100 + levels.Max(x => x.Count()) * 200;
            canvas.Height = levels.Max(x => x.Key) * 100 + 100;

            foreach (var level in levels)
            {
                var cx = 50;
                foreach (var node in level)
                {
                    var b = new Button()
                    {
                        Content = new StackPanel()
                        {
                            Children =
                            {
                                new TextBlock(new Run(node.Name.Name)),
                                new TextBlock(new Run(node.Name.Version.ToString()))
                            }
                        }
                    };

                    if (node.Error != null)
                    {
                        b.Background = Brushes.Red;
                        b.ToolTip = node.Error.ToString();
                    }

                    Canvas.SetTop(b, level.Key * 100);
                    Canvas.SetLeft(b, cx);
                    Canvas.SetZIndex(b, 100);

                    lookup[node] = b;
                    canvas.Children.Add(b);

                    cx += 200;
                }
            }

            foreach (var edge in Map.Edges)
            {
                var b1 = lookup[edge.From];
                var b2 = lookup[edge.To];

                var lstyle = new Style(typeof(Line))
                {
                    Setters =
                    {
                        new Setter(Line.StrokeProperty, Brushes.LightBlue),
                        new Setter(Canvas.ZIndexProperty, 50)
                    },
                    Triggers =
                    {
                        new DataTrigger()
                        {
                            Binding = new Binding("SelectedNode.Name.Name"),
                            Value = edge.From.Name.Name,
                            Setters =
                            {
                                new Setter(Line.StrokeProperty, Brushes.Blue),
                                new Setter(Canvas.ZIndexProperty, 75)
                            }
                        },
                        new DataTrigger()
                        {
                            Binding = new Binding("SelectedNode.Name.Name"),
                            Value = edge.To.Name.Name,
                            Setters =
                            {
                                new Setter(Line.StrokeProperty, Brushes.Green),
                                new Setter(Canvas.ZIndexProperty, 75)
                            }
                        }
                    }
                };

                var connector = new Line()
                {
                    X1 = Canvas.GetLeft(b1) + 50,
                    Y1 = Canvas.GetTop(b1) + 20,
                    X2 = Canvas.GetLeft(b2) + 50,
                    Y2 = Canvas.GetTop(b2) + 20,
                    StrokeThickness = 2,
                    Style = lstyle
                };

                Canvas.SetZIndex(connector, 50);

                canvas.Children.Add(connector);
            }

            //canvas.Children.Add(new Button() { Content = "Test" });

            return canvas;
        }

    }
}
