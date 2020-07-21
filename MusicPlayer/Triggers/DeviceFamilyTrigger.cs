using System;

using Windows.System.Profile;
using Windows.UI.Xaml;

namespace MusicPlayer.Triggers
{
    internal class DeviceFamilyTrigger : StateTriggerBase
    {
        private string _currentDeviceFamily, _queriedDeviceFamily;

        public string DeviceFamily
        {
            get
            {
                return this._queriedDeviceFamily;
            }

            set
            {
                this._queriedDeviceFamily = value;
                this._currentDeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;
                this.SetActive(this._queriedDeviceFamily == this._currentDeviceFamily);
            }
        }
    }

}
