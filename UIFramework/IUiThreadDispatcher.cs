using System;

namespace PurpleValley.UIFramework
{
    public interface IUiThreadDispatcher
    {
        void Invoke(System.Action action);
        Task InvokeAsync(Func<Task> action);
        Task ExecuteOnMainThreadAsync(System.Action action);
    }
}
