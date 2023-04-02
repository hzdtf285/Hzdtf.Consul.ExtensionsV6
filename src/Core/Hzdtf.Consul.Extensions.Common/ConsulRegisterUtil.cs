using Consul;
using Hzdtf.Utility.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace Hzdtf.Consul.Extensions.Common
{
    /// <summary>
    /// Consul注册工具类
    /// @ 黄振东
    /// </summary>
    public static class ConsulRegisterUtil
    {
        /// <summary>
        /// 创建Consul客户端注册并根据选项进行配置
        /// </summary>
        /// <param name="lifetime">生命周期</param>
        /// <param name="optionsJsonFile">选项配置JSON文件</param>
        /// <param name="getLocalServiceAddress">回调获取本地服务地址</param>
        /// <param name="config">回调选项配置</param>
        /// <returns>Consul客户端</returns>
        public static ConsulClient CreateConsulClientRegister(IHostApplicationLifetime lifetime, string optionsJsonFile = "Config/consulConfig.json", Func<string, string[], string> getLocalServiceAddress = null, Action<ConsulOptions> config = null)
        {
            if (string.IsNullOrWhiteSpace(optionsJsonFile))
            {
                throw new ArgumentNullException("配置JSON文件路径不能为空");
            }

            var options = optionsJsonFile.ToJsonObjectFromFile<ConsulOptions>();
            if (config != null)
            {
                config(options);
            }

            return CreateConsulClientRegister(options, lifetime, getLocalServiceAddress);
        }

        /// <summary>
        /// 创建Consul客户端注册并根据选项进行配置
        /// </summary>
        /// <param name="options">选项配置</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="getLocalServiceAddress">回调获取本地服务地址</param>
        /// <returns>Consul客户端</returns>
        public static ConsulClient CreateConsulClientRegister(ConsulOptions options, IHostApplicationLifetime lifetime, Func<string, string[], string> getLocalServiceAddress = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException("选项配置不能为null");
            }
            if (string.IsNullOrWhiteSpace(options.ConsulAddress))
            {
                throw new ArgumentNullException("Consul地址不能为空");
            }
            if (string.IsNullOrWhiteSpace(options.ServiceName))
            {
                throw new ArgumentNullException("服务名不能为空");
            }

            // 服务ID，如果有配置指定，则使用配置，否则由程序生成唯一
            options.ServiceId = options.ServiceId ?? $"{NetworkUtil.LocalIP}_{StringUtil.NewShortGuid()}";

            // 定义consul客户端对象
            var consulClient = new ConsulClient(clientConfig =>
            {
                clientConfig.Address = new Uri(options.ConsulAddress);
                clientConfig.Datacenter = options.Datacenter;
            });

            // 获取本服务的地址，如果不为空，则直接取。否则取配置选项里的服务地址
            if (string.IsNullOrWhiteSpace(options.ServiceAddress) && getLocalServiceAddress != null)
            {
                options.ServiceAddress = NetworkUtil.FilterUrl(getLocalServiceAddress(options.ServiceName, options.Tags));
            }
            if (string.IsNullOrWhiteSpace(options.ServiceAddress))
            {
                throw new ArgumentNullException("服务地址不能为空");
            }

            // 定义一个代理服务注册对象
            var registration = CreateAgent(options.ServiceAddress, options.ServiceId, options.Tags, options.ServiceName, options.ServiceCheck);
           
            lifetime.ApplicationStarted.Register(() =>
            {
                // 将注册配置信息注册到consul里
                consulClient.Agent.ServiceRegister(registration).Wait();
            });
            lifetime.ApplicationStopping.Register(() =>
            {
                // 程序退出时需要反注册服务
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });

            return consulClient;
        }

        /// <summary>
        /// 创建代理
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="serviceId">服务ID，唯一</param>
        /// <param name="tags">标签</param>
        /// <param name="serviceName">服务名</param>
        /// <param name="check">检测</param>
        /// <returns>代理</returns>
        private static AgentServiceRegistration CreateAgent(string address, string serviceId, string[] tags, string serviceName, ServiceCheckOptions check)
        {
            var serviceUri = new Uri(address);

            // 定义一个代理服务注册对象
            return new AgentServiceRegistration()
            {
                ID = serviceId,
                Tags = tags,
                Check = new AgentServiceCheck() // 定义健康检测对象
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(check.DeregisterCriticalServiceAfter), // 服务停止后多久注销服务
                    HTTP = $"{serviceUri.Scheme}://{serviceUri.Host}:{serviceUri.Port}{check.HealthCheck}", // 健康检测URL地址
                    Interval = TimeSpan.FromSeconds(check.Interval), // 每隔多少时间检测
                    Timeout = TimeSpan.FromSeconds(check.Timeout), // 注册超时时间                   
                },
                Name = serviceName,
                Address = serviceUri.Host,
                Port = serviceUri.Port,
            };
        }
    }
}
