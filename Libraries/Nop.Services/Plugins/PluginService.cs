﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Data.Migrations;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;

namespace Nop.Services.Plugins
{
    /// <summary>
    /// Represents the plugin service implementation
    /// </summary>
    public partial class PluginService : IPluginService
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly ICustomerService _customerService;
        private readonly IMigrationManager _migrationManager;
        private readonly ILogger _logger;
        private readonly INopFileProvider _fileProvider;
        private readonly IPluginsInfo _pluginsInfo;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public PluginService(CatalogSettings catalogSettings,
            ICustomerService customerService,
            IMigrationManager migrationManager,
            ILogger logger,
            INopFileProvider fileProvider,
            IWebHelper webHelper)
        {
            _catalogSettings = catalogSettings;
            _customerService = customerService;
            _migrationManager = migrationManager;
            _logger = logger;
            _fileProvider = fileProvider;
            _pluginsInfo = Singleton<IPluginsInfo>.Instance;
            _webHelper = webHelper;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Check whether to load the plugin based on the load mode passed
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <returns>Result of check</returns>
        protected virtual bool FilterByLoadMode(PluginDescriptor pluginDescriptor, LoadPluginsMode loadMode)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            return loadMode switch
            {
                LoadPluginsMode.All => true,
                LoadPluginsMode.InstalledOnly => pluginDescriptor.Installed,
                LoadPluginsMode.NotInstalledOnly => !pluginDescriptor.Installed,
                _ => throw new NotSupportedException(nameof(loadMode)),
            };
        }

        /// <summary>
        /// Check whether to load the plugin based on the plugin group passed
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="group">Group name</param>
        /// <returns>Result of check</returns>
        protected virtual bool FilterByPluginGroup(PluginDescriptor pluginDescriptor, string group)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (string.IsNullOrEmpty(group))
                return true;

            return group.Equals(pluginDescriptor.Group, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Check whether to load the plugin based on the customer passed
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="customer">Customer</param>
        /// <returns>Result of check</returns>
        protected virtual bool FilterByCustomer(PluginDescriptor pluginDescriptor, Customer customer)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (customer == null || !pluginDescriptor.LimitedToCustomerRoles.Any())
                return true;

            if (_catalogSettings.IgnoreAcl)
                return true;

            return pluginDescriptor.LimitedToCustomerRoles.Intersect(_customerService.GetCustomerRoleIds(customer)).Any();
        }

        /// <summary>
        /// Check whether to load the plugin based on the store identifier passed
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Result of check</returns>
        protected virtual bool FilterByStore(PluginDescriptor pluginDescriptor, int storeId)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            //no validation required
            if (storeId == 0)
                return true;

            if (!pluginDescriptor.LimitedToStores.Any())
                return true;

