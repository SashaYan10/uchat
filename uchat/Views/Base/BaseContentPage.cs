using CommunityToolkit.Mvvm.ComponentModel;

namespace uchat.Views.Base;

public abstract class BaseContentPage<TViewModel> : ContentPage where TViewModel : ObservableObject
{
    protected BaseContentPage(TViewModel ViewModel)
    {
        base.BindingContext = ViewModel;
    }

    protected new TViewModel BindingContext => (TViewModel)base.BindingContext;
}