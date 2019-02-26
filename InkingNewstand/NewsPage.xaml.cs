﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace InkingNewstand
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NewsPage : Page
    {
        public NewsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string content;
            if (!(e.Parameter is NewsItem))
            {
                throw new Exception();
            }
            NewsItem news = (NewsItem)(e.Parameter);
            if (news.Item.Content != null)
            {
                content = news.Item.Content.Text;
            }
            else
            {
                content = news.Item.Summary.Text;
            }
            Html = content;
            this.Frame.LayoutUpdated += Frame_LayoutUpdated;
            //foreach (var link1 in news.Item.Links)
            //{
            //    ;
            //}
            //var link = news.Item.Links[0];
            //newsWebView.Source = news.Item.Links[0].Uri;

        }

        private void Frame_LayoutUpdated(object sender, object e)
        {
            
        }

        public Double WindowHeight
        {
            get
            {
                return Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Height;
            }
        }

        public Double WindowsWidth
        {
            get
            {
                return Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Width;
            }
        }

        public string Html { get; set; }
    }
}