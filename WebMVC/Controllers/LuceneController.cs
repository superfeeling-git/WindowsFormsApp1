using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lucene;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using WebMVC.Models;
using PanGu.HighLight;

namespace WebMVC.Controllers
{
    public class LuceneController : Controller
    {
        [HttpGet]
        // GET: Lucene
        public ActionResult CreateIndex()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateIndex(Product Model)
        {
            LuceneHelper.helper.CreateIndex(Model);
            return RedirectToAction("CreateIndex");
        }

        [HttpGet]
        public ActionResult Search(string keyWords)
        {
            if(!string.IsNullOrWhiteSpace(keyWords))
                return View(LuceneHelper.helper.Search(keyWords)); 
            return View();
        }
    }

    public class LuceneHelper
    {
        public static readonly LuceneHelper helper = new LuceneHelper(); 
        public LuceneHelper()
        {

        }

        static LuceneHelper()
        {

        }

        /// <summary>
        /// 索引目录
        /// </summary>
        string path = HttpContext.Current.Server.MapPath("/Indexs");

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="Model"></param>
        public void CreateIndex(Product Model)
        {            
            System.IO.DirectoryInfo IndexDir = new System.IO.DirectoryInfo(path);
            Directory dict = FSDirectory.Open(IndexDir, new NativeFSLockFactory());
            
            //利用IndexReader的静态方法判断是否存在索引目录
            bool isUpdate = IndexReader.IndexExists(dict);
            if(isUpdate)
            {
                //如果目录是锁定状态，则对目录进行解锁；
                if (IndexWriter.IsLocked(dict))
                    IndexWriter.Unlock(dict);
            }

            using (IndexWriter writer = new IndexWriter(dict, new PanGuAnalyzer(), !isUpdate, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                Random random = new Random();
                for (int i = 1; i < 10; i++)
                {
                    DateTime dt = DateTime.Now.AddHours(random.Next(100, 10000));
                    Document doc = new Document();
                    doc.Add(new NumericField("ProductId", Field.Store.YES, true).SetIntValue(i));
                    doc.Add(new Field("ProductName", Model.ProductName, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("Detail", Model.Detail, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));
                    doc.Add(new Field("Content", Model.Detail, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));
                    doc.Add(new Field("CreateTime", dt.ToString("yyyy-MM-dd HH:mm:ss"), Field.Store.YES, Field.Index.NO));
                    doc.Add(new NumericField("OrderId", Field.Store.YES, true).SetLongValue(Convert.ToInt64(DateTools.DateToString(dt, DateTools.Resolution.SECOND))));
                    writer.AddDocument(doc);
                    writer.Optimize();
                }
            }
        }

        public List<Product> Search(string keyWords)
        {
            List<Product> product = new List<Product>();

            Query query = new TermQuery(new Term("Content", keyWords));

            System.IO.DirectoryInfo IndexDir = new System.IO.DirectoryInfo(path);
            Directory dict = FSDirectory.Open(IndexDir, new SimpleFSLockFactory());
            
            //搜索器
            IndexSearcher search = new IndexSearcher(dict, true);

            //过滤器
            NumericRangeFilter<int> filter = NumericRangeFilter.NewIntRange("ProductId", 1, 6, true, true);

            //排序字段
            Sort sort = new Sort(new SortField("OrderId", SortField.LONG, true));

            //执行搜索
            TopFieldDocs docs = search.Search(query, null, 1000, sort);

            foreach(var p in docs.ScoreDocs)
            {
                Product pro = new Product();
                Document doc = search.Doc(p.Doc);
                pro.ProductId = Convert.ToInt32(doc.Get("ProductId"));
                pro.ProductName = $"{doc.Get("ProductName")}文档ID：{p.Doc},内部ID：{pro.ProductId}";
                SimpleHTMLFormatter html = new SimpleHTMLFormatter("<span style=\"color:red\">", "</span>");
                Highlighter high = new Highlighter(html, new PanGu.Segment());
                high.FragmentSize = 120;
                pro.Detail = high.GetBestFragment(keyWords, doc.Get("Content"));
                pro.Detail = doc.Get("Content");
                pro.CreateTime = doc.Get("CreateTime");
                pro.OrderId = doc.Get("OrderId");
                product.Add(pro);
            }

            search.Dispose();
            dict.Dispose();

            return product;
        }
    }
}