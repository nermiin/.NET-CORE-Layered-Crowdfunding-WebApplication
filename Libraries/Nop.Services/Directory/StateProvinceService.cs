﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Data;
using Nop.Services.Caching;
using Nop.Services.Caching.Extensions;
using Nop.Services.Events;
using Nop.Services.Localization;

namespace Nop.Services.Directory
{
    /// <summary>
    /// State province service
    /// </summary>
    public partial class StateProvinceService : IStateProvinceService
    {
        #region Fields

        private readonly ICacheKeyService _cacheKeyService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocalizationService _localizationService;
        private readonly IRepository<StateProvince> _stateProvinceRepository;

        #endregion

        #region Ctor

        public StateProvinceService(ICacheKeyService cacheKeyService,
            IStaticCacheManager staticCacheManager,
            IEventPublisher eventPublisher,
            ILocalizationService localizationService,
            IRepository<StateProvince> stateProvinceRepository)
        {
            _cacheKeyService = cacheKeyService;
            _staticCacheManager = staticCacheManager;
            _eventPublisher = eventPublisher;
            _localizationService = localizationService;
            _stateProvinceRepository = stateProvinceRepository;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Deletes a state/province
        /// </summary>
        /// <param name="stateProvince">The state/province</param>
        public virtual void DeleteStateProvince(StateProvince stateProvince)
        {
            if (stateProvince == null)
                throw new ArgumentNullException(nameof(stateProvince));

            _stateProvinceRepository.Delete(stateProvince);

            //event notification
            _eventPublisher.EntityDeleted(stateProvince);
        }

        /// <summary>
        /// Gets a state/province
        /// </summary>
        /// <param name="stateProvinceId">The state/province identifier</param>
        /// <returns>State/province</returns>
        public virtual StateProvince GetStateProvinceById(int stateProvinceId)
        {
            if (stateProvinceId == 0)
                return null;

            return _stateProvinceRepository.ToCachedGetById(stateProvinceId);
        }

        /// <summary>
        /// Gets a state/province by abbreviation
        /// </summary>
        /// <param name="abbreviation">The state/province abbreviation</param>
        /// <param name="countryId">Country identifier; pass null to load the state regardless of a country</param>
        /// <returns>State/province</returns>
        public virtual StateProvince GetStateProvinceByAbbreviation(string abbreviation, int? countryId = null)
        {
            if (string.IsNullOrEmpty(abbreviation))
                return null;

            var key = _cacheKeyService.PrepareKeyForDefaultCache(NopDirectoryDefaults.StateProvincesByAbbreviationCacheKey
                , abbreviation, countryId ?? 0);

            var query = _stateProvinceRepository.Table.Where(state => state.Abbreviation == abbreviation);

            //filter by country
            if (countryId.HasValue)
                query = query.Where(state => state.CountryId == countryId);

            return query.ToCachedFirstOrDefault(key);
        }

        /// <summary>
        /// Gets a state/province by address 
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>Country</returns>
        public virtual StateProvince GetStateProvinceByAddress(Address address)
        {
            return GetStateProvinceById(address?.StateProvinceId ?? 0);
        }

        /// <summary>
        /// Gets a state/province collection by country identifier
        /// </summary>
        /// <param name="countryId">Country identifier</param>
        /// <param name="languageId">Language identifier. It's used to sort states by localized names (if specified); pass 0 to skip it</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>States</returns>
        public virtual IList<StateProvince> GetStateProvincesByCountryId(int countryId, int languageId = 0, bool showHidden = false)
        {
            var key = _cacheKeyService.PrepareKeyForDefaultCache(NopDirectoryDefaults.StateProvincesByCountryCacheKey, countryId, languageId, showHidden);

            return _staticCacheManager.Get(key, () =>
            {
                var query = from sp in _stateProvinceRepository.Table
                            orderby sp.DisplayOrder, sp.Name
                            where sp.CountryId == countryId &&
                            (showHidden || sp.Published)
                            select sp;
                var stateProvinces = query.ToList();

                if (languageId > 0)
                {
                    //we should sort states by localized names when they have the same display order
                    stateProvinces = stateProvinces
                        .OrderBy(c => c.DisplayOrder)
                        .ThenBy(c => _localizationService.GetLocalized(c, x => x.Name, languageId))
                        .ToList();
                }

                return stateProvinces;
            });
        }

        /// <summary>
        /// Gets all states/provinces
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>States</returns>
        public virtual IList<StateProvince> GetStateProvinces(bool showHidden = false)
        {
            var query = from sp in _stateProvinceRepository.Table
                        orderby sp.CountryId, sp.DisplayOrder, sp.Name
                        where showHidden || sp.Published
                        select sp;

            var stateProvinces = query.ToCachedList(_cacheKeyService.PrepareKeyForDefaultCache(NopDirectoryDefaults.StateProvincesAllCacheKey, showHidden));

            return stateProvinces;
        }

        /// <summary>
        /// Inserts a state/province
        /// </summary>
        /// <param name="stateProvince">State/province</param>
        public virtual void InsertStateProvince(StateProvince stateProvince)
        {
            if (stateProvince == null)
                throw new ArgumentNullException(nameof(stateProvince));

            _stateProvinceRepository.Insert(stateProvince);

            //event notification
            _eventPublisher.EntityInserted(stateProvince);
        }

        /// <summary>
        /// Updates a state/province
        /// </summary>
        /// <param name="stateProvince">State/province</param>
        public virtual void UpdateStateProvince(StateProvince stateProvince)
        {
            if (stateProvince == null)
                throw new ArgumentNullException(nameof(stateProvince));

            _stateProvinceRepository.Update(stateProvince);

            //event notification
            _eventPublisher.EntityUpdated(stateProvince);
        }

        #endregion
    }
}