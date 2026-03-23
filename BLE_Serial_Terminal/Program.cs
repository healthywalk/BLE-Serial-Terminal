using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace BLE_Serial_Terminal
{
    static class Program
    {
        [DllImport("ole32.dll")]
        public static extern int CoInitializeSecurity(
        IntPtr pSecDesc,
        int cAuthSvc,
        IntPtr asAuthSvc,
        IntPtr pReserved1,
        uint dwAuthnLevel,
        uint dwImpLevel,
        IntPtr pAuthList,
        uint dwCapabilities,
        IntPtr pReserved3);

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            int hr = CoInitializeSecurity(
            IntPtr.Zero,
            -1,
            IntPtr.Zero,
            IntPtr.Zero,
            1, // RPC_C_AUTHN_LEVEL_NONE
            3, // RPC_C_IMP_LEVEL_IMPERSONATE
            IntPtr.Zero,
            0x20, // EOAC_NONE (または 0)
            IntPtr.Zero);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
