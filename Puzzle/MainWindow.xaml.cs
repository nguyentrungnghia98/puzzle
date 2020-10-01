using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Puzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
        }
        int n = 3;
        Image[,] imageButton;
        ImageSource[,] imageSources;        
        const int width = 100;
        const int height = 100;
        const int startY = 100;
        const int startX = 100;
        const int padding = 5;
        Image EmptyImage;
        // game data 
        bool _isDragging = false;
        Image _selectedBitmap = null;
        Point _lastPosition;
        Tuple<int, int> _originIndex;
        string gameStatus = "Click start to play";
        string imagePath;
        //count time
        Timer timer;
        int timePerRound = 3*60; //3*60
        bool isContinute = false;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            imagePath = AppDomain.CurrentDomain.BaseDirectory + "Images/example.jpg";
            Debug.Write(imagePath);

            initialGame(false);           
        }

        private void initialGame(bool isSwap = true)
        {
            ImageSource[,] images;
            try
            {
                images = CutImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cut image failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            imageSources = images;
            renderImagesCutted();
            //swap image
            if (isSwap) swapImages();
        }

        private void clearCanvas()
        {
            //clear canvas
            if (imageButton != null)
            {
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        game.Children.Remove(imageButton[i, j]);
            }
        }

        private void renderImagesCutted()
        {
            clearCanvas();

            // Model - Tao ra ma tran nut bam 
            imageButton = new Image[n, n];

            // UI
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {

                    var image = new Image();

                    image.Source = imageSources[i, j];
                    image.Width = width;
                    image.Height = height;
                    image.Tag = new Tuple <int>( i * 3 + j ) ;
                    //button.Click += SquareButton_Click;
                    if (i == n - 1 && j == n - 1)
                    {

                        image.Source = new BitmapImage();
                        EmptyImage = image;
                    }
                    imageButton[i, j] = image;

                    // Dua vao UI
                    game.Children.Add(image);
                    image.MouseLeftButtonDown += CropImage_MouseLeftButtonDown;
                    image.PreviewMouseLeftButtonUp += CropImage_PreviewMouseLeftButtonUp;

                    Canvas.SetLeft(image, startX + j * (width + padding));
                    Canvas.SetTop(image, startY + i * (height + padding));
                }
            }
        }

        private static BitmapFrame CreateResizedImage(BitmapImage source, int width, int height, int margin)
        {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        private ImageSource[,] CutImage()
        {
            ImageSource[,] _imageSources = new ImageSource[n, n];
            BitmapImage src = new BitmapImage();
                          
            if (imagePath != null)
            {
                src.BeginInit();
                src.UriSource = new Uri(imagePath, UriKind.Absolute);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
            }
            
            BitmapFrame resizeImg = CreateResizedImage(src, 300, 300, 0);
            // resize 300x300
            //update preview image
            previewImage.Source = resizeImg;

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    _imageSources[i, j] = new CroppedBitmap(resizeImg, new Int32Rect(j * width, i * height, width, height));
            return _imageSources;
        }

        private void swapImages()
        {
            
            MoveImageTo("bottom",true);
            MoveImageTo("bottom", true);
            MoveImageTo("right", true);
            MoveImageTo("right", true);
            string[] postions = { "bottom", "top", "left", "right" };
            int count = 0;
            Random random = new Random();

            while (count < 120)
            {
                MoveImageTo(postions[random.Next(0, 4)], true);
                count++;
            }
        }

        
        private void CropImage_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectedBitmap == null) return;
            _isDragging = false;
            Grid.SetZIndex(_selectedBitmap, 9);

            var position = e.GetPosition(this);
            int currentI = ((int)position.Y - startY) / height;
            int currentJ = ((int)position.X - startX) / width;
            var (originI, originJ) = _originIndex;

            // get index empty image
            var lastLeft = Canvas.GetLeft(EmptyImage);
            var lastTop = Canvas.GetTop(EmptyImage);
            int emptyI = ((int)lastTop - startY) / height;
            int emptyJ = ((int)lastLeft - startX) / width;

            Debug.WriteLine($"current-{currentI}-{currentJ},empty{emptyI}-{emptyJ}");
            if ((Math.Abs(originI - emptyI) <= 1  && originJ == emptyJ) || 
                (Math.Abs(originJ - emptyJ) <= 1 && originI == emptyI))
            {
                SwapImage(emptyI, emptyJ, originI, originJ);
                if (isCorrectImage())
                {
                    youWin();
                }
            }
            else
            {
                Canvas.SetLeft(_selectedBitmap, startX + originJ * (width + padding));
                Canvas.SetTop(_selectedBitmap, startY + originI * (height + padding));
            }
        }

        private void CropImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isContinute || gameStatus != "Playing")
            {
                MessageBox.Show("Click start to play!", "Warning");
                return;
            }
            _isDragging = true;
            _selectedBitmap = sender as Image;
            Grid.SetZIndex(_selectedBitmap, 99);
            _lastPosition = e.GetPosition(this);

            var lastLeft = Canvas.GetLeft(_selectedBitmap);
            var lastTop = Canvas.GetTop(_selectedBitmap);
            int i = ((int)lastTop - startY) / height;
            int j = ((int)lastLeft - startX) / width;
            _originIndex = new Tuple<int, int>(i, j);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isContinute || gameStatus != "Playing")
            {
                return;
            }
            var position = e.GetPosition(this);
            int i = ((int)position.Y - startY) / height;
            int j = ((int)position.X - startX) / width;
            //this.Title = $"{position.X} - {position.Y}, a[{i}][{j}]";

            if (_isDragging)
            {
                var dx = position.X - _lastPosition.X;
                var dy = position.Y - _lastPosition.Y;

                var lastLeft = Canvas.GetLeft(_selectedBitmap);
                var lastTop = Canvas.GetTop(_selectedBitmap);
                Canvas.SetLeft(_selectedBitmap, lastLeft + dx);
                Canvas.SetTop(_selectedBitmap, lastTop + dy);

                _lastPosition = position;
            }
        }


        private bool isCorrectImage()
        {

            int prev = (imageButton[0, 0].Tag as Tuple<int>).Item1;
            
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int current = (imageButton[i, j].Tag as Tuple<int>).Item1;
                    if (prev + 1 != current) return false;
                    prev = current;
                }
            return true;
        }


        private void MoveImageTo(string position, bool isInitial = false)
        {
            if (isContinute || (!isInitial && gameStatus != "Playing")) return;
            var lastLeft = Canvas.GetLeft(EmptyImage);
            var lastTop = Canvas.GetTop(EmptyImage);
            int i = ((int)lastTop - startY) / height;
            int j = ((int)lastLeft - startX) / width;
            //var (i, j) = EmptyImage.Tag as Tuple<int, int>;
            switch (position)
            {
                case "left":
                    if (j == 2) return;
                    SwapImage(i, j + 1, i, j);
                    break;
                case "right":
                    if (j == 0) return;
                    SwapImage(i, j - 1, i, j);
                    break;
                case "top":
                    if (i == 2) return;
                    SwapImage(i + 1, j, i, j);
                    break;
                case "bottom":
                    if (i == 0) return;
                    SwapImage(i - 1, j, i, j);
                    break;
                
                default:
                    break;
            }
            if (!isInitial)
            {
               if(isCorrectImage()) youWin();
            }
        }

        private void SwapImage(int i1, int j1, int i2, int j2)
        {
            Image tmp = imageButton[i1, j1];

            imageButton[i1, j1] = imageButton[i2, j2];
            imageButton[i2, j2] = tmp;

            Canvas.SetLeft(imageButton[i1, j1], startX + j1 * (width + padding));
            Canvas.SetTop(imageButton[i1, j1], startY + i1 * (height + padding));

            Canvas.SetLeft(imageButton[i2, j2], startX + j2 * (width + padding));
            Canvas.SetTop(imageButton[i2, j2], startY + i2 * (height + padding));
        }

        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key.ToString())
            {
                case "Left":
                    MoveImageTo("left");
                    break;
                case "Right":
                    MoveImageTo("right");
                    break;
                case "Up":
                    MoveImageTo("top");
                    break;
                case "Down":
                    MoveImageTo("bottom");
                    break;
                default:
                    break;
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveImageTo("bottom");
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveImageTo("top");
        }

        private void MoveRightButton_Click(object sender, RoutedEventArgs e)
        {
            MoveImageTo("right");
        }

        private void MoveLeftButton_Click(object sender, RoutedEventArgs e)
        {
            MoveImageTo("left");
        }

        private void youWin()
        {
            if (timer != null) timer.Stop();
            MessageBox.Show("You win!");
            gameStatus = "You win";
            txtStatus.Text = gameStatus;
        }

        private void youLose()
        {
            gameStatus = "You lose";
            txtStatus.Text = gameStatus;
            MessageBox.Show("You lose!");
            
        }

        private void endGame()
        {
            restartButton.Content = "Start";
            if(timer!=null) timer.Stop();
            gameStatus = "Click start to play";
            txtStatus.Text = gameStatus;
            txtMinute.Text = "3";
            txtSecond1.Text = "0";
            txtSecond2.Text = "0";
        }

        private void restart()
        {
            if(!isContinute) initialGame();
            gameStatus = "Playing";
            txtStatus.Text = gameStatus;
            restartButton.Content = "Restart";
            startCountTime();
            isContinute = false;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            restart();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var screen = new OpenFileDialog();

            if (screen.ShowDialog() == true)
            {
                imagePath = screen.FileName;
                initialGame(false);
                endGame();
            }
        }

        // count time
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timePerRound--;
            Dispatcher.Invoke(() =>
                {
                    TimeSpan result = TimeSpan.FromSeconds(timePerRound);
                    string fromTimeString = result.ToString("mm':'ss");

                    txtMinute.Text = fromTimeString[1].ToString(); 
                    txtSecond1.Text = fromTimeString[3].ToString();
                    txtSecond2.Text = fromTimeString[4].ToString();
                });

            if (timePerRound == 0)
            {
                timer.Stop();
                Dispatcher.Invoke(() => youLose());

            }
        }

        private void startCountTime()
        {
            if (timer != null) timer.Stop();
            timer = new Timer();
            //timePerRound = 3*60;
            if(!isContinute) timePerRound = 3*60;
            Dispatcher.Invoke(() =>
            {
                TimeSpan result = TimeSpan.FromSeconds(timePerRound);
                string fromTimeString = result.ToString("mm':'ss");

                txtMinute.Text = fromTimeString[1].ToString();
                txtSecond1.Text = fromTimeString[3].ToString();
                txtSecond2.Text = fromTimeString[4].ToString();
            });
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }
        // save and load game
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string SelectedFilePath = "";
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save an Txt";
            saveFileDialog.FileName = "Save.txt";
            saveFileDialog.Filter = "Text File | *.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    SelectedFilePath = saveFileDialog.FileName;
                    if (String.IsNullOrEmpty(SelectedFilePath)) return;
                    var writer = new StreamWriter(SelectedFilePath);
                    writer.WriteLine(imagePath);
                    writer.WriteLine(timePerRound);
                    writer.WriteLine(gameStatus);
                    // Theo sau la ma tran bieu dien game
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int index = (imageButton[i, j].Tag as Tuple<int>).Item1;
                            writer.Write($"{index}");
                            if (j != 2)
                            {
                                writer.Write(" ");
                            }
                        }
                        writer.WriteLine("");
                    }

                    writer.Close();

                    MessageBox.Show("Game is saved");
                }
                catch (DivideByZeroException err)
                {
                    MessageBox.Show(err.Message, "Error");
                }

            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var screen = new OpenFileDialog();
            if (screen.ShowDialog() == true)
            {
                string filename, firstLine, secondLine, thirdLine;
                StreamReader reader;
                Image[,] tmp = new Image[n, n];
                int[,] indexs = new int[n, n];

                try
                {
                    filename = screen.FileName;

                    reader = new StreamReader(filename);
                    firstLine = reader.ReadLine();
                    secondLine = reader.ReadLine();
                    thirdLine = reader.ReadLine();
                    if(firstLine == null || secondLine == null || thirdLine == null)
                        throw new System.InvalidOperationException("Missing value!");
                    if(!File.Exists(firstLine))
                        throw new System.InvalidOperationException($"File not exist!\n{firstLine}");
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message, $"Read file error");
                    return;
                }

                try
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var line = reader.ReadLine();
                        if(line == null) throw new System.InvalidOperationException("Missing value!");
                        var tokens = line.Split(
                            new string[] { " " }, StringSplitOptions.None);
                        // Model
                        
                        for (int j = 0; j < 3; j++)
                        {
                            indexs[i, j] = Int32.Parse(tokens[j]);
                            if(indexs[i, j] < 0 || indexs[i, j]> 8)
                                throw new System.InvalidOperationException("Invalid location index!");
                        }
                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message, "Read file error");
                    return;
                }

                endGame();
                var a = tmp;
                imagePath = firstLine;
                timePerRound = Int32.Parse(secondLine);
                gameStatus = thirdLine;
                initialGame(false);

                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        tmp[i, j] = imageButton[i, j];

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        //tokens[j]
                        int index = indexs[i,j];
                        Debug.Write(index);
                        imageButton[i, j] = tmp[index / 3, index % 3];

                        Canvas.SetLeft(imageButton[i, j], startX + j * (width + padding));
                        Canvas.SetTop(imageButton[i, j], startY + i * (height + padding));
                    }
                }

                switch (gameStatus)
                {
                    case "You win":
                        youWin();
                        break;
                    case "You lose":
                        youLose();
                        break;
                    case "Playing":
                        TimeSpan result = TimeSpan.FromSeconds(timePerRound);
                        string fromTimeString = result.ToString("mm':'ss");

                        txtMinute.Text = fromTimeString[1].ToString();
                        txtSecond1.Text = fromTimeString[3].ToString();
                        txtSecond2.Text = fromTimeString[4].ToString();
                        isContinute = true;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

