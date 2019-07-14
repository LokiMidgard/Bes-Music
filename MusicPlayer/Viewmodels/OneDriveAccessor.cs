using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Viewmodels
{
    public class OneDriveAccessor
    {
        public OneDriveLibrary Instance => OneDriveLibrary.Instance;

        public event Func<string, Task<bool>> OnAskForPermission
        {
            add =>
                this.Instance.OnAskForPermission += value;
            remove =>
                this.Instance.OnAskForPermission -= value;
        }

    }

}
