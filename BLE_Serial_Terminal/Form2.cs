using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BLE_Serial_Terminal
{
    public partial class Form2 : Form
    {
        //Form1から受け取ったオブジェクト配列
        private List<object> argObjs = new List<object>();
        //Form1へ返す戻り値用オブジェクト配列
        public List<object> resultObjs = new List<object>();

        bool justclose = true;
        public Form2(List<object> argObjs)
        {
            InitializeComponent();
            //自身のメンバへ格納
            this.argObjs = argObjs;

            //Form1のテキストボックス文字列を
            //Form2のテキストボックス文字列へ設定
            textBox1.Text = (string)this.argObjs[0];
            textBox2.Text = (string)this.argObjs[1];
            checkBox1.Checked = (string)this.argObjs[2] == "yes";
        }

        //Form2のインスタンス作成用自家製メソッド
        static public List<object> ShowForm2(List<object> argObjs)
        {
            //Form2を生成
            using (Form2 subForm = new Form2(argObjs))
            {
                subForm.ShowDialog();//表示
                                     //戻り値用オブジェクト配列を返す
                return subForm.resultObjs;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            justclose = false;
            this.Close();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (justclose)
            {
                resultObjs = argObjs;
            }
            else
            {
                resultObjs.Add(textBox1.Text);
                resultObjs.Add(textBox2.Text);
                resultObjs.Add(checkBox1.Checked? "yes": "no");
            }

        }

        private void buttonClaer_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            checkBox1.Checked = true;
        }
    }
}
