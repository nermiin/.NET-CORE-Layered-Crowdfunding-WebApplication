﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Services.Caching;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Topics;
using Nop.Services.Vendors;
using Nop.Web.Framework.Events;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

namespace Nop.Web.Factories
{
    public partial class CatalogModelFactory : ICatalogModelFactory
    {
        #region Fields

        private readonly BlogSettings _blogSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly DisplayDefaultMenuItemSettings _displayDefaultMenuItemSettings;
        private readonly ForumSettings _forumSettings;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ICacheKeyService _cacheKeyService;
        private readonly ICategoryService _categoryService;
        private readonly ICategoryTemplateService _categoryTemplateService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IManufacturerTemplateService _manufacturerTemplateService;
        private readonly IPictureService _pictureService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IProductService _productService;
        private readonly IProductTagService _productTagService;
        private readonly ISearchTermService _searchTermService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private readonly ITopicService _topicService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IVendorService _vendorService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly MediaSettings _mediaSettings;
        private readonly VendorSettings _vendorSettings;

        #endregion

        #region Ctor

        public CatalogModelFactory(BlogSettings blogSettings,
            CatalogSettings catalogSettings,
            DisplayDefaultMenuItemSettings displayDefaultMenuItemSettings,
            ForumSettings forumSettings,
            IActionContextAccessor actionContextAccessor,
            ICacheKeyService cacheKeyService,
            ICategoryService categoryService,
            ICategoryTemplateService categoryTemplateService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IManufacturerTemplateService manufacturerTemplateService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IProductTagService productTagService,
            ISearchTermService searchTermService,
            ISpecificationAttributeService specificationAttributeService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            ITopicService topicService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            VendorSettings vendorSettings)
        {
            _blogSettings = blogSettings;
            _catalogSettings = catalogSettings;
            _displayDefaultMenuItemSettings = displayDefaultMenuItemSettings;
            _forumSettings = forumSettings;
            _actionContextAccessor = actionContextAccessor;
            _cacheKeyService = cacheKeyService;
            _categoryService = categoryService;
            _categoryTemplateService = categoryTemplateService;
            _currencyService = currencyService;
            _customerService = customerService;
            _eventPublisher = eventPublisher;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _manufacturerService = manufacturerService;
            _manufacturerTemplateService = manufacturerTemplateService;
            _pictureService = pictureService;
            _priceFormatter = priceFormatter;
            _productModelFactory = productModelFactory;
            _productService = productService;
            _productTagService = productTagService;
            _searchTermService = searchTermService;
            _specificationAttributeService = specificationAttributeService;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
            _topicService = topicService;
            _urlHelperFactory = urlHelperFactory;
            _urlRecordService = urlRecordService;
            _vendorService = vendorService;
            _webHelper = webHelper;
            _workContext = workContext;
            _mediaSettings = mediaSettings;
            _vendorSettings = vendorSettings;
        }

        #endregion

        #region Common

        /// <summary>
        /// Prepare sorting options
        /// </summary>
        /// <param name="pagingFilteringModel">Catalog paging filtering model</param>
        /// <param name="command">Catalog paging filtering command</param>
        public virtual void PrepareSortingOptions(CatalogPagingFilteringModel pagingFilteringModel, CatalogPagingFilteringModel command)
        {
            if (pagingFilteringModel == null)
                throw new ArgumentNullException(nameof(pagingFilteringModel));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            //set the order by position by default
            pagingFilteringModel.OrderBy = command.OrderBy;
            command.OrderBy = (int)ProductSortingEnum.Position;

            //ensure that product sorting is enabled
            if (!_catalogSettings.AllowProductSorting)
                return;

            //get active sorting options
            var activeSortingOptionsIds = Enum.GetValues(typeof(ProductSortingEnum)).Cast<int>()
                .Except(_catalogSettings.ProductSortingEnumDisabled).ToList();
            if (!activeSortingOptionsIds.Any())
                return;

            //order sorting options
            var orderedActiveSortingOptions = activeSortingOptionsIds
                .Select(id => new { Id = id, Order = _catalogSettings.ProductSortingEnumDisplayOrder.TryGetValue(id, out var order) ? order : id })
                .OrderBy(option => option.Order).ToList();

            pagingFilteringModel.AllowProductSorting = true;
            command.OrderBy = pagingFilteringModel.OrderBy ?? orderedActiveSortingOptions.FirstOrDefault().Id;

            //prepare available model sorting options
            var currentPageUrl = _webHelper.GetThisPageUrl(true);
            foreach (var option in orderedActiveSortingOptions)
            {
                pagingFilteringModel.AvailableSortOptions.Add(new SelectListItem
                {
                    Text = _localizationService.GetLocalizedEnum((ProductSortingEnum)option.Id),
                    Value = _webHelper.ModifyQueryString(currentPageUrl, "orderby", option.Id.ToString()),
                    Selected = option.Id == command.OrderBy
                });
            }
        }

        /// <summary>
        /// Prepare view modes
        /// </summary>
        /// <param name="pagingFilteringModel">Catalog paging filtering model</param>
        /// <param name="command">Catalog paging filtering command</param>
        public virtual void PrepareViewModes(CatalogPagingFilteringModel pagingFilteringModel, CatalogPagingFilteringModel command)
        {
            if (pagingFilteringModel == null)
                throw new ArgumentNullException(nameof(pagingFilteringModel));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            pagingFilteringModel.AllowProductViewModeChanging = _catalogSettings.AllowProductViewModeChanging;

            var viewMode = !string.IsNullOrEmpty(command.ViewMode)
                ? command.ViewMode
                : _catalogSettings.DefaultViewMode;
            pagingFilteringModel.ViewMode = viewMode;
            if (pagingFilteringModel.AllowProductViewModeChanging)
            {
                var currentPageUrl = _webHelper.GetThisPageUrl(true);
                //grid
                pagingFilteringModel.AvailableViewModes.Add(new SelectListItem
                {
                    Text = _localizationService.GetResource("Catalog.ViewMode.Grid"),
                    Value = _webHelper.ModifyQueryString(currentPageUrl, "viewmode", "grid"),
                    Selected = viewMode == "grid"
                });
                //list
                pagingFilteringModel.AvailableViewModes.Add(new SelectListItem
                {
                    Text = _localizationService.GetResource("Catalog.ViewMode.List"),
                    Value = _webHelper.ModifyQueryString(currentPageUrl, "viewmode", "list"),
                    Selected = viewMode == "list"
                });
            }
        }

