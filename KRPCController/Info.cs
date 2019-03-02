using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPCController
{
    static class Info
    {
        public static bool paused = false;

        public class InfoValue
        {
            public string info;
            public int times;
            public InfoValue(string info)
            {
                this.info = info;
                this.times = 1;
            }
        }
        public static Dictionary<string, InfoValue> infos = new Dictionary<string, InfoValue>();
        public static void AddInfo(string key, string value)
        {
            if (infos.ContainsKey(key))
            {
                infos[key].times++;
                infos[key].info = value;
            }
            else
            {
                infos.Add(key, new InfoValue(value));
            }
        }

        public static void RemoveInfo(string name)
        {
            if (infos.ContainsKey(name))
            {
                infos.Remove(name);
            }
        }
        public static void RemoveAll()
        {
            infos.Clear();
        }

        public static void Update()
        {
            if (!paused)
            {
                var str = "";

                foreach (var i in infos)
                {
                    var stat = "N";
                    if (i.Value.times == 0)
                        stat = "O";
                    else if (i.Value.times > 1)
                        stat = "D";
                    //var outdated = i.Value.times == 0 ? "(Outdated)" : "";
                    //var duplicated = i.Value.times > 1 ? "(Duplicated)" : "";
                    str += string.Format("[{2}]{0} :  {1}   \r\n", i.Key, i.Value.info, stat);
                }
                ConnectionInitializer.UpdateInfo(str);

                Clear();
            }
        }

        static void Clear()
        {
            foreach(var i in infos)
            {
                i.Value.times = 0;
            }
        }
    }
}
