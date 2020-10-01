using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace GameService
{
    /// <summary>
    /// Описывает область настроек приложения в его конфигурационном файле
    /// </summary>
    public class ConfigSettings : ConfigurationSection
    {
        [ConfigurationProperty("ServerAddress", IsRequired = true, DefaultValue = "localhost")]
        public string ServerAddress
        {
            get
            {
                return this["ServerAddress"] as string;
            }
            set
            {
                this["ServerAddress"] = value;
            }
        }

        [ConfigurationProperty("ServerPort", IsRequired = true, DefaultValue = "6666")]
        public string ServerPort
        {
            get
            {
                return this["ServerPort"] as string;
            }
            set
            {
                this["ServerPort"] = value;
            }
        }
    }
}
