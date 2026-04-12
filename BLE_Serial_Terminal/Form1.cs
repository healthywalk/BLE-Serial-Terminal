using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;        //Asbuffer
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
//using System.Runtime.InteropServices;

namespace BLE_Serial_Terminal
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11; //描画停止再開メッセージ
        private const int EM_LINESCROLL = 0x00B6; // スクロールメッセージ

        private BluetoothLEAdvertisementWatcher watcher;
        private bool deviceConnected = false;
        public Form1()
        {
            //Debug.WriteLine("Form1");
            InitializeComponent();
            //Properties.Settings.Default.Reload();
            Load += Form1_Load;
            ScanBle();                 //起動と同時にBleデバイスとの接続を試みる。
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            //ApplicationExitイベントハンドラを追加
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);

            // 送信時の改行
            this.cmbBoxLBSend.Items.Clear();
            this.cmbBoxLBSend.Items.Add("CR");
            this.cmbBoxLBSend.Items.Add("LF");
            this.cmbBoxLBSend.Items.Add("CR+LF");
            this.cmbBoxLBSend.Items.Add("NONE");
            this.cmbBoxLBSend.SelectedIndex = Properties.Settings.Default.linebreaks;

            //ローカルエコー
            this.cBoxLocalEcho.Checked = Properties.Settings.Default.localecho;

            //タイムスタンプ
            this.cBoxTimeStamp.Checked = Properties.Settings.Default.timestamp;

            this.cBoxAutoScroll.Checked = true;

            //デバイス候補コンボボックス
            this.cmbBoxDevice.Items.Clear();
            this.cmbBoxDevice.SelectedIndex = -1;
            NumItems = 0;

            //入力textbox
            this.textToBeSent.Enabled = false;

            //sendボタン
            this.btnSend.Enabled = false;

            //costum button setting
            generateCustumButton();

            //CoInitializeSecurity(IntPtr.Zero, -1, IntPtr.Zero, IntPtr.Zero, RpcAuthnLevel.Default, RpcImpLevel.Identify, IntPtr.Zero, EoAuthnCap.None, IntPtr.Zero);


        }

        //スキャン他　起動時に呼び出し
        private async void ScanBle()
        {
            watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += Watcher_Received;
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Start();
            this.btnScan.Enabled = false;
            //this.Cursor = Cursors.WaitCursor;
            //Debug.WriteLine("ScanBle");
            //5秒間スキャンする
            await Task.Delay(5000);
            //this.Cursor = Cursors.Default;
            watcher.Stop();
            if (!this.deviceConnected)
            {
                this.btnScan.Enabled = true;
            }
        }

        private int NumItems;
        public void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            //登録する
            //BLEAddress = args.BluetoothAddress;
            string adr = args.BluetoothAddress.ToString("X12");
            Debug.WriteLine("Watcher_Received > " + adr);
            if (adr.Length != 12) return;
            string devaddress = adr.Substring(0, 2)+":"+ adr.Substring(2, 2) + ":" + adr.Substring(4, 2) + ":"
                              + adr.Substring(6, 2) + ":" + adr.Substring(8, 2) + ":" + adr.Substring(10, 2);
            string devname = args.Advertisement.LocalName;
            //string deviceID = args.Id; これはない
            if (devname.Length == 0) return;
            Debug.WriteLine("Watcher_Received > " + devname + " :  " + devaddress);
            foreach (string item in this.cmbBoxDevice.Items)
            {
                if (item.Contains(devaddress) && item.Contains(devname))
                {
                    return;
                }
            }
            string tempItem = devname + " :  " + devaddress;

            this.Invoke(new MethodInvoker(delegate
            {
                cmbBoxDevice.Items.Add(tempItem);
                if (++NumItems == 1) this.cmbBoxDevice.SelectedIndex = 0;
            }));
        }


        private const String SERVICE_UUID = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E"; // UART service UUID
        private const String CHARACTERISTIC_UUID_RX = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
        private const String CHARACTERISTIC_UUID_TX = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

        private BluetoothLEDevice device;

        //受信関係はTX，送信関係はRXになる。
        //Serverを中心に考えるため，このアプリはClientなので，意味が逆になる
        //元の意味はTX:Transmitter，RX:Receiver
        private GattCharacteristic cTX;
        private GattCharacteristic cRX;

        private UInt64 address;
        private String devicename;
        static private GattSession session = null;

        async Task connectSelectedDevice()
        {
            try
            {
                //デバイスに接続する
                Debug.WriteLine($"connecting Selected Device > {devicename}");
                device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
                if (device == null)
                {
                    Debug.WriteLine("Device connection failed.");
                    this.deviceConnected = false;
                    return;
                }
                else
                {
                    Debug.WriteLine("The device has been connected.");
                }

                Debug.WriteLine($"0 device.ConnectionStatus: {device.ConnectionStatus}");

                if (device.DeviceInformation.Pairing.CanPair && !device.DeviceInformation.Pairing.IsPaired)
                {
                    // パスキー入力のポップアップを出す、またはシステムに任せる
                    //evicePairingProtectionLevel
                    //  Encryption 暗号化を使用してデバイスをペアリングします。
                    //  EncryptionAndAuthentication 暗号化と認証を使用してデバイスをペアリングします。
                    //  None 保護レベルを使用せず、デバイスをペアリングします。
                    //var pairingResult = await device.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                    //デフォルトの設定でペアリングを試みます
                    var pairingResult = await device.DeviceInformation.Pairing.PairAsync();
                    Console.WriteLine($"Pairing Status: {pairingResult.Status}");
                }
                else
                {
                    Console.WriteLine($"Pairing CanPair: {device.DeviceInformation.Pairing.CanPair}");
                    Console.WriteLine($"Pairing IsPaired: {device.DeviceInformation.Pairing.IsPaired}");
                }

                DeviceAccessStatus accessStatus = await device.RequestAccessAsync(); //追加20260301
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    Debug.WriteLine("BLE device accessStatu: Allowed");
                }
                else
                {
                    Debug.WriteLine("BLE device accessStatu: Not allowed");
                    this.deviceConnected = false;
                    return;
                }

                //using (var session = await GattSession.FromDeviceIdAsync(device.BluetoothDeviceId))
                session = await GattSession.FromDeviceIdAsync(device.BluetoothDeviceId);
                {
                    session.MaintainConnection = true; // Keep the session as is.
                    //NimBLEはここでonConnectが発火する
                    //BLE (esp32/Espressif Systems 3.2.1) もここでonConnectが発火する

                    // Retrieve the service while maintaining the session.
                    //var services0 = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                    var services0 = await device.GetGattServicesAsync(BluetoothCacheMode.Cached);
                    //NimBLEはここでonConnectが発火する（session.MaintainConnection = true; がなかった場合）
                    Debug.WriteLine($"Status with Session: {services0.Status}");
                }

                Debug.WriteLine($"1 device.ConnectionStatus: {device.ConnectionStatus}");

                var maxPduSize = session.MaxPduSize; //試しに接続する
                Debug.WriteLine($"Max PDU Size: {maxPduSize}");
                

                // debug information  ---From here
                var allServices = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                // If the first attempt is unreachable, wait a bit and retry.
                if (allServices.Status == GattCommunicationStatus.Unreachable)
                {
                    Debug.WriteLine("The first attempt failed, so we will retry.");
                    await Task.Delay(1000);
                    allServices = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                }

                Debug.WriteLine($"All Services Status: {allServices.Status}");
                

                // debug information
                /*
                    connecting Selected Device > UART test
                    The device has been connected.
                    BLE device accessStatu: Allowed
                    Status with Session: Success
                    device.ConnectionStatus: Connected
                    Max PDU Size: 256
                    All Services Status: Success
                    Max PDU Size: 256
                    Service.status: Success
                    Service.count: 1
                    Max PDU Size: 256
                    CharacteristicsTX.Status: Success
                    CharacteristicsTX.String: Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicsResult
                    Max PDU Size: 256
                    Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic
                    CharacteristicsRX.Status: Success
                    CharacteristicsRX.String: Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicsResult
                    Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic
                    device.GetGattServicesAsync Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceServicesResult
                    device.GetGattServicesAsync : Success
                    status: Success
                    Server has been informed of clients interest.
                */

                //Retrieve services from UUID
                //var services = await device.GetGattServicesForUuidAsync(new Guid(SERVICE_UUID), BluetoothCacheMode.Uncached);
                var services = await device.GetGattServicesForUuidAsync(new Guid(SERVICE_UUID), BluetoothCacheMode.Cached);
                Debug.WriteLine($"Service.status: {services.Status}");
                Debug.WriteLine($"Service.count: {services.Services.Count}");

                //Retrieve characteristics from UUID
                var characteristicsTX = await services.Services[0].GetCharacteristicsForUuidAsync(new Guid(CHARACTERISTIC_UUID_TX), BluetoothCacheMode.Uncached);
                Debug.WriteLine($"CharacteristicsTX.Status: {characteristicsTX.Status}");
                Debug.WriteLine($"CharacteristicsTX.String: {characteristicsTX.ToString()}");

                cTX = characteristicsTX.Characteristics[0];
                Debug.WriteLine(cTX.ToString());

                //Retrieve characteristics from UUID
                var characteristicsRX = await services.Services[0].GetCharacteristicsForUuidAsync(new Guid(CHARACTERISTIC_UUID_RX));
                Debug.WriteLine($"CharacteristicsRX.Status: {characteristicsRX.Status}");
                Debug.WriteLine($"CharacteristicsRX.String: {characteristicsRX.ToString()}");

                cRX = characteristicsRX.Characteristics[0];
                Debug.WriteLine(cRX.ToString());

                /*TrialSend("test");*/

                GattDeviceServicesResult result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                //This should ensure that the Connect command is reliably transmitted to the server.
                Debug.WriteLine("device.GetGattServicesAsync " + result);
                if (result.Status == GattCommunicationStatus.Success)
                {
                    Debug.WriteLine("device.GetGattServicesAsync : Success");
                }
                else
                {
                    Debug.WriteLine("device.GetGattServicesAsync : false");
                    this.deviceConnected = false;
                    return;
                }

                //Set up a callback to receive notifications.
                cTX.ValueChanged += characteristicBleDevice;

                //Notification Subscription
                GattCommunicationStatus status = await cTX.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                Debug.WriteLine("status: " + status);
                if (status == GattCommunicationStatus.Success)
                {
                    this.deviceConnected = true;
                    Debug.WriteLine("Server has been informed of clients interest.");
                }
                else
                {
                    Debug.WriteLine("** error ** Server has not been informed of clients interest.");
                    this.deviceConnected = false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private async void disconnectDevice()
        {
            try
            {
                //Debug.WriteLine("device.ConnectionStatus = " + device.ConnectionStatus);
                if (device!=null && device.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    if (session != null) session.MaintainConnection = false;
                    cTX.Service.Dispose();
                    cTX = null;
                    device.Dispose();
                    device = null;
                    this.deviceConnected = false;
                    this.btnScan.Enabled = true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        //Processing upon receiving a Notify
        void characteristicBleDevice(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Debug.Write("received: ");
            var streamNotify = args.CharacteristicValue.AsStream();
            //PrintFromStream(streamNotify);
            byte[] byte_text = StreamToBytes(streamNotify);
            string string_text = Encoding.Default.GetString(byte_text);
            string_text = string_text.TrimEnd('\r', '\n');
            Debug.WriteLine(string_text);
            this.Invoke(new MethodInvoker(delegate
            {
                addtextbox(string_text + "\r\n");
            }));
        }

        private void btnScan_clicked(object sender, EventArgs e)
        {
            ScanBle();
        }

        private void btnConnect_clicked(object sender, EventArgs e)
        {
            if (this.btnConnect.Text == "Connect")
            {
                string itemstr = (string)cmbBoxDevice.SelectedItem;
                if (itemstr == null) return;
                if (itemstr.Length <= 17) return;
                this.btnConnect.Enabled = false;
                string adr1 = itemstr.Substring(itemstr.Length - 17);
                //Debug.WriteLine(adr1);
                string adr2 = adr1.Replace(":", "");
                //Debug.WriteLine(adr2);
                address = Convert.ToUInt64(adr2, 16);
                devicename = itemstr.Substring(0, itemstr.IndexOf(" :  ",0));
                //devicename = devicename.Replace(" ", "");
                //Debug.WriteLine("btnConnect_clicked pass");
                addtextbox(">connecting " + devicename + " ..\r\n");
                Task.Run(connectSelectedDevice).Wait();
                if (this.deviceConnected)
                {
                    this.btnConnect.Text = "Disonnect";
                    addtextbox(">connected\r\n");
                    this.btnScan.Enabled = false;
                    this.textToBeSent.Enabled = true;
                    this.btnSend.Enabled = true;
                }
                else
                {
                    addtextbox(">** not connected **\r\n");
                }
                this.btnConnect.Enabled = true;
            }
            else //this.btnConnect.Text == "Disonnect"
            {
                this.btnConnect.Text = "Connect";
                disconnectDevice();
                addtextbox(">disconnected\r\n");
                this.textToBeSent.Enabled = false;
                this.btnSend.Enabled = false;
            }
        }

        private void btnSend_click(object sender, EventArgs e)
        {
            sendCustumbuttonstr(this.textToBeSent.Text);
        }

        private async void sendCustumbuttonstr(string str)
        {
            string[] linebreaks = { "\r", "\n", "\r\n", "" };
            if (str == "") return;
            if (this.cBoxLocalEcho.Checked)
            {
                addtextbox("$$" + str + "\r\n");
            }
            str += linebreaks[this.cmbBoxLBSend.SelectedIndex];
            Debug.WriteLine(str);
            byte[] byte_str = System.Text.Encoding.ASCII.GetBytes(str);
            try
            {
                await cRX.WriteValueAsync(byte_str.AsBuffer());
            }
            catch (Exception e1)
            {
                Debug.WriteLine(e1);
            }
            if (this.cBoxClearSending.Checked) this.textToBeSent.Clear();
        }

        /*private async void TrialSend(string str)
        {
            if (str == "") return;
            byte[] byte_str = System.Text.Encoding.ASCII.GetBytes(str);
            try
            {
                await cRX.WriteValueAsync(byte_str.AsBuffer());
            }
            catch (Exception e1)
            {
                Debug.WriteLine(e1);
            }
        }*/

        private byte[] StreamToBytes(Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            //   
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        void addtextbox(string text1)
        {
            if (this.cBoxTimeStamp.Checked)
            {
                DateTime dt = DateTime.Now;
                text1 = dt.ToString("HH:mm:ss") + ": " + text1;
            }

            if (this.cBoxAutoScroll.Checked)
            {
                this.textBoxReceived.AppendText(text1);
            }
            else
            {
                // 1. 表示エリアの左上隅(0,0)の文字インデックスを取得
                int charIndex = this.textBoxReceived.GetCharIndexFromPosition(new System.Drawing.Point(0, 0));
                // 2. その文字インデックスから行番号を取得
                int firstVisibleLine = this.textBoxReceived.GetLineFromCharIndex(charIndex);
                Debug.WriteLine($"0 st->{firstVisibleLine} ln->{this.textBoxReceived.Lines.Length} tx->[{text1}]");
                int nScroll = firstVisibleLine;
                if (nScroll != 0) nScroll++;
                // 描画停止
                SendMessage(this.textBoxReceived.Handle, WM_SETREDRAW, false, 0);
                this.textBoxReceived.Text += text1;
                //先頭からの表示になるので必要な行数スクロール
                SendMessage(this.textBoxReceived.Handle, EM_LINESCROLL, IntPtr.Zero, (IntPtr)nScroll);
                // 描画再開
                SendMessage(this.textBoxReceived.Handle, WM_SETREDRAW, true, 0);
                this.textBoxReceived.Refresh(); // 再描画を強制
            }
        }

        private void clearLog_clicked(object sender, EventArgs e)
        {
            this.textBoxReceived.Clear();
        }

        private String Getnowstringforfile()
        {
            DateTime dt = DateTime.Now;
            String now = dt.ToString("yyyyMMdd_HHmmss");
            return now;
        }

        private void btnSave_clicked(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = "BLESerialTerminal" + Getnowstringforfile(),
                RestoreDirectory = true
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog1.FileName, this.textBoxReceived.Text);
            }
        }

       //終了時の処理
        //ApplicationExitイベントハンドラ
        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            Properties.Settings.Default.linebreaks = this.cmbBoxLBSend.SelectedIndex;
            Properties.Settings.Default.localecho = this.cBoxLocalEcho.Checked;
            Properties.Settings.Default.timestamp = this.cBoxTimeStamp.Checked;

            Properties.Settings.Default.commandstring = this.custumbuttons[0].Text;
            for (int i = 1; i < NumButtons; i++)
            {
                Properties.Settings.Default.commandstring += '\t' + this.custumbuttons[i].Text;
            }
            Properties.Settings.Default.stringforsend = this.stringtobesent[0];
            for (int i = 1; i < NumButtons; i++)
            {
                Properties.Settings.Default.stringforsend += '\t' + this.stringtobesent[i];
            }
            Properties.Settings.Default.justinsert = this.justinsert[0];
            for (int i = 1; i < NumButtons; i++)
            {
                Properties.Settings.Default.justinsert += '\t' + this.justinsert[i];
            }

            Properties.Settings.Default.Save();
            //Debug.WriteLine("default properties saved");

            //ApplicationExitイベントハンドラを削除
            Application.ApplicationExit -= new EventHandler(Application_ApplicationExit);

            disconnectDevice();
        }

        private Button[] custumbuttons;
        private string[] stringtobesent;
        private string[] justinsert;
        private const String Notsetyet = "undefined";
        private const String Notsetyet_old = "Not set yet";
        private const int NumButtons = 20;

        private void generateCustumButton()
        {
            this.custumbuttons = new Button[NumButtons];
            string[] buttontext = new string[NumButtons]; //これは作業用
            this.stringtobesent = new string[NumButtons];
            this.justinsert = new string[NumButtons];
            int num = 0;

            for (int i0 = 0; i0 < custumbuttons.Length; i0++)
            {
                buttontext[i0] = Notsetyet;
                this.stringtobesent[i0] = "";
                this.justinsert[i0] = "yes";
            }
            if (Properties.Settings.Default.commandstring != "none")
            {
                string[] tmpstr = Properties.Settings.Default.commandstring.Split('\t');
                num = tmpstr.Length;
                if (NumButtons < num) num = NumButtons;
                for (int i0 = 0; i0 < num; i0++)
                {
                    buttontext[i0] = tmpstr[i0];
                }
            }
            if (Properties.Settings.Default.stringforsend != "none")
            {
                string[] tmpstr = Properties.Settings.Default.stringforsend.Split('\t');
                num = tmpstr.Length;
                if (NumButtons < num) num = NumButtons;
                for (int i0 = 0; i0 < num; i0++)
                {
                    this.stringtobesent[i0] = tmpstr[i0];
                }
            }
            if (Properties.Settings.Default.justinsert != "none")
            {
                string[] tmpstr = Properties.Settings.Default.justinsert.Split('\t');
                num = tmpstr.Length;
                if (NumButtons < num) num = NumButtons;
                for (int i0 = 0; i0 < num; i0++)
                {
                    this.justinsert[i0] = tmpstr[i0];
                }
            }

            for (int i0 = 0; i0 < custumbuttons.Length; i0++)
            {
                int i = i0 % 10;
                int j = i0 / 10;
                //ボタンコントロールのインスタンス作成
                this.custumbuttons[i0] = new Button();

                //プロパティ設定
                this.custumbuttons[i0].Name = "custumbtn" + (i0 + 1).ToString();
                this.custumbuttons[i0].Text = buttontext[i0];

                this.custumbuttons[i0].Top = this.textToBeSent.Bottom + 20 + j * 27;
                this.custumbuttons[i0].Height = 25;
                this.custumbuttons[i0].Width = 62;
                this.custumbuttons[i0].Left = this.textToBeSent.Left + 67 * i + (i / 5) * 13 - 3;
                this.custumbuttons[i0].Tag = i0;

                //コントロールをフォームに追加
                this.Controls.Add(this.custumbuttons[i0]);
                this.custumbuttons[i0].Click += new System.EventHandler(custumbtnclick);
                this.custumbuttons[i0].MouseDown += new MouseEventHandler(Buttons_MouseDown);
            }
            //Debug.WriteLine("generateCustumButton() num elements = " + this.custumbuttons.Length + ", " + this.stringtobesent.Length + ", " + this.justinsert.Length);
        }

        private void custumbtnclick(object sender, System.EventArgs e)
        {
            Button btn = (Button)sender;
            int no = (int)(btn.Tag);
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift || rightbutton)
            {
                //Debug.WriteLine("Shift + " + btn.Name);
                //Debug.WriteLine("custumbtnclick no = " + no);
                //Debug.WriteLine("num elements = " + this.custumbuttons.Length + ", " + this.stringtobesent.Length + ", " + this.justinsert.Length);
                List<object> sendList = new List<object>
                {
                    this.custumbuttons[no].Text,
                    this.stringtobesent[no],
                    this.justinsert[no]
                };
                List<object> resultObjs = Form2.ShowForm2(sendList);
                if ((string)resultObjs[0] != "")
                {
                    this.custumbuttons[no].Text = (string)resultObjs[0];
                }
                else
                {
                    this.custumbuttons[no].Text = Notsetyet;
                }
                this.stringtobesent[no] = (string)resultObjs[1];
                this.justinsert[no] = (string)resultObjs[2];
            }
            else if (btn.Text != Notsetyet && btn.Text != Notsetyet_old && !rightbutton)
            {
                //MessageBox.Show(btn.Text);
                if (this.justinsert[no] == "yes")
                {
                    textToBeSent.Text = this.stringtobesent[no];
                }
                else
                {
                    sendCustumbuttonstr(this.stringtobesent[no]);
                }
            }
            rightbutton = false;
        }

        private bool rightbutton;
        private void Buttons_MouseDown(object sender, MouseEventArgs e)
        {
            //MessageBox.Show("right button");
            rightbutton = false;
            if (e.Button == MouseButtons.Right)
            {
                rightbutton = true;
                custumbtnclick(sender, (EventArgs)e);
            }
        }

        private void exportSettinfs(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = "BLESerialTerminal_buttons",
                RestoreDirectory = true
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //MessageBox.Show(saveFileDialog1.FileName);
                String mystring = "";
                for (int i = 0; i < custumbuttons.Length; i++)
                {
                    mystring += "" + i + "," + this.custumbuttons[i].Text + "," + this.stringtobesent[i] + "," + this.justinsert[i] + "\n";
                }
                File.WriteAllText(saveFileDialog1.FileName, mystring);
            }

        }

        private void importSettings(object sender, EventArgs e)
        {
            OpenFileDialog loadFileDialog1 = new OpenFileDialog
            {
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = "SerialTerminalPlus_buttons",
                RestoreDirectory = true
            };

            if (loadFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] txtArray = File.ReadAllLines(loadFileDialog1.FileName);
                int index;
                string[] textb;
                foreach (var line in txtArray)
                {
                    //Debug.WriteLine("importSettings [" + line + "]");
                    textb = line.Split(',');
                    if (textb.Length == 4)
                    {
                        try
                        {
                            //Debug.WriteLine("importSettings num = " + textb.Length); //4
                            index = Int32.Parse(textb[0]);
                            if (index < custumbuttons.Length)
                            {
                                this.custumbuttons[index].Text = textb[1];
                                this.stringtobesent[index] = textb[2];
                                string debtmp = textb[3];
                                this.justinsert[index] = debtmp;
                            }
                        }
                        catch (FormatException)
                        {
                            MessageBox.Show(textb[0]);
                        }
                    }

                }
            }
        }
    }
}
