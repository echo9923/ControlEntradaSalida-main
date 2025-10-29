using System.Configuration;

namespace ControlEntradaSalida.Configuration
{
    /// <summary>
    /// 表示自定义设备配置节，用于在App.config中定义门禁设备连接信息。
    /// </summary>
    public sealed class DeviceSettingsSection : ConfigurationSection
    {
        private const string DevicesCollectionName = "devices";

        [ConfigurationProperty(DevicesCollectionName, IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(DeviceElementCollection),
            AddItemName = "device", ClearItemsName = "clear", RemoveItemName = "remove")]
        public DeviceElementCollection Devices => (DeviceElementCollection)this[DevicesCollectionName];
    }

    /// <summary>
    /// 设备配置元素集合。
    /// </summary>
    public sealed class DeviceElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DeviceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DeviceElement)element).Id;
        }
    }

    /// <summary>
    /// 设备配置元素。
    /// </summary>
    public sealed class DeviceElement : ConfigurationElement
    {
        private const string IdPropertyName = "id";
        private const string NamePropertyName = "name";
        private const string IpAddressPropertyName = "ipAddress";
        private const string PortPropertyName = "port";
        private const string UsernamePropertyName = "username";
        private const string PasswordPropertyName = "password";
        private const string EnabledPropertyName = "enabled";

        [ConfigurationProperty(IdPropertyName, IsRequired = true)]
        public int Id
        {
            get => (int)this[IdPropertyName];
            set => this[IdPropertyName] = value;
        }

        [ConfigurationProperty(NamePropertyName, IsRequired = true)]
        public string Name
        {
            get => (string)this[NamePropertyName];
            set => this[NamePropertyName] = value;
        }

        [ConfigurationProperty(IpAddressPropertyName, IsRequired = true)]
        public string IpAddress
        {
            get => (string)this[IpAddressPropertyName];
            set => this[IpAddressPropertyName] = value;
        }

        [ConfigurationProperty(PortPropertyName, IsRequired = true)]
        public string Port
        {
            get => (string)this[PortPropertyName];
            set => this[PortPropertyName] = value;
        }

        [ConfigurationProperty(UsernamePropertyName, IsRequired = true)]
        public string Username
        {
            get => (string)this[UsernamePropertyName];
            set => this[UsernamePropertyName] = value;
        }

        [ConfigurationProperty(PasswordPropertyName, IsRequired = true)]
        public string Password
        {
            get => (string)this[PasswordPropertyName];
            set => this[PasswordPropertyName] = value;
        }

        [ConfigurationProperty(EnabledPropertyName, DefaultValue = true, IsRequired = false)]
        public bool Enabled
        {
            get => (bool)this[EnabledPropertyName];
            set => this[EnabledPropertyName] = value;
        }
    }
}
