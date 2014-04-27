using System;
using System.IO;
using System.Net;
using System.Text;

namespace HTMLValid
{
    public enum HTMLValidFileType : byte
    {
        CSS,
        HTML,
        URL,
        NonSupportedFile
    }

    public enum HTMLValidStatus : byte
    {
        Abort,
        Invalid,
        Valid
    }

    internal struct HTMLValidResults
    {
        public readonly int Errors, Warnings;

        public readonly HTMLValidFileType FileType;

        public readonly bool IsConnected;

        public readonly HTMLValidStatus Status;

        public HTMLValidResults(bool isConnected, HTMLValidStatus status, HTMLValidFileType fileType, int errors, int warnings) // Constructor.
        {
            IsConnected = isConnected;
            FileType = fileType;
            Status = status;
            Errors = errors;
            Warnings = warnings;
        }
    }

    internal class HTMLValid
    {
        private string userAgent = string.Empty;

        public HTMLValid()
        {
            UserAgent = null;
        }

        public HTMLValid(string userAgent)
        {
            UserAgent = userAgent;
        }

        public string UserAgent
        {
            get
            {
                return userAgent;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) // If not null or whitespace then set the UserAgent.
                {
                    userAgent = value;
                }
                else
                {
                    userAgent = "HTMLValid";
                }
            }
        }

        public HTMLValidResults ValidateFilePath(string filePath, bool isReportSource)
        {
            // Values that will later be passed to HTMLValidResults().
            bool isConnected = false;
            int errors = 0, warnings = 0;
            HTMLValidFileType fileType = HTMLValidFileType.NonSupportedFile;
            HTMLValidStatus status = HTMLValidStatus.Abort;

            bool isCSS = IsCSSFile(filePath), isHTML = IsHTMLFile(filePath);
            if (isCSS || isHTML)
            {
                string fileParams = File.ReadAllText(filePath, Encoding.UTF8); // Read the file using UTF8 encoding.
                fileParams = Uri.EscapeDataString(fileParams); // Encode the file data by escaping certain characters.

                HttpWebResponse webResponse = null;
                if (isCSS)
                {
                    fileType = HTMLValidFileType.CSS;
                    fileParams = "?text=" + fileParams + (isReportSource ? "&ss=1" : "");
                    webResponse = HttpGet(@"http://jigsaw.w3.org/css-validator/validator", fileParams);
                }
                else
                {
                    fileType = HTMLValidFileType.HTML;
                    fileParams = "uploaded_file=" + fileParams + (isReportSource ? "&output=text" : "");
                    webResponse = HttpPost(@"http://validator.w3.org/check", fileParams);
                }

                if (webResponse != null)
                {
                    using (webResponse)
                    {
                        isConnected = IsOnline(webResponse.StatusCode); // Set the IsOnline property.

                        // Parsing the source is a better approach, but for some reason the regular expressions were causing an exception.
                        int.TryParse(webResponse.GetResponseHeader("X-W3C-Validator-Errors"), out errors); // Set the number of errors field.
                        switch (webResponse.GetResponseHeader("X-W3C-Validator-Status").ToLower())
                        {
                            case "valid":
                                status = HTMLValidStatus.Valid;
                                break;

                            case "invalid":
                                status = HTMLValidStatus.Invalid;
                                break;

                            case "abort":
                                status = HTMLValidStatus.Abort;
                                break;

                            default:
                                status = HTMLValidStatus.Abort;
                                break;
                        }
                        if (isHTML) // Only returned when validating a HTML file.
                        {
                            int.TryParse(webResponse.GetResponseHeader("X-W3C-Validator-Warnings"), out warnings); // Set the number of warnings field.
                        }
                    }
                }
            }
            return new HTMLValidResults(isConnected, status, fileType, errors, warnings);
        }

        private HttpWebResponse HttpGet(string url, string parameters)
        {
            HttpWebResponse webResponse = null;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url + parameters);
                webRequest.Method = "GET";
                webRequest.UserAgent = UserAgent;
                webResponse = (HttpWebResponse)webRequest.GetResponse();
            }
            catch { }
            return webResponse;
        }

        private HttpWebResponse HttpPost(string url, string parameters)
        {
            HttpWebResponse webResponse = null;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                webRequest.UserAgent = UserAgent;

                byte[] postData = Encoding.UTF8.GetBytes(parameters); // Create a byte array of data to write to the request stream.
                webRequest.ContentLength = postData.Length; // Set the Content-Length of the header.

                using (Stream webStream = webRequest.GetRequestStream())
                {
                    webStream.Write(postData, 0, postData.Length);
                }
                webResponse = (HttpWebResponse)webRequest.GetResponse(); // Get the response from the server.
            }
            catch { }
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

        private bool IsOnline(HttpStatusCode statusCode) // Parse the status code to see if it was a good response.
        {
            HttpStatusCode[] httpGoodStatuses = { HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.Redirect, HttpStatusCode.RedirectMethod, HttpStatusCode.NotModified, HttpStatusCode.RedirectKeepVerb  ,
                                   HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed };
            foreach (HttpStatusCode httpGoodStatus in httpGoodStatuses)
            {
                if (statusCode == httpGoodStatus)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsURL(string url)
        {
            Uri urlResult;
            return Uri.TryCreate(url, UriKind.Absolute, out urlResult) && urlResult.Scheme == Uri.UriSchemeHttp;
        }

        private HTMLValidResults ValidateURL(string urlPath, bool isReportSource) // Currently not implemented.
        {
            // Values that will later be passed to HTMLValidResults().
            bool isConnected = false;
            int errors = 0, warnings = 0;
            HTMLValidFileType fileType = HTMLValidFileType.NonSupportedFile;
            HTMLValidStatus status = HTMLValidStatus.Abort;

            if (IsURL(urlPath))
            {
                fileType = HTMLValidFileType.URL;
                string fileData = "uri=" + Uri.EscapeUriString(urlPath); // Escape the URL.
            }

            return new HTMLValidResults(isConnected, status, fileType, errors, warnings);
        }
    }
}