using Newtonsoft.Json;

namespace DotVVM.Maui.App;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		BindingContext = new MainPageViewModel() { RouteName = "Default" };
	}

    private async void GetViewModelStateButton_Clicked(object sender, EventArgs e)
    {
		var json = JsonConvert.SerializeObject(await DotvvmPage.GetViewModelSnapshot());
		await DisplayAlert("ViewModel", json, "OK");
	}

	private async void PatchViewModelButton_Clicked(object sender, EventArgs e)
	{
		await DotvvmPage.PatchViewModel(new { Value = 3 });
	}

	private async void CallNamedCommandButton_Clicked(object sender, EventArgs e)
	{
		// TODO: element selector
		await DotvvmPage.CallNamedCommand("p0", "IncrementBy", 10);
	}

    private async void DotvvmPage_PageNotificationReceived(object sender, Hosting.Maui.Controls.PageNotificationEventArgs e)
    {
		await DisplayAlert("PageNotification", e.MethodName + "(" + string.Join(", ", e.Arguments.Select(JsonConvert.SerializeObject)) + ")", "OK");
    }
}

