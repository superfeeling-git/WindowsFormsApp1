using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lucene;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.IO;
using WebMVC.Models;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.PanGu;

namespace WebMVC.Controllers
{
    public class DefaultController : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(News Model)
        {
            foreach(var p in Analyzers(Model.Content,new PanGuAnalyzer()))
            {
                Response.Write(p + "<br>");
            }
            return null;
        }

        public List<string> Analyzers(string Content,Analyzer analyzer)
        {
            List<string> strs = new List<string>();
            StringReader reader = new StringReader(Content);
            TokenStream ts = analyzer.TokenStream(Content, reader);
            bool hasnext = ts.IncrementToken();
            ITermAttribute ita;
            while (hasnext)
            {
                ita = ts.GetAttribute<ITermAttribute>();
                strs.Add(ita.Term);
                hasnext = ts.IncrementToken();
            }
            ts.CloneAttributes();
            reader.Close();
            analyzer.Close();
            return strs;
        }

        public List<string> Analyzers(string Content)
        {
            return Analyzers(Content, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="Model"></param>
        private void createIndex(News Model)
        {
            //定义索引目录路径
            string path = Server.MapPath("/Index");

            //定义一个分词器
            Analyzer analyzer = new PanGuAnalyzer();

            //定义索引用到的目录
            FSDirectory d = FSDirectory.Open(new DirectoryInfo(path), new NativeFSLockFactory());

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

                doc.Add(new Field("Id", Model.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

                doc.Add(new Field("Title", Model.Title, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));

                doc.Add(new Field("Content", Model.Content, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));

                doc.Add(new Field("AddTime", Model.AddTime.ToString("yyyy-MM-dd HH:mm:ss"), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));

                iw.AddDocument(doc);

                iw.Optimize();
            }
        }

    }
}