        /// <summary>
        /// Prepare page size options
        /// </summary>
        /// <param name="pagingFilteringModel">Catalog paging filtering model</param>
        /// <param name="command">Catalog paging filtering command</param>
        /// <param name="allowCustomersToSelectPageSize">Are customers allowed to select page size?</param>
        /// <param name="pageSizeOptions">Page size options</param>
        /// <param name="fixedPageSize">Fixed page size</param>
        public virtual void PreparePageSizeOptions(CatalogPagingFilteringModel pagingFilteringModel, CatalogPagingFilteringModel command,
            bool allowCustomersToSelectPageSize, string pageSizeOptions, int fixedPageSize)
        {
            if (pagingFilteringModel == null)
                throw new ArgumentNullException(nameof(pagingFilteringModel));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (command.PageNumber <= 0)
                command.PageNumber = 1;

            pagingFilteringModel.AllowCustomersToSelectPageSize = false;
            if (allowCustomersToSelectPageSize && pageSizeOptions != null)
            {
                var pageSizes = pageSizeOptions.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (pageSizes.Any())
                {
                    // get the first page size entry to use as the default (category page load) or if customer enters invalid value via query string
                    if (command.PageSize <= 0 || !pageSizes.Contains(command.PageSize.ToString()))
                    {
                        if (int.TryParse(pageSizes.FirstOrDefault(), out var temp))
                        {
                            if (temp > 0)
                            {
                                command.PageSize = temp;
                            }
                        }
                    }

                    var currentPageUrl = _webHelper.GetThisPageUrl(true);
                    var sortUrl = _webHelper.RemoveQueryString(currentPageUrl, "pagenumber");

                    foreach (var pageSize in pageSizes)
                    {
                        if (!int.TryParse(pageSize, out var temp))
                            continue;

                        if (temp <= 0)
                            continue;

                        pagingFilteringModel.PageSizeOptions.Add(new SelectListItem
                        {
                            Text = pageSize,
                            Value = _webHelper.ModifyQueryString(sortUrl, "pagesize", pageSize),
                            Selected = pageSize.Equals(command.PageSize.ToString(), StringComparison.InvariantCultureIgnoreCase)
                        });
                    }

                    if (pagingFilteringModel.PageSizeOptions.Any())
                    {
                        pagingFilteringModel.PageSizeOptions = pagingFilteringModel.PageSizeOptions.OrderBy(x => int.Parse(x.Text)).ToList();
                        pagingFilteringModel.AllowCustomersToSelectPageSize = true;

                        if (command.PageSize <= 0)
                        {
                            command.PageSize = int.Parse(pagingFilteringModel.PageSizeOptions.First().Text);
                        }
                    }
                }
            }
            else
            {
                //customer is not allowed to select a page size
                command.PageSize = fixedPageSize;
            }

            //ensure pge size is specified
            if (command.PageSize <= 0)
            {
                command.PageSize = fixedPageSize;
            }
        }

        #endregion

        #region Categories

        /// <summary>
        /// Prepare category model
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="command">Catalog paging filtering command</param>
        /// <returns>Category model</returns>
        public virtual CategoryModel PrepareCategoryModel(Category category, CatalogPagingFilteringModel command)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var model = new CategoryModel
            {
                Id = category.Id,
                Name = _localizationService.GetLocalized(category, x => x.Name),
                Description = _localizationService.GetLocalized(category, x => x.Description),
                MetaKeywords = _localizationService.GetLocalized(category, x => x.MetaKeywords),
                MetaDescription = _localizationService.GetLocalized(category, x => x.MetaDescription),
                MetaTitle = _localizationService.GetLocalized(category, x => x.MetaTitle),
                SeName = _urlRecordService.GetSeName(category),
            };

            //sorting
            PrepareSortingOptions(model.PagingFilteringContext, command);
            //view mode
            PrepareViewModes(model.PagingFilteringContext, command);
            //page size
            PreparePageSizeOptions(model.PagingFilteringContext, command,
                category.AllowCustomersToSelectPageSize,
                category.PageSizeOptions,
                category.PageSize);

            //price ranges
            model.PagingFilteringContext.PriceRangeFilter.LoadPriceRangeFilters(category.PriceRanges, _webHelper, _priceFormatter);
            var selectedPriceRange = model.PagingFilteringContext.PriceRangeFilter.GetSelectedPriceRange(_webHelper, category.PriceRanges);
            decimal? minPriceConverted = null;
            decimal? maxPriceConverted = null;
            if (selectedPriceRange != null)
            {
                if (selectedPriceRange.From.HasValue)
                    minPriceConverted = _currencyService.ConvertToPrimaryStoreCurrency(selectedPriceRange.From.Value, _workContext.WorkingCurrency);

                if (selectedPriceRange.To.HasValue)
                    maxPriceConverted = _currencyService.ConvertToPrimaryStoreCurrency(selectedPriceRange.To.Value, _workContext.WorkingCurrency);
            }

            //category breadcrumb
            if (_catalogSettings.CategoryBreadcrumbEnabled)
            {
                model.DisplayCategoryBreadcrumb = true;

                model.CategoryBreadcrumb = _categoryService.GetCategoryBreadCrumb(category).Select(catBr =>
                    new CategoryModel
                    {
                        Id = catBr.Id,
                        Name = _localizationService.GetLocalized(catBr, x => x.Name),
                        SeName = _urlRecordService.GetSeName(catBr)
                    }).ToList();
            }

            var pictureSize = _mediaSettings.CategoryThumbPictureSize;

            //subcategories
            model.SubCategories = _categoryService.GetAllCategoriesByParentCategoryId(category.Id)
                    .Select(curCategory =>
                    {
                        var subCatModel = new CategoryModel.SubCategoryModel
                        {
                            Id = curCategory.Id,
                            Name = _localizationService.GetLocalized(curCategory, y => y.Name),
                            SeName = _urlRecordService.GetSeName(curCategory),
                            Description = _localizationService.GetLocalized(curCategory, y => y.Description)
                        };

                        //prepare picture model
                        var categoryPictureCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.CategoryPictureModelKey, curCategory,
                            pictureSize, true, _workContext.WorkingLanguage, _webHelper.IsCurrentConnectionSecured(),
                            _storeContext.CurrentStore);

                        subCatModel.PictureModel = _staticCacheManager.Get(categoryPictureCacheKey, () =>
                        {
                            var picture = _pictureService.GetPictureById(curCategory.PictureId);
                            var pictureModel = new PictureModel
                            {
                                FullSizeImageUrl = _pictureService.GetPictureUrl(ref picture),
                                ImageUrl = _pictureService.GetPictureUrl(ref picture, pictureSize),
                                Title = string.Format(
                                    _localizationService.GetResource("Media.Category.ImageLinkTitleFormat"),
                                    subCatModel.Name),
                                AlternateText =
                                    string.Format(
                                        _localizationService.GetResource("Media.Category.ImageAlternateTextFormat"),
                                        subCatModel.Name)
                            };

                            return pictureModel;
                        });

                        return subCatModel;
                    }).ToList();

