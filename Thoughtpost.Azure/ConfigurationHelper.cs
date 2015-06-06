using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using System.Configuration;

using Microsoft.WindowsAzure;
using Microsoft.Azure;

namespace Thoughtpost.Azure
{
    class ConfigurationHelper
    {
        public static string GetSetting(string key)
        {
            try
            {
                string ret = CloudConfigurationManager.GetSetting(key);

                if (string.IsNullOrEmpty(ret))
                {
                    throw new InvalidOperationException("No cloud setting available for '" + key + "'.");
                }

                return ret;
            }
            catch (Exception ex)
            {
                try
                {
                    Trace.WriteLine(ex.Message);

                    return ConfigurationManager.AppSettings[key];
                }
                catch (Exception ex2)
                {
                    Trace.WriteLine(ex2.Message);

                    throw new InvalidOperationException("The key '" + key + "' could not be found in configuration settings.");
                }
            }

            throw new InvalidOperationException("The key '" + key + "' could not be found in configuration settings.");
        }

        public static string GetSetting(string key, string defaultValue)
        {
            try
            {
                string ret = CloudConfigurationManager.GetSetting(key);

                if (string.IsNullOrEmpty(ret))
                {
                    throw new InvalidOperationException("No cloud setting available for '" + key + "'.");
                }

                return ret;
            }
            catch (Exception ex)
            {
                try
                {
                    Trace.WriteLine(ex.Message);

                    return ConfigurationManager.AppSettings[key];
                }
                catch (Exception ex2)
                {
                    Trace.WriteLine(ex2.Message);

                    return defaultValue;
                }
            }

        }
    }
}
