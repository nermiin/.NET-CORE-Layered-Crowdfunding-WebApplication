﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Caching;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Tax.Avalara.Services;
using Nop.Services.Caching;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Tax;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    public class AvalaraTaxController : TaxController
    {
        #region Fields

        private readonly AvalaraTaxManager _avalaraTaxManager;
        private readonly ICacheKeyService _cacheKeyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IStaticCacheManager _cacheManager;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ITaxPluginManager _taxPluginManager;

        #endregion

        #region Ctor

        public AvalaraTaxController(AvalaraTaxManager avalaraTaxManager,
            ICacheKeyService cacheKeyService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStaticCacheManager cacheManager,
            ITaxCategoryService taxCategoryService,
            ITaxModelFactory taxModelFactory,
            ITaxPluginManager taxPluginManager,
            TaxSettings taxSettings) : base(permissionService,
                settingService,
                taxCategoryService,
                taxModelFactory,
                taxPluginManager,
                taxSettings)
        {
            _avalaraTaxManager = avalaraTaxManager;
            _cacheKeyService = cacheKeyService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _cacheManager = cacheManager;
            _taxCategoryService = taxCategoryService;
            _taxPluginManager = taxPluginManager;
        }

        #endregion

        #region Methods

        public override IActionResult Categories()
        {
            //ensure that Avalara tax provider is active
            if (!_taxPluginManager.IsPluginActive(AvalaraTaxDefaults.SystemName))
            {
                //if isn't active return base action result
                RouteData.Values["controller"] = "Tax";
                return base.Categories();
            }

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //prepare model
            var model = new Models.Tax.TaxCategorySearchModel();
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(AvalaraTaxDefaults.TaxCodeTypesCacheKey);
            var taxCodeTypes = _cacheManager.Get(cacheKey, () => _avalaraTaxManager.GetTaxCodeTypes());
            if (taxCodeTypes != null)
                model.AvailableTypes = taxCodeTypes.Select(type => new SelectListItem(type.Value, type.Key)).ToList();
            model.SetGridPageSize();

            //use overridden view
            return View("~/Plugins/Tax.Avalara/Views/Tax/Categories.cshtml", model);
        }

        [HttpPost]
        public override IActionResult Categories(TaxCategorySearchModel searchModel)
        {
            //ensure that Avalara tax provider is active
            if (!_taxPluginManager.IsPluginActive(AvalaraTaxDefaults.SystemName))
                return base.Categories(searchModel);

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedDataTablesJson();

            //get tax categories
            var taxCategories = _taxCategoryService.GetAllTaxCategories().ToPagedList(searchModel);

            //get tax types and define the default value
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(AvalaraTaxDefaults.TaxCodeTypesCacheKey);
            var taxTypes = _cacheManager.Get(cacheKey, () => _avalaraTaxManager.GetTaxCodeTypes())
                ?.Select(taxType => new { Id = taxType.Key, Name = taxType.Value });
            var defaultType = taxTypes
                ?.FirstOrDefault(taxType => taxType.Name.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase))
                ?? taxTypes?.FirstOrDefault();

            //prepare grid model
            var model = new Models.Tax.TaxCategoryListModel().PrepareToGrid(searchModel, taxCategories, () =>
            {
                //fill in model values from the entity
                return taxCategories.Select(taxCategory =>
                {
                    //fill in model values from the entity
                    var taxCategoryModel = new Models.Tax.TaxCategoryModel
                    {
                        Id = taxCategory.Id,
                        Name = taxCategory.Name,
                        DisplayOrder = taxCategory.DisplayOrder
                    };

                    //try to get previously saved tax code type and description
                    var taxCodeType = taxTypes?.FirstOrDefault(type =>
                        type.Id.Equals(_genericAttributeService.GetAttribute<string>(taxCategory, AvalaraTaxDefaults.TaxCodeTypeAttribute) ?? string.Empty))
                        ?? defaultType;
                    taxCategoryModel.Type = taxCodeType?.Name ?? string.Empty;
                    taxCategoryModel.TypeId = taxCodeType?.Id ?? Guid.Empty.ToString();
                    taxCategoryModel.Description = _genericAttributeService
                        .GetAttribute<string>(taxCategory, AvalaraTaxDefaults.TaxCodeDescriptionAttribute) ?? string.Empty;

                    return taxCategoryModel;
                });
            });

            return Json(model);
        }

        [HttpPost]
        public IActionResult TaxCategoryUpdate(Models.Tax.TaxCategoryModel model)
        {
            return base.CategoryUpdate(model);
        }

        [HttpPost]
        public IActionResult TaxCategoryAdd(Models.Tax.TaxCategoryModel model)
        {
            //ensure that Avalara tax provider is active
            if (!_taxPluginManager.IsPluginActive(AvalaraTaxDefaults.SystemName))
                return new NullJsonResult();

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return ErrorJson(ModelState.SerializeErrors());

            var taxCategory = new TaxCategory();
            taxCategory = model.ToEntity(taxCategory);
            _taxCategoryService.InsertTaxCategory(taxCategory);

            //save tax code type as generic attribute
            if (!string.IsNullOrEmpty(model.TypeId) && !model.TypeId.Equals(Guid.Empty.ToString()))
                _genericAttributeService.SaveAttribute(taxCategory, AvalaraTaxDefaults.TaxCodeTypeAttribute, model.TypeId);

            return Json(new { Result = true });
        }

        [HttpPost]
        public override IActionResult CategoryDelete(int id)
        {
            //ensure that Avalara tax provider is active
            if (!_taxPluginManager.IsPluginActive(AvalaraTaxDefaults.SystemName))
                return new NullJsonResult();

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //try to get a tax category with the specified id
            var taxCategory = _taxCategoryService.GetTaxCategoryById(id);
            if (taxCategory == null)
                throw new ArgumentException("No tax category found with the specified id");

            //delete generic attributes 
            _genericAttributeService.SaveAttribute<string>(taxCategory, AvalaraTaxDefaults.TaxCodeDescriptionAttribute, null);
            _genericAttributeService.SaveAttribute<string>(taxCategory, AvalaraTaxDefaults.TaxCodeTypeAttribute, null);

            _taxCategoryService.DeleteTaxCategory(taxCategory);

            return new NullJsonResult();
        }

        [HttpPost, ActionName("Categories")]
        [FormValueRequired("importTaxCodes")]
        public IActionResult ImportTaxCodes()
        {
            //ensure that Avalara tax provider is active
            if (!_taxPluginManager.IsPluginActive(AvalaraTaxDefaults.SystemName))
                return Categories();

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //import tax caodes
            var importedTaxCodesNumber = _avalaraTaxManager.ImportTaxCodes();
            if (importedTaxCodesNumber.HasValue)
            {
                //successfully imported
                var successMessage = _localizationService.GetResource("Plugins.Tax.Avalara.TaxCodes.Import.Success");
                _notificationService.SuccessNotification(string.Format(successMessage, importedTaxCodesNumber));
            }
            else
                _notificationService.ErrorNotification(_localizationService.GetResource("Plugins.Tax.Avalara.TaxCodes.Import.Error"));

            return Categories();
        }

        [HttpPost, ActionName("Categories")]
        [FormValueRequired("exportTaxCodes")]
        public IActionResult ExportTaxCodes()
        {
            //ensure that Avalara tax provider is active
            if (!_taxPluginManager.IsPluginActive(AvalaraTaxDefaults.SystemName))
                return Categories();

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            //export tax codes
            var exportedTaxCodes = _avalaraTaxManager.ExportTaxCodes();
            if (exportedTaxCodes.HasValue)
            {
                if (exportedTaxCodes > 0)
                    _notificationService.SuccessNotification(string.Format(_localizationService.GetResource("Plugins.Tax.Avalara.TaxCodes.Export.Success"), exportedTaxCodes));
                else
                    _notificationService.SuccessNotification(_localizationService.GetResource("Plugins.Tax.Avalara.TaxCodes.Export.AlreadyExported"));
            }
            else
                _notificationService.ErrorNotification(_localizationService.GetResource("Plugins.Tax.Avalara.TaxCodes.Export.Error"));

            return Categories();
        }

        [HttpPost, ActionName("Categories")]
        [FormValueRequired("deleteTaxCodes")]
        public IActionResult DeleteSystemTaxCodes()
        {
            //ensure that Avalara tax provider is active
            if (!_taxPluginManager.IsPluginActive(AvalaraTaxDefaults.SystemName))
                return Categories();

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            var deleted = _avalaraTaxManager.DeleteSystemTaxCodes();
            if (deleted)
                _notificationService.SuccessNotification(_localizationService.GetResource("Plugins.Tax.Avalara.TaxCodes.Delete.Success"));
            else
                _notificationService.ErrorNotification(_localizationService.GetResource("Plugins.Tax.Avalara.TaxCodes.Delete.Error"));

            return Categories();
        }

        #endregion
    }
}