            //featured products
            if (!_catalogSettings.IgnoreFeaturedProducts)
            {
                //We cache a value indicating whether we have featured products
                IPagedList<Product> featuredProducts = null;
                var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.CategoryHasFeaturedProductsKey, category,
                    _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer), _storeContext.CurrentStore);
                var hasFeaturedProductsCache = _staticCacheManager.Get(cacheKey, () =>
                {
                    //no value in the cache yet
                    //let's load products and cache the result (true/false)
                    featuredProducts = _productService.SearchProducts(
                       categoryIds: new List<int> { category.Id },
                       storeId: _storeContext.CurrentStore.Id,
                       visibleIndividuallyOnly: true,
                       featuredProducts: true);

                    return featuredProducts.TotalCount > 0;
                });

                if (hasFeaturedProductsCache && featuredProducts == null)
                {
                    //cache indicates that the category has featured products
                    //let's load them
                    featuredProducts = _productService.SearchProducts(
                       categoryIds: new List<int> { category.Id },
                       storeId: _storeContext.CurrentStore.Id,
                       visibleIndividuallyOnly: true,
                       featuredProducts: true);
                }

                if (featuredProducts != null)
                {
                    model.FeaturedProducts = _productModelFactory.PrepareProductOverviewModels(featuredProducts).ToList();
                }
            }

            var categoryIds = new List<int> { category.Id };

            //include subcategories
            if (_catalogSettings.ShowProductsFromSubcategories)
                categoryIds.AddRange(model.SubCategories.Select(sc => sc.Id));

            //products
            IList<int> alreadyFilteredSpecOptionIds = model.PagingFilteringContext.SpecificationFilter.GetAlreadyFilteredSpecOptionIds(_webHelper);
            var products = _productService.SearchProducts(out var filterableSpecificationAttributeOptionIds,
                true,
                categoryIds: categoryIds,
                storeId: _storeContext.CurrentStore.Id,
                visibleIndividuallyOnly: true,
                featuredProducts: _catalogSettings.IncludeFeaturedProductsInNormalLists ? null : (bool?)false,
                priceMin: minPriceConverted,
                priceMax: maxPriceConverted,
                filteredSpecs: alreadyFilteredSpecOptionIds,
                orderBy: (ProductSortingEnum)command.OrderBy,
                pageIndex: command.PageNumber - 1,
                pageSize: command.PageSize);
            model.Products = _productModelFactory.PrepareProductOverviewModels(products).ToList();

            model.PagingFilteringContext.LoadPagedList(products);

            //specs
            model.PagingFilteringContext.SpecificationFilter.PrepareSpecsFilters(alreadyFilteredSpecOptionIds,
                filterableSpecificationAttributeOptionIds?.ToArray(), _cacheKeyService,
                _specificationAttributeService, _localizationService, _webHelper, _workContext, _staticCacheManager);

            return model;
        }

        /// <summary>
        /// Prepare category template view path
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <returns>Category template view path</returns>
        public virtual string PrepareCategoryTemplateViewPath(int templateId)
        {
            var template = _categoryTemplateService.GetCategoryTemplateById(templateId) ??
                           _categoryTemplateService.GetAllCategoryTemplates().FirstOrDefault();

            if (template == null)
                throw new Exception("No default template could be loaded");

            return template.ViewPath;
        }

        /// <summary>
        /// Prepare category navigation model
        /// </summary>
        /// <param name="currentCategoryId">Current category identifier</param>
        /// <param name="currentProductId">Current product identifier</param>
        /// <returns>Category navigation model</returns>
        public virtual CategoryNavigationModel PrepareCategoryNavigationModel(int currentCategoryId, int currentProductId)
        {
            //get active category
            var activeCategoryId = 0;
            if (currentCategoryId > 0)
            {
                //category details page
                activeCategoryId = currentCategoryId;
            }
            else if (currentProductId > 0)
            {
                //product details page
                var productCategories = _categoryService.GetProductCategoriesByProductId(currentProductId);
                if (productCategories.Any())
                    activeCategoryId = productCategories[0].CategoryId;
            }

            var cachedCategoriesModel = PrepareCategorySimpleModels();
            var model = new CategoryNavigationModel
            {
                CurrentCategoryId = activeCategoryId,
                Categories = cachedCategoriesModel
            };

            return model;
        }

        /// <summary>
        /// Prepare top menu model
        /// </summary>
        /// <returns>Top menu model</returns>
        public virtual TopMenuModel PrepareTopMenuModel()
        {
            var cachedCategoriesModel = new List<CategorySimpleModel>();
            //categories
            if (!_catalogSettings.UseAjaxLoadMenu)
                cachedCategoriesModel = PrepareCategorySimpleModels();

            //top menu topics
            var topicModel = _topicService.GetAllTopics(_storeContext.CurrentStore.Id, onlyIncludedInTopMenu: true)
                    .Select(t => new TopMenuModel.TopicModel
                    {
                        Id = t.Id,
                        Name = _localizationService.GetLocalized(t, x => x.Title),
                        SeName = _urlRecordService.GetSeName(t)
                    }).ToList();

            var model = new TopMenuModel
            {
                Categories = cachedCategoriesModel,
                Topics = topicModel,
                NewProductsEnabled = _catalogSettings.NewProductsEnabled,
                BlogEnabled = _blogSettings.Enabled,
                ForumEnabled = _forumSettings.ForumsEnabled,
                DisplayHomepageMenuItem = _displayDefaultMenuItemSettings.DisplayHomepageMenuItem,
                DisplayNewProductsMenuItem = _displayDefaultMenuItemSettings.DisplayNewProductsMenuItem,
                DisplayProductSearchMenuItem = _displayDefaultMenuItemSettings.DisplayProductSearchMenuItem,
                DisplayCustomerInfoMenuItem = _displayDefaultMenuItemSettings.DisplayCustomerInfoMenuItem,
                DisplayBlogMenuItem = _displayDefaultMenuItemSettings.DisplayBlogMenuItem,
                DisplayForumsMenuItem = _displayDefaultMenuItemSettings.DisplayForumsMenuItem,
                DisplayContactUsMenuItem = _displayDefaultMenuItemSettings.DisplayContactUsMenuItem,
                UseAjaxMenu = _catalogSettings.UseAjaxLoadMenu
            };

            return model;
        }

        /// <summary>
        /// Prepare homepage category models
        /// </summary>
        /// <returns>List of homepage category models</returns>
        public virtual List<CategoryModel> PrepareHomepageCategoryModels()
        {
            var pictureSize = _mediaSettings.CategoryThumbPictureSize;

            var categoriesCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.CategoryHomepageKey, 
                pictureSize,
                _workContext.WorkingLanguage,
                _webHelper.IsCurrentConnectionSecured());

            var model = _staticCacheManager.Get(categoriesCacheKey, () =>
                _categoryService.GetAllCategoriesDisplayedOnHomepage()
                    .Select(category =>
                    {
                        var catModel = new CategoryModel
                        {
                            Id = category.Id,
                            Name = _localizationService.GetLocalized(category, x => x.Name),
                            Description = _localizationService.GetLocalized(category, x => x.Description),
                            MetaKeywords = _localizationService.GetLocalized(category, x => x.MetaKeywords),
                            MetaDescription = _localizationService.GetLocalized(category, x => x.MetaDescription),
                            MetaTitle = _localizationService.GetLocalized(category, x => x.MetaTitle),
                            SeName = _urlRecordService.GetSeName(category),
                        };

                        //prepare picture model
                        var categoryPictureCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.CategoryPictureModelKey, 
                            category, pictureSize, true, _workContext.WorkingLanguage,
                            _webHelper.IsCurrentConnectionSecured(), _storeContext.CurrentStore);
                        catModel.PictureModel = _staticCacheManager.Get(categoryPictureCacheKey, () =>
                        {
                            var picture = _pictureService.GetPictureById(category.PictureId);
                            var pictureModel = new PictureModel
                            {
                                FullSizeImageUrl = _pictureService.GetPictureUrl(ref picture),
                                ImageUrl = _pictureService.GetPictureUrl(ref picture, pictureSize),
                                Title = string.Format(
                                    _localizationService.GetResource("Media.Category.ImageLinkTitleFormat"),
                                    catModel.Name),
                                AlternateText =
                                    string.Format(
                                        _localizationService.GetResource("Media.Category.ImageAlternateTextFormat"),
                                        catModel.Name)
                            };
                            return pictureModel;
                        });

                        return catModel;
                    }).ToList());

            return model;
        }

        /// <summary>
        /// Prepare category (simple) models
        /// </summary>
        /// <returns>List of category (simple) models</returns>
        public virtual List<CategorySimpleModel> PrepareCategorySimpleModels()
        {
            //load and cache them
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.CategoryAllModelKey, 
                _workContext.WorkingLanguage,
                _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer),
                _storeContext.CurrentStore);

            return _staticCacheManager.Get(cacheKey, () => PrepareCategorySimpleModels(0));
        }

        /// <summary>
        /// Prepare category (simple) models
        /// </summary>
        /// <param name="rootCategoryId">Root category identifier</param>
        /// <param name="loadSubCategories">A value indicating whether subcategories should be loaded</param>
        /// <returns>List of category (simple) models</returns>
        public virtual List<CategorySimpleModel> PrepareCategorySimpleModels(int rootCategoryId, bool loadSubCategories = true)
        {
            var result = new List<CategorySimpleModel>();

            //little hack for performance optimization
            //we know that this method is used to load top and left menu for categories.
            //it'll load all categories anyway.
            //so there's no need to invoke "GetAllCategoriesByParentCategoryId" multiple times (extra SQL commands) to load childs
            //so we load all categories at once (we know they are cached)
            var allCategories = _categoryService.GetAllCategories(storeId: _storeContext.CurrentStore.Id);
            var categories = allCategories.Where(c => c.ParentCategoryId == rootCategoryId).OrderBy(c => c.DisplayOrder).ToList();
            foreach (var category in categories)
            {
                var categoryModel = new CategorySimpleModel
                {
                    Id = category.Id,
                    Name = _localizationService.GetLocalized(category, x => x.Name),
                    SeName = _urlRecordService.GetSeName(category),
                    IncludeInTopMenu = category.IncludeInTopMenu
                };

                //number of products in each category
                if (_catalogSettings.ShowCategoryProductNumber)
                {
                    var categoryIds = new List<int> { category.Id };
                    //include subcategories
                    if (_catalogSettings.ShowCategoryProductNumberIncludingSubcategories)
                        categoryIds.AddRange(
                            _categoryService.GetChildCategoryIds(category.Id, _storeContext.CurrentStore.Id));

                    categoryModel.NumberOfProducts =
                        _productService.GetNumberOfProductsInCategory(categoryIds, _storeContext.CurrentStore.Id);
                }

                if (loadSubCategories)
                {
                    var subCategories = PrepareCategorySimpleModels(category.Id);
                    categoryModel.SubCategories.AddRange(subCategories);
                }

                categoryModel.HaveSubCategories = categoryModel.SubCategories.Count > 0 &
                    categoryModel.SubCategories.Any(x => x.IncludeInTopMenu);

                result.Add(categoryModel);
            }

            return result;
        }

        /// <summary>
        /// Prepare category (simple) xml document
        /// </summary>
        /// <returns>Xml document of category (simple) models</returns>
        public virtual XDocument PrepareCategoryXmlDocument()
        {
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.CategoryXmlAllModelKey, 
                _workContext.WorkingLanguage,
                _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer),
                _storeContext.CurrentStore);

            return _staticCacheManager.Get(cacheKey, () =>
            {
                var categories = PrepareCategorySimpleModels();

                var xsSubmit = new XmlSerializer(typeof(List<CategorySimpleModel>));

                using var strWriter = new StringWriter();
                using var writer = XmlWriter.Create(strWriter);
                xsSubmit.Serialize(writer, categories);
                var xml = strWriter.ToString();

                return XDocument.Parse(xml);
            });
        }

        /// <summary>
        /// Prepare root categories for menu
        /// </summary>
        /// <returns>List of category (simple) models</returns>
        public virtual List<CategorySimpleModel> PrepareRootCategories()
        {
            var doc = PrepareCategoryXmlDocument();

            var models = from xe in doc.Root.XPathSelectElements("CategorySimpleModel")
                         select GetCategorySimpleModel(xe);

            return models.ToList();
        }

        /// <summary>
        /// Prepare subcategories for menu
        /// </summary>
        /// <param name="id">Id of category to get subcategory</param>
        /// <returns></returns>
        public virtual List<CategorySimpleModel> PrepareSubCategories(int id)
        {
            var doc = PrepareCategoryXmlDocument();

            var model = from xe in doc.Descendants("CategorySimpleModel")
                        where xe.XPathSelectElement("Id").Value == id.ToString()
                        select xe;

            var models = from xe in model.First().XPathSelectElements("SubCategories/CategorySimpleModel")
                         select GetCategorySimpleModel(xe);

            return models.ToList();
        }

        #endregion

        #region Manufacturers

        /// <summary>
        /// Prepare manufacturer model
        /// </summary>
        /// <param name="manufacturer">Manufacturer identifier</param>
        /// <param name="command">Catalog paging filtering command</param>
        /// <returns>Manufacturer model</returns>
        public virtual ManufacturerModel PrepareManufacturerModel(Manufacturer manufacturer, CatalogPagingFilteringModel command)
        {
            if (manufacturer == null)
                throw new ArgumentNullException(nameof(manufacturer));

            var model = new ManufacturerModel
            {
                Id = manufacturer.Id,
                Name = _localizationService.GetLocalized(manufacturer, x => x.Name),
                Description = _localizationService.GetLocalized(manufacturer, x => x.Description),
                MetaKeywords = _localizationService.GetLocalized(manufacturer, x => x.MetaKeywords),
                MetaDescription = _localizationService.GetLocalized(manufacturer, x => x.MetaDescription),
                MetaTitle = _localizationService.GetLocalized(manufacturer, x => x.MetaTitle),
                SeName = _urlRecordService.GetSeName(manufacturer),
            };

            //sorting
            PrepareSortingOptions(model.PagingFilteringContext, command);
            //view mode
            PrepareViewModes(model.PagingFilteringContext, command);
            //page size
            PreparePageSizeOptions(model.PagingFilteringContext, command,
                manufacturer.AllowCustomersToSelectPageSize,
                manufacturer.PageSizeOptions,
                manufacturer.PageSize);

            //price ranges
            model.PagingFilteringContext.PriceRangeFilter.LoadPriceRangeFilters(manufacturer.PriceRanges, _webHelper, _priceFormatter);
            var selectedPriceRange = model.PagingFilteringContext.PriceRangeFilter.GetSelectedPriceRange(_webHelper, manufacturer.PriceRanges);
            decimal? minPriceConverted = null;
            decimal? maxPriceConverted = null;
            if (selectedPriceRange != null)
            {
                if (selectedPriceRange.From.HasValue)
                    minPriceConverted = _currencyService.ConvertToPrimaryStoreCurrency(selectedPriceRange.From.Value, _workContext.WorkingCurrency);

                if (selectedPriceRange.To.HasValue)
                    maxPriceConverted = _currencyService.ConvertToPrimaryStoreCurrency(selectedPriceRange.To.Value, _workContext.WorkingCurrency);
            }

            //featured products
            if (!_catalogSettings.IgnoreFeaturedProducts)
            {
                IPagedList<Product> featuredProducts = null;

                //We cache a value indicating whether we have featured products
                var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.ManufacturerHasFeaturedProductsKey, 
                    manufacturer,
                    _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer),
                    _storeContext.CurrentStore);
                var hasFeaturedProductsCache = _staticCacheManager.Get(cacheKey, () =>
                {
                    //no value in the cache yet
                    //let's load products and cache the result (true/false)
                    featuredProducts = _productService.SearchProducts(
                       manufacturerId: manufacturer.Id,
                       storeId: _storeContext.CurrentStore.Id,
                       visibleIndividuallyOnly: true,
                       featuredProducts: true);

                    return featuredProducts.TotalCount > 0;
                });

                if (hasFeaturedProductsCache && featuredProducts == null)
                {
                    //cache indicates that the manufacturer has featured products
                    //let's load them
                    featuredProducts = _productService.SearchProducts(
                       manufacturerId: manufacturer.Id,
                       storeId: _storeContext.CurrentStore.Id,
                       visibleIndividuallyOnly: true,
                       featuredProducts: true);
                }

                if (featuredProducts != null)
                {
                    model.FeaturedProducts = _productModelFactory.PrepareProductOverviewModels(featuredProducts).ToList();
                }
            }

            //products
            var products = _productService.SearchProducts(out _, true,
                manufacturerId: manufacturer.Id,
                storeId: _storeContext.CurrentStore.Id,
                visibleIndividuallyOnly: true,
                featuredProducts: _catalogSettings.IncludeFeaturedProductsInNormalLists ? null : (bool?)false,
                priceMin: minPriceConverted,
                priceMax: maxPriceConverted,
                orderBy: (ProductSortingEnum)command.OrderBy,
                pageIndex: command.PageNumber - 1,
                pageSize: command.PageSize);
            model.Products = _productModelFactory.PrepareProductOverviewModels(products).ToList();

            model.PagingFilteringContext.LoadPagedList(products);

            return model;
        }

        /// <summary>
        /// Prepare manufacturer template view path
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <returns>Manufacturer template view path</returns>
        public virtual string PrepareManufacturerTemplateViewPath(int templateId)
        {
            var template = _manufacturerTemplateService.GetManufacturerTemplateById(templateId) ??
                           _manufacturerTemplateService.GetAllManufacturerTemplates().FirstOrDefault();

            if (template == null)
                throw new Exception("No default template could be loaded");

            return template.ViewPath;
        }

        /// <summary>
        /// Prepare manufacturer all models
        /// </summary>
        /// <returns>List of manufacturer models</returns>
        public virtual List<ManufacturerModel> PrepareManufacturerAllModels()
        {
            var model = new List<ManufacturerModel>();
            var manufacturers = _manufacturerService.GetAllManufacturers(storeId: _storeContext.CurrentStore.Id);
            foreach (var manufacturer in manufacturers)
            {
                var modelMan = new ManufacturerModel
                {
                    Id = manufacturer.Id,
                    Name = _localizationService.GetLocalized(manufacturer, x => x.Name),
                    Description = _localizationService.GetLocalized(manufacturer, x => x.Description),
                    MetaKeywords = _localizationService.GetLocalized(manufacturer, x => x.MetaKeywords),
                    MetaDescription = _localizationService.GetLocalized(manufacturer, x => x.MetaDescription),
                    MetaTitle = _localizationService.GetLocalized(manufacturer, x => x.MetaTitle),
                    SeName = _urlRecordService.GetSeName(manufacturer),
                };

                //prepare picture model
                var pictureSize = _mediaSettings.ManufacturerThumbPictureSize;
                var manufacturerPictureCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.ManufacturerPictureModelKey, 
                    manufacturer, pictureSize, true, _workContext.WorkingLanguage, 
                    _webHelper.IsCurrentConnectionSecured(), _storeContext.CurrentStore);
                modelMan.PictureModel = _staticCacheManager.Get(manufacturerPictureCacheKey, () =>
                {
                    var picture = _pictureService.GetPictureById(manufacturer.PictureId);
                    var pictureModel = new PictureModel
                    {
                        FullSizeImageUrl = _pictureService.GetPictureUrl(ref picture),
                        ImageUrl = _pictureService.GetPictureUrl(ref picture, pictureSize),
                        Title = string.Format(_localizationService.GetResource("Media.Manufacturer.ImageLinkTitleFormat"), modelMan.Name),
                        AlternateText = string.Format(_localizationService.GetResource("Media.Manufacturer.ImageAlternateTextFormat"), modelMan.Name)
                    };

                    return pictureModel;
                });

                model.Add(modelMan);
            }

            return model;
        }

        /// <summary>
        /// Prepare manufacturer navigation model
        /// </summary>
        /// <param name="currentManufacturerId">Current manufacturer identifier</param>
        /// <returns>Manufacturer navigation model</returns>
        public virtual ManufacturerNavigationModel PrepareManufacturerNavigationModel(int currentManufacturerId)
        {
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.ManufacturerNavigationModelKey, 
                currentManufacturerId,
                _workContext.WorkingLanguage,
                _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer),
                _storeContext.CurrentStore);
            var cachedModel = _staticCacheManager.Get(cacheKey, () =>
            {
                var currentManufacturer = _manufacturerService.GetManufacturerById(currentManufacturerId);

                var manufacturers = _manufacturerService.GetAllManufacturers(storeId: _storeContext.CurrentStore.Id,
                    pageSize: _catalogSettings.ManufacturersBlockItemsToDisplay);
                var model = new ManufacturerNavigationModel
                {
                    TotalManufacturers = manufacturers.TotalCount
                };

                foreach (var manufacturer in manufacturers)
                {
                    var modelMan = new ManufacturerBriefInfoModel
                    {
                        Id = manufacturer.Id,
                        Name = _localizationService.GetLocalized(manufacturer, x => x.Name),
                        SeName = _urlRecordService.GetSeName(manufacturer),
                        IsActive = currentManufacturer != null && currentManufacturer.Id == manufacturer.Id,
                    };
                    model.Manufacturers.Add(modelMan);
                }

                return model;
            });

            return cachedModel;
        }

        #endregion

        #region Vendors

        /// <summary>
        /// Prepare vendor model
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="command">Catalog paging filtering command</param>
        /// <returns>Vendor model</returns>
        public virtual VendorModel PrepareVendorModel(Vendor vendor, CatalogPagingFilteringModel command)
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            var model = new VendorModel
            {
                Id = vendor.Id,
                Name = _localizationService.GetLocalized(vendor, x => x.Name),
                Description = _localizationService.GetLocalized(vendor, x => x.Description),
                MetaKeywords = _localizationService.GetLocalized(vendor, x => x.MetaKeywords),
                MetaDescription = _localizationService.GetLocalized(vendor, x => x.MetaDescription),
                MetaTitle = _localizationService.GetLocalized(vendor, x => x.MetaTitle),
                SeName = _urlRecordService.GetSeName(vendor),
                AllowCustomersToContactVendors = _vendorSettings.AllowCustomersToContactVendors
            };

            //sorting
            PrepareSortingOptions(model.PagingFilteringContext, command);
            //view mode
            PrepareViewModes(model.PagingFilteringContext, command);
            //page size
            PreparePageSizeOptions(model.PagingFilteringContext, command,
                vendor.AllowCustomersToSelectPageSize,
                vendor.PageSizeOptions,
                vendor.PageSize);

            //products
            var products = _productService.SearchProducts(out _,
                true,
                vendorId: vendor.Id,
                storeId: _storeContext.CurrentStore.Id,
                visibleIndividuallyOnly: true,
                orderBy: (ProductSortingEnum)command.OrderBy,
                pageIndex: command.PageNumber - 1,
                pageSize: command.PageSize);
            model.Products = _productModelFactory.PrepareProductOverviewModels(products).ToList();

            model.PagingFilteringContext.LoadPagedList(products);

            return model;
        }

        /// <summary>
        /// Prepare vendor all models
        /// </summary>
        /// <returns>List of vendor models</returns>
        public virtual List<VendorModel> PrepareVendorAllModels()
        {
            var model = new List<VendorModel>();
            var vendors = _vendorService.GetAllVendors();
            foreach (var vendor in vendors)
            {
                var vendorModel = new VendorModel
                {
                    Id = vendor.Id,
                    Name = _localizationService.GetLocalized(vendor, x => x.Name),
                    Description = _localizationService.GetLocalized(vendor, x => x.Description),
                    MetaKeywords = _localizationService.GetLocalized(vendor, x => x.MetaKeywords),
                    MetaDescription = _localizationService.GetLocalized(vendor, x => x.MetaDescription),
                    MetaTitle = _localizationService.GetLocalized(vendor, x => x.MetaTitle),
                    SeName = _urlRecordService.GetSeName(vendor),
                    AllowCustomersToContactVendors = _vendorSettings.AllowCustomersToContactVendors
                };

                //prepare picture model
                var pictureSize = _mediaSettings.VendorThumbPictureSize;
                var pictureCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopModelCacheDefaults.VendorPictureModelKey, 
                    vendor, pictureSize, true, _workContext.WorkingLanguage, _webHelper.IsCurrentConnectionSecured(), _storeContext.CurrentStore);
                vendorModel.PictureModel = _staticCacheManager.Get(pictureCacheKey, () =>
                {
                    var picture = _pictureService.GetPictureById(vendor.PictureId);
                    var pictureModel = new PictureModel
                    {
                        FullSizeImageUrl = _pictureService.GetPictureUrl(ref picture),
                        ImageUrl = _pictureService.GetPictureUrl(ref picture, pictureSize),
                        Title = string.Format(_localizationService.GetResource("Media.Vendor.ImageLinkTitleFormat"), vendorModel.Name),
                        AlternateText = string.Format(_localizationService.GetResource("Media.Vendor.ImageAlternateTextFormat"), vendorModel.Name)
                    };

                    return pictureModel;
                });

                model.Add(vendorModel);
            }

            return model;
        }

        /// <summary>
        /// Prepare vendor navigation model
        /// </summary>
        /// <returns>Vendor navigation model</returns>
        public virtual VendorNavigationModel PrepareVendorNavigationModel()
        {
            var cacheKey = NopModelCacheDefaults.VendorNavigationModelKey;
            var cachedModel = _staticCacheManager.Get(cacheKey, () =>
            {
                var vendors = _vendorService.GetAllVendors(pageSize: _vendorSettings.VendorsBlockItemsToDisplay);
                var model = new VendorNavigationModel
                {
                    TotalVendors = vendors.TotalCount
                };

                foreach (var vendor in vendors)
                {
                    model.Vendors.Add(new VendorBriefInfoModel
                    {
                        Id = vendor.Id,
                        Name = _localizationService.GetLocalized(vendor, x => x.Name),
                        SeName = _urlRecordService.GetSeName(vendor),
                    });
                }

                return model;
            });

            return cachedModel;
        }

        #endregion

        #region Product tags

        /// <summary>
        /// Prepare popular product tags model
        /// </summary>
        /// <returns>Product tags model</returns>
        public virtual PopularProductTagsModel PreparePopularProductTagsModel()
        {
            var model = new PopularProductTagsModel();

            //get all tags
            var tags = _productTagService
                .GetAllProductTags()
                //filter by current store
                .Where(x => _productTagService.GetProductCount(x.Id, _storeContext.CurrentStore.Id) > 0)
                .ToList();

            model.TotalTags = tags.Count;

            model.Tags.AddRange(tags
                //order by product count
                .OrderByDescending(x => _productTagService.GetProductCount(x.Id, _storeContext.CurrentStore.Id))
                .Take(_catalogSettings.NumberOfProductTags)
                //sorting
                .OrderBy(x => _localizationService.GetLocalized(x, y => y.Name))
                .Select(tag => new ProductTagModel
                {
                    Id = tag.Id,
                    Name = _localizationService.GetLocalized(tag, y => y.Name),
                    SeName = _urlRecordService.GetSeName(tag),
                    ProductCount = _productTagService.GetProductCount(tag.Id, _storeContext.CurrentStore.Id)
                }));

            return model;
        }

        /// <summary>
        /// Prepare products by tag model
        /// </summary>
        /// <param name="productTag">Product tag</param>
        /// <param name="command">Catalog paging filtering command</param>
        /// <returns>Products by tag model</returns>
        public virtual ProductsByTagModel PrepareProductsByTagModel(ProductTag productTag, CatalogPagingFilteringModel command)
        {
            if (productTag == null)
                throw new ArgumentNullException(nameof(productTag));

            var model = new ProductsByTagModel
            {
                Id = productTag.Id,
                TagName = _localizationService.GetLocalized(productTag, y => y.Name),
                TagSeName = _urlRecordService.GetSeName(productTag)
            };

            //sorting
            PrepareSortingOptions(model.PagingFilteringContext, command);
            //view mode
            PrepareViewModes(model.PagingFilteringContext, command);
            //page size
            PreparePageSizeOptions(model.PagingFilteringContext, command,
                _catalogSettings.ProductsByTagAllowCustomersToSelectPageSize,
                _catalogSettings.ProductsByTagPageSizeOptions,
                _catalogSettings.ProductsByTagPageSize);

            //products
            var products = _productService.SearchProducts(
                storeId: _storeContext.CurrentStore.Id,
                productTagId: productTag.Id,
                visibleIndividuallyOnly: true,
                orderBy: (ProductSortingEnum)command.OrderBy,
                pageIndex: command.PageNumber - 1,
                pageSize: command.PageSize);
            model.Products = _productModelFactory.PrepareProductOverviewModels(products).ToList();

            model.PagingFilteringContext.LoadPagedList(products);
            return model;
        }

        /// <summary>
        /// Prepare product tags all model
        /// </summary>
        /// <returns>Popular product tags model</returns>
        public virtual PopularProductTagsModel PrepareProductTagsAllModel()
        {
            var model = new PopularProductTagsModel
            {
                Tags = _productTagService
                .GetAllProductTags()
                //filter by current store
                .Where(x => _productTagService.GetProductCount(x.Id, _storeContext.CurrentStore.Id) > 0)
                //sort by name
                .OrderBy(x => _localizationService.GetLocalized(x, y => y.Name))
                .Select(x =>
                {
                    var ptModel = new ProductTagModel
                    {
                        Id = x.Id,
                        Name = _localizationService.GetLocalized(x, y => y.Name),
                        SeName = _urlRecordService.GetSeName(x),
                        ProductCount = _productTagService.GetProductCount(x.Id, _storeContext.CurrentStore.Id)
                    };
                    return ptModel;
                })
                .ToList()
            };
            return model;
        }

        #endregion

        #region Searching

        /// <summary>
        /// Prepare search model
        /// </summary>
        /// <param name="model">Search model</param>
        /// <param name="command">Catalog paging filtering command</param>
        /// <returns>Search model</returns>
        public virtual SearchModel PrepareSearchModel(SearchModel model, CatalogPagingFilteringModel command)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var searchTerms = model.q ?? string.Empty;

            searchTerms = searchTerms.Trim();

            //sorting
            PrepareSortingOptions(model.PagingFilteringContext, command);
            //view mode
            PrepareViewModes(model.PagingFilteringContext, command);
            //page size
            PreparePageSizeOptions(model.PagingFilteringContext, command,
                _catalogSettings.SearchPageAllowCustomersToSelectPageSize,
                _catalogSettings.SearchPagePageSizeOptions,
                _catalogSettings.SearchPageProductsPerPage);


            var categoriesModels = new List<SearchModel.CategoryModel>();
            //all categories
            var allCategories = _categoryService.GetAllCategories(storeId: _storeContext.CurrentStore.Id);
            foreach (var c in allCategories)
            {
                //generate full category name (breadcrumb)
                var categoryBreadcrumb = string.Empty;
                var breadcrumb = _categoryService.GetCategoryBreadCrumb(c, allCategories);
                for (var i = 0; i <= breadcrumb.Count - 1; i++)
                {
                    categoryBreadcrumb += _localizationService.GetLocalized(breadcrumb[i], x => x.Name);
                    if (i != breadcrumb.Count - 1)
                        categoryBreadcrumb += " >> ";
                }

                categoriesModels.Add(new SearchModel.CategoryModel
                {
                    Id = c.Id,
                    Breadcrumb = categoryBreadcrumb
                });
            }

            if (categoriesModels.Any())
            {
                //first empty entry
                model.AvailableCategories.Add(new SelectListItem
                {
                    Value = "0",
                    Text = _localizationService.GetResource("Common.All")
                });
                //all other categories
                foreach (var c in categoriesModels)
                {
                    model.AvailableCategories.Add(new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Breadcrumb,
                        Selected = model.cid == c.Id
                    });
                }
            }

            var manufacturers = _manufacturerService.GetAllManufacturers(storeId: _storeContext.CurrentStore.Id);
            if (manufacturers.Any())
            {
                model.AvailableManufacturers.Add(new SelectListItem
                {
                    Value = "0",
                    Text = _localizationService.GetResource("Common.All")
                });
                foreach (var m in manufacturers)
                    model.AvailableManufacturers.Add(new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = _localizationService.GetLocalized(m, x => x.Name),
                        Selected = model.mid == m.Id
                    });
            }

            model.asv = _vendorSettings.AllowSearchByVendor;
            if (model.asv)
            {
                var vendors = _vendorService.GetAllVendors();
                if (vendors.Any())
                {
                    model.AvailableVendors.Add(new SelectListItem
                    {
                        Value = "0",
                        Text = _localizationService.GetResource("Common.All")
                    });
                    foreach (var vendor in vendors)
                        model.AvailableVendors.Add(new SelectListItem
                        {
                            Value = vendor.Id.ToString(),
                            Text = _localizationService.GetLocalized(vendor, x => x.Name),
                            Selected = model.vid == vendor.Id
                        });
                }
            }

            IPagedList<Product> products = new PagedList<Product>(new List<Product>(), 0, 1);
            // only search if query string search keyword is set (used to avoid searching or displaying search term min length error message on /search page load)
            //we don't use "!string.IsNullOrEmpty(searchTerms)" in cases of "ProductSearchTermMinimumLength" set to 0 but searching by other parameters (e.g. category or price filter)
            var isSearchTermSpecified = _httpContextAccessor.HttpContext.Request.Query.ContainsKey("q");
            if (isSearchTermSpecified)
            {
                if (searchTerms.Length < _catalogSettings.ProductSearchTermMinimumLength)
                {
                    model.Warning =
                        string.Format(_localizationService.GetResource("Search.SearchTermMinimumLengthIsNCharacters"),
                            _catalogSettings.ProductSearchTermMinimumLength);
                }
                else
                {
                    var categoryIds = new List<int>();
                    var manufacturerId = 0;
                    decimal? minPriceConverted = null;
                    decimal? maxPriceConverted = null;
                    var searchInDescriptions = false;
                    var vendorId = 0;
                    if (model.adv)
                    {
                        //advanced search
                        var categoryId = model.cid;
                        if (categoryId > 0)
                        {
                            categoryIds.Add(categoryId);
                            if (model.isc)
                            {
                                //include subcategories
                                categoryIds.AddRange(
                                    _categoryService.GetChildCategoryIds(categoryId, _storeContext.CurrentStore.Id));
                            }
                        }

                        manufacturerId = model.mid;

                        //min price
                        if (!string.IsNullOrEmpty(model.pf))
                        {
                            if (decimal.TryParse(model.pf, out var minPrice))
                                minPriceConverted =
                                    _currencyService.ConvertToPrimaryStoreCurrency(minPrice,
                                        _workContext.WorkingCurrency);
                        }

                        //max price
                        if (!string.IsNullOrEmpty(model.pt))
                        {
                            if (decimal.TryParse(model.pt, out var maxPrice))
                                maxPriceConverted =
                                    _currencyService.ConvertToPrimaryStoreCurrency(maxPrice,
                                        _workContext.WorkingCurrency);
                        }

                        if (model.asv)
                            vendorId = model.vid;

                        searchInDescriptions = model.sid;
                    }

                    //var searchInProductTags = false;
                    var searchInProductTags = searchInDescriptions;

                    //products
                    products = _productService.SearchProducts(
                        categoryIds: categoryIds,
                        manufacturerId: manufacturerId,
                        storeId: _storeContext.CurrentStore.Id,
                        visibleIndividuallyOnly: true,
                        priceMin: minPriceConverted,
                        priceMax: maxPriceConverted,
                        keywords: searchTerms,
                        searchDescriptions: searchInDescriptions,
                        searchProductTags: searchInProductTags,
                        languageId: _workContext.WorkingLanguage.Id,
                        orderBy: (ProductSortingEnum)command.OrderBy,
                        pageIndex: command.PageNumber - 1,
                        pageSize: command.PageSize,
                        vendorId: vendorId);
                    model.Products = _productModelFactory.PrepareProductOverviewModels(products).ToList();

                    model.NoResults = !model.Products.Any();

                    //search term statistics
                    if (!string.IsNullOrEmpty(searchTerms))
                    {
                        var searchTerm =
                            _searchTermService.GetSearchTermByKeyword(searchTerms, _storeContext.CurrentStore.Id);
                        if (searchTerm != null)
                        {
                            searchTerm.Count++;
                            _searchTermService.UpdateSearchTerm(searchTerm);
                        }
                        else
                        {
                            searchTerm = new SearchTerm
                            {
                                Keyword = searchTerms,
                                StoreId = _storeContext.CurrentStore.Id,
                                Count = 1
                            };
                            _searchTermService.InsertSearchTerm(searchTerm);
                        }
                    }

                    //event
                    _eventPublisher.Publish(new ProductSearchEvent
                    {
                        SearchTerm = searchTerms,
                        SearchInDescriptions = searchInDescriptions,
                        CategoryIds = categoryIds,
                        ManufacturerId = manufacturerId,
                        WorkingLanguageId = _workContext.WorkingLanguage.Id,
                        VendorId = vendorId
                    });
                }
            }

            model.PagingFilteringContext.LoadPagedList(products);
            return model;
        }

        /// <summary>
        /// Prepare search box model
        /// </summary>
        /// <returns>Search box model</returns>
        public virtual SearchBoxModel PrepareSearchBoxModel()
        {
            var model = new SearchBoxModel
            {
                AutoCompleteEnabled = _catalogSettings.ProductSearchAutoCompleteEnabled,
                ShowProductImagesInSearchAutoComplete = _catalogSettings.ShowProductImagesInSearchAutoComplete,
                SearchTermMinimumLength = _catalogSettings.ProductSearchTermMinimumLength,
                ShowSearchBox = _catalogSettings.ProductSearchEnabled
            };
            return model;
        }

        #endregion

        #region Utilities

        protected virtual CategorySimpleModel GetCategorySimpleModel(XElement elem)
        {
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            return new CategorySimpleModel
            {
                Id = int.Parse(elem.XPathSelectElement("Id").Value),
                Name = elem.XPathSelectElement("Name").Value,
                SeName = elem.XPathSelectElement("SeName").Value,

                NumberOfProducts = !string.IsNullOrEmpty(elem.XPathSelectElement("NumberOfProducts").Value)
                    ? int.Parse(elem.XPathSelectElement("NumberOfProducts").Value)
                    : (int?)null,

                IncludeInTopMenu = bool.Parse(elem.XPathSelectElement("IncludeInTopMenu").Value),
                HaveSubCategories = bool.Parse(elem.XPathSelectElement("HaveSubCategories").Value),
                Route = urlHelper.RouteUrl("Category", new { SeName = elem.XPathSelectElement("SeName").Value })
            };
        }

        #endregion
    }
}