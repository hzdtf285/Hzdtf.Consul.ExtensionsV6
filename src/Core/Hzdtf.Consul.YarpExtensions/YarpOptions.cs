using Hzdtf.Consul.Extensions.AspNet;

namespace Hzdtf.Consul.YarpExtensions
{
    /// <summary>
    /// Yarp选项
    /// @ 黄振东
    /// </summary>
    public class YarpOptions
    {
        /// <summary>
        /// Yarp Json文件
        /// </summary>
        public string YarpJsonFile
        {
            get;
            set;
        } = "yarp.json";

        /// <summary>
        /// 使用Consul,默认为是
        /// </summary>
        public bool UseConsul
        {
            get;
            set;
        } = true;

        /// <summary>
        /// 使用Consul配置中心，默认为否
        /// </summary>
        public bool UseConsulConfigCenter
        {
            get;
            set;
        }

        /// <summary>
        /// 统计Consul选项回调
        /// </summary>
        public Action<UnityConsulOptions> ConsulOptionsCall
        {
            get;
            set;
        }
    }
}
