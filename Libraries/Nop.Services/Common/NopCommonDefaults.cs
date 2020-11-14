﻿using Nop.Core.Caching;

namespace Nop.Services.Common
{
    /// <summary>
    /// Represents default values related to common services
    /// </summary>
    public static partial class NopCommonDefaults
    {
        /// <summary>
        /// Gets a request path to the keep alive URL
        /// </summary>
        public static string KeepAlivePath => "keepalive/index";

        #region Address attributes

        /// <summary>
        /// Gets a name of the custom address attribute control
        /// </summary>
        /// <remarks>
        /// {0} : address attribute id
        /// </remarks>
        public static string AddressAttributeControlName => "address_attribute_{0}";

        #endregion

        #region Maintenance

        /// <summary>
        /// Gets a default timeout (in milliseconds) before restarting the application
        /// </summary>
        public static int RestartTimeout => 3000;

        /// <summary>
        /// Gets a path to the database backup files
        /// </summary>
        public static string DbBackupsPath => "db_backups\\";

        /// <summary>
        /// Gets a database backup file extension
        /// </summary>
        public static string DbBackupFileExtension => "bak";

        #endregion

        #region Favicon and app icons

        /// <summary>
        /// Gets a name of the file with code for the head element
        /// </summary>
        public static string HeadCodeFileName => "html_code.html";

        /// <summary>
        ///  Gets a head link for the favicon
        /// </summary>
        public static string SingleFaviconHeadLink => "<link rel=\"shortcut icon\" href=\"\\icons\\icons_{0}\\{1}\">";

        /// <summary>
        /// Gets a path to the favicon and app icons
        /// </summary>
        public static string FaviconAndAppIconsPath => "icons\\icons_{0}";

        /// <summary>
        /// Gets a name of the old favicon icon for current store
        /// </summary>
        public static string OldFaviconIconName => "favicon-{0}.ico";

        #endregion

        #region Localization client-side validation

        /// <summary>
        /// Gets a path to the localization client-side validation 
        /// </summary>
        public static string LocalePatternPath => "lib\\cldr-data\\main\\{0}";

        /// <summary>
        /// Gets a name of the archive with localization of templates
        /// </summary>
        public static string LocalePatternArchiveName => "main.zip";

        /// <summary>
        /// Gets a name of the default pattern locale
        /// </summary>
        public static string DefaultLocalePattern => "en";

        #endregion

        #region nopCommerce official site

        /// <summary>
        /// Gets a path to request the nopCommerce official site for copyright warning
        /// </summary>
        /// <remarks>
        /// {0} : store URL
        /// {1} : whether the store based is on the localhost
        /// {2} : language code
        /// </remarks>
        public static string NopCopyrightWarningPath => "site-warnings?url={0}&local={1}&language={2}";

        /// <summary>
        /// Gets a path to request the nopCommerce official site for news RSS
        /// </summary>
        /// <remarks>
        /// {0} : nopCommerce version
        /// {1} : whether the store based is on the localhost
        /// {2} : whether advertisements are hidden
        /// {3} : store URL
        /// {4} : language code
        /// </remarks>
        public static string NopNewsRssPath => "nopcommerce-news-rss?version={0}&localhost={1}&hideAdvertisements={2}&storeUrl={3}&language={4}";

        /// <summary>
        /// Gets a path to request the nopCommerce official site to notify about successful installation
        /// </summary>
        /// <remarks>
        /// {0} : nopCommerce version
        /// {1} : whether the store based is on the localhost
        /// {2} : admin email
        /// {3} : store URL
        /// {4} : language code
        /// </remarks>
        public static string NopInstallationCompletedPath => "installation-completed?version={0}&local={1}&email={2}&url={3}&language={4}";

        /// <summary>
        /// Gets a path to request the nopCommerce official site for available categories of marketplace extensions
        /// </summary>
        /// <remarks>
        /// {0} : language code
        /// </remarks>
        public static string NopExtensionsCategoriesPath => "extensions-feed?getCategories=1&language={0}";

        /// <summary>
        /// Gets a path to request the nopCommerce official site for available versions of marketplace extensions
        /// </summary>
        /// <remarks>
        /// {0} : language code
        /// </remarks>
        public static string NopExtensionsVersionsPath => "extensions-feed?getVersions=1&language={0}";

        /// <summary>
        /// Gets a path to request the nopCommerce official site for marketplace extensions
        /// </summary>
        /// <remarks>
        /// {0} : extension category identifier
        /// {1} : extension version identifier
        /// {2} : extension price identifier
        /// {3} : search term
        /// {4} : page index
        /// {5} : page size
        /// {6} : language code
        /// </remarks>
        public static string NopExtensionsPath => "extensions-feed?category={0}&version={1}&price={2}&searchTerm={3}&pageIndex={4}&pageSize={5}&language={6}";

        #endregion

        #region Caching defaults

        #region Address attributes

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        public static CacheKey AddressAttributesAllCacheKey => new CacheKey("Nop.addressattribute.all");

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : address attribute ID
        /// </remarks>
        public static CacheKey AddressAttributeValuesAllCacheKey => new CacheKey("Nop.addressattributevalue.all-{0}");

        #endregion

        #region Generic attributes

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : entity ID
        /// {1} : key group
        /// </remarks>
        public static CacheKey GenericAttributeCacheKey => new CacheKey("Nop.genericattribute.{0}-{1}");

        #endregion

        #endregion
    }
}