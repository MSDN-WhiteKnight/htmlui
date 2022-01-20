﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using HttpMultipartParser;

namespace HtmlUiTest
{
    public abstract class Page
    {
        public string Name { get; set; }
        public string Html { get; set; }
        public Application Owner { get; set; }

        public abstract void OnLoad(LoadEventArgs args);
        
        public string ProcessRequest(HttpListenerRequest request)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();

            if (Utils.StrEquals(request.HttpMethod, "GET"))
            {
                //разбираем параметры запроса
                string command = request.QueryString["command"];
                string argument = request.QueryString["argument"];

                //обработать команду

                if (argument == null) argument = "";

                switch (command)
                {
                    case "exec":
                        var method = this.GetType().GetMethod(argument);
                        method.Invoke(this, new object[] { });
                        return this.Html;

                    default:                        
                        foreach (object x in request.QueryString.Keys)
                        {
                            string name = x.ToString();
                            string val = request.QueryString[name];
                            fields[name] = val;
                        }

                        break;
                }
            }
            else if(Utils.StrEquals(request.HttpMethod,"POST") && request.ContentType.StartsWith("multipart/form-data"))
            {
                MultipartFormDataParser parser = MultipartFormDataParser.Parse(request.InputStream);
                
                for (int i = 0; i < parser.Parameters.Count; i++)
                {
                    ParameterPart x = parser.Parameters[i];
                    string name = x.Name;
                    fields[name] = x.Data;
                }

                for (int i = 0; i < parser.Files.Count; i++)
                {
                    FilePart x = parser.Files[i];
                    string name = x.Name;
                    MemoryStream msData = new MemoryStream();
                    x.Data.CopyTo(msData);

                    if (Utils.StrEquals(x.ContentType,"text/plain") || Utils.StrEquals(x.ContentType, "text/html"))
                    {
                        fields[name] = Encoding.UTF8.GetString(msData.ToArray());
                    }
                    else 
                    { 
                        fields[name] = msData.ToArray(); 
                    }
                }
            }
            else return this.Html;

            LoadEventArgs args = new LoadEventArgs(fields);
            this.OnLoad(args);

            if (args.SendCustomResponse) return args.CustomResponse;
            else return this.Html;
        }
    }

    public class LoadEventArgs : EventArgs
    {
        Dictionary<string, object> _fields;

        internal LoadEventArgs(Dictionary<string, object> fields)
        {
            this._fields = fields;
        }

        public Dictionary<string, object> Fields { get { return this._fields; } }

        public bool SendCustomResponse { get; set; }

        public string CustomResponse { get; set; }
    }
}