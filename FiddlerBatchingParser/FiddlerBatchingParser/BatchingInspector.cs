using EPocalipse.Json.Viewer;
using Fiddler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Shagman.Codes.FiddlerBatchingParser
{
    public class BatchingInspector : Inspector2, IBaseInspector2
    {
        #region Private Members

        private JsonViewer _view;
        private byte[] _body;
        private string _header;
        private string _boundary;
        private string _contentType;
        private readonly string Request = "Content-Type: application/http; msgtype=request";
        private readonly string Response = "Content-Type: application/http; msgtype=response";
        private readonly string MultipartBatching = "multipart/batching";

        #endregion

        #region Protected Members

        protected HTTPRequestHeaders _reqHeaders;
        protected HTTPResponseHeaders _resHeaders;
        protected readonly string ContentType = "Content-Type";
        protected readonly string ContentLength = "Content-Length";
        protected readonly string TransferEncodingChunked = "\"Transfer-Encoding\":\"chunked\"";

        #endregion

        #region Inspector2 Members

        public override void AddToTab(TabPage tabpPage)
        {
            _view = new JsonViewer();
            _view.Dock = DockStyle.Fill;

            tabpPage.Text = MultipartBatching;
            tabpPage.Controls.Add(_view);
        }

        public override int GetOrder()
        {
            return 0;
        }

        #endregion

        #region IBaseInspector2 Members

        public void Clear()
        {
            _view.Clear();
            _header = null;
            _boundary = null;
            _contentType = null;
            _body = null;
            _reqHeaders = null;
            _resHeaders = null;
        }

        public bool bDirty
        {
            get { return false; }
        }

        public bool bReadOnly
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public byte[] body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;

                if (_body != null)
                {
                    _view.ShowTab(Tabs.Viewer);
                    RenderBody(Encoding.UTF8.GetString(_body));
                }
            }
        }

        #endregion

        #region Overrides

        public override void SetFontSize(float flSizeInPoints)
        {
            _view.Font = new Font(_view.Font.FontFamily, flSizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
        }

        #endregion

        #region Private Methods

        public void RenderBody(string body)
        {
            if (string.IsNullOrEmpty(_boundary) && string.IsNullOrEmpty(_contentType))
            {
                return;
            }

            if (_header.ToLowerInvariant().Contains(TransferEncodingChunked.ToLowerInvariant()))
            {
                Clear();
                return;
            }

            var messages = new List<string>();

            foreach (var item in body.Trim().Split(new string[] { "--" + _boundary }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item.Length == 2 && item.Equals("--"))
                {
                    // This should be the last entry --boundary--
                    break;
                }
                else
                {
                    var lines = item.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    if (_contentType.Contains(MultipartBatching))
                    {
                        if (lines[0].Equals(Request, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (lines[1].StartsWith(ContentLength, StringComparison.InvariantCultureIgnoreCase))
                            {
                                lines[1] = string.Empty;
                            }

                            lines = Array.FindAll(lines, l => !l.Equals(Request, StringComparison.InvariantCultureIgnoreCase));
                            messages.Add(BuildJsonRequest(lines));
                        }
                        else if (lines[0].Equals(Response, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (lines[1].StartsWith(ContentLength, StringComparison.InvariantCultureIgnoreCase))
                            {
                                lines[1] = string.Empty;
                            }

                            lines = Array.FindAll(lines, l => !l.Equals(Response, StringComparison.InvariantCultureIgnoreCase));
                            messages.Add(BuildJsonResponse(lines));
                        }
                    }
                    else if (_contentType.Contains("multipart/alternative") || _contentType.Contains("multipart/related"))
                    {
                        messages.Add(BuildJsonRelatedResponse(lines));
                    }
                }
            }

            _view.Json = BuildJsonMimeArray(messages);
        }

        private string BuildJsonRequest(string[] pieces)
        {
            var writer = new StringWriter();
            var jWriter = new JsonTextWriter(writer);
            var index = 0;
            jWriter.WriteStartObject();

            while (string.IsNullOrEmpty(pieces[index]))
            {
                index++;
            }

            var request = pieces[index].Split(' ');

            jWriter.WritePropertyName("request");
            jWriter.WriteStartObject();
            jWriter.WritePropertyName("verb");
            jWriter.WriteValue(request[0]);
            jWriter.WritePropertyName("url");
            jWriter.WriteValue(request[1]);
            jWriter.WriteEndObject();

            ProcessHeadersAndBody(pieces, ++index, jWriter);

            return writer.GetStringBuilder().ToString();
        }

        private string BuildJsonResponse(string[] pieces)
        {
            var writer = new StringWriter();
            var jWriter = new JsonTextWriter(writer);
            var index = 0;
            jWriter.WriteStartObject();

            while (string.IsNullOrEmpty(pieces[index]))
            {
                index++;
            }

            var request = pieces[index].Split(new char[] { ' ' }, 3);

            jWriter.WritePropertyName("response");
            jWriter.WriteStartObject();
            jWriter.WritePropertyName("status code");
            jWriter.WriteValue(request[1]);
            jWriter.WritePropertyName("status");
            jWriter.WriteValue(request[2]);
            jWriter.WriteEndObject();

            ProcessHeadersAndBody(pieces, ++index, jWriter);

            return writer.GetStringBuilder().ToString();
        }

        private void ProcessHeadersAndBody(string[] pieces, int index, JsonTextWriter jWriter)
        {
            var contentType = string.Empty;

            jWriter.WritePropertyName("headers");
            jWriter.WriteStartObject();

            for (; index < pieces.Length; index++)
            {
                if (string.IsNullOrEmpty(pieces[index]))
                {
                    index++;
                    break;
                }

                var temp = pieces[index].Split(':');

                jWriter.WritePropertyName(temp[0].Trim());
                jWriter.WriteValue(temp[1].Trim());

                if (temp[0].Trim().Equals(ContentType, StringComparison.InvariantCultureIgnoreCase))
                {
                    contentType = temp[1].Trim();
                }
            }

            jWriter.WriteEndObject();

            if (!string.IsNullOrEmpty(contentType) && index != pieces.Length)
            {
                if (contentType.Contains("json"))
                {
                    jWriter.WritePropertyName("body");
                    jWriter.WriteRawValue(string.Join("", pieces, index, pieces.Length - index));
                }
                else
                {
                    jWriter.WritePropertyName("body");
                    jWriter.WriteValue(string.Join("", pieces, index, pieces.Length - index));
                }
            }

            jWriter.WriteEndObject();
            jWriter.Flush();
        }

        private string BuildJsonRelatedResponse(string[] pieces)
        {
            var writer = new StringWriter();
            var jWriter = new JsonTextWriter(writer);
            jWriter.WriteStartObject();

            var contentType = string.Empty;
            int i;

            jWriter.WritePropertyName("headers");
            jWriter.WriteStartObject();

            for (i = 0; i < pieces.Length; i++)
            {
                if (string.IsNullOrEmpty(pieces[i]))
                {
                    i++;
                    break;
                }

                var temp = pieces[i].Split(':');

                jWriter.WritePropertyName(temp[0].Trim());
                jWriter.WriteValue(temp[1].Trim());

                if (temp[0].Trim().Equals(ContentType, StringComparison.InvariantCultureIgnoreCase))
                {
                    contentType = temp[1].Trim();
                }
            }

            jWriter.WriteEndObject();

            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.Contains("json"))
                {
                    jWriter.WritePropertyName("body");
                    jWriter.WriteRawValue(string.Join("", pieces, i, pieces.Length - i));
                }
                else
                {
                    jWriter.WritePropertyName("body");
                    jWriter.WriteValue(string.Join("", pieces, i, pieces.Length - i));
                }
            }

            jWriter.WriteEndObject();
            jWriter.Flush();

            return writer.GetStringBuilder().ToString();
        }

        private string BuildJsonMimeArray(List<string> messages)
        {
            var result = string.Empty;

            if (messages.Count != 0)
            {
                var writer = new StringWriter();
                var jWriter = new JsonTextWriter(writer);
                jWriter.WriteStartObject();
                jWriter.WritePropertyName("header");
                jWriter.WriteRawValue(_header);
                jWriter.WritePropertyName("messages");
                jWriter.WriteStartArray();

                foreach (var item in messages)
                {
                    jWriter.WriteRawValue(item);
                }

                jWriter.WriteEndArray();
                jWriter.WriteEndObject();
                jWriter.Flush();

                result = writer.GetStringBuilder().ToString().Replace("﻿", "");
            }

            return result;
        }

        #endregion

        #region Protected Methods

        protected void RenderHeader(string header, string contentType)
        {
            if (!string.IsNullOrEmpty(header) && !string.IsNullOrEmpty(contentType))
            {
                int index = contentType.IndexOf("boundary", StringComparison.InvariantCultureIgnoreCase);

                if (index != -1)
                {
                    _boundary = contentType.Substring(index).Trim().Split('\n')[0].Split('=')[1].Trim();
                    _boundary = _boundary.Replace("\"", "");

                    if (_boundary.EndsWith(";type"))
                    {
                        _boundary = _boundary.Split(new[] { ";type" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    }

                    _contentType = contentType;

                    BuildHeaderJson(header);
                }
                else
                {
                    Clear();
                }
            }
            else
            {
                Clear();
            }
        }

        protected void BuildHeaderJson(string header)
        {
            var writer = new StringWriter();
            var jWriter = new JsonTextWriter(writer);
            jWriter.WriteStartObject();

            var lines = header.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var index = line.IndexOf(":");

                if (index != -1)
                {
                    jWriter.WritePropertyName(line.Substring(0, index).Trim());
                    jWriter.WriteValue(line.Substring(index + 1).Trim());
                }
            }

            jWriter.WriteEndObject();
            jWriter.Flush();

            _header = writer.GetStringBuilder().ToString();
        }

        #endregion
    }
}