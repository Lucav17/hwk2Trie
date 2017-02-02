using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for getQuerySuggestions
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class getQuerySuggestions : System.Web.Services.WebService
    {


        private MemoryStream memStream;
        private Trie trie;
        private PerformanceCounter theMemCounter;
        private MemoryCache memoryCache;

        [WebMethod]
        public void downloadWiki()
        {
            memoryCache = MemoryCache.Default;
            if(!memoryCache.Contains("trie"))
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("pa2txt");

                if (container.Exists())
                {
                    foreach (IListBlobItem item in container.ListBlobs(null, false))
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;
                            memStream = new MemoryStream();
                            blob.DownloadToStream(memStream);
                            memStream.Position = 0;

                        }
                    }
                    buildTrie();
                }
            }
             else
            {
                trie = (Trie)memoryCache.Get("trie", null);
            }
        }
    
        [WebMethod]
        public void buildTrie()
        {
            trie = new Trie();
            StreamReader sr = new StreamReader(memStream);
            theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
            bool hasMem = true;
            int counter = 1;

            string line = sr.ReadLine();
            while (line != null && hasMem)
            {
                if(counter % 1000 == 0)
                {
                    hasMem = theMemCounter.NextValue() > 20;
                    Debug.Print(theMemCounter.NextValue().ToString() + " " + line);
                }

                trie.AddWord(line);
                line = sr.ReadLine();
                counter++;
                Debug.Print(line);
            }
            
            memoryCache.Set("trie", trie, null);
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string input)
        {
            downloadWiki();
            List<string> l = trie.searchAll(input);
            List<string> results = new List<string>();
                foreach(string s in l)
                {
                    results.Add(s);
                }
            return new JavaScriptSerializer().Serialize(results);
        }


        public class Trie
        {
            private TrieNode head;
            List<string> returnList;

            public Trie()
            {
                head = new TrieNode();
                returnList = new List<string>();
            }

            public List<string> searchAll(string input)
            {
                StringBuilder sb = new StringBuilder();
                TrieNode currentNode = head;
                TrieNode child = null;


                char[] chars = input.ToCharArray();

                for (int counter = 0; counter < chars.Length; counter++)
                {
                    if (counter < chars.Length - 1)
                    {
                        sb.Append(chars[counter]);
                    }

                    child = currentNode.GetChild(chars[counter]);

                    if (child == null)  // At the end, we're done here
                    {
                        break;
                    }

                    currentNode = child;
                }
                returnList.Clear();
                Create_Branches(currentNode, sb.ToString(), input);

                return returnList;
            }

            private void Create_Branches(TrieNode node, string sub_string, string input)
            {
                if (node == null || returnList.Count > 10)
                {
                    return;
                }

                sub_string = sub_string + node.data;

                if (node.GetChild(node.data, false) == null  && sub_string.StartsWith(input, StringComparison.OrdinalIgnoreCase))  // Until we've reached the end of the word, keep adding
                {
                    returnList.Add(sub_string);
                }

                foreach (var n in node.children)   // Recursively generate branches for the subnodes
                {
                    Create_Branches(n, sub_string, input);
                }
            }

            /**
             * Add a word to the trie.
             * Adding is O (|A| * |W|), where A is the alphabet and W is the word being searched.
             */
            public void AddWord(string word)
            {
                TrieNode curr = head;

                curr = curr.GetChild(word[0], true);

                for (int i = 1; i < word.Length; i++)
                {
                    curr = curr.GetChild(word[i], true);
                }

                curr.AddCount();
            }

            /**
             * Get the count of a partictlar word.
             * Retrieval is O (|A| * |W|), where A is the alphabet and W is the word being searched.
             */
            public int GetCount(string word)
            {
                TrieNode curr = head;

                foreach (char c in word)
                {
                    curr = curr.GetChild(c);

                    if (curr == null)
                    {
                        return 0;
                    }
                }

                return curr.count;
            }

            internal class TrieNode
            {
                public LinkedList<TrieNode> children { set; get; }

                public int count { private set; get; }
                public char data { private set; get; }

                public TrieNode(char data = ' ')
                {
                    this.data = data;
                    count = 0;
                    children = new LinkedList<TrieNode>();
                }

                public bool isLeaf()
                {
                    return children != null;
                }

                public TrieNode GetChild(char c, bool createIfNotExist = false)
                {
                    foreach (var child in children)
                    {
                        if (child.data == c)
                        {
                            return child;
                        }
                    }

                    if (createIfNotExist)
                    {
                        return CreateChild(c);
                    }

                    return null;
                }

                public void AddCount()
                {
                    count++;
                }

                public TrieNode CreateChild(char c)
                {
                    var child = new TrieNode(c);
                    children.AddLast(child);

                    return child;
                }
            }
        }
        


    }
}
