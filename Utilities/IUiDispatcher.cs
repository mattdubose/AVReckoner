using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurpleValley.Utilities
{
    public interface IUiThreadDispatcher
    {
        Task ExecuteOnMainThreadAsync(System.Action action);
    }

    public class AvaloniaMainThreadDispatcher : IUiThreadDispatcher
    {
        public async Task ExecuteOnMainThreadAsync(System.Action action)
        {
            await Dispatcher.UIThread.InvokeAsync(action);
        }
    }
}
