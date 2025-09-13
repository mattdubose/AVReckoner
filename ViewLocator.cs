using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AvReckoner.ViewModels;
using AvReckoner.Views;
using Reckoner.Views;
using System;

namespace AvReckoner
{
    public class ViewLocator : IDataTemplate
    {
        private readonly Dictionary<Type, Type> _viewModelToViewMap = new()
        {
            { typeof(Page1ViewModel), typeof(Page1View) },
            { typeof(Page2ViewModel), typeof(Page2View) },
            { typeof(Page3ViewModel), typeof(Page3View) },
            { typeof(ClientWelcomeViewModel), typeof(ClientWelcomePage) }
        };
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            var viewModelType = param.GetType();
            if (_viewModelToViewMap.TryGetValue(viewModelType, out var viewType))
            {
                var view = Activator.CreateInstance(viewType) as Control;
                if (view != null)
                {
                    view.DataContext = param;
                    return view;
                }
            }

            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is PurpleValley.UIFramework.BaseViewModel;
        }
    }
}
