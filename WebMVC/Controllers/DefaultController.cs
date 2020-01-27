﻿using System;
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
using Lucene.Net.Search;
using PanGu.HighLight;

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
            createIndex(Model);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Search(string Keywords)
        {
            if(Request.IsAjaxRequest())
            {
                return Json(search(Keywords), JsonRequestBehavior.AllowGet);
            }
            return View();
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
            string path = Server.MapPath("/Indexs");

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

                doc.Add(new Field("Content", Model.Content, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));

                doc.Add(new Field("AddTime", Model.AddTime.ToString("yyyy-MM-dd HH:mm:ss"), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));

                iw.AddDocument(doc);

                iw.Optimize();
            }
        }

        public List<News> search(string keyWord)
        {
            //定义分词器
            Analyzer analyzer = new PanGuAnalyzer();

            //定义索引目录路径
            string path = Server.MapPath("/Indexs");

            //定义索引用到的目录
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(path), new NoLockFactory());

            //返回读取给定目录中索引的IndexReader。
            //您应该传递readOnly =true，因为它提供了更好的并发性能，除非您打算对读取器执行写操作（删除文档或更改规范）。
            IndexReader reader = IndexReader.Open(directory, true);

            IndexSearcher searcher = new IndexSearcher(reader);

            //设置查询
            Query query = new TermQuery(new Term("Content", keyWord));

            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);

            // 使用query这个查询条件进行搜索，搜索结果放入collector
            searcher.Search(query, null, collector);

            // 从查询结果中取出第m条到第n条的数据
            // collector.GetTotalHits()表示总的结果条数
            ScoreDoc[] docs = collector.TopDocs(0, collector.TotalHits).ScoreDocs;

            // 遍历查询结果
            List<News> resultList = new List<News>();
            for (int i = 0; i < docs.Length; i++)
            {
                // 拿到文档的id，因为Document可能非常占内存（DataSet和DataReader的区别）
                int docId = docs[i].Doc;
                //// 所以查询结果中只有id，具体内容需要二次查询
                //// 根据id查询内容：放进去的是Document，查出来的还是Document
                Document doc = searcher.Doc(docId);
                News result = new News();
                result.Id = Convert.ToInt32(doc.Get("id"));

                result.Title = doc.Get("Title");

                result.AddTime = Convert.ToDateTime(doc.Get("AddTime"));

                SimpleHTMLFormatter formatter = new SimpleHTMLFormatter("<font color='red'>", "</font>");
                //构造一个高亮对象，它将应用改革才创建的格式化
                Highlighter highter = new Highlighter(formatter, new PanGu.Segment());

                //设置片段的长度，应该是格式化搜索词后带html标签的长度 
                highter.FragmentSize = 120;

                //调用方法，替换数据title中的关键词，也就是高亮此关键词                
                result.Content = highter.GetBestFragment(keyWord, doc.Get("Content"));

                resultList.Add(result);
            }

            return resultList;
        }


    }
}