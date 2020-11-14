﻿using Nop.Core.Caching;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Configuration;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Polls;
using Nop.Core.Domain.Topics;
using Nop.Core.Domain.Vendors;
using Nop.Core.Events;
using Nop.Services.Cms;
using Nop.Services.Events;
using Nop.Services.Plugins;

namespace Nop.Web.Infrastructure.Cache
{
    /// <summary>
    /// Model cache event consumer (used for caching of presentation layer models)
    /// </summary>
    public partial class ModelCacheEventConsumer :
        //languages
        IConsumer<EntityInsertedEvent<Language>>,
        IConsumer<EntityUpdatedEvent<Language>>,
        IConsumer<EntityDeletedEvent<Language>>,
        //settings
        IConsumer<EntityUpdatedEvent<Setting>>,
        //manufacturers
        IConsumer<EntityInsertedEvent<Manufacturer>>,
        IConsumer<EntityUpdatedEvent<Manufacturer>>,
        IConsumer<EntityDeletedEvent<Manufacturer>>,
        //vendors
        IConsumer<EntityInsertedEvent<Vendor>>,
        IConsumer<EntityUpdatedEvent<Vendor>>,
        IConsumer<EntityDeletedEvent<Vendor>>,
        //product manufacturers
        IConsumer<EntityInsertedEvent<ProductManufacturer>>,
        IConsumer<EntityUpdatedEvent<ProductManufacturer>>,
        IConsumer<EntityDeletedEvent<ProductManufacturer>>,
        //categories
        IConsumer<EntityInsertedEvent<Category>>,
        IConsumer<EntityUpdatedEvent<Category>>,
        IConsumer<EntityDeletedEvent<Category>>,
        //product categories
        IConsumer<EntityInsertedEvent<ProductCategory>>,
        IConsumer<EntityUpdatedEvent<ProductCategory>>,
        IConsumer<EntityDeletedEvent<ProductCategory>>,
        //products
        IConsumer<EntityInsertedEvent<Product>>,
        IConsumer<EntityUpdatedEvent<Product>>,
        IConsumer<EntityDeletedEvent<Product>>,
        //product tags
        IConsumer<EntityInsertedEvent<ProductTag>>,
        IConsumer<EntityUpdatedEvent<ProductTag>>,
        IConsumer<EntityDeletedEvent<ProductTag>>,
        //specification attributes
        IConsumer<EntityUpdatedEvent<SpecificationAttribute>>,
        IConsumer<EntityDeletedEvent<SpecificationAttribute>>,
        //specification attribute options
        IConsumer<EntityUpdatedEvent<SpecificationAttributeOption>>,
        IConsumer<EntityDeletedEvent<SpecificationAttributeOption>>,
        //Product specification attribute
        IConsumer<EntityInsertedEvent<ProductSpecificationAttribute>>,
        IConsumer<EntityUpdatedEvent<ProductSpecificationAttribute>>,
        IConsumer<EntityDeletedEvent<ProductSpecificationAttribute>>,
        //Product attribute values
        IConsumer<EntityUpdatedEvent<ProductAttributeValue>>,
        //Topics
        IConsumer<EntityInsertedEvent<Topic>>,
        IConsumer<EntityUpdatedEvent<Topic>>,
        IConsumer<EntityDeletedEvent<Topic>>,
        //Orders
        IConsumer<EntityInsertedEvent<Order>>,
        IConsumer<EntityUpdatedEvent<Order>>,
        IConsumer<EntityDeletedEvent<Order>>,
        //Picture
        IConsumer<EntityInsertedEvent<Picture>>,
        IConsumer<EntityUpdatedEvent<Picture>>,
        IConsumer<EntityDeletedEvent<Picture>>,
        //Product picture mapping
        IConsumer<EntityInsertedEvent<ProductPicture>>,
        IConsumer<EntityUpdatedEvent<ProductPicture>>,
        IConsumer<EntityDeletedEvent<ProductPicture>>,
        //Product review
        IConsumer<EntityDeletedEvent<ProductReview>>,
        //polls
        IConsumer<EntityInsertedEvent<Poll>>,
        IConsumer<EntityUpdatedEvent<Poll>>,
        IConsumer<EntityDeletedEvent<Poll>>,
        //blog posts
        IConsumer<EntityInsertedEvent<BlogPost>>,
        IConsumer<EntityUpdatedEvent<BlogPost>>,
        IConsumer<EntityDeletedEvent<BlogPost>>,
        //news items
        IConsumer<EntityInsertedEvent<NewsItem>>,
        IConsumer<EntityUpdatedEvent<NewsItem>>,
        IConsumer<EntityDeletedEvent<NewsItem>>,
        //shopping cart items
        IConsumer<EntityUpdatedEvent<ShoppingCartItem>>,
        //plugins
        IConsumer<PluginUpdatedEvent>
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly IStaticCacheManager _staticCacheManager;

