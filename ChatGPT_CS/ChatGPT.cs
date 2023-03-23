using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Drawing;

namespace ChatGPTCS
{
    public partial class ChatGPT : Form
    {

        public ChatGPT()
        {
            InitializeComponent();
        }
        private List<string> messageHistory = new List<string>();


        private void btnSend_Click(object sender, EventArgs e)
        {
            label5.Text = "发送请求中...";
            string sayWord = richTextBox1.Text;
            if (string.IsNullOrEmpty(sayWord))
            {
                MessageBox.Show("说点什么");
                richTextBox1.Focus();
            }
            if (richTextBox2.Text != "")
            {
                richTextBox2.AppendText("\r\n");
            }
            richTextBox2.AppendText("Me: " + sayWord + "\r\n");
            try
            {
                Task<string> sAnswer = SendMsg(textBox1.Text, sayWord, textBox2.Text, textBox3.Text,cbModel.Text);
                sAnswer.ContinueWith(t => {
                    string result = t.Result;
                    this.Invoke((MethodInvoker)delegate {
                        richTextBox2.SelectionColor = Color.Red;
                        richTextBox2.AppendText("Chat GPT: " + result);
                        label5.Text = "请求完成，等待下次请求。";
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("出错了: " + ex.Message);
            }
            messageHistory.Add(sayWord);

        }

        
        public async Task<string> SendMsg(string API,string askQuestion,string proxyIP,string proxyPort,string model)
        {
            System.Net.ServicePointManager.SecurityProtocol = 
                System.Net.SecurityProtocolType.Ssl3 | 
                System.Net.SecurityProtocolType.Tls12 | 
                System.Net.SecurityProtocolType.Tls11 | 
                System.Net.SecurityProtocolType.Tls;
            var proxy = new WebProxy($"http://{proxyIP}:{proxyPort}");

            var clientHandler = new HttpClientHandler()
            {
                Proxy = proxy,
                UseProxy = true
            };
            var client = new HttpClient(clientHandler);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + API);
            if (model ==null)
            {
                MessageBox.Show("先获取model");
            }
            else if (model.StartsWith("gpt"))
            {
                var data = new
                {
                    model = model,
                    messages = messageHistory.Select(message => new { role = "system", content = message }).Concat(new[] { new { role = "user", content = askQuestion } }),
                    temperature = 0.7
                };


                var content = new StringContent(JsonConvert.SerializeObject(data));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
                        while (!streamReader.EndOfStream)
                        {
                            string message = await streamReader.ReadLineAsync();
                            JObject jsonObject = JObject.Parse(message);
                            string contentValue = jsonObject["choices"][0]["message"]["content"].ToString();
                            contentValue = string.Join(Environment.NewLine, contentValue.Split('\n'));
                            messageHistory.Add(contentValue);
                            return contentValue;
                        }
                    }
                }
            }
            else if (model == "text-davinci-003" || model == "text-davinci-002" || model == "text-curie-001" || model == "text-babbage-001" || model == "text-ada-001")
            {
                var data = new
                {
                    model = model,
                    prompt = askQuestion,
                    temperature = 0.7
                };
                var content = new StringContent(JsonConvert.SerializeObject(data));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/completions", content);
                using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
                        while (!streamReader.EndOfStream)
                        {
                            string message = await streamReader.ReadLineAsync();
                            JObject jsonObject = JObject.Parse(message);
                            string contentValue = jsonObject["choices"][0]["text"].ToString();
                            contentValue = string.Join(Environment.NewLine, contentValue.Split('\n'));
                            messageHistory.Add(contentValue);
                            return contentValue;
                        }
                    }
                }
            }
            else
                MessageBox.Show("暂时不支持");
            return null;
        }

        
        private void SetModels(string API,string proxyIP,string proxyPort)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Ssl3 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
            string proxyAddress = $"http://{proxyIP}:{proxyPort}";
            WebProxy proxy = new WebProxy(proxyAddress, true);
            WebRequest.DefaultWebProxy = proxy;
            string apiEndpoint = "https://api.openai.com/v1/models";
            var  request = WebRequest.Create(apiEndpoint);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Bearer " + API);

            var response = request.GetResponse();
            StreamReader streamReader = new StreamReader(response.GetResponseStream());
            string sJson = streamReader.ReadToEnd();

            cbModel.Items.Clear();

            SortedList oSortedList = new SortedList();
            System.Web.Script.Serialization.JavaScriptSerializer oJavaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            Dictionary<string, object> oJson = (Dictionary<string, object>)oJavaScriptSerializer.DeserializeObject(sJson);
            Object[] oList = (Object[])oJson["data"];
            for (int i = 0; i <= oList.Length - 1; i++)
            {
                Dictionary<string, object> oItem = (Dictionary<string, object>)oList[i];
                string sId = (String) oItem["id"];
                if (oSortedList.ContainsKey(sId) == false)
                {
                    oSortedList.Add(sId, sId);
                }                
            }

            foreach (DictionaryEntry oItem in oSortedList)
                cbModel.Items.Add(oItem.Key);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("不同model请求的URL不同，有可能用不了");
            if (textBox1.Text == "" || textBox2.Text == "" || textBox3.Text == "")
            {
                MessageBox.Show("先输入必填数据");
            }
            else
                SetModels(textBox1.Text,textBox2.Text,textBox3.Text); 
        }

        private void ChatGPT_Load(object sender, EventArgs e)
        {
            cbModel.SelectedIndex = 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            messageHistory.Clear();
            richTextBox2.Clear();
            label5.Text = "等待中";
        }
    }
}
