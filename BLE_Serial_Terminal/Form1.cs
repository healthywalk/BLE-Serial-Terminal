using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Runtime.InteropServices.WindowsRuntime;        //Asbuffer

using System.Diagnostics;
using System.Threading;

namespace BLE_Serial_Terminal
{
    public partial class Form1 : Form
    {
        private BluetoothLEAdvertisementWatcher watcher;
        private bool deviceConnected = false;
        public Form1()
        {
            //Console.WriteLine("Form1");
            InitializeComponent();
            //Properties.Settings.Default.Reload();
            Load += Form1_Load;
            ScanBle();                 //起動と同時にBleデバイスとの接続を試みる。
        }

        private void Form1_Load(object sender, EventArgs e)
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
            //Console.WriteLine("ScanBle");
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
            Console.WriteLine("Watcher_Received 2" + adr);
            if (adr.Length != 12) return;
            string devaddress = adr.Substring(0, 2)+":"+ adr.Substring(2, 2) + ":" + adr.Substring(4, 2) + ":"
                              + adr.Substring(6, 2) + ":" + adr.Substring(8, 2) + ":" + adr.Substring(10, 2);
            string devname = args.Advertisement.LocalName;
            if (devname.Length == 0) return;
            Console.WriteLine("Watcher_Received 2" + devname + " :  " + devaddress);
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

        private BluetoothLEDevice device;

        //受信関係はTX，送信関係はRXになる。
        //Serverを中心に考えるため，このアプリはClientなので，意味が逆になる
        //元の意味はTX:Transmitter，RX:Receiver
        private GattCharacteristic cTX;
        private GattCharacteristic cRX;

        private UInt64 address;

