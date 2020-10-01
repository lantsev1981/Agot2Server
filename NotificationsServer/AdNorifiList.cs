using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using MyLibrary;

namespace Notifications
{
    class AdNorifiList
    {
        public List<Notifi> Value
        {
            get
            {
                UpdateValue(); return _Value.Select(p => p.Value).ToList();
            }
        }

        string adPath;
        Dictionary<string, Notifi> _Value = new Dictionary<string, Notifi>();

        public AdNorifiList(string dirName)
        {
            adPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dirName);
            Directory.CreateDirectory(adPath);

            UpdateValue();
        }

        private void UpdateValue()
        {
            var adList = Directory.GetFiles(adPath, "*.html", SearchOption.AllDirectories);
            foreach (var item in _Value.ToList())
            {
                if (!adList.Any(p => p == item.Key))
                    _Value.Remove(item.Key);
            }

            foreach (var item in adList)
                LoadAd(item);
        }

        private void LoadAd(string fileName)
        {
            if (_Value.ContainsKey(fileName))
                return;

            var configPath = string.Format("{0}.config", fileName);
            if (!File.Exists(configPath))
                return;

            Notifi adNotifi = new Notifi();
            adNotifi.Content = File.ReadAllText(fileName);
            PublicFileJson<NotifiSettings> notifiSettings = new PublicFileJson<NotifiSettings>(configPath);
            adNotifi.Settings = notifiSettings.Read();
            if (adNotifi.Settings == null)
                return;

            _Value.Add(fileName, adNotifi);
        }
    }
}
