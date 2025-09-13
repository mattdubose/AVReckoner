using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurpleValley.UIFramework
{
    public interface IDialogService
    {
        Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
        Task AlertAsync(string title, string message, string ok = "OK");
    }
}
