using Hzdtf.Consul.YarpExtensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Yarp扩展类
    /// @ 黄振东
    /// </summary>
    public static class YarpExtensions
    {
        /// <summary>
        /// 添加Yarp
        /// </summary>
        /// <param name="services">服务收藏</param>
        /// <param name="options">选项配置</param>
        /// <returns>服务收藏</returns>
        public static IServiceCollection AddYarp(this IServiceCollection services, Action<YarpOptions> options = null)
        {
            YarpGlobal.YarpOptions = new YarpOptions();

            if (options != null)
            {
                options(YarpGlobal.YarpOptions);
            }

            YarpGlobal.Options = YarpGlobal.YarpOptions.YarpJsonFile.ToJsonObjectFromFile<ReverseProxyOptions>();
            var routes = YarpGlobal.Options.ReverseProxy.ToRouteList();
            var clusters = YarpGlobal.Options.ReverseProxy.ToClusterList();

            services.AddReverseProxy().LoadFromMemory(routes, clusters);

            if (YarpGlobal.YarpOptions.UseConsul)
            {
                if (YarpGlobal.YarpOptions.UseConsulConfigCenter)
                {
                    services.AddDiscoveryConsulConfigCenter(YarpGlobal.YarpOptions.ConsulOptionsCall);
                }
                else
                {
                    services.AddDiscoveryConsul(YarpGlobal.YarpOptions.ConsulOptionsCall);
                }
            }

            return services;
        }

        /// <summary>
        /// 使用Yarp
        /// </summary>
        /// <param name="app">应用</param>
        /// <param name="lifetime">生命周期</param>
        /// <returns>应用</returns>
        public static IEndpointRouteBuilder UseYarp(this IEndpointRouteBuilder app, IHostApplicationLifetime lifetime)
        {
            app.MapReverseProxy();

            if (YarpGlobal.YarpOptions.UseConsul)
            {
                lifetime.ApplicationStarted.Register(() =>
                {
                    YarpPrivater.InitClusterFromServicePrivater();
                });
            }

            return app;
        }
    }
}
