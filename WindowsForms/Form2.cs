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
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using PanGu.HighLight;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Search;
using System.IO;

namespace WindowsForms
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public List<Item> search(string keyWord)
        {
            //定义分词器
            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            //定义索引目录路径
            string path = Path.GetFullPath("../../Indexs/");

            //定义索引用到的目录
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(path), new NoLockFactory());

            //返回读取给定目录中索引的IndexReader。
            //您应该传递readOnly =true，因为它提供了更好的并发性能，除非您打算对读取器执行写操作（删除文档或更改规范）。
            IndexReader reader = IndexReader.Open(directory, true);

            IndexSearcher searcher = new IndexSearcher(reader);

            //设置查询
            Query query = new TermQuery(new Term("content","汽车"));

            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);

            // 使用query这个查询条件进行搜索，搜索结果放入collector
            searcher.Search(query, null, collector);

            // 从查询结果中取出第m条到第n条的数据
            // collector.GetTotalHits()表示总的结果条数
            ScoreDoc[] docs = collector.TopDocs(0, collector.TotalHits).ScoreDocs;

            // 遍历查询结果
            List<Item> resultList = new List<Item>();
            for (int i = 0; i < docs.Length; i++)
            {
                // 拿到文档的id，因为Document可能非常占内存（DataSet和DataReader的区别）
                int docId = docs[i].Doc;
                // 所以查询结果中只有id，具体内容需要二次查询
                // 根据id查询内容：放进去的是Document，查出来的还是Document
                Document doc = searcher.Doc(docId);
                Item result = new Item();
                result.title = doc.Get("id");

                SimpleHTMLFormatter formatter = new SimpleHTMLFormatter("<font color='red'>", "</font>");
                //构造一个高亮对象，它将应用改革才创建的格式化
                Highlighter highter = new Highlighter(formatter, new PanGu.Segment());

                //设置片段的长度，应该是格式化搜索词后带html标签的长度 
                highter.FragmentSize = 120;

                //调用方法，替换数据title中的关键词，也就是高亮此关键词                
                result.content = highter.GetBestFragment("汽车", doc.Get("id"));



                resultList.Add(result);
            }

            return resultList;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<Item> items = search("");
            dataGridView1.DataSource = items;
        }
    }
    ;
    public class Item
    {
        public string title { get; set; }
        public string content { get; set; }
    }
}
