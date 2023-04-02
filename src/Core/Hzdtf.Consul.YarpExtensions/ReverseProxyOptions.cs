using Hzdtf.Utility.Utils;
using Yarp.ReverseProxy.Configuration;

namespace Hzdtf.Consul.YarpExtensions
{
    /// <summary>
    /// 反向代理选项
    /// @ 黄振东
    /// </summary>
    public class ReverseProxyOptions
    {
        /// <summary>
        /// 反向代理配置
        /// </summary>
       public ReverseProxyConfig ReverseProxy { get; set;}
    }

    /// <summary>
    /// 反向代理配置
    /// @ 黄振东
    /// </summary>
    public class ReverseProxyConfig
    {
        /// <summary>
        /// 路由字典
        /// </summary>
        public IDictionary<string, RouteConfig> Routes { get; set; }

        /// <summary>
        /// 集群字典
        /// </summary>
        public IDictionary<string, ClusterConfig> Clusters { get; set; }

        /// <summary>
        /// 转换为路由列表
        /// </summary>
        /// <returns>路由列表</returns>
        public IReadOnlyList<RouteConfig> ToRouteList()
        {
            if (Routes == null || Routes.Count == 0)
            {
                return null;
            }

            var routes = new List<RouteConfig>();
            foreach (var item in Routes)
            {
                var original = item.Value;
                routes.Add(new RouteConfig()
                {
                    RouteId = item.Key,
                    Match = original.Match,
                    Order = original.Order,
                    ClusterId = original.ClusterId,
                    AuthorizationPolicy = original.AuthorizationPolicy,
                    CorsPolicy = original.CorsPolicy,
                    MaxRequestBodySize = original.MaxRequestBodySize,
                    Metadata = original.Metadata,
                    Transforms = original.Transforms,
                });
            }

            return routes;
        }

        /// <summary>
        /// 转换为集群列表
        /// </summary>
        /// <returns>集群列表</returns>
        public IReadOnlyList<ClusterConfig> ToClusterList()
        {
            if (Clusters == null || Clusters.Count == 0)
            {
                return null;
            }

            var clusters = new List<ClusterConfig>();
            foreach (var item in Clusters)
            {
                var original = item.Value;
                clusters.Add(new ClusterConfig()
                {
                    ClusterId = item.Key,
                    LoadBalancingPolicy = original.LoadBalancingPolicy,
                    SessionAffinity = original.SessionAffinity,
                    HealthCheck = original.HealthCheck,
                    HttpClient = original.HttpClient,
                    HttpRequest = original.HttpRequest,
                    Metadata = original.Metadata,
                    Destinations = original.Destinations,
                });
            }

            return clusters;
        }

        /// <summary>
        /// 转换为集群字典
        /// </summary>
        /// <param name="ignoreDestinationKey">忽略目的键</param>
        /// <returns>集群字典</returns>
        public IDictionary<string, ClusterConfig> ToClusterDic(params string[] ignoreDestinationKey)
        {
            if (Clusters == null || Clusters.Count == 0)
            {
                return null;
            }

            var clusters = new Dictionary<string, ClusterConfig>();
            foreach (var item in Clusters)
            {
                var original = item.Value;

                IReadOnlyDictionary<string, DestinationConfig> dicDeses = null;
                if (original.Destinations == null || original.Destinations.Count == 0
                    || ignoreDestinationKey == null || ignoreDestinationKey.Length == 0)
                {
                    dicDeses = original.Destinations;
                }
                else
                {
                    var newDicDeses = new Dictionary<string, DestinationConfig>();
                    foreach (var des in original.Destinations)
                    {
                        if (ignoreDestinationKey.Contains(des.Key))
                        {
                            continue;
                        }
                        newDicDeses.Add(des.Key, des.Value);
                    }
                    dicDeses = newDicDeses;
                }

                clusters.Add(item.Key, new ClusterConfig()
                {
                    ClusterId = item.Key,
                    LoadBalancingPolicy = original.LoadBalancingPolicy,
                    SessionAffinity = original.SessionAffinity,
                    HealthCheck = original.HealthCheck,
                    HttpClient = original.HttpClient,
                    HttpRequest = original.HttpRequest,
                    Metadata = original.Metadata,
                    Destinations = dicDeses,
                });
            }

            Clusters = clusters;

            return clusters;
        }

        /// <summary>
        /// 更新集群
        /// </summary>
        /// <param name="cluster">集群</param>
        /// <param name="addresses">地址数组</param>
        /// <param name="isChange">是否改变</param>
        /// <returns>集群列表</returns>
        public IReadOnlyList<ClusterConfig> UpdateClusters(string cluster, string[] addresses, out bool isChange)
        {
            isChange = false;
            if (Clusters == null || Clusters.Count == 0)
            {
                return null;
            }

            var clusters = new Dictionary<string, ClusterConfig>();
            foreach (var item in Clusters)
            {
                var original = item.Value;

                var thisChange = false;

                IReadOnlyDictionary<string, DestinationConfig> dicDeses = null;
                if (item.Key.Equals(cluster))
                {
                    var length = addresses.IsNullOrLength0() ? 0 : addresses.Length;
                    var tempDicDeses = new Dictionary<string, DestinationConfig>(length);

                    var existsDesces = item.Value.Destinations;
                    var isExistsDescesData = existsDesces != null && existsDesces.Count > 0;
                    if (isExistsDescesData)
                    {
                        if (addresses.IsNullOrLength0())
                        {
                            thisChange = true;
                        }
                        else
                        {
                            foreach (var add in addresses)
                            {
                                var exists = existsDesces.FirstOrDefault(p => p.Value.Address.Equals(add));
                                if (exists.Value == null)
                                {
                                    var newDes = new DestinationConfig()
                                    {
                                        Address = add,
                                        Metadata = original.Metadata,
                                    };
                                    tempDicDeses.Add(StringUtil.NewShortGuid(), newDes);
                                    thisChange = true;
                                }
                                else
                                {
                                    tempDicDeses.Add(exists.Key, exists.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!addresses.IsNullOrLength0())
                        {
                            foreach (var add in addresses)
                            {
                                var newDes = new DestinationConfig()
                                {
                                    Address = add,
                                    Metadata = original.Metadata,
                                };
                                tempDicDeses.Add(StringUtil.NewShortGuid(), newDes);
                            }

                            thisChange = true;
                        }
                    }
                    // 如果本次未更改（相当于上面未有新增），则判断是否有不存在的
                    if (!isChange && !thisChange && isExistsDescesData && addresses != null && addresses.Length == 0)
                    {
                        foreach (var existsD in existsDesces)
                        {
                            if (addresses.Any(p => p == existsD.Value.Address))
                            {
                                continue;
                            }
                            thisChange = true;
                            break;
                        }
                    }

                    if (thisChange)
                    {
                        dicDeses = tempDicDeses;
                    }
                    else
                    {
                        dicDeses = item.Value.Destinations;
                    }
                }
                else
                {
                    dicDeses = item.Value.Destinations;
                }

                if (!isChange && thisChange)
                {
                    isChange = true;
                }


                clusters.Add(item.Key, new ClusterConfig()
                {
                    ClusterId = item.Key,
                    LoadBalancingPolicy = original.LoadBalancingPolicy,
                    SessionAffinity = original.SessionAffinity,
                    HealthCheck = original.HealthCheck,
                    HttpClient = original.HttpClient,
                    HttpRequest = original.HttpRequest,
                    Metadata = original.Metadata,
                    Destinations = dicDeses,
                });
            }

            if (isChange)
            {
                Clusters = clusters;
            }

            return ToClusterList();
        }
    }
}
