﻿using Abp.Localization;
using Abp.Modules;
using System.Reflection;
using Abp.Reflection;
using AutoMapper;
using Castle.Core.Logging;

namespace Abp.AutoMapper
{
    public class AbpAutoMapperModule : AbpModule
    {
        public ILogger Logger { get; set; }

        public ILocalizationManager LocalizationManager { get; set; }

        private readonly ITypeFinder _typeFinder;

        private static bool _createdMappingsBefore;
        private static readonly object _syncObj = new object();

        public AbpAutoMapperModule(ITypeFinder typeFinder)
        {
            _typeFinder = typeFinder;
            Logger = NullLogger.Instance;
            LocalizationManager = NullLocalizationManager.Instance;
        }

        public override void PostInitialize<TTenantId, TUserId>()
        {
            CreateMappings();
        }

        private void CreateMappings()
        {
            lock (_syncObj)
            {
                //We should prevent duplicate mapping in an application, since AutoMapper is static.
                if (_createdMappingsBefore)
                {
                    return;
                }

                AutoMapperHelper.Initialize(configuration =>
                {

                    FindAndAutoMapTypes(configuration);
                    CreateOtherMappings(configuration);
                });

                _createdMappingsBefore = true;
            }
        }

        private void FindAndAutoMapTypes(IConfiguration configuration)
        {
            var types = _typeFinder.Find(type =>
                type.IsDefined(typeof(AutoMapAttribute)) ||
                type.IsDefined(typeof(AutoMapFromAttribute)) ||
                type.IsDefined(typeof(AutoMapToAttribute))
                );

            Logger.DebugFormat("Found {0} classes defines auto mapping attributes", types.Length);
            foreach (var type in types)
            {
                Logger.Debug(type.FullName);
                AutoMapperHelper.CreateMap(configuration, type);
            }
        }

        private void CreateOtherMappings(IConfiguration configuration)
        {
            configuration.CreateMap<LocalizableString, string>().ConvertUsing(ls => LocalizationManager.GetString(ls.SourceName, ls.Name));
        }
    }
}
