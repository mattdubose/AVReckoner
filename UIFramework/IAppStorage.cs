using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurpleValley.UIFramework
{
    public interface IAppStorage
    {
        Task SaveAsync(string key, string value);
        Task<string?> LoadAsync(string key);
    }

}