        #endregion

        #region Ctor

        public ModelCacheEventConsumer(CatalogSettings catalogSettings, IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Methods

        #region Languages

        public void HandleEvent(EntityInsertedEvent<Language> eventMessage)
        {
            //clear all localizable models
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerNavigationPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Language> eventMessage)
        {
            //clear all localizable models
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerNavigationPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<Language> eventMessage)
        {
            //clear all localizable models
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerNavigationPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
        }

        #endregion
        
        #region Setting

        public void HandleEvent(EntityUpdatedEvent<Setting> eventMessage)
        {
            //clear models which depend on settings
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerNavigationPrefixCacheKey); //depends on CatalogSettings.ManufacturersBlockItemsToDisplay
            _staticCacheManager.Remove(NopModelCacheDefaults.VendorNavigationModelKey); //depends on VendorSettings.VendorBlockItemsToDisplay
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey); //depends on CatalogSettings.ShowCategoryProductNumber and CatalogSettings.ShowCategoryProductNumberIncludingSubcategories
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.HomepageBestsellersIdsPrefixCacheKey); //depends on CatalogSettings.NumberOfBestsellersOnHomepage
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductsAlsoPurchasedIdsPrefixCacheKey); //depends on CatalogSettings.ProductsAlsoPurchasedNumber
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.BlogPrefixCacheKey); //depends on BlogSettings.NumberOfTags
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.NewsPrefixCacheKey); //depends on NewsSettings.MainPageNewsCount
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey); //depends on distinct sitemap settings
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.WidgetPrefixCacheKey); //depends on WidgetSettings and certain settings of widgets
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.StoreLogoPathPrefixCacheKey); //depends on StoreInformationSettings.LogoPictureId
        }

        #endregion

        #region Vendors

        public void HandleEvent(EntityInsertedEvent<Vendor> eventMessage)
        {
            _staticCacheManager.Remove(NopModelCacheDefaults.VendorNavigationModelKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Vendor> eventMessage)
        {
            _staticCacheManager.Remove(NopModelCacheDefaults.VendorNavigationModelKey);
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.VendorPicturePrefixCacheKeyById, eventMessage.Entity.Id));
        }

        public void HandleEvent(EntityDeletedEvent<Vendor> eventMessage)
        {
            _staticCacheManager.Remove(NopModelCacheDefaults.VendorNavigationModelKey);
        }

        #endregion

        #region  Manufacturers

        public void HandleEvent(EntityInsertedEvent<Manufacturer> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerNavigationPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Manufacturer> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerNavigationPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ManufacturerPicturePrefixCacheKeyById, eventMessage.Entity.Id));
        }

        public void HandleEvent(EntityDeletedEvent<Manufacturer> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerNavigationPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        #endregion

        #region  Product manufacturers

        public void HandleEvent(EntityInsertedEvent<ProductManufacturer> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ManufacturerHasFeaturedProductsPrefixCacheKeyById, eventMessage.Entity.ManufacturerId));
        }

        public void HandleEvent(EntityUpdatedEvent<ProductManufacturer> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ManufacturerHasFeaturedProductsPrefixCacheKeyById, eventMessage.Entity.ManufacturerId));
        }

        public void HandleEvent(EntityDeletedEvent<ProductManufacturer> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ManufacturerHasFeaturedProductsPrefixCacheKeyById, eventMessage.Entity.ManufacturerId));
        }

        #endregion

        #region Categories

        public void HandleEvent(EntityInsertedEvent<Category> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryHomepagePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Category> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryHomepagePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.CategoryPicturePrefixCacheKeyById, eventMessage.Entity.Id));
        }

        public void HandleEvent(EntityDeletedEvent<Category> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryHomepagePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        #endregion

        #region Product categories

        public void HandleEvent(EntityInsertedEvent<ProductCategory> eventMessage)
        {
            if (_catalogSettings.ShowCategoryProductNumber)
            {
                //depends on CatalogSettings.ShowCategoryProductNumber (when enabled)
                //so there's no need to clear this cache in other cases
                _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
                _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
            }

            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.CategoryHasFeaturedProductsPrefixCacheKeyById, eventMessage.Entity.CategoryId));
        }

        public void HandleEvent(EntityUpdatedEvent<ProductCategory> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.CategoryHasFeaturedProductsPrefixCacheKeyById, eventMessage.Entity.CategoryId));
        }

        public void HandleEvent(EntityDeletedEvent<ProductCategory> eventMessage)
        {
            if (_catalogSettings.ShowCategoryProductNumber)
            {
                //depends on CatalogSettings.ShowCategoryProductNumber (when enabled)
                //so there's no need to clear this cache in other cases
                _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryAllPrefixCacheKey);
                _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryXmlAllPrefixCacheKey);
            }

            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.CategoryHasFeaturedProductsPrefixCacheKeyById, eventMessage.Entity.CategoryId));
        }

        #endregion

        #region Products

        public void HandleEvent(EntityInsertedEvent<Product> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Product> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.HomepageBestsellersIdsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductsAlsoPurchasedIdsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductReviewsPrefixCacheKeyById, eventMessage.Entity.Id));
        }

        public void HandleEvent(EntityDeletedEvent<Product> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.HomepageBestsellersIdsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductsAlsoPurchasedIdsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        #endregion

        #region Product tags

        public void HandleEvent(EntityInsertedEvent<ProductTag> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<ProductTag> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<ProductTag> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        #endregion

        #region Specification attributes

        public void HandleEvent(EntityUpdatedEvent<SpecificationAttribute> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<SpecificationAttribute> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
        }

        #endregion

        #region Specification attribute options

        public void HandleEvent(EntityUpdatedEvent<SpecificationAttributeOption> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<SpecificationAttributeOption> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
        }

        #endregion

        #region Product specification attribute

        public void HandleEvent(EntityInsertedEvent<ProductSpecificationAttribute> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<ProductSpecificationAttribute> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<ProductSpecificationAttribute> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SpecsFilterPrefixCacheKey);
        }

        #endregion

        #region Product attributes

        public void HandleEvent(EntityUpdatedEvent<ProductAttributeValue> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributePicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributeImageSquarePicturePrefixCacheKey);
        }

        #endregion

        #region Topics

        public void HandleEvent(EntityInsertedEvent<Topic> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Topic> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<Topic> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        #endregion

        #region Orders

        public void HandleEvent(EntityInsertedEvent<Order> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.HomepageBestsellersIdsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductsAlsoPurchasedIdsPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Order> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.HomepageBestsellersIdsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductsAlsoPurchasedIdsPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<Order> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.HomepageBestsellersIdsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductsAlsoPurchasedIdsPrefixCacheKey);
        }

        #endregion

        #region Pictures

        public void HandleEvent(EntityInsertedEvent<Picture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributePicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CartPicturePrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Picture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributePicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CartPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductDetailsPicturesPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductDefaultPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.VendorPicturePrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<Picture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributePicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CartPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductDetailsPicturesPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductDefaultPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CategoryPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ManufacturerPicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.VendorPicturePrefixCacheKey);
        }

        #endregion

        #region Product picture mappings

        public void HandleEvent(EntityInsertedEvent<ProductPicture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductDefaultPicturePrefixCacheKeyById, eventMessage.Entity.ProductId));
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductDetailsPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributePicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CartPicturePrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<ProductPicture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductDefaultPicturePrefixCacheKeyById, eventMessage.Entity.ProductId));
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductDetailsPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributePicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CartPicturePrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<ProductPicture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductDefaultPicturePrefixCacheKeyById, eventMessage.Entity.ProductId));
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductDetailsPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.ProductAttributePicturePrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CartPicturePrefixCacheKey);
        }

        #endregion

        #region Polls

        public void HandleEvent(EntityInsertedEvent<Poll> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.PollsPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<Poll> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.PollsPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<Poll> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.PollsPrefixCacheKey);
        }

        #endregion

        #region Blog posts

        public void HandleEvent(EntityInsertedEvent<BlogPost> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.BlogPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<BlogPost> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.BlogPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<BlogPost> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.BlogPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        #endregion

        #region News items

        public void HandleEvent(EntityInsertedEvent<NewsItem> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.NewsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityUpdatedEvent<NewsItem> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.NewsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        public void HandleEvent(EntityDeletedEvent<NewsItem> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.NewsPrefixCacheKey);
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.SitemapPrefixCacheKey);
        }

        #endregion
        
        #region Shopping cart items

        public void HandleEvent(EntityUpdatedEvent<ShoppingCartItem> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.CartPicturePrefixCacheKey);
        }

        #endregion

        #region Product reviews

        public void HandleEvent(EntityDeletedEvent<ProductReview> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(NopModelCacheDefaults.ProductReviewsPrefixCacheKeyById, eventMessage.Entity.ProductId));
        }

        #endregion

        #region Plugin

        /// <summary>
        /// Handle plugin updated event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public void HandleEvent(PluginUpdatedEvent eventMessage)
        {
            if (eventMessage?.Plugin?.Instance<IWidgetPlugin>() != null)
                _staticCacheManager.RemoveByPrefix(NopModelCacheDefaults.WidgetPrefixCacheKey);
        }

        #endregion

        #endregion
    }
}