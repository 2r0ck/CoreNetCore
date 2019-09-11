using CoreNetCore.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CoreNetCore.MQ
{
    public class AppId : IAppId
    {
        public const string DEFAULT_UUID_FILE_NAME = "selfUUID.txt";
        public const string CONFIG_KEY_UUID_FILE_NAME = "UUID_FILE_NAME";

        private string _currentUID;

        private IConfiguration configuration;

        public AppId(IConfiguration _configuration)
        {
            configuration = _configuration;
        }

        public string CurrentUID
        {
            get
            {
                if (string.IsNullOrEmpty(_currentUID))
                {
                    _currentUID = GetUID();
                }
                return _currentUID;
            }
        }      

        //APPID
        private string GetUID()
        {
            var uid = string.Empty;
            var filename = configuration.GetStrValue(AppId.CONFIG_KEY_UUID_FILE_NAME);

            if (string.IsNullOrEmpty(filename))
            {
                filename = AppId.DEFAULT_UUID_FILE_NAME;
            }

            if (File.Exists(filename))
            {
                uid = File.ReadAllText(filename);
            }
            else
            {
                uid = Guid.NewGuid().ToString();
                File.WriteAllText(filename, uid, Encoding.UTF8);
                Trace.TraceInformation($"Write new appid to file [{filename}]");
            }
            return uid;
        }
    }
}