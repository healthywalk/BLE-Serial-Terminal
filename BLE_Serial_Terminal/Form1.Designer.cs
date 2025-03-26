/*
 * using Windows.Devices.Bluetooth; などのWindows.Deviced....を有効にする
 * １．<TargetPlatformVersion>8.1</TargetPlatformVersion> をC:\Users\tommy\source\repos\BLE_Serial_Terminal.csprojに加える
 * ２．[参照の追加] を開きます。
 * 　　[参照(B)] ボタンを選択し、ファイル選択ダイアログを表示します。
 * 　　C:\Program Files (x86)\Windows Kits\10\UnionMetadata<sdk version>\Facade　から　windows.winmdを加える
 * 
 * 
 * 
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Runtime.InteropServices.WindowsRuntime;        //Asbuffer

namespace BLE_Serial_Terminal
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.btnSend = new System.Windows.Forms.Button();
            this.cmbBoxDevice = new System.Windows.Forms.ComboBox();
            this.SelectBleDevice = new System.Windows.Forms.Label();
            this.btnScan = new System.Windows.Forms.Button();
            this.textToBeSent = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.cBoxLocalEcho = new System.Windows.Forms.CheckBox();
            this.cBoxTimeStamp = new System.Windows.Forms.CheckBox();
            this.cBoxClearSending = new System.Windows.Forms.CheckBox();
            this.cmbBoxLBSend = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btmClearLog = new System.Windows.Forms.Button();
            this.btnSaveLog = new System.Windows.Forms.Button();
            this.textBoxReceived = new System.Windows.Forms.TextBox();
            this.btn_Export = new System.Windows.Forms.Button();
            this.btn_Import = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSend
            // 
            this.btnSend.BackColor = System.Drawing.SystemColors.Control;
            this.btnSend.Location = new System.Drawing.Point(352, 71);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(55, 26);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_click);
            // 
            // cmbBoxDevice
            // 
            this.cmbBoxDevice.FormattingEnabled = true;
            this.cmbBoxDevice.Location = new System.Drawing.Point(12, 36);
            this.cmbBoxDevice.Name = "cmbBoxDevice";
            this.cmbBoxDevice.Size = new System.Drawing.Size(278, 20);
            this.cmbBoxDevice.TabIndex = 3;
            // 
            // SelectBleDevice
            // 
            this.SelectBleDevice.AutoSize = true;
            this.SelectBleDevice.Location = new System.Drawing.Point(12, 21);
            this.SelectBleDevice.Name = "SelectBleDevice";
            this.SelectBleDevice.Size = new System.Drawing.Size(101, 12);
            this.SelectBleDevice.TabIndex = 4;
            this.SelectBleDevice.Text = "Select BLE Device";
            // 
            // btnScan
            // 
            this.btnScan.BackColor = System.Drawing.SystemColors.Control;
            this.btnScan.Location = new System.Drawing.Point(170, 10);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(120, 23);
            this.btnScan.TabIndex = 5;
            this.btnScan.Text = "Scan BLE Dev";
            this.btnScan.UseVisualStyleBackColor = true;
            this.btnScan.Click += new System.EventHandler(this.btnScan_clicked);
            // 
            // textToBeSent
            // 
            this.textToBeSent.Location = new System.Drawing.Point(14, 78);
            this.textToBeSent.Name = "textToBeSent";
            this.textToBeSent.Size = new System.Drawing.Size(332, 19);
            this.textToBeSent.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 12);
            this.label1.TabIndex = 7;
            this.label1.Text = "Text to be Sent";
            // 
            // btnConnect
            // 
            this.btnConnect.BackColor = System.Drawing.SystemColors.Control;
            this.btnConnect.Location = new System.Drawing.Point(557, 9);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(132, 47);
            this.btnConnect.TabIndex = 8;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_clicked);
            // 
            // cBoxLocalEcho
            // 
            this.cBoxLocalEcho.AutoSize = true;
            this.cBoxLocalEcho.Location = new System.Drawing.Point(314, 10);
            this.cBoxLocalEcho.Name = "cBoxLocalEcho";
            this.cBoxLocalEcho.Size = new System.Drawing.Size(80, 16);
            this.cBoxLocalEcho.TabIndex = 9;
            this.cBoxLocalEcho.Text = "Local Echo";
            this.cBoxLocalEcho.UseVisualStyleBackColor = true;
            // 
            // cBoxTimeStamp
            // 
            this.cBoxTimeStamp.AutoSize = true;
            this.cBoxTimeStamp.Location = new System.Drawing.Point(314, 28);
            this.cBoxTimeStamp.Name = "cBoxTimeStamp";
            this.cBoxTimeStamp.Size = new System.Drawing.Size(85, 16);
            this.cBoxTimeStamp.TabIndex = 10;
            this.cBoxTimeStamp.Text = "Time Stamp";
            this.cBoxTimeStamp.UseVisualStyleBackColor = true;
            // 
            // cBoxClearSending
            // 
            this.cBoxClearSending.AutoSize = true;
            this.cBoxClearSending.Location = new System.Drawing.Point(194, 61);
            this.cBoxClearSending.Name = "cBoxClearSending";
            this.cBoxClearSending.Size = new System.Drawing.Size(152, 16);
            this.cBoxClearSending.TabIndex = 11;
            this.cBoxClearSending.Text = "Clear Text After Sending";
            this.cBoxClearSending.UseVisualStyleBackColor = true;
            // 
            // cmbBoxLBSend
            // 
            this.cmbBoxLBSend.FormattingEnabled = true;
            this.cmbBoxLBSend.Location = new System.Drawing.Point(413, 24);
            this.cmbBoxLBSend.Name = "cmbBoxLBSend";
            this.cmbBoxLBSend.Size = new System.Drawing.Size(70, 20);
            this.cmbBoxLBSend.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(411, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 12);
            this.label2.TabIndex = 14;
            this.label2.Text = "Line Break on Sending";
            // 
            // btmClearLog
            // 
            this.btmClearLog.BackColor = System.Drawing.SystemColors.Control;
            this.btmClearLog.Location = new System.Drawing.Point(557, 71);
            this.btmClearLog.Name = "btmClearLog";
            this.btmClearLog.Size = new System.Drawing.Size(63, 26);
            this.btmClearLog.TabIndex = 16;
            this.btmClearLog.Text = "Clear Log";
            this.btmClearLog.UseVisualStyleBackColor = true;
            this.btmClearLog.Click += new System.EventHandler(this.clearLog_clicked);
            // 
            // btnSaveLog
            // 
            this.btnSaveLog.BackColor = System.Drawing.SystemColors.Control;
            this.btnSaveLog.Location = new System.Drawing.Point(626, 71);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(63, 26);
            this.btnSaveLog.TabIndex = 17;
            this.btnSaveLog.Text = "Save Log";
            this.btnSaveLog.UseVisualStyleBackColor = true;
            this.btnSaveLog.Click += new System.EventHandler(this.btnSave_clicked);
            // 
            // textBoxReceived
            // 
            this.textBoxReceived.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.textBoxReceived.ForeColor = System.Drawing.SystemColors.Info;
            this.textBoxReceived.Location = new System.Drawing.Point(12, 143);
            this.textBoxReceived.Multiline = true;
            this.textBoxReceived.Name = "textBoxReceived";
            this.textBoxReceived.ReadOnly = true;
            this.textBoxReceived.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxReceived.Size = new System.Drawing.Size(677, 295);
            this.textBoxReceived.TabIndex = 18;
            // 
            // btn_Export
            // 
            this.btn_Export.Location = new System.Drawing.Point(427, 71);
            this.btn_Export.Name = "btn_Export";
            this.btn_Export.Size = new System.Drawing.Size(56, 26);
            this.btn_Export.TabIndex = 19;
            this.btn_Export.Text = "Export";
            this.btn_Export.UseVisualStyleBackColor = true;
            this.btn_Export.Click += new System.EventHandler(this.exportSettinfs);
            // 
            // btn_Import
            // 
            this.btn_Import.Location = new System.Drawing.Point(489, 71);
            this.btn_Import.Name = "btn_Import";
            this.btn_Import.Size = new System.Drawing.Size(56, 26);
            this.btn_Import.TabIndex = 20;
            this.btn_Import.Text = "Import";
            this.btn_Import.UseVisualStyleBackColor = true;
            this.btn_Import.Click += new System.EventHandler(this.importSettings);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(425, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(128, 12);
            this.label3.TabIndex = 21;
            this.label3.Text = "Custom Button Settings";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 104);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(296, 12);
            this.label4.TabIndex = 22;
            this.label4.Text = "Custom Buttons (Shift+click or right-click to customize.)";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(702, 450);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btn_Import);
            this.Controls.Add(this.btn_Export);
            this.Controls.Add(this.textBoxReceived);
            this.Controls.Add(this.btnSaveLog);
            this.Controls.Add(this.btmClearLog);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbBoxLBSend);
            this.Controls.Add(this.cBoxClearSending);
            this.Controls.Add(this.cBoxTimeStamp);
            this.Controls.Add(this.cBoxLocalEcho);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textToBeSent);
            this.Controls.Add(this.btnScan);
            this.Controls.Add(this.SelectBleDevice);
            this.Controls.Add(this.cmbBoxDevice);
            this.Controls.Add(this.btnSend);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "BLE_Serial_Terminal";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnSend;
        private ComboBox cmbBoxDevice;
        private Label SelectBleDevice;
        private Button btnScan;
        private TextBox textToBeSent;
        private Label label1;
        private Button btnConnect;
        private CheckBox cBoxLocalEcho;
        private CheckBox cBoxTimeStamp;
        private CheckBox cBoxClearSending;
        private ComboBox cmbBoxLBSend;
        private Label label2;
        private Button btmClearLog;
        private Button btnSaveLog;
        private TextBox textBoxReceived;
        private Button btn_Export;
        private Button btn_Import;
        private Label label3;
        private Label label4;
    }
}

