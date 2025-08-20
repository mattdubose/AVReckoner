using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Utilities
{
  public static class PlatformUtils
  {
    public static bool IsMauiBuild =>
#if ANDROID || IOS || WINDOWS || MACCATALYST
        true;
#else
        false;
#endif
  }
}
