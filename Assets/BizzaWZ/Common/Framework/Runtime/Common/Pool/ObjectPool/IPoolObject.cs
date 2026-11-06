using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// namespace BizzaCommon
// {
    /// <summary>
    /// 接口：对象池对象
    /// </summary>
    public interface IPoolObject
    {
        /// <summary>
        /// 从对象池中取出后进行初始化的接口
        /// </summary>
        void GetFromPool();

        /// <summary>
        /// 放回对象池前进行卸载的接口
        /// </summary>
        void ReleaseToPool();
    }
// }
