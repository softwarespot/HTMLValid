using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace HTMLValid
{
    class HtmlValid
    {
        private string _userAgent = String.Empty;

        public string UserAgent
        {
            get
            {
                return _userAgent;
            }
            set
            {
                // If not null or whitespace then set the UserAgent
                _userAgent = !String.IsNullOrWhiteSpace(value) ? value : "HTMLValid";
            }
        }

        public HtmlValid()
        {
            UserAgent = null;
        }

        public HtmlValid(string userAgent)
        {
            UserAgent = userAgent;
        }

        public HtmlValidResults ValidateFilePath(string filePath, bool isReportSource)
        {
            // Values that will later be passed to HTMLValidResults()
            bool isConnected = false;
            int errors = 0, warnings = 0;
            HtmlValidFileType fileType = HtmlValidFileType.NonSupportedFile;
            HtmlValidStatus status = HtmlValidStatus.Abort;

            bool isCss = IsCSSFile(filePath), isHtml = IsHTMLFile(filePath);
            if (isCss || isHtml)
            {
                string fileParams = File.ReadAllText(filePath, Encoding.UTF8); // Read the file using UTF8 encoding
                fileParams = Uri.EscapeDataString(fileParams); // Encode the file data by escaping certain characters

                HttpWebResponse webResponse;
                if (isCss)
                {
                    fileType = HtmlValidFileType.Css;
                    fileParams = "?text=" + fileParams + (isReportSource ? "&ss=1" : "");
                    webResponse = HttpGet(@"http://jigsaw.w3.org/css-validator/validator", fileParams);
                }
                else
                {
                    fileType = HtmlValidFileType.Html;
                    fileParams = "uploaded_file=" + fileParams + (isReportSource ? "&output=text" : "");
                    webResponse = HttpPost(@"http://validator.w3.org/check", fileParams);
                }

                if (webResponse != null)
                {
                    using (webResponse)
                    {
                        isConnected = IsOnline(webResponse.StatusCode); // Set the IsOnline property

                        // Parsing the source is a better approach, but for some reason the regular expressions were causing an exception
                        Int32.TryParse(webResponse.GetResponseHeader("X-W3C-Validator-Errors"), out errors); // Set the number of errors field
                        switch (webResponse.GetResponseHeader("X-W3C-Validator-Status").ToLower())
                        {
                            case "valid":
                                status = HtmlValidStatus.Valid;
                                break;

                            case "invalid":
                                status = HtmlValidStatus.Invalid;
                                break;

                            case "abort":
                                status = HtmlValidStatus.Abort;
                                break;

                            default:
                                status = HtmlValidStatus.Abort;
                                break;
                        }
                        if (isHtml) // Only returned when validating a HTML file
                        {
                            Int32.TryParse(webResponse.GetResponseHeader("X-W3C-Validator-Warnings"), out warnings); // Set the number of warnings field
                        }
                    }
                }
            }

            return new HtmlValidResults(isConnected, status, fileType, errors, warnings);
        }

        private HttpWebResponse HttpGet(string url, string parameters)
        {
            HttpWebResponse webResponse = null;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url + parameters);
                webRequest.Method = "GET";
                webRequest.UserAgent = UserAgent;
                webResponse = (HttpWebResponse) webRequest.GetResponse();
            }
            catch
            {
                // Ignored
            }

            return webResponse;
        }

        private HttpWebResponse HttpPost(string url, string parameters)
        {
            HttpWebResponse webResponse = null;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                webRequest.UserAgent = UserAgent;

                byte[] postData = Encoding.UTF8.GetBytes(parameters); // Create a byte array of data to write to the request stream
                webRequest.ContentLength = postData.Length; // Set the Content-Length of the header

                using (Stream webStream = webRequest.GetRequestStream())
                {
                    webStream.Write(postData, 0, postData.Length);
                }
                webResponse = (HttpWebResponse) webRequest.GetResponse(); // Get the response from the server
            }
            catch
            {
                // Ignored
            }

            return webResponse;
        }

        private bool IsCSSFile(string filePath)
        {
            return File.Exists(filePath) && filePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && new FileInfo(filePath).Length > 0;
        }

        private bool IsHTMLFile(string filePath)
        {
            return File.Exists(filePath) && (filePath.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && new FileInfo(filePath).Length > 0);
        }

        private bool IsOnline(HttpStatusCode statusCode) // Parse the status code to see if it was a good response
        {
            HttpStatusCode[] httpGoodStatuses = { HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.Redirect, HttpStatusCode.RedirectMethod, HttpStatusCode.NotModified, HttpStatusCode.RedirectKeepVerb  ,
                                   HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed };

            return httpGoodStatuses.Any(httpGoodStatus => statusCode == httpGoodStatus);
        }

        private bool IsUrl(string url)
        {
            Uri urlResult;

            return Uri.TryCreate(url, UriKind.Absolute, out urlResult) && urlResult.Scheme == Uri.UriSchemeHttp;
        }

        private HtmlValidResults ValidateUrl(string urlPath, bool isReportSource) // Currently not implemented
        {
            // Values that will later be passed to HTMLValidResults()
            const bool isConnected = false;
            const int errors = 0;
            const int warnings = 0;
            HtmlValidFileType fileType = HtmlValidFileType.NonSupportedFile;
            const HtmlValidStatus status = HtmlValidStatus.Abort;

            if (IsUrl(urlPath))
            {
                fileType = HtmlValidFileType.Url;
                string fileData = "uri=" + Uri.EscapeUriString(urlPath); // Escape the URL
            }

            return new HtmlValidResults(isConnected, status, fileType, errors, warnings);
        }
    }
}