        async Task connectSelectedDevice()
        {
            try
            {
                //デバイスに接続する
                Console.WriteLine("Watcher_Received 2 Connect...");
                device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

                //UUIDからサービスを取得する
                Console.Write("Service: ");
                var services = await device.GetGattServicesForUuidAsync(new Guid("6E400001-B5A3-F393-E0A9-E50E24DCCA9E"));
                Console.WriteLine(services.Status);

                //UUIDからキャラクタリスティックを取得する
                Console.Write("CharacteristicsTX: ");
                var characteristicsTX = await services.Services[0].GetCharacteristicsForUuidAsync(new Guid("6E400003-B5A3-F393-E0A9-E50E24DCCA9E"));
                Console.WriteLine(characteristicsTX.Status);
                Console.WriteLine(characteristicsTX.ToString());

                cTX = characteristicsTX.Characteristics[0];
                Console.WriteLine(cTX.ToString());

                //UUIDからキャラクタリスティックを取得する
                Console.Write("CharacteristicsRX: ");
                var characteristicsRX = await services.Services[0].GetCharacteristicsForUuidAsync(new Guid("6E400002-B5A3-F393-E0A9-E50E24DCCA9E"));
                Console.WriteLine(characteristicsRX.Status);
                Console.WriteLine(characteristicsRX.ToString());

                cRX = characteristicsRX.Characteristics[0];
                Console.WriteLine(cRX.ToString());

                //通知を受け取るコールバックを設定
                cTX.ValueChanged += characteristicBleDevice;

                //通知購読登録
                GattCommunicationStatus status = await cTX.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                Console.WriteLine("status: " + status);
                if (status == GattCommunicationStatus.Success)
                {
                    this.deviceConnected = true;
                    Console.WriteLine("Server has been informed of clients interest.");
                }
                else
                {
                    Console.WriteLine("** error ** Server has not been informed of clients interest.");
                }
                // こここまでが成功するとConnectが成立
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void disconnectDevice()
        {
            try
            {
                //Console.WriteLine("device.ConnectionStatus = " + device.ConnectionStatus);
                if (device!=null && device.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
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
                Console.WriteLine(e);
            }
        }

        //Notifyによる受信時の処理
        void characteristicBleDevice(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Console.Write("received: ");
            var streamNotify = args.CharacteristicValue.AsStream();
            //PrintFromStream(streamNotify);
            byte[] byte_text = StreamToBytes(streamNotify);
            string string_text = Encoding.Default.GetString(byte_text);
            string_text = string_text.TrimEnd('\r', '\n');
            Console.WriteLine(string_text);
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
                this.btnConnect.Enabled = false;
                string itemstr = (string)cmbBoxDevice.SelectedItem;
                string adr1 = itemstr.Substring(itemstr.Length - 17);
                //Console.WriteLine(adr1);
                string adr2 = adr1.Replace(":", "");
                //Console.WriteLine(adr2);
                address = Convert.ToUInt64(adr2, 16);
                string devicename = itemstr.Substring(0, itemstr.IndexOf(" :  ",0));
                //devicename = devicename.Replace(" ", "");
                //Console.WriteLine("btnConnect_clicked pass");
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
            Console.WriteLine(str);
            byte[] byte_str = System.Text.Encoding.ASCII.GetBytes(str);
            try
            {
                await cRX.WriteValueAsync(byte_str.AsBuffer());
            }
            catch (Exception e1)
            {
                Console.WriteLine(e1);
            }
            if (this.cBoxClearSending.Checked) this.textToBeSent.Clear();
        }

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
            this.textBoxReceived.AppendText(text1);
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
            for (int i = 1; i < 10; i++)
            {
                Properties.Settings.Default.commandstring += '\t' + this.custumbuttons[i].Text;
            }
            Properties.Settings.Default.stringforsend = this.stringtobesent[0];
            for (int i = 1; i < 10; i++)
            {
                Properties.Settings.Default.stringforsend += '\t' + this.stringtobesent[i];
            }
            Properties.Settings.Default.stringforsend += "\tdummy";
            Properties.Settings.Default.justinsert = this.justinsert[0];
            for (int i = 1; i < 10; i++)
            {
                Properties.Settings.Default.justinsert += '\t' + this.justinsert[i];
            }

            Properties.Settings.Default.Save();
            //Console.WriteLine("default properties saved");

            //ApplicationExitイベントハンドラを削除
            Application.ApplicationExit -= new EventHandler(Application_ApplicationExit);

            disconnectDevice();
        }

        private Button[] custumbuttons;
        private string[] stringtobesent;
        private string[] justinsert;
        private const String Notsetyet = "undefined";
        private const String Notsetyet_old = "Not set yet";

        private void generateCustumButton()
        {
            this.custumbuttons = new Button[10];
            string[] buttontext = new string[10]; //これは更新されない
            this.stringtobesent = new string[10+1]; //dummy文字列の分
            this.justinsert = new string[10];

            Boolean canrestore = false;
            if (Properties.Settings.Default.commandstring != "none")
            {
                canrestore = true;
                buttontext = Properties.Settings.Default.commandstring.Split('\t');
            }
            if (Properties.Settings.Default.stringforsend != "none")
            {
                this.stringtobesent = Properties.Settings.Default.stringforsend.Split('\t');
            }
            else
            {
                for (int i0 = 0; i0 < custumbuttons.Length; i0++)
                {
                    this.stringtobesent[i0] = "";
                }
            }
            if (Properties.Settings.Default.justinsert != "none")
            {
                justinsert = Properties.Settings.Default.justinsert.Split('\t');
            }
            else
            {
                for (int i0 = 0; i0 < custumbuttons.Length; i0++)
                {
                    justinsert[i0] = "yes";
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
                if (canrestore == true)
                {
                    this.custumbuttons[i0].Text = buttontext[i0];
                }
                else
                {
                    this.custumbuttons[i0].Text = Notsetyet;
                }
                this.custumbuttons[i0].Top = this.textToBeSent.Bottom + 20 + j * 45;
                this.custumbuttons[i0].Height = 20;
                this.custumbuttons[i0].Width = 62;
                this.custumbuttons[i0].Left = this.textToBeSent.Left + 67 * i + (i / 5) * 13 - 3;
                this.custumbuttons[i0].Tag = i0;

                //コントロールをフォームに追加
                this.Controls.Add(this.custumbuttons[i0]);
                this.custumbuttons[i0].Click += new System.EventHandler(custumbtnclick);
                this.custumbuttons[i0].MouseDown += new MouseEventHandler(Buttons_MouseDown);
            }
            //Console.WriteLine("generateCustumButton() num elements = " + this.custumbuttons.Length + ", " + this.stringtobesent.Length + ", " + this.justinsert.Length);
        }

        private void custumbtnclick(object sender, System.EventArgs e)
        {
            Button btn = (Button)sender;
            int no = (int)(btn.Tag);
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift || rightbutton)
            {
                //Console.WriteLine("Shift + " + btn.Name);
                //Console.WriteLine("custumbtnclick no = " + no);
                //Console.WriteLine("num elements = " + this.custumbuttons.Length + ", " + this.stringtobesent.Length + ", " + this.justinsert.Length);
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
                    //Console.WriteLine("importSettings [" + line + "]");
                    textb = line.Split(',');
                    try
                    {
                        //Console.WriteLine("importSettings num = " + textb.Length);
                        index = Int32.Parse(textb[0]);
                        this.custumbuttons[index].Text = textb[1];
                        this.stringtobesent[index] = textb[2];
                        string debtmp = textb[3];
                        this.justinsert[index] = debtmp;
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
