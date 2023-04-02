using Hzdtf.Consul.Extensions.Common;
using Hzdtf.Utility;
using Hzdtf.Utility.RemoteService;
using Hzdtf.Utility.RemoteService.Options;
using Hzdtf.Utility.Utils;
using Yarp.ReverseProxy.Configuration;

namespace Hzdtf.Consul.YarpExtensions
{
    /// <summary>
    /// Yarp提供者
    /// @ 黄振东
    /// </summary>
    internal static class YarpPrivater
    {
        /// <summary>
        /// 路由列表
        /// </summary>
        private static IReadOnlyList<RouteConfig> routes;

        /// <summary>
        /// 同步路由列表
        /// </summary>
        private static readonly object syncRoutes = new object();

        /// <summary>
        /// 路由列表
        /// </summary>
        private static IReadOnlyList<RouteConfig> Routes
        {
            get
            {
                if (routes == null)
                {
                    lock (syncRoutes)
                    {
                        if (routes == null)
                        {
                            routes = YarpGlobal.Options.ReverseProxy.ToRouteList();
                        }
                    }
                }

                return routes;
            }
        }

        /// <summary>
        /// 从服务里初始化集群Consul
        /// </summary>
        public static void InitClusterFromServicePrivater()
        {
            if (YarpGlobal.Options.ReverseProxy.Clusters.IsNullOrCount0())
            {
                return;
            }

            var servicePrivider = UnitServiceBuilderOptions.Instance.ServicesProvider;
            ConsulServicesProvider.GlobalGetAddressesed += ServicePrivider_GetAddressesed;
            foreach (var cluster in YarpGlobal.Options.ReverseProxy.Clusters)
            {
                servicePrivider.GetAddresses(cluster.Key);
            }
        }

        /// <summary>
        /// 获取到地址数组后
        /// </summary>
        /// <param name="arg1">服务名</param>
        /// <param name="arg2">标签</param>
        /// <param name="arg3">地址数组</param>
        private static void ServicePrivider_GetAddressesed(string arg1, string arg2, string[] arg3)
        {
            var unityService = App.GetServiceFromInstance<IUnityServicesOptions>();
            var unityOptions = unityService.Reader();
            var sheme = unityOptions.GetServiceSheme(arg1, arg2);
            if (!arg3.IsNullOrLength0())
            {
                for (var i = 0; i < arg3.Length; i++)
                {
                    arg3[i] = sheme + "://" + arg3[i];
                }
            }
            bool isChange;
            var newCluses = YarpGlobal.Options.ReverseProxy.UpdateClusters(arg1, arg3, out isChange);
            if (isChange)
            {
                App.GetServiceFromInstance<InMemoryConfigProvider>().Update(Routes, newCluses);
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="cluster">集群</param>
        /// <param name="addresses">地址数组</param>
        public static void UpdateClusterFromServicePrivater(string cluster, string[] addresses)
        {
            bool isChange;
            var newCluses = YarpGlobal.Options.ReverseProxy.UpdateClusters(cluster, addresses, out isChange);
            if (isChange)
            {
                App.GetServiceFromInstance<InMemoryConfigProvider>().Update(Routes, newCluses);
            }
        }
    }
}
