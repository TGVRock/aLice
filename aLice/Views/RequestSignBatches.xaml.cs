using aLice.Models;
using aLice.ViewModels;

namespace aLice.Views;

public partial class RequestSignBatches : ContentPage
{
    public RequestSignBatches()
    {
        InitializeComponent();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            AccountViewModel.DeletePasswordByTimestamp();
            await ShowRequestSign();
            if (RequestViewModel.Notification.SetPublicKey != null && AccountViewModel.MainAccount.publicKey != RequestViewModel.Notification.SetPublicKey)
            {
                var requestAccount = RequestViewModel.GetRequestAccount();
                var isChangeMainAccount = await DisplayAlert("確認", requestAccount.accountName + "をメインアカウントに変更しますか？\nいいえを選択するとこのページを閉じます", "はい", "いいえ");
                if (isChangeMainAccount)
                {
                    await AccountViewModel.ChangeMainAccount(requestAccount.address);
                    RequestViewModel.SetMainAccountSignerPublicKey();
                    Password.Text = "";
                    Ask.Text = $"{AccountViewModel.MainAccount.accountName}で署名しますか？";
                }
                else
                {
                    if (Navigation.ModalStack.Count > 0)
                    {
                        await Navigation.PopModalAsync();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
            if (Navigation.ModalStack.Count > 0)
            {
                await Navigation.PopModalAsync();
            }
        }
    }

    // 署名を要求されたときに呼び出される
    private async Task ShowRequestSign()
    {
        try
        {
            var (domainText, typeText, dataText, askText) = RequestViewModel.GetShowTexts();
            Domain.Text = domainText;
            Type.Text = typeText;
            Data.Text = dataText;
            Ask.Text = askText;
            Password.Text = "";
            
            try
            {
                var p = (await SecureStorage.GetAsync("CurrentPassword")).Split("_");
                Password.Text = p[0];
            }
            catch
            {
                // 何もしない
            }
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestSign(object sender, EventArgs e)
    {
        try
        {
            var password = Password.Text;
            var (resultType, result) = await RequestViewModel.Accept(password);
            await Application.Current.MainPage.Navigation.PopModalAsync();
        
            if (resultType == ResultType.Callback)
            {
                await Launcher.OpenAsync(new Uri(result));
            }
            else
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(new ShowPage("署名データ", result));
            }

            await AccountViewModel.DeletePassword();
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
    
    private async void OnChangeAccount(object sender, EventArgs e)
    {
        try
        {
            var accName = await DisplayActionSheet("アカウント切り替え", "cancel", null, AccountViewModel.AccountNames);
            if (accName is null or "cancel")
            {
                return;
            }
            var address = AccountViewModel.Accounts.accounts.ToList().Find(a=>a.accountName == accName).address;
            await AccountViewModel.ChangeMainAccount(address);
            RequestViewModel.SetMainAccountSignerPublicKey();
            Password.Text = "";
            Ask.Text = $"{AccountViewModel.MainAccount.accountName}で署名しますか？";
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
    
    // 署名を拒否したときに呼び出される
    private async void RejectedRequestSign(object sender, EventArgs e)
    {
        try
        {
            var (isCallBack, result) = RequestViewModel.Reject();
            if (isCallBack)
            {
                await Launcher.OpenAsync(new Uri(result));
            }
            await Navigation.PopModalAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
}