            return pluginDescriptor.LimitedToStores.Contains(storeId);
        }

        /// <summary>
        /// Check whether to load the plugin based on dependency from other plugin
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="dependsOnSystemName">Other plugin system name</param>
        /// <returns>Result of check</returns>
        protected virtual bool FilterByDependsOn(PluginDescriptor pluginDescriptor, string dependsOnSystemName)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (string.IsNullOrEmpty(dependsOnSystemName))
                return true;

            return pluginDescriptor.DependsOn?.Contains(dependsOnSystemName) ?? false;
        }

        /// <summary>
        /// Check whether to load the plugin based on the plugin friendly name passed
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="friendlyName">Plugin friendly name</param>
        /// <returns>Result of check</returns>
        protected virtual bool FilterByPluginFriendlyName(PluginDescriptor pluginDescriptor, string friendlyName)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (string.IsNullOrEmpty(friendlyName))
                return true;

            return pluginDescriptor.FriendlyName.Contains(friendlyName, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Check whether to load the plugin based on the plugin author passed
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="author">Plugin author</param>
        /// <returns>Result of check</returns>
        protected virtual bool FilterByPluginAuthor(PluginDescriptor pluginDescriptor, string author)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (string.IsNullOrEmpty(author))
                return true;

            return pluginDescriptor.Author.Contains(author, StringComparison.InvariantCultureIgnoreCase);
        }

        protected virtual void DeletePluginData(Type pluginType)
        {
            var assembly = Assembly.GetAssembly(pluginType);
            _migrationManager.ApplyDownMigrations(assembly);
        }

        protected virtual void InsertPluginData(Type pluginType, bool isUpdateProcess = false)
        {
            var assembly = Assembly.GetAssembly(pluginType);
            _migrationManager.ApplyUpMigrations(assembly, isUpdateProcess);
        }

        #endregion
                 
        #region Methods

        /// <summary>
        /// Get plugin descriptors
        /// </summary>
        /// <typeparam name="TPlugin">The type of plugins to get</typeparam>
        /// <param name="loadMode">Filter by load plugins mode</param>
        /// <param name="customer">Filter by  customer; pass null to load all records</param>
        /// <param name="storeId">Filter by store; pass 0 to load all records</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <param name="friendlyName">Filter by plugin friendly name; pass null to load all records</param>
        /// <param name="author">Filter by plugin author; pass null to load all records</param>
        /// <param name="dependsOnSystemName">System name of the plugin to define dependencies</param>
        /// <returns>Plugin descriptors</returns>
        public virtual IEnumerable<PluginDescriptor> GetPluginDescriptors<TPlugin>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null, string dependsOnSystemName = "", string friendlyName = null, string author = null) where TPlugin : class, IPlugin
        {
            var pluginDescriptors = _pluginsInfo.PluginDescriptors;

            //filter plugins
            pluginDescriptors = pluginDescriptors.Where(descriptor =>
                FilterByLoadMode(descriptor, loadMode) &&
                FilterByCustomer(descriptor, customer) &&
                FilterByStore(descriptor, storeId) &&
                FilterByPluginGroup(descriptor, group) &&
                FilterByDependsOn(descriptor, dependsOnSystemName) &&
                FilterByPluginFriendlyName(descriptor, friendlyName) &&
                FilterByPluginAuthor(descriptor, author)).ToList();

            //filter by the passed type
            if (typeof(TPlugin) != typeof(IPlugin))
                pluginDescriptors = pluginDescriptors.Where(descriptor => typeof(TPlugin).IsAssignableFrom(descriptor.PluginType)).ToList();

            //order by group name
            pluginDescriptors = pluginDescriptors.OrderBy(descriptor => descriptor.Group)
                .ThenBy(descriptor => descriptor.DisplayOrder).ToList();

            return pluginDescriptors;
        }

        /// <summary>
        /// Get a plugin descriptor by the system name
        /// </summary>
        /// <typeparam name="TPlugin">The type of plugin to get</typeparam>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="customer">Filter by  customer; pass null to load all records</param>
        /// <param name="storeId">Filter by store; pass 0 to load all records</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <returns>>Plugin descriptor</returns>
        public virtual PluginDescriptor GetPluginDescriptorBySystemName<TPlugin>(string systemName,
            LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null) where TPlugin : class, IPlugin
        {
            return GetPluginDescriptors<TPlugin>(loadMode, customer, storeId, group)
                .FirstOrDefault(descriptor => descriptor.SystemName.Equals(systemName));
        }

        /// <summary>
        /// Get plugins
        /// </summary>
        /// <typeparam name="TPlugin">The type of plugins to get</typeparam>
        /// <param name="loadMode">Filter by load plugins mode</param>
        /// <param name="customer">Filter by customer; pass null to load all records</param>
        /// <param name="storeId">Filter by store; pass 0 to load all records</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <returns>Plugins</returns>
        public virtual IEnumerable<TPlugin> GetPlugins<TPlugin>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null) where TPlugin : class, IPlugin
        {
            return GetPluginDescriptors<TPlugin>(loadMode, customer, storeId, group)
                .Select(descriptor => descriptor.Instance<TPlugin>());
        }

        /// <summary>
        /// Find a plugin by the type which is located into the same assembly as a plugin
        /// </summary>
        /// <param name="typeInAssembly">Type</param>
        /// <returns>Plugin</returns>
        public virtual IPlugin FindPluginByTypeInAssembly(Type typeInAssembly)
        {
            if (typeInAssembly == null)
                throw new ArgumentNullException(nameof(typeInAssembly));

            //try to do magic
            var pluginDescriptor = _pluginsInfo.PluginDescriptors.FirstOrDefault(descriptor =>
               descriptor.ReferencedAssembly?.FullName.Equals(typeInAssembly.Assembly.FullName, StringComparison.InvariantCultureIgnoreCase) ?? false);

            return pluginDescriptor?.Instance<IPlugin>();
        }

        /// <summary>
        /// Get plugin logo URL
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor</param>
        /// <returns>Logo URL</returns>
        public virtual string GetPluginLogoUrl(PluginDescriptor pluginDescriptor)
        {
            var pluginDirectory = _fileProvider.GetDirectoryName(pluginDescriptor.OriginalAssemblyFile);
            if (string.IsNullOrEmpty(pluginDirectory))
                return null;

            //check for supported extensions
            var logoExtension = NopPluginDefaults.SupportedLogoImageExtensions
                .FirstOrDefault(ext => _fileProvider.FileExists(_fileProvider.Combine(pluginDirectory, $"{NopPluginDefaults.LogoFileName}.{ext}")));
            if (string.IsNullOrWhiteSpace(logoExtension))
                return null;

            var storeLocation = _webHelper.GetStoreLocation();
            var logoUrl = $"{storeLocation}{NopPluginDefaults.PathName}/" +
                $"{_fileProvider.GetDirectoryNameOnly(pluginDirectory)}/{NopPluginDefaults.LogoFileName}.{logoExtension}";

            return logoUrl;
        }

        /// <summary>
        /// Prepare plugin to the installation
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="customer">Customer</param>
        /// <param name="checkDependencies">Specifies whether to check plugin dependencies</param>
        public virtual void PreparePluginToInstall(string systemName, Customer customer = null, bool checkDependencies = true)
        {
            //add plugin name to the appropriate list (if not yet contained) and save changes
            if (_pluginsInfo.PluginNamesToInstall.Any(item => item.SystemName == systemName))
                return;

            var pluginsAfterRestart = _pluginsInfo.InstalledPlugins.Select(pd => pd.SystemName).Where(installedSystemName => !_pluginsInfo.PluginNamesToUninstall.Contains(installedSystemName)).ToList();
            pluginsAfterRestart.AddRange(_pluginsInfo.PluginNamesToInstall.Select(item => item.SystemName));

            if (checkDependencies)
            {
                var descriptor = GetPluginDescriptorBySystemName<IPlugin>(systemName, LoadPluginsMode.NotInstalledOnly);

                if (descriptor.DependsOn?.Any() ?? false)
                {
                    var dependsOn = descriptor.DependsOn
                        .Where(dependsOnSystemName => !pluginsAfterRestart.Contains(dependsOnSystemName)).ToList();

                    if (dependsOn.Any())
                    {
                        var dependsOnSystemNames = dependsOn.Aggregate((all, current) => $"{all}, {current}");

                        //do not inject services via constructor because it'll cause circular references
                        var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

                        var errorMessage = string.Format(localizationService.GetResource("Admin.Plugins.Errors.InstallDependsOn"), string.IsNullOrEmpty(descriptor.FriendlyName) ? descriptor.SystemName : descriptor.FriendlyName, dependsOnSystemNames);

                        throw new NopException(errorMessage);
                    }
                }
            }

            _pluginsInfo.PluginNamesToInstall.Add((systemName, customer?.CustomerGuid));
            _pluginsInfo.Save();
        }

        /// <summary>
        /// Prepare plugin to the uninstallation
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        public virtual void PreparePluginToUninstall(string systemName)
        {
            //add plugin name to the appropriate list (if not yet contained) and save changes
            if (_pluginsInfo.PluginNamesToUninstall.Contains(systemName))
                return;

            var dependentPlugins = GetPluginDescriptors<IPlugin>(dependsOnSystemName: systemName).ToList();
            var descriptor = GetPluginDescriptorBySystemName<IPlugin>(systemName);

            if (dependentPlugins.Any())
            {
                var dependsOn = new List<string>();

                foreach (var dependentPlugin in dependentPlugins)
                {
                    if (!_pluginsInfo.InstalledPlugins.Select(pd => pd.SystemName).Contains(dependentPlugin.SystemName))
                        continue;
                    if (_pluginsInfo.PluginNamesToUninstall.Contains(dependentPlugin.SystemName))
                        continue;

                    dependsOn.Add(string.IsNullOrEmpty(dependentPlugin.FriendlyName)
                        ? dependentPlugin.SystemName
                        : dependentPlugin.FriendlyName);
                }

                if (dependsOn.Any())
                {
                    var dependsOnSystemNames = dependsOn.Aggregate((all, current) => $"{all}, {current}");

                    //do not inject services via constructor because it'll cause circular references
                    var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

                    var errorMessage = string.Format(localizationService.GetResource("Admin.Plugins.Errors.UninstallDependsOn"),
                        string.IsNullOrEmpty(descriptor.FriendlyName) ? descriptor.SystemName : descriptor.FriendlyName,
                        dependsOnSystemNames);

                    throw new NopException(errorMessage);
                }
            }

            var plugin = descriptor?.Instance<IPlugin>();
            plugin?.PreparePluginToUninstall();

            _pluginsInfo.PluginNamesToUninstall.Add(systemName);
            _pluginsInfo.Save();
        }

        /// <summary>
        /// Prepare plugin to the removing
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        public virtual void PreparePluginToDelete(string systemName)
        {
            //add plugin name to the appropriate list (if not yet contained) and save changes
            if (_pluginsInfo.PluginNamesToDelete.Contains(systemName))
                return;

            _pluginsInfo.PluginNamesToDelete.Add(systemName);
            _pluginsInfo.Save();
        }

        /// <summary>
        /// Reset changes
        /// </summary>
        public virtual void ResetChanges()
        {
            //clear lists and save changes
            _pluginsInfo.PluginNamesToDelete.Clear();
            _pluginsInfo.PluginNamesToInstall.Clear();
            _pluginsInfo.PluginNamesToUninstall.Clear();
            _pluginsInfo.Save();

            //display all plugins on the plugin list page
            _pluginsInfo.PluginDescriptors.ToList().ForEach(pluginDescriptor => pluginDescriptor.ShowInPluginsList = true);
        }

        /// <summary>
        /// Clear installed plugins list
        /// </summary>
        public virtual void ClearInstalledPluginsList()
        {
            _pluginsInfo.InstalledPlugins.Clear();
        }

        /// <summary>
        /// Install plugins
        /// </summary>
        public virtual void InstallPlugins()
        {
            //get all uninstalled plugins
            var pluginDescriptors = _pluginsInfo.PluginDescriptors.Where(descriptor => !descriptor.Installed).ToList();

            //filter plugins need to install
            pluginDescriptors = pluginDescriptors.Where(descriptor => _pluginsInfo.PluginNamesToInstall
                .Any(item => item.SystemName.Equals(descriptor.SystemName))).ToList();
            if (!pluginDescriptors.Any())
                return;

            //do not inject services via constructor because it'll cause circular references
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            var customerActivityService = EngineContext.Current.Resolve<ICustomerActivityService>();

            //install plugins
            foreach (var descriptor in pluginDescriptors.OrderBy(pluginDescriptor => pluginDescriptor.DisplayOrder))
            {
                try
                {
                    InsertPluginData(descriptor.PluginType);

                    //try to install an instance
                    descriptor.Instance<IPlugin>().Install();

                    //remove and add plugin system name to appropriate lists
                    var pluginToInstall = _pluginsInfo.PluginNamesToInstall
                        .FirstOrDefault(plugin => plugin.SystemName.Equals(descriptor.SystemName));
                    _pluginsInfo.InstalledPlugins.Add(descriptor.GetBaseInfoCopy);
                    _pluginsInfo.PluginNamesToInstall.Remove(pluginToInstall);

                    //activity log
                    var customer = _customerService.GetCustomerByGuid(pluginToInstall.CustomerGuid ?? Guid.Empty);
                    customerActivityService.InsertActivity(customer, "InstallNewPlugin",
                        string.Format(localizationService.GetResource("ActivityLog.InstallNewPlugin"), descriptor.SystemName));

                    //mark the plugin as installed
                    descriptor.Installed = true;
                    descriptor.ShowInPluginsList = true;
                }
                catch (Exception exception)
                {
                    //log error
                    var message = string.Format(localizationService.GetResource("Admin.Plugins.Errors.NotInstalled"), descriptor.SystemName);
                    _logger.Error(message, exception);
                }
            }

            //save changes
            _pluginsInfo.Save();
        }

        /// <summary>
        /// Uninstall plugins
        /// </summary>
        public virtual void UninstallPlugins()
        {
            //get all installed plugins
            var pluginDescriptors = _pluginsInfo.PluginDescriptors.Where(descriptor => descriptor.Installed).ToList();

            //filter plugins need to uninstall
            pluginDescriptors = pluginDescriptors
                .Where(descriptor => _pluginsInfo.PluginNamesToUninstall.Contains(descriptor.SystemName)).ToList();
            if (!pluginDescriptors.Any())
                return;

            //do not inject services via constructor because it'll cause circular references
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            var customerActivityService = EngineContext.Current.Resolve<ICustomerActivityService>();

            //uninstall plugins
            foreach (var descriptor in pluginDescriptors.OrderByDescending(pluginDescriptor => pluginDescriptor.DisplayOrder))
            {
                try
                {
                    var plugin = descriptor.Instance<IPlugin>();
                    //try to uninstall an instance
                    plugin.Uninstall();

                    //clear plugin data on the database
                    DeletePluginData(descriptor.PluginType);

                    //remove plugin system name from appropriate lists
                    _pluginsInfo.InstalledPlugins.Remove(descriptor);
                    _pluginsInfo.PluginNamesToUninstall.Remove(descriptor.SystemName);

                    //activity log
                    customerActivityService.InsertActivity("UninstallPlugin",
                        string.Format(localizationService.GetResource("ActivityLog.UninstallPlugin"), descriptor.SystemName));

                    //mark the plugin as uninstalled
                    descriptor.Installed = false;
                    descriptor.ShowInPluginsList = true;
                }
                catch (Exception exception)
                {
                    //log error
                    var message = string.Format(localizationService.GetResource("Admin.Plugins.Errors.NotUninstalled"), descriptor.SystemName);
                    _logger.Error(message, exception);
                }
            }

            //save changes
            _pluginsInfo.Save();
        }

        /// <summary>
        /// Delete plugins
        /// </summary>
        public virtual void DeletePlugins()
        {
            //get all uninstalled plugins (delete plugin only previously uninstalled)
            var pluginDescriptors = _pluginsInfo.PluginDescriptors.Where(descriptor => !descriptor.Installed).ToList();

            //filter plugins need to delete
            pluginDescriptors = pluginDescriptors
                .Where(descriptor => _pluginsInfo.PluginNamesToDelete.Contains(descriptor.SystemName)).ToList();
            if (!pluginDescriptors.Any())
                return;

            //do not inject services via constructor because it'll cause circular references
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            var customerActivityService = EngineContext.Current.Resolve<ICustomerActivityService>();

            //delete plugins
            foreach (var descriptor in pluginDescriptors)
            {
                try
                {
                    //try to delete a plugin directory from disk storage
                    var pluginDirectory = _fileProvider.GetDirectoryName(descriptor.OriginalAssemblyFile);
                    if (_fileProvider.DirectoryExists(pluginDirectory))
                        _fileProvider.DeleteDirectory(pluginDirectory);

                    //remove plugin system name from the appropriate list
                    _pluginsInfo.PluginNamesToDelete.Remove(descriptor.SystemName);

                    //activity log
                    customerActivityService.InsertActivity("DeletePlugin",
                        string.Format(localizationService.GetResource("ActivityLog.DeletePlugin"), descriptor.SystemName));
                }
                catch (Exception exception)
                {
                    //log error
                    var message = string.Format(localizationService.GetResource("Admin.Plugins.Errors.NotDeleted"), descriptor.SystemName);
                    _logger.Error(message, exception);
                }
            }

            //save changes
            _pluginsInfo.Save();
        }

        /// <summary>
        /// Check whether application restart is required to apply changes to plugins
        /// </summary>
        /// <returns>Result of check</returns>
        public virtual bool IsRestartRequired()
        {
            //return true if any of lists contains items
            return _pluginsInfo.PluginNamesToInstall.Any()
                || _pluginsInfo.PluginNamesToUninstall.Any()
                || _pluginsInfo.PluginNamesToDelete.Any();
        }

        /// <summary>
        /// Update plugins
        /// </summary>
        public virtual void UpdatePlugins()
        {
            foreach (var installedPlugin in _pluginsInfo.InstalledPlugins)
            {
                var newVersion = _pluginsInfo.PluginDescriptors.FirstOrDefault(pd =>
                    pd.SystemName.Equals(installedPlugin.SystemName, StringComparison.InvariantCultureIgnoreCase));

                if (newVersion == null)
                    continue;

                if (installedPlugin.Version == newVersion.Version)
                    continue;
                
                //run new migrations from the plugin if there are exists
                InsertPluginData(newVersion.PluginType, true);

                //run the plugin update logic
                newVersion.Instance<IPlugin>().Update(installedPlugin.Version, newVersion.Version);

                //update installed plugin info
                installedPlugin.Version = newVersion.Version;
            }

            _pluginsInfo.Save();
        }

        /// <summary>
        /// Get names of incompatible plugins
        /// </summary>
        /// <returns>List of plugin names</returns>
        public virtual IList<string> GetIncompatiblePlugins()
        {
            return _pluginsInfo.IncompatiblePlugins;
        }

        /// <summary>
        /// Get all assembly loaded collisions
        /// </summary>
        /// <returns>List of plugin loaded assembly info</returns>
        public virtual IList<PluginLoadedAssemblyInfo> GetAssemblyCollisions()
        {
            return _pluginsInfo.AssemblyLoadedCollision;
        }

        #endregion
    }
}