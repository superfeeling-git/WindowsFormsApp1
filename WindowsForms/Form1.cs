using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lucene;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.IO;

namespace WindowsForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            createIndex(textBox1.Text, textBox2.Text);
            textBox1.Text = string.Empty;
            textBox2.Text = string.Empty;
            MessageBox.Show("添加索引成功");
        }

        private void createIndex(string title,string content)
        {
            //定义索引目录路径
            string path = Path.GetFullPath("../../Indexs/");

            //定义一个分词器
            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            //定义索引用到的目录
            FSDirectory d = FSDirectory.Open(new DirectoryInfo(path),new NativeFSLockFactory());

            //如果指定目录中存在索引，则返回true,否则返回假  6217 0001 4002 9603 964
            bool isUpdate = IndexReader.IndexExists(d);
            if (isUpdate)
            {
                //如果当前目录中的索引是锁定状态，则解锁当前目录
                if (IndexWriter.IsLocked(d))
                    IndexWriter.Unlock(d);
            }

            //第三个参数：true：创建索引或覆盖现有索引，false：追加索引
            using (IndexWriter iw = new IndexWriter(d, analyzer, !isUpdate, IndexWriter.MaxFieldLength.LIMITED))
            {
                Document doc = new Document();

                doc.Add(new Field("id", title, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));

                doc.Add(new Field("content", content, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));

                iw.AddDocument(doc);

                iw.Optimize();  
            }
        }
    }
}