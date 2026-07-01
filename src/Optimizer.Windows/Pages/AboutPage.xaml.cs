using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;

namespace Optimizer_Windows.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        PageTitleText.Text = AppText.Get("NavAbout");
        IntroTitleText.Text = AppText.Get("AppTitle");
        IntroBody1Text.Text = AppText.Get("AboutIntro1");
        IntroBody2Text.Text = AppText.Get("AboutIntro2");
        RoadmapTitleText.Text = AppText.Get("AboutRoadmap");
        RoadmapItem1Text.Text = AppText.Get("AboutRoadmap1");
        RoadmapItem2Text.Text = AppText.Get("AboutRoadmap2");
        RoadmapItem3Text.Text = AppText.Get("AboutRoadmap3");
        RoadmapItem4Text.Text = AppText.Get("AboutRoadmap4");
    }
}
