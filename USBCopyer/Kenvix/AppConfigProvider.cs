using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Configuration;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace Kenvix
{
    public class AppConfigProvider : SettingsProvider
    {
        const string SettingsRootNode = "Settings";     // XML Root Node
        const bool SkipRoamingCheck = true;             //if true, all settings will be forcely marked as Roaming 

        public string AppSettingsPath => USBCopyer.Host.confdir; //Use application path

        public string AppSettingsFilename => "Config.xml";

        private XmlDocument _settingsXML;

        public override void Initialize(string _, NameValueCollection col)
            => base.Initialize(ApplicationName, col);

        private string _applicationName = Application.ProductName;
        public override string ApplicationName { get => _applicationName; set => _applicationName = value; }


        /// <summary>
        /// Iterate through the settings to be stored
        /// Only dirty settings are included in propvals, and only ones relevant to this provider
        /// </summary>
        /// <param name="context"></param>
        /// <param name="propvals"></param>
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection propvals)
        {
            foreach (var propvalObj in propvals)
            {
                SettingsPropertyValue propval = propvalObj as SettingsPropertyValue;
                SetValue(propval);
            }

            SettingsXML.Save(Path.Combine(AppSettingsPath, AppSettingsFilename));
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
        {
            // Create new collection of values
            var values = new SettingsPropertyValueCollection();

            // Iterate through the settings to be retrieved
            foreach (var settingObj in props)
            {
                var setting = settingObj as SettingsProperty;
                var value = new SettingsPropertyValue(setting)
                {
                    IsDirty = false,
                    SerializedValue = GetValue(setting)
                };
                values.Add(value);
            }
            return values;
        }



        private XmlDocument SettingsXML
        {
            get
            {
                // If we dont hold an xml document, try opening one.  
                // If it doesnt exist then create a new one ready.
                if (_settingsXML is null)
                {
                    _settingsXML = new XmlDocument();

                    try
                    {
                        _settingsXML.Load(Path.Combine(AppSettingsPath, AppSettingsFilename));
                    }
                    catch (Exception)
                    {
                        // Create new document
                        var dec = _settingsXML.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                        var nodeRoot = _settingsXML.CreateNode(XmlNodeType.Element, SettingsRootNode, string.Empty);

                        _settingsXML.AppendChild(dec);
                        _settingsXML.AppendChild(nodeRoot);

                    }
                }

                return _settingsXML;
            }
        }



        private string GetValue(SettingsProperty setting)
        {
            string ret;

            try
            {
                if (IsRoaming(setting))
                    ret = SettingsXML.SelectSingleNode($"{SettingsRootNode}/{setting.Name}")?.InnerText ?? string.Empty;
                else
                    ret = SettingsXML.SelectSingleNode($"{SettingsRootNode}/{Environment.MachineName}/{setting.Name}")?.InnerText ?? string.Empty;
            }
            catch (Exception)
            {
                if (setting.DefaultValue != null)
                    ret = setting.DefaultValue.ToString();
                else
                    ret = string.Empty;
            }

            return ret;
        }

        private void SetValue(SettingsPropertyValue propVal)
        {
            XmlElement machineNode;
            XmlElement settingNode;

            // Determine if the setting is roaming.
            // If roaming then the value is stored as an element under the root
            // Otherwise it is stored under a machine name node 
            try
            {
                if (IsRoaming(propVal.Property))
                    settingNode = (XmlElement)SettingsXML.SelectSingleNode(SettingsRootNode + "/" + propVal.Name);
                else
                    settingNode = (XmlElement)SettingsXML.SelectSingleNode(SettingsRootNode + "/" + Environment.MachineName + "/" + propVal.Name);
            }
            catch (Exception)
            {
                settingNode = null;
            }

            // Check to see if the node exists, if so then set its new value
            if (settingNode != null)
                settingNode.InnerText = propVal.SerializedValue.ToString();
            else if (IsRoaming(propVal.Property))
            {
                // Store the value as an element of the Settings Root Node
                settingNode = SettingsXML.CreateElement(propVal.Name);
                settingNode.InnerText = propVal.SerializedValue.ToString();
                SettingsXML.SelectSingleNode(SettingsRootNode).AppendChild(settingNode);
            }
            else
            {
                // Its machine specific, store as an element of the machine name node,
                // creating a new machine name node if one doesnt exist.
                try
                {
                    machineNode = (XmlElement)SettingsXML.SelectSingleNode(SettingsRootNode + "/" + Environment.MachineName);
                }
                catch (Exception)
                {
                    machineNode = SettingsXML.CreateElement(Environment.MachineName);
                    SettingsXML.SelectSingleNode(SettingsRootNode).AppendChild(machineNode);
                }

                if (machineNode == null)
                {
                    machineNode = SettingsXML.CreateElement(Environment.MachineName);
                    SettingsXML.SelectSingleNode(SettingsRootNode).AppendChild(machineNode);
                }

                settingNode = SettingsXML.CreateElement(propVal.Name);
                settingNode.InnerText = propVal.SerializedValue.ToString();
                machineNode.AppendChild(settingNode);
            }
        }

        /// <summary>
        /// Determine if the setting is marked as Roaming
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        private bool IsRoaming(SettingsProperty prop)
        {
            if (SkipRoamingCheck) return true;
            foreach (DictionaryEntry d in prop.Attributes)
            {
                Attribute a = (Attribute)d.Value;
                if (a is SettingsManageabilityAttribute)
                    return true;
            }
            return false;
        }
    }

}