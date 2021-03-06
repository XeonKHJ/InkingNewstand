﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using InkingNewsstand.Utilities;
using Windows.UI.Input.Inking.Core;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Windows.UI.Input.Inking;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Printing;
using Windows.Graphics.Printing;
using RichTextControls;
using Windows.UI.Xaml.Documents;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace InkingNewsstand
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NewsDetailPage : Page
    {
        public NewsDetailPage()
        {
            this.InitializeComponent();
            printManager = PrintManager.GetForCurrentView();
            printManager.PrintTaskRequested += PrintManager_PrintTaskRequested;
        }
        PrintManager printManager;

        NewsItem News { set; get; }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HtmlConverter.OnReadingHtmlConvertCompleted += HtmlConverter_OnReadingHtmlConvertCompleted;
            SettingPage.OnBindingWindowCheckBoxChanged += SettingPage_OnBindingWindowCheckBoxChanged;
            SettingPage.ValueChanged += SettingPage_ValueChanged;
            if (!(e.Parameter is NewsItem))
            {
                throw new Exception();
            }
            News = (NewsItem)(e.Parameter);

            //调整宽度
            if (!Settings.BindingNewsWidthwithFrame)
            {
                contentGrid.Width = Settings.NewsWidth;
            }

            //修改收藏图标
            if (News.IsFavorite)
            {
                favoriteButton.Icon = new SymbolIcon(Symbol.UnFavorite);
            }
            else
            {
                favoriteButton.Icon = new SymbolIcon(Symbol.Favorite);
            }

            //修改扩展模式图标
            if (Settings.ExtendedFeeds.Contains(News.Feed.Id))
            {
                extendButton.Icon = new SymbolIcon(Symbol.DockRight);
                isExtend = true;
            }
            else
            {
                isExtend = false;
                extendButton.Icon = new SymbolIcon(Symbol.DockLeft);
            }

            GetReadingHtml(News.NewsLink);
            if (News.CoverUrl == "")
            {
                CoverUrlforPage = "Nopic.jpg";
            }
            else
            {
                CoverUrlforPage = News.CoverUrl;
            }
            LoadInkStrokes(News.InkStrokes);
        }

        private void SettingPage_OnBindingWindowCheckBoxChanged(CheckBox sender)
        {
            if (Settings.BindingNewsWidthwithFrame)
            {
                contentGrid.Width = Double.NaN;
                System.Diagnostics.Debug.WriteLine("BindingNewsWidthwithFrame");
            }
            else
            {
                contentGrid.Width = Settings.NewsWidth;
                System.Diagnostics.Debug.WriteLine("NaN");
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SettingPage.ValueChanged -= SettingPage_ValueChanged;
            HtmlConverter.OnReadingHtmlConvertCompleted -= HtmlConverter_OnReadingHtmlConvertCompleted;
            SettingPage.OnBindingWindowCheckBoxChanged -= SettingPage_OnBindingWindowCheckBoxChanged;
            printManager.PrintTaskRequested -= PrintManager_PrintTaskRequested;
        }

        private void SettingPage_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SettingPage_OnBindingWindowCheckBoxChanged(null); //每次滑块更新更改宽度
            Bindings.Update();
        }

        private void HtmlConverter_OnReadingHtmlConvertCompleted(string html)
        {
            Html = html;
            Properties.SetHtml(htmlBlock, html);
            try
            {
                if (new Uri(News.CoverUrl) == new Uri(HtmlConverter.GetFirstImages(html)))
                {
                    headerImg.Visibility = Visibility.Collapsed;
                }
                else
                {
                    headerImg.Visibility = Visibility.Visible;
                }
            }
            catch (UriFormatException exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
                System.Diagnostics.Debug.WriteLine(exception.StackTrace);
            }
        }

        private bool isExtend = false;

        /// <summary>
        /// 获取文章HTML
        /// </summary>
        /// <param name="url"></param>
        private async void GetReadingHtml(Uri url)
        {
            if (isExtend)
            {
                if (News.ExtendedHtml == null)
                {
                    try
                    {
                        await HtmlConverter.ExtractReadableContent(url);
                        News.ExtendedHtml = Html;
                    }
                    catch (Exception readException)
                    {
                        System.Diagnostics.Debug.WriteLine(readException.Message);
                        System.Diagnostics.Debug.WriteLine(readException.StackTrace);
                    }
                }
                else
                {
                    HtmlConverter_OnReadingHtmlConvertCompleted(News.ExtendedHtml);
                }
            }
            else
            {
                HtmlConverter_OnReadingHtmlConvertCompleted(News.Content);
            }
        }

        public double WindowHeight
        {
            get
            {
                return ApplicationView.GetForCurrentView().VisibleBounds.Height;
            }
        }

        public double WindowsWidth
        {
            get
            {
                return ApplicationView.GetForCurrentView().VisibleBounds.Width;
            }
        }

        public string Html { get; set; }

        string CoverUrlforPage { set; get; }

        NewsPaper newsPaper = null;
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            newsPaper = NewsPaper.NewsPapers.Find((NewsPaper paper) => paper.PaperTitle == News.PaperTitle);

            //保存笔迹
            var currentStrokes = newsCanvas.InkPresenter.StrokeContainer.GetStrokes(); //获取笔迹
            byte[] serializedStrokes = await SerializeStrokes(currentStrokes); //序列化笔迹
            News.InkStrokes = serializedStrokes; //将笔迹保存到新闻实例里
            newsPaper.UpdateNewsList(News);
            await NewsPaper.SaveToFile(newsPaper); //保存报纸
        }

        /// <summary>
        /// 序列化笔迹
        /// </summary>
        /// <param name="strokes">笔迹列表</param>
        /// <returns>序列化后的字节数组</returns>
        private async Task<byte[]> SerializeStrokes(IReadOnlyCollection<InkStroke> strokes)
        {
            byte[] bytes = null;

            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                //先把笔迹输出到输出流
                using (var outputStream = stream.GetOutputStreamAt(0))
                {
                    await newsCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                }

                //从输入流中读出序列化结果
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    var dataReader = new DataReader(inputStream); //在该输入流中附着一个数据读取器
                    uint loadBytes = await dataReader.LoadAsync((uint)stream.Size); //加载数据数据到中间缓冲区
                    bytes = new byte[stream.Size];
                    dataReader.ReadBytes(bytes);
                }
            }
            return bytes;
        }

        /// <summary>
        /// 加载笔迹
        /// </summary>
        /// <param name="serializedStrokes">序列化后的笔迹的字节数组</param>
        private async void LoadInkStrokes(byte[] serializedStrokes)
        {
            if (serializedStrokes.Count() > 0)
            {
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    using (var outputStream = stream.GetOutputStreamAt(0))
                    {
                        DataWriter writer = new DataWriter(outputStream);
                        writer.WriteBytes(serializedStrokes);
                        await writer.StoreAsync();
                    }
                    using (var inputStream = stream.GetInputStreamAt(0))
                    {
                        await newsCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                    }
                }
            }
        }

        IPrintDocumentSource printDocumentSource;
        PrintDocument printDocument;

        //“导出”按钮点击事件
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            printDocument = new PrintDocument();
            printDocumentSource = printDocument.DocumentSource;


            printDocument.Paginate += PrintDocument_Paginate;
            printDocument.GetPreviewPage += PrintDocument_GetPreviewPage;
            printDocument.AddPages += PrintDocument_AddPages;

            await PrintManager.ShowPrintUIAsync();
        }

        private void PrintManager_PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            var printTask = args.Request.CreatePrintTask("新闻打印", PrintTaskSourceRequested);
            printTask.Completed += PrintTask_Completed;
        }

        private void PrintTaskSourceRequested(PrintTaskSourceRequestedArgs args)
        {
            args.SetSource(printDocumentSource);
        }

        private void PrintTask_Completed(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            //恢复属性
            Invoke(() =>
            {
                newsPanel.Width = double.NaN;
                newsPanel.Height = double.NaN;
                //newsPanel.Padding = new Thickness(40);
                htmlBlock.Width = double.NaN;
                htmlBlock.Height = double.NaN;
                grid.Children.Remove(newsPanel);
                contentGrid.Children.Add(newsPanel);
                newsPanel.InvalidateMeasure();
                newsPanel.UpdateLayout();
                UpdateInlineUIElementsLayout(htmlBlock, new Size(htmlBlock.ActualWidth, htmlBlock.ActualHeight));
                newsPanel.InvalidateMeasure();
                newsPanel.UpdateLayout();
            });
        }

        private List<UIElement> pagesToPrint;
        private Grid grid;
        private PageToPrint page;
        private double oldCanvasSize;

        /// <summary>
        /// “导出”的关键方法，用于添加打印选项和设置打印页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintDocument_Paginate(object sender, PaginateEventArgs e)
        {
            PrintTaskOptions printingOptions = e.PrintTaskOptions;
            PrintPageDescription printPageDescription = printingOptions.GetPageDescription(0);
            RememberInlineUIElementsLayout(htmlBlock);

            //创建新打印页面
            page = new PageToPrint
            {
                Width = printPageDescription.PageSize.Width,
                Height = printPageDescription.PageSize.Height,
                VerticalAlignment = VerticalAlignment.Top
            };
            printPanel.Children.Add(page);
            printPanel.InvalidateMeasure();
            printPanel.UpdateLayout();

            //设置新页面
            pagesToPrint = new List<UIElement>();

            //记录RichTextBlock中每个控件的大小
            RememberInlineUIElementsLayout(htmlBlock);

            //将控件迁移到打印页面
            newsPanel.Width = newsPanel.ActualWidth;
            newsPanel.Height = printPageDescription.PageSize.Height;
            oldCanvasSize = newsCanvas.ActualHeight;
            headerPanel.Width = headerPanel.ActualWidth;
            htmlBlock.Width = htmlBlock.ActualWidth;
            htmlBlock.Height = printPageDescription.PageSize.Height - headerPanel.ActualHeight;
            grid = new Grid()
            {
                Width = newsPanel.Width,
                Height = newsPanel.Height,
                VerticalAlignment = VerticalAlignment.Top
            };
            contentGrid.Children.Remove(newsPanel);
            grid.Children.Add(newsPanel);
            page.PrintableContentViewBox.Child = grid;
            UpdateInlineUIElementsLayout(htmlBlock, new Size(htmlBlock.Width, htmlBlock.Height));
            //更新布局
            page.InvalidateMeasure();
            page.UpdateLayout();

            //将页面添加到页面列表
            pagesToPrint.Add(page);

            bool hasOverflowContent = false;
            RichTextBlockOverflow richTextBlockOverflow = null;
            if (htmlBlock.HasOverflowContent)
            {
                hasOverflowContent = true;
                richTextBlockOverflow = new RichTextBlockOverflow
                {
                    Height = grid.ActualHeight,
                    Width = grid.Width,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                htmlBlock.OverflowContentTarget = richTextBlockOverflow;
            }


            while (hasOverflowContent)
            {
                ContinueonPageToPrint continueonPageToPrint = new ContinueonPageToPrint()
                {
                    Width = printPageDescription.PageSize.Width,
                    Height = printPageDescription.PageSize.Height,
                    VerticalAlignment = VerticalAlignment.Top
                };
                printPanel.Children.Add(continueonPageToPrint);
                printPanel.InvalidateMeasure();
                printPanel.UpdateLayout();

                Grid overFlowGrid = new Grid()
                {
                    Width = grid.Width,
                    Height = grid.Height
                };
                StackPanel stackPanel = new StackPanel()
                {
                    Height = newsPanel.Height,
                    Width = newsPanel.Width
                };
                stackPanel.Children.Add(richTextBlockOverflow);
                continueonPageToPrint.PrintableContentViewBox.Child = stackPanel;
                continueonPageToPrint.InvalidateMeasure();
                continueonPageToPrint.UpdateLayout();
                pagesToPrint.Add(continueonPageToPrint);

                if (richTextBlockOverflow.HasOverflowContent)
                {
                    hasOverflowContent = true;
                    var oldRichTextBlockOverflow = richTextBlockOverflow;
                    richTextBlockOverflow = new RichTextBlockOverflow
                    {
                        Height = grid.ActualHeight,
                        Width = htmlBlock.Width,
                        VerticalAlignment = VerticalAlignment.Top,
                    };
                    oldRichTextBlockOverflow.OverflowContentTarget = richTextBlockOverflow;
                }
                else
                {
                    hasOverflowContent = false;
                }
            }
            printDocument.SetPreviewPageCount(pagesToPrint.Count, PreviewPageCountType.Final);
        }

        /// <summary>
        /// 更新RichTextBlock中的布局，用于导出时添加页面用
        /// </summary>
        /// <param name="richTextBlock"></param>
        /// <param name="compareSize"></param>
        private void UpdateInlineUIElementsLayout(RichTextBlock richTextBlock, Size compareSize)
        {
            int i = 0;
            foreach (var paragraph in richTextBlock.Blocks)
            {
                foreach (var child in ((Paragraph)paragraph).Inlines)
                {
                    if (child is InlineUIContainer)
                    {
                        var inlineUIContainerChild = (child as InlineUIContainer).Child;
                        if (inlineUIContainerChild is FrameworkElement)
                        {
                            var inlineUIContainerFramworkElement = inlineUIContainerChild as FrameworkElement;
                            inlineUIContainerFramworkElement.Width = oldSizes[i].Width * compareSize.Width;
                            inlineUIContainerFramworkElement.Height = oldSizes[i++].Height * inlineUIContainerFramworkElement.Width;

                            inlineUIContainerChild.InvalidateMeasure();
                            inlineUIContainerChild.UpdateLayout();
                        }
                    }
                }
            }
        }

        private List<Size> oldSizes;

        /// <summary>
        /// 用于记住HtmlBlock中的的InlineUIElement的布局，以便以后还原
        /// </summary>
        /// <param name="richTextBlock"></param>
        private void RememberInlineUIElementsLayout(RichTextBlock richTextBlock)
        {
            oldSizes = new List<Size>();
            foreach (var paragraph in richTextBlock.Blocks)
            {
                foreach (var child in ((Paragraph)paragraph).Inlines)
                {
                    if (child is InlineUIContainer)
                    {
                        var inlineUIContainerChild = (child as InlineUIContainer).Child;
                        if (inlineUIContainerChild is FrameworkElement)
                        {
                            var inlineUIContainerFramworkElement = inlineUIContainerChild as FrameworkElement;
                            oldSizes.Add(new Size(inlineUIContainerFramworkElement.ActualWidth / htmlBlock.ActualWidth, inlineUIContainerFramworkElement.ActualHeight / inlineUIContainerFramworkElement.ActualWidth));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// “导出”功能获取预览页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PrintDocument_GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("开始设置预览页面");

            //将墨迹转换成图片
            //将笔迹转换成图片

            CanvasDevice canvasDevice = new CanvasDevice();
            CanvasRenderTarget renderTarget = new CanvasRenderTarget(canvasDevice, (int)newsCanvas.ActualWidth, (int)oldCanvasSize, 96);

            using (var ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(Colors.Transparent);
                ds.DrawInk(newsCanvas.InkPresenter.StrokeContainer.GetStrokes());
            }
            List<InkStroke> dehighlightedInkStrokes = new List<InkStroke>();
            foreach (var inkStroke in newsCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                if (inkStroke.DrawingAttributes.DrawAsHighlighter == true)
                {
                    var noHighlightedStroke = inkStroke.Clone();
                    InkDrawingAttributes inkDrawingAttributes = new InkDrawingAttributes()
                    {
                        DrawAsHighlighter = false,
                        Color = noHighlightedStroke.DrawingAttributes.Color,
                        PenTip = noHighlightedStroke.DrawingAttributes.PenTip,
                        Size = noHighlightedStroke.DrawingAttributes.Size
                    };
                    noHighlightedStroke.DrawingAttributes = inkDrawingAttributes;
                    dehighlightedInkStrokes.Add(noHighlightedStroke);
                }
            }
            newsCanvas.InkPresenter.StrokeContainer.AddStrokes(dehighlightedInkStrokes);
            using (var ds = renderTarget.CreateDrawingSession())
            {
                using (ds.CreateLayer(0.5f))
                {
                    ds.Clear(Colors.Transparent);
                    ds.DrawInk(newsCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }
            }

            BitmapSource inkBitmapImage;
            using (var stream = new InMemoryRandomAccessStream())
            {
                await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                inkBitmapImage = new BitmapImage();
                stream.Seek(0);
                await inkBitmapImage.SetSourceAsync(stream);
            }
            Image inkImage = null;

            //调高图片
            Invoke(() =>
            {
                inkImage = new Image
                {
                    Stretch = Stretch.Uniform,
                    Source = inkBitmapImage,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                grid.Children.Add(inkImage);
                inkImage.Width = newsPanel.Width;
                inkImage.Height = grid.Height;
                inkImage.Stretch = Stretch.UniformToFill;

                page.InvalidateMeasure();
                page.UpdateLayout();
                System.Diagnostics.Debug.WriteLine(inkImage.Translation);
                printDocument.SetPreviewPage(e.PageNumber, pagesToPrint[e.PageNumber - 1]);
            });
            //System.Diagnostics.Debug.WriteLine("图片添加完成");
        }

        /// <summary>
        /// “导出”功能添加页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintDocument_AddPages(object sender, AddPagesEventArgs e)
        {
            foreach (var page in pagesToPrint)
            {
                printDocument.AddPage(page);
            }

            printDocument.AddPagesComplete();
        }

        /// <summary>
        /// “收藏”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            News.IsFavorite = !News.IsFavorite;
            if (News.IsFavorite)
            {
                favoriteButton.Icon = new SymbolIcon(Symbol.UnFavorite);
                App.Favorites.Add(new Models.FavoriteModel(News));
            }
            else
            {
                App.Favorites.Remove(new Models.FavoriteModel(News));
                favoriteButton.Icon = new SymbolIcon(Symbol.Favorite);
            }
            //newsPaper = NewsPaper.NewsPapers.Find((NewsPaper paper) => paper.PaperTitle == News.PaperTitle);
        }

        /// <summary>
        /// “在浏览器中打开”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenInBroswerButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await Windows.System.Launcher.LaunchUriAsync(News.NewsLink);
        }

        /// <summary>
        /// 在阅读模式下打开按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenInEdgeReadingModeButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new Windows.System.LauncherOptions
            {
                TargetApplicationPackageFamilyName = "Microsoft.MicrosoftEdge_8wekyb3d8bbwe"
            };

            var readingModeUriString = "read:" + News.NewsLink.AbsoluteUri;

            // Launch the URI
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri(readingModeUriString), options);
        }

        /// <summary>
        /// “分享”点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;

            DataTransferManager.ShowShareUI(); //显示分享界面
        }

        /// <summary>
        /// "分享"处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.SetWebLink(News.NewsLink);
            request.Data.Properties.Title = News.Title;
            request.Data.Properties.Description = "该新闻的链接";
        }

        /// <summary>
        /// “扩展模式”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExtendButton_Click(object sender, RoutedEventArgs e)
        {
            if (isExtend)
            {
                extendButton.Icon = new SymbolIcon(Symbol.DockLeft);
                Settings.ExtendedFeeds.Remove(News.Feed.Id);
                isExtend = false;
            }
            else
            {
                Settings.ExtendedFeeds.Add(News.Feed.Id);
                extendButton.Icon = new SymbolIcon(Symbol.DockRight);
                isExtend = true;
            }
            Settings.SaveSettings();
            GetReadingHtml(News.NewsLink);
        }

        private int selectionCount = 0;
        private TextPointer selectionEnd;
        bool translationFlyoutIsNullorClosed = false;

        /// <summary>
        /// 选中新闻中的文本后触发该事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HtmlBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (selectionCount == 0)
            {
                DetectSelectionFinished();
            }
            ++selectionCount;

            selectionEnd = htmlBlock.SelectionEnd;
            translationFlyoutIsNullorClosed = (translationFlyout == null) ? true : !translationFlyout.IsOpen;

            System.Diagnostics.Debug.WriteLine(selectionCount.ToString());
            //WordPicker wordPicker = new WordPicker();
            //var result = wordPicker.Lookfor(htmlBlock.SelectedText, Language_t.en);
        }

        private Flyout translationFlyout;
        //WordPicker wordPicker = new WordPicker();
        Translator translator = new Translator();

        /// <summary>
        /// 检测到完成一次选择文本
        /// </summary>
        private async void DetectSelectionFinished()
        {
            bool flyoutShowed = false;
            while (true)
            {
                var oldSelectionCount = selectionCount;
                var oldSelectionEnd = selectionEnd;

                //每个100毫秒检测一次变动。
                await Task.Delay(100); 

                var newSelectionEnd = selectionEnd;
                System.Diagnostics.Debug.WriteLine(translationFlyoutIsNullorClosed.ToString());
                if (translationFlyoutIsNullorClosed) //如果translationFlyout被初始化了但没有开着
                {
                    if (selectionCount == oldSelectionCount)
                    {
                        selectionCount = 0;

                            //是否已经显示了Flyout
                        if (!flyoutShowed
                            //是否是重复选择
                            || ((oldSelectionEnd != null && newSelectionEnd != null) && oldSelectionEnd != newSelectionEnd)) 
                        {
                            //如果没有显示Flyout并且两次触发SelectionChanged的内容一样。
                            Invoke(() =>
                            {
                                var selectedText = htmlBlock.SelectedText;
                                if (selectedText != "" && selectedText != null)
                                {
                                    try
                                    {
                                        translationFlyout = new Flyout
                                        {
                                            Content = new ScrollViewer
                                            {
                                                Content = new TextBlock()
                                                {
                                                    Text = translator.Translate(selectedText, LanguageCode.zh),
                                                    IsTextSelectionEnabled = true,
                                                    TextWrapping = TextWrapping.WrapWholeWords
                                                },
                                                MaxWidth = 400,
                                                MaxHeight = 500
                                            }
                                            
                                        };
                                    }
                                    catch (Exception)
                                    {
                                        translationFlyout = new Flyout
                                        {
                                            Content = new TextBlock() { Text = "翻译出错", IsTextSelectionEnabled = true },
                                        };
                                    }
                                    FlyoutShowOptions flyoutShowOptions = new FlyoutShowOptions
                                    {
                                        Position = new Point(newSelectionEnd.GetCharacterRect(LogicalDirection.Forward).X, newSelectionEnd.GetCharacterRect(LogicalDirection.Forward).Y),
                                        ShowMode = FlyoutShowMode.Transient
                                    };
                                    try
                                    {
                                        translationFlyout.ShowAt(htmlBlock, flyoutShowOptions);
                                    }
                                    catch (ArgumentException exception)
                                    {
                                        System.Diagnostics.Debug.WriteLine(exception.Message);
                                    }
                                    flyoutShowed = true;
                                }
                            });
                        }
                        System.Diagnostics.Debug.WriteLine("break!");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 获取光标位置
        /// </summary>
        /// <returns></returns>
        private Point GetPointerPosition()
        {
            var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;

            if (pointerPosition.X > Window.Current.Bounds.Right)
            {
                pointerPosition.X = Window.Current.Bounds.Right;
            }
            else if (pointerPosition.X < Window.Current.Bounds.Left)
            {
                pointerPosition.X = Window.Current.Bounds.Left;
            }

            if (pointerPosition.Y > Window.Current.Bounds.Bottom)
            {
                pointerPosition.Y = Window.Current.Bounds.Bottom;
            }
            else if (pointerPosition.Y < Window.Current.Bounds.Top)
            {
                pointerPosition.Y = Window.Current.Bounds.Top;
            }


            var x = pointerPosition.X - Window.Current.Bounds.X;
            var y = pointerPosition.Y - Window.Current.Bounds.Y;


            var ttv = newsDetailPageGrid.TransformToVisual(Window.Current.Content);
            Point screenCoords = ttv.TransformPoint(new Point(0, 0));

            x -= screenCoords.X;
            y -= screenCoords.Y + 10;

            return new Point(x, y);
        }

        public async void Invoke(Action action, Windows.UI.Core.CoreDispatcherPriority Priority = Windows.UI.Core.CoreDispatcherPriority.Normal)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Priority, () => { action(); });
        }
    }
}
