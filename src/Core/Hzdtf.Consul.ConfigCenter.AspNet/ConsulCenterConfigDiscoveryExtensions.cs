﻿using Hzdtf.Consul.Extensions.AspNet;
using Hzdtf.Utility.SystemV2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hzdtf.Consul.Extensions.Common;
using Microsoft.Extensions.Configuration;
using Hzdtf.Utility.RemoteService.Provider;
using Microsoft.AspNetCore.Builder;
using Hzdtf.Consul.ConfigCenter.AspNet;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Consul配置中心服务发现扩展类
    /// @ 黄振东
    /// </summary>
    public static class ConsulCenterConfigDiscoveryExtensions
    {
        /// <summary>
        /// 添加发现Consul配置中心
        /// </summary>
        /// <param name="services">服务</param>
        /// <param name="options">配置回调</param>
        /// <returns>服务</returns>
        public static IServiceCollection AddDiscoveryConsulConfigCenter(this IServiceCollection services, Action<UnityConsulOptions> options = null)
        {
            var unityConsulOptions = new UnityConsulOptions();
            if (options != null)
            {
                options(unityConsulOptions);
            }
            if (unityConsulOptions.ConsulBasicOption == null)
            {
                var config = new ConfigurationBuilder().AddJsonFile(unityConsulOptions.ConsulBasicOptionJsonFile).Build();
                services.Configure<ConsulBasicOption>(config);

                unityConsulOptions.ConsulBasicOption = config.Get<ConsulBasicOption>();
            }

            if (unityConsulOptions.ConsulBasicOption != null)
            {
                services.AddBasicConsul(unityConsulOptions.ConsulBasicOption);
            }
            else if (unityConsulOptions.ConsulBasicOptionJsonFile == null)
            {
                services.AddBasicConsul();
            }
            else
            {
                services.AddBasicConsul(unityConsulOptions.ConsulBasicOptionJsonFile);
            }

            services.AddUnityServicesBuilderConfigure(builder =>
            {
                if (unityConsulOptions.UnityServicesOptionsJsonFile != null)
                {
                    builder.ServiceBuilderConfigJsonFile = unityConsulOptions.UnityServicesOptionsJsonFile;
                }

                switch (unityConsulOptions.CacheType)
                {
                    case ServiceProviderCacheType.TIMER_REFRESH:
                        builder.ServicesProvider = new ConsulServiceProviderAgg(unityConsulOptions.ConsulBasicOption.IntervalMillseconds, unityConsulOptions.ConsulBasicOption);

                        break;

                    case ServiceProviderCacheType.DALAY_REFRESH:
                        services.AddMemoryCache(options =>
                        {
                            options.Clock = new LocalSystemClock();
                        });

                        builder.ServicesProvider = new ConsulServicesProviderMemory(Microsoft.Extensions.Options.Options.Create<ConsulBasicOption>(unityConsulOptions.ConsulBasicOption));

                        break;

                    default:
                        builder.ServicesProvider = new ConsulServicesProvider(unityConsulOptions.ConsulBasicOption);

                        break;
                }
            }, (builder, file, data) =>
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    return;
                }

                builder.AddConsulConfigCenter(options: op =>
                {
                    if (unityConsulOptions.ConsulBasicOption == null)
                    {
                        if (string.IsNullOrWhiteSpace(unityConsulOptions.ConsulBasicOptionJsonFile))
                        {
                            throw new ArgumentNullException("Consul基本配置Json文件不能为空");
                        }
                        else
                        {
                            var centerOptions = unityConsulOptions.ConsulBasicOptionJsonFile.ToJsonObjectFromFile<ConfigCenterOptions>();
                            op.FillFrom(centerOptions);
                        }
                    }
                    else
                    {
                        op.FillFrom(unityConsulOptions.ConsulBasicOption);
                    }

                    var key = ConfigCenterUtil.GetKeyPath(file, op.ServiceName);
                    op.Keys.Add(key);
                });
            });

            return services;
        }
    }
}
