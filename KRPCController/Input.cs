using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KRPCController
{
    /// <summary>
    /// 用于在Behaviour.Update()中获取键盘输入（类似Unity）
    /// </summary>
    static class Input
    {
        public static Dictionary<Keys, bool> keysStatus = new Dictionary<Keys, bool>();
        public static Dictionary<Keys, bool> keysChangeTemp = new Dictionary<Keys, bool>();
        public static Dictionary<Keys, bool> keysChange = new Dictionary<Keys, bool>();

        public static void Update()
        {
            keysChange.Clear();
            foreach(var key in keysChangeTemp)
            {
                keysChange.Add(key.Key, key.Value);
            }
            keysChangeTemp.Clear();
        }

        public static void OnKeyDown(KeyEventArgs e)
        {
            UpdateKeyStatus(e.KeyCode, true);
            //ConnectionInitializer.Log("Key:" + e.KeyCode.ToString());
        }

        public static void OnKeyUp(KeyEventArgs e)
        {
            UpdateKeyStatus(e.KeyCode, false);
        }

        public static void UpdateKeyStatus(Keys keyCode, bool isDown)
        {

            if (keysChangeTemp.ContainsKey(keyCode))
            {
                keysChangeTemp[keyCode] = isDown;
            }
            else
            {
                if (GetKey(keyCode) ^ isDown)
                {
                    keysChangeTemp.Add(keyCode, isDown);
                }
            }

            if (keysStatus.ContainsKey(keyCode))
            {
                keysStatus[keyCode] = isDown;
            }
            else
            {
                keysStatus.Add(keyCode, isDown);
            }
        }

        public static bool GetKey(Keys keyCode)
        {
            if (keysStatus.ContainsKey(keyCode))
            {
                return keysStatus[keyCode];
            }
            return false;
        }

        public static bool GetKeyDown(Keys keyCode)
        {
            if (keysChange.ContainsKey(keyCode))
            {
                return keysChange[keyCode];
            }
            else
            {
                return false;
            }
        }

        public static bool GetKeyUp(Keys keyCode)
        {
            if (keysChange.ContainsKey(keyCode))
            {
                return !keysChange[keyCode];
            }
            else
            {
                return false;
            }
        }
    